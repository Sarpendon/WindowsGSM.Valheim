using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;

namespace WindowsGSM.Plugins
{
    public class Valheim : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.Valheim",
            author = "Sarpendon",
            description = "WindowsGSM plugin for supporting Valheim Dedicated Server",
            version = "1.2",
            url = "https://github.com/Sarpendon/WindowsGSM.Valheim",
            color = "#8802db"
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "896660";

        // - Standard Constructor and properties
        public Valheim(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;

        // Error and Notice are deliberately not redeclared here. SteamCMDAgent already provides
        // both, and a field of the same name hides the base property: WindowsGSM reads
        // gameServer.Error dynamically and binds to the most derived member, so everything the
        // base wrote - the reason Install() failed, for one - never reached the UI.

        // - Game server Fixed variables
        public override string StartPath => @"valheim_server.exe";
        public string FullName = "Valheim Dedicated Server";

        // Capability flag only. WindowsGSM overwrites it with the server's own Embed Console
        // setting (MainWindow.Server_BeginStart) right before calling Start(), which is why the
        // check in Start() reads this rather than _serverData.EmbedConsole.
        public bool AllowsEmbedConsole = true;

        // Valheim binds the game port, the Steam query port (+1) and +2, so the next server
        // installed has to start three ports further up to avoid overlapping.
        public int PortIncrements = 3;
        public object QueryMethod = new A2S();

        // - Game server default values (applied to newly installed servers only)
        public string Port = "2456";
        public string QueryPort = "2457";

        // -world takes the world NAME, not a seed - Valheim generates the seed itself on first
        // start and keeps it in the .fwl file. "Dedicated" is what Valheim's own
        // start_headless_server.bat uses.
        public string Defaultmap = "Dedicated";

        // Vanilla Valheim caps the server at 10 players.
        public string Maxplayers = "10";

        public string Additional = "-public 1 -password \"123456\" -savedir \".\\save-data\" -crossplay -saveinterval 1800 -backups 4 -backupshort 7200 -backuplong 43200";

        // - Valheim is configured entirely on the command line, so there is no config file to
        //   write. WindowsGSM calls this after installing, so provide it rather than letting the
        //   call fail as a swallowed binder exception.
        public void CreateServerCFG() { }

        public async Task<Process> Start()
        {
            string shipExePath = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found ({shipExePath})";
                return null;
            }

            // Prepare start parameter
            string param = "-nographics -batchmode";
            param += string.IsNullOrWhiteSpace(_serverData.ServerName) ? string.Empty : $" -name \"{_serverData.ServerName}\"";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" -port {_serverData.ServerPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerMap) ? string.Empty : $" -world \"{_serverData.ServerMap}\"";
            param += string.IsNullOrWhiteSpace(_serverData.ServerParam) ? string.Empty : $" {_serverData.ServerParam}";

            // -public used to be hardcoded, so a server could not be made private without editing
            // the plugin. It lives in the Additional Parameters now, where the UI can change it.
            // Servers created before that change have no -public in their saved parameters, so
            // keep defaulting those to public instead of silently delisting them.
            if (!HasParameter(_serverData.ServerParam, "-public"))
            {
                param += " -public 1";
            }

            // Prepare Process
            var gameServerProcess = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            bool embedConsole = AllowsEmbedConsole;
            if (embedConsole)
            {
                gameServerProcess.StartInfo.CreateNoWindow = true;
                gameServerProcess.StartInfo.RedirectStandardInput = true;
                gameServerProcess.StartInfo.RedirectStandardOutput = true;
                gameServerProcess.StartInfo.RedirectStandardError = true;

                // Valheim writes UTF-8. Without this, server and player names carrying umlauts or
                // any other non-ASCII character arrive mangled in the WindowsGSM console pane.
                gameServerProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                gameServerProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                var serverConsole = new ServerConsole(_serverData.ServerID);
                gameServerProcess.OutputDataReceived += serverConsole.AddOutput;
                gameServerProcess.ErrorDataReceived += serverConsole.AddOutput;
            }

            // Start Process
            try
            {
                gameServerProcess.Start();
            }
            catch (FileNotFoundException e)
            {
                Error = $"File not found: {e.Message}";
                return null;
            }
            catch (UnauthorizedAccessException e)
            {
                Error = $"Access denied: {e.Message}";
                return null;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null;
            }

            if (embedConsole)
            {
                gameServerProcess.BeginOutputReadLine();
                gameServerProcess.BeginErrorReadLine();
            }

            return gameServerProcess;
        }

        // - Graceful shutdown support
        //
        // Valheim only flushes the world to disk when it receives Ctrl+C. The previous stop path
        // sent that keystroke to the process window, but the embedded console starts the server
        // with CreateNoWindow, so MainWindowHandle was always IntPtr.Zero: SetForegroundWindow
        // did nothing and SendKeys.SendWait went to whatever window happened to have focus on the
        // host machine. The server never saw the signal, never saved on shutdown, and WindowsGSM
        // killed it after its own timeout - which is what produced "Server fail to stop
        // gracefully" on every stop.
        //
        // valheim_server.exe is a console application, so CREATE_NO_WINDOW still allocates it a
        // console. Attaching to that console and raising CTRL_C_EVENT there reaches the server in
        // the embedded case as well as the windowed one.

        private delegate bool ConsoleCtrlDelegate(uint ctrlType);

        private const uint CTRL_C_EVENT = 0;

        // Saving a large world takes far longer than the 5 seconds the old code allowed.
        // Server_BeginStop awaits Stop() without a timeout of its own, so this is honoured.
        private const int GRACEFUL_EXIT_TIMEOUT_MS = 120000;

        // How long to wait when nothing could be delivered. There is nothing to wait for in that
        // case, so don't hold up the kill that follows.
        private const int FORCED_EXIT_TIMEOUT_MS = 5000;

        // WindowsGSM clears the console pane as soon as Stop() returns, taking Valheim's shutdown
        // output - the final world save included - with it before it can be read. Since the pane
        // is an in-memory list with no persistence, lingering here is the only way to keep it.
        // Set to 0 to hand back to WindowsGSM immediately.
        private const int CONSOLE_LINGER_MS = 3000;

        // Console attachment is per process, not per thread, and WindowsGSM can run two stops at
        // once (auto restart, restart crontab and update-on-start each drive their own timer).
        // Without this lock one stop can detach the console another is about to signal, sending
        // Ctrl+C to the wrong server or to nothing at all.
        private static readonly object _consoleSignalLock = new object();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

        public async Task Stop(Process gameServerProcess)
        {
            if (gameServerProcess == null) { return; }

            await Task.Run(() =>
            {
                // Note: WindowsGSM builds the instance behind Stop() without a ServerConfig, so
                // _serverData is null in here. Everything this method needs comes from the
                // process itself.
                try
                {
                    if (gameServerProcess.HasExited) { return; }
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return;
                }

                lock (_consoleSignalLock)
                {
                    bool attached = false;
                    bool signalled = false;

                    try
                    {
                        // AttachConsole fails while this process still owns a console of its own.
                        FreeConsole();

                        attached = AttachConsole((uint)gameServerProcess.Id);
                        if (attached)
                        {
                            // The event reaches every process on that console, which now includes
                            // WindowsGSM. Ignore it here first, or the manager goes down with the
                            // server it is trying to stop.
                            SetConsoleCtrlHandler(null, true);
                            signalled = GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);
                        }

                        if (!signalled && gameServerProcess.MainWindowHandle != IntPtr.Zero)
                        {
                            // Fallback for a server that really does have a window. Guarded on the
                            // handle because SendKeys is global: with no window to bring forward,
                            // the keystroke lands in whatever the user is working in instead.
                            ServerConsole.SetMainWindow(gameServerProcess.MainWindowHandle);
                            ServerConsole.SendWaitToMainWindow("^c");
                            signalled = true;
                        }

                        gameServerProcess.WaitForExit(signalled ? GRACEFUL_EXIT_TIMEOUT_MS : FORCED_EXIT_TIMEOUT_MS);

                        if (signalled && CONSOLE_LINGER_MS > 0 && gameServerProcess.HasExited)
                        {
                            Thread.Sleep(CONSOLE_LINGER_MS);
                        }
                    }
                    catch (Exception e)
                    {
                        Error = e.Message;
                    }
                    finally
                    {
                        if (attached)
                        {
                            try { SetConsoleCtrlHandler(null, false); } catch { /* ignore */ }
                            try { FreeConsole(); } catch { /* ignore */ }
                        }
                    }
                }
            });
        }

        public new async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(serverData.ServerID, AppId, validate, custom: custom, loginAnonymous: loginAnonymous);
            Error = error;

            // UpdateEx hands back null when steamcmd could not be downloaded or the Steam account
            // is not set up. The old code dereferenced it unconditionally, so a failed update
            // threw a NullReferenceException out of WindowsGSM's update path - twice, since the
            // retry hits the same line - instead of reporting the error it was given.
            if (p == null) { return null; }

            // Auto Update restarts the server as soon as this returns, so the update has to be
            // finished by then, not merely started.
            await Task.Run(() => p.WaitForExit());
            return p;
        }

        public new bool IsInstallValid()
        {
            string installPath = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (File.Exists(installPath)) { return true; }

            // Keep a message the installer already left behind - that one says why it failed.
            if (string.IsNullOrWhiteSpace(Error))
            {
                Error = $"Fail to find {installPath}";
            }

            return false;
        }

        public new bool IsImportValid(string path)
        {
            // This used to look for PackageInfo.bin, carried over from the ARK plugin. Valheim
            // does not ship that file, so importing an existing server always failed.
            string importPath = Path.Combine(path, StartPath);
            Error = $"Invalid Path! Fail to find {Path.GetFileName(StartPath)}";
            return File.Exists(importPath);
        }

        public new string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
        }

        public new async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }

        // Splits a parameter string the way a command line does - quoted values stay in one piece,
        // so a password that happens to contain "-public" is not mistaken for the flag itself.
        private static bool HasParameter(string parameters, string name)
        {
            if (string.IsNullOrWhiteSpace(parameters)) { return false; }

            var token = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in parameters)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && char.IsWhiteSpace(c))
                {
                    if (token.Length > 0)
                    {
                        if (string.Equals(token.ToString(), name, StringComparison.OrdinalIgnoreCase)) { return true; }
                        token.Clear();
                    }

                    continue;
                }

                token.Append(c);
            }

            return token.Length > 0 && string.Equals(token.ToString(), name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
