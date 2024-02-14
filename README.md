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
>- **Server Maxplayer:** *Maximum Number of Players on your Server*
>- **Server Start Map:** *Enter the Seed of the Map you want to generate here!*
>- **Server Start Param:** *Some Parameter are already filled in by default you can add or remove them as you wish! 

> [!TIP]
>  These three files, located in the default save path, are called adminlist.txt,
> bannedlist.txt, and permittedlist.txt. Add one Platform User ID per line to set
> desired roles. The Platform User ID can be obtained from the Server log or from within the
> game using the F2 panel and follows the format [Platform]_[User ID] (case sensitive).

# Other Server Settings:
| Server Start Param| Description |
| --- | --- | 
| `-savedir [PATH]` | Overrides the default save path where Worlds and Permission files are stored. |
| `-logFile “d:\log.txt”` |  Sets the location to save the log file. | 
| `-saveinterval 1800` |  Change how often the world will save in seconds. Default is 30 minutes (1800 seconds). |
| `-backups 4` |  Sets how many automatic backups will be kept. The first is the ‘short’ backup length, and the rest are the ‘long’ backup length. By default that means one backup that is 2 hours old, and 3 backups that are 12 hours apart.|
| `-backupshort 7200` | Sets the interval between the first automatic backups. Default is 2 hours (7200 seconds). |
| `-backuplong 43200` | Sets the interval between the subsequent automatic backups. Default is 12 hours (43200 seconds). |


> [!NOTE]
>For more Settings use the Valheim Dedicated Server Manual, you can find it as PDF in the Server directory!

# Other WinGSM Plugins:
| Icon | Game Name | Link | Version |
| --- | --- | --- | --- |
| <img src="https://i.imgur.com/LI1uPIJ.png" width="100" height="100"> | Myth of Empires Dedicated Server | [GitHub Link](https://github.com/Sarpendon/WindowsGSM.MythofEmpires) | 1.9 |
| <img src="https://i.imgur.com/25x4Ohs.png" width="100" height="100"> | Valheim Dedicated Server | [GitHub Link](https://github.com/Sarpendon/WindowsGSM.MythofEmpires) | 1.0 |
