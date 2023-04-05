# Description

WFMautostatus is an app to connect Warframe and [warframe.market](https://warframe.market/).
This app detects if Warframe is running and sets your online status accordingly.

# Installation

You can build it from source or you can download binaries from releases.
It is recommended to create a registry entry to start the application with the system.
1. win_key + r -> `regedit`
1. Create a new string here: `HKEY_USERS\S-1-5-21-1133526535-3252321873-4106936650-1001\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
1. Give any name and set the value to the path of the .exe (you can also use `--silent` to start in the background)

# Usage
1. Run WFMautostatus.exe
1. Log in with your warframe.market account
1. You can manually set your status with the buttons
1. If you want to use it with another account you have to log out
