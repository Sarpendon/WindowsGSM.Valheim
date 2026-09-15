using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using WindowsGSM.GameServer.Engine;
using System.IO;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
            version = "1.1",
            url = "https://github.com/Sarpendon/WindowsGSM.Valheim",
            color = "#8802db"
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "896660";

        // - Standard Constructor and properties
        public Valheim(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;

        // - Game server Fixed variables
        public override string StartPath => @"valheim_server.exe";
        public string FullName = "Valheim Dedicated Server";
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 3;
        public object QueryMethod = new A2S();

        // - Game server default values
        public string Port = "2456";
        public string QueryPort = "2457";
        public string Defaultmap = "MapSeed";
        public string Maxplayers = "4";
        public string Additional = "-password \"123456\" -savedir \".\\save-data\"  -public 1 -crossplay -saveinterval 1800 -backups 4 -backupshort 7200 -backuplong 43200";

        public async Task<Process> Start()
        {
            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);

            // Prepare start parameter
            string param = $"-nographics -batchmode"; 
            param += string.IsNullOrWhiteSpace(_serverData.ServerName) ? string.Empty : $" -name \"{_serverData.ServerName}\"";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" -port {_serverData.ServerPort}"; 
            param += string.IsNullOrWhiteSpace(_serverData.ServerMap) ? string.Empty : $" -world \"{_serverData.ServerMap}\"";
            param += string.IsNullOrWhiteSpace(_serverData.ServerParam) ? string.Empty : $" {_serverData.ServerParam}";

            // Prepare Process
            var gameServerProcess = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath),
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on.
            // This has to test the server's own setting - testing AllowsEmbedConsole, which is
            // hardcoded true, made the UI toggle a no-op and left the windowed path unreachable.
            if (AllowsEmbedConsole && _serverData.EmbedConsole)
            {
                gameServerProcess.StartInfo.CreateNoWindow = true;
                gameServerProcess.StartInfo.RedirectStandardInput = true;
                gameServerProcess.StartInfo.RedirectStandardOutput = true;
                gameServerProcess.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                gameServerProcess.OutputDataReceived += serverConsole.AddOutput;
                gameServerProcess.ErrorDataReceived += serverConsole.AddOutput;

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

                gameServerProcess.BeginOutputReadLine();
                gameServerProcess.BeginErrorReadLine();
                return gameServerProcess;
            }

            // Start Process
            try
            {
                gameServerProcess.Start();
                return gameServerProcess;
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
        }

        // - Graceful shutdown support
        // Valheim only flushes the world to disk when it receives Ctrl+C. When the server
        // is started with CreateNoWindow there is no window to send keystrokes to, so the
        // signal has to be raised on the process's console instead.
        private delegate bool ConsoleCtrlDelegate(uint ctrlType);
        private const uint CTRL_C_EVENT = 0;

        // How long to hold the console pane open after the server exits, so the shutdown
        // output stays readable. Set to 0 to hand back to WindowsGSM immediately.
        private const int CONSOLE_LINGER_MS = 3000;

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
            if (gameServerProcess == null || gameServerProcess.HasExited) { return; }

            await Task.Run(() =>
            {
                bool signalled = false;

                try
                {
                    // AttachConsole fails if this process already owns a console.
                    FreeConsole();

                    if (AttachConsole((uint)gameServerProcess.Id))
                    {
                        // The event goes to every process sharing that console, which now
                        // includes WindowsGSM. Ignore it here before raising it, or the
                        // manager takes itself down along with the server.
                        SetConsoleCtrlHandler(null, true);
                        signalled = GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);
                    }

                    if (!signalled)
                    {
                        // Fall back to the original behaviour for a server started with a window.
                        Functions.ServerConsole.SetMainWindow(gameServerProcess.MainWindowHandle);
                        Functions.ServerConsole.SendWaitToMainWindow("^c");
                    }

                    // Saving a large world takes far longer than the old 5 second cap allowed,
                    // and WindowsGSM awaits this call without a timeout of its own. Only wait
                    // out a long shutdown if a signal actually reached the server - otherwise
                    // there is nothing to wait for and the kill should not be held up.
                    gameServerProcess.WaitForExit(signalled ? 120000 : 5000);

                    // WindowsGSM wipes the console pane as soon as this call returns, which
                    // takes Valheim's shutdown output - including the final world save - with
                    // it before it can be read. Core awaits Stop() without a timeout, so
                    // lingering here holds the pane open long enough to check the save landed.
                    if (signalled && gameServerProcess.HasExited)
                    {
                        System.Threading.Thread.Sleep(CONSOLE_LINGER_MS);
                    }
                }
                catch (Exception e)
                {
                    Error = e.Message;
                }
                finally
                {
                    try { FreeConsole(); } catch { }
                    try { SetConsoleCtrlHandler(null, false); } catch { }
                }
            });
        }

        public async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(serverData.ServerID, AppId, validate, custom: custom, loginAnonymous: loginAnonymous);
            Error = error;
            await Task.Run(() => { p.WaitForExit(); });
            return p;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            string exePath = Path.Combine(path, "PackageInfo.bin");
            Error = $"Invalid Path! Fail to find {Path.GetFileName(exePath)}";
            return File.Exists(exePath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }
    }
}
