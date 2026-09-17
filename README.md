# WindowsGSM.Valheim
 🧩WindowsGSM plugin that provides Valheim Dedicated server

 🏷️ To be used with https://windowsgsm.com/ 

> [!CAUTION]
> Please read about the Server Settings below before opening a ticket!

# Basic Installation: 
1. Download  WindowsGSM from the Link above.
2. Download this Plugin as .zip container and don't unpack it.
3. Create a Folder at a Location you wan't all Server to be Installed and Run.
4. Drag WindowsGSM.Exe into previoulsy created folder and execute it.
5. Press on the Puzzle Icon in the left bottom side and install this plugin by navigating to it and select the Zip File.
6. Wait a couple of seconds then close the plugin menu and install the game server.


# The Game:
- 🕹️ **Steam Site:** https://store.steampowered.com/app/892970/Valheim/
- 📁 **Homepage:** https://www.valheimgame.com/

# Requirements:
- 🖥️ **WindowsGSM** >= 1.21.0

# Server Settings:
> [!IMPORTANT]
>- **Server Name:** *Fill in the Name of your Server in this Section it's the name that will be listed in the Server List!*
>- **Server IP Adress:** *Local IP of your Server there is no need to change this GSM should get the right IP adress itself*
>- **Server Port:** *Game Port of the Server*
>- **Server Query Port:** *it's normally your Server Port +1*
>- **Server Maxplayer:** *Display only. Valheim has no parameter for this, the server is capped at 10 players.*
>- **Server Start Map:** *The **name** of the world (`-world`), not a seed. Valheim rolls the seed itself on first start and keeps it in the world's `.fwl` file. To play a specific seed, create the world in the game and copy it into the save path.*
>- **Server Start Param:** *Some Parameter are already filled in by default you can add or remove them as you wish!*

> [!IMPORTANT]
> **Password rules:** Valheim refuses to start when the password is shorter than 5 characters,
> or when it is contained in the server name or the world name. Change the default `123456`
> before you go public.

> [!TIP]
>  These three files, located in the default save path, are called adminlist.txt,
> bannedlist.txt, and permittedlist.txt. Add one Platform User ID per line to set
> desired roles. The Platform User ID can be obtained from the Server log or from within the
> game using the F2 panel and follows the format [Platform]_[User ID] (case sensitive).

# Other Server Settings:
| Server Start Param| Description |
| --- | --- | 
| `-public 1` / `-public 0` | Lists the server in the public Community Server list, or keeps it private. Since plugin v1.2 this lives here instead of being hardcoded, so it can be switched from the UI. Servers created with an older version keep starting as public. |
| `-password "[PASSWORD]"` | Join password. At least 5 characters, and it must not appear in the server or world name. |
| `-crossplay` | Runs the server on the PlayFab network so Xbox/Game Pass players can join. |
| `-preset [NAME]` / `-modifier [KEY] [VALUE]` / `-setkey [KEY]` | World modifiers - difficulty presets, single settings such as `-modifier combat veryhard`, and global keys such as `-setkey nomap`. |
| `-savedir [PATH]` | Overrides the default save path where Worlds and Permission files are stored. |
| `-logFile “d:\log.txt”` |  Sets the location to save the log file. | 
| `-saveinterval 1800` |  Change how often the world will save in seconds. Default is 30 minutes (1800 seconds). |
| `-backups 4` |  Sets how many automatic backups will be kept. The first is the ‘short’ backup length, and the rest are the ‘long’ backup length. By default that means one backup that is 2 hours old, and 3 backups that are 12 hours apart.|
| `-backupshort 7200` | Sets the interval between the first automatic backups. Default is 2 hours (7200 seconds). |
| `-backuplong 43200` | Sets the interval between the subsequent automatic backups. Default is 12 hours (43200 seconds). |


> [!NOTE]
>For more Settings use the Valheim Dedicated Server Manual, you can find it as PDF in the Server directory!

# Changelog:
### 1.2
- **Stopping the server now saves the world.** Valheim only writes the world to disk when it
  receives Ctrl+C. That signal was sent to the server's window, but the embedded console runs it
  without one, so it never arrived: every stop ran into the timeout and ended in a hard kill
  (`[NOTICE] Server fail to stop gracefully`), and the world was only ever as current as the last
  `-saveinterval` tick. The signal is now raised on the server's console, so it shuts down
  properly, saves, and disconnects its players.
- The shutdown output stays readable for a few seconds instead of being cleared instantly, so you
  can check that the save actually happened.
- `-public` moved from the plugin into the Start Parameters and can be changed in the UI.
  Existing servers keep starting as public.
- Port increment per installed server is 3 instead of 2 - Valheim uses 2456-2458, so servers
  installed back to back could end up sharing a port.
- Importing an existing server works: it looked for `PackageInfo.bin`, a file Valheim does not
  ship.
- Failed installs and failed updates report the actual reason instead of an empty message, and a
  failed update no longer throws a `NullReferenceException`.
- Console output is read as UTF-8, so umlauts and other non-ASCII characters are no longer
  mangled.
- A missing `valheim_server.exe` is reported as such instead of a generic Win32 error.
- Defaults for new servers: world name `Dedicated` (was `MapSeed`, which suggested it takes a
  seed) and max players `10` (Valheim's actual cap).

# Other WinGSM Plugins:
| Icon | Game Name | Link | Version |
| --- | --- | --- | --- |
| <img src="https://i.imgur.com/LI1uPIJ.png" width="100" height="100"> | Myth of Empires Dedicated Server | [GitHub Link](https://github.com/Sarpendon/WindowsGSM.MythofEmpires) | 1.9 |
| <img src="https://i.imgur.com/25x4Ohs.png" width="100" height="100"> | Valheim Dedicated Server | [GitHub Link](https://github.com/Sarpendon/WindowsGSM.Valheim) | 1.2 |
| <img src="https://i.imgur.com/A9jtLPQ.png" width="100" height="100"> | V Rising Dedicated Server | [GitHub Link](https://github.com/Sarpendon/WindowsGSM.VRising) | 1.0 |
| <img src="https://i.imgur.com/A6dCSy9.png" width="100" height="100"> | Life is Feudal Dedicated Server | [GitHub Link](https://github.com/Sarpendon/WindowsGSM.LifeIsFeudal) | 1.0 |
