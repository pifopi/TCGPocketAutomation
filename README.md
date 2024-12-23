# Pokemon TCG Pocket automation

Those are simple scripts to automate pokemon TCG pocket over adb and using open cv for template matching + discord message for logging what happened

---

# How to Set Up

## First Step
1. Go to [this link](https://nightly.link/pifopi/TCGPocketAutomation/workflows/csharp-ci/main/TCGPocketAutomation%20for%20windows.zip) and download the file.
2. Unzip the downloaded file (`TCGPocketAutomation for windows.zip`).
3. Download and install ADB from [Android Platform Tools](https://developer.android.com/tools/releases/platform-tools#downloads).
4. Add ADB to your system PATH.
5. Edit the log configuration file located in `config/nlog.config`. Focus only on the `rules` section and ignore other parts. For example:
   - Remove the line related to Discord logging if you are not interested in logging messages to a Discord server. If you keep it, you will need to complete the Discord setup detailed below.
6. Decide how you wish to run the program. Currently, there are three supported methods:
   - **BlueStacks (recommended)**
   - **LDPlayer**
   - **Real Phone**

   If you plan to use multiple options, follow the setup instructions for each one below.

---

## Discord Setup
1. Create a Discord bot using [Discord Developer Portal](https://discord.com/developers/applications).
2. Copy the bot token and add it as an environment variable named `DISCORD_BOT_TOKEN`.
3. Edit the program configuration file in `config/settings.json`. Ensure it remains valid JSON after editing. Update the following fields:
   - `DiscordChannelId`: The channel where your bot will post messages.
   - `DiscordUserId`: Your Discord ID to receive notifications when warnings are triggered.

---

## BlueStacks Setup
1. Download and install [BlueStacks](https://www.bluestacks.com/fr/index.html).
2. Add BlueStacks to your system PATH (e.g., `C:\Program Files\BlueStacks_nxt\` on Windows).
3. Create as many BlueStacks instances as required.
4. For each instance:
   - Launch the instance and install Pokémon TCG Pocket as you would on a normal phone.
   - Open the game and proceed to the main menu where you see Boosters, Wonder Picks, and Shop.
   - Go to the instance settings (while running), navigate to **Advanced**, enable ADB debugging, and note the port number (e.g., XXXX in `127.0.0.1:XXXX`).
   - Go to the instance settings, then go to **Performance** settings, and allocate resources based on your machine (e.g., 2 cores and 2 GB RAM per instance).
5. Edit the `config/settings.json` file and update the following fields:
   - `BlueStacksMaxParallelInstance`: Maximum number of parallel BlueStacks instances.
   - `BlueStacksInstances`: Details about each instance:
     - `Name`: Friendly identifier for the instance.
     - `IP`: Use `127.0.0.1` if running BlueStacks and the program on the same machine. Otherwise, specify the IP of the machine hosting BlueStacks.
     - `Port`: The port number noted earlier.
     - `ADBName`: Instance names follow a pattern: `Pie64`, `Pie64_2`, `Pie64_3`, etc.

---

## LDPlayer Setup
1. Download and install [LDPlayer](https://en.ldplayer.net/).
2. Add LDPlayer to your system PATH (e.g., `C:\LDPlayer\LDPlayer9`).
3. Create as many LDPlayer instances as required.
4. For each instance:
   - Launch the instance and install Pokémon TCG Pocket as you would on a normal phone.
   - Open the game and proceed to the main menu where you see Boosters, Wonder Picks, and Shop.
   - Go to the instance settings, navigate to **Other**, enable **ADB debugging in local connection**.
   - Go to the instance settings, navigates to **Advanced** and allocate resources based on your machine (e.g., 2 cores and 2 GB RAM per instance).
5. Edit the `config/settings.json` file and update the following fields:
   - `LDPlayerMaxParallelInstance`: Maximum number of parallel LDPlayer instances.
   - `LDPlayerInstances`: Details about each instance:
     - `Name`: Friendly identifier for the instance.
     - `ADBName`: Instance names follow a pattern: `emulator-5554`, `emulator-5556`, `emulator-5558`, etc.

---

## Real Phone Setup
1. Install Pokémon TCG Pocket on your phone.
2. Launch the game and proceed to the main menu where you see Boosters, Wonder Picks, and Shop.
3. Enable ADB debugging on your phone.
4. Connect your phone to the PC via USB (repeat this step after every phone reboot).
5. Edit the `config/settings.json` file and update the following fields:
   - `RealPhoneInstances`: Details about each instance:
     - `Name`: Friendly identifier for the phone.
     - `IP`: The phone's IP address.
     - `Port`: The port number used by ADB (default is 5555).

---

# Using the program

1. Once the setup is done, execute `TCGPocketAutomation.exe`.
2. Choose the program you which to run on each instance by clicking on the apprioriate button. Right now, there is only two programs : `Check wonder pick periodically` and `Check wonder pick once`. For emulator, start the pogram with the instance closed, the program will manage everything. For a real phone, start the program while standing in the main menu.

---

# Known issues

## Bluestacks issues

- Sometimes, when an instance is opened, Pokémon TCG Pocket will be missing from the phone, leading to repeated issues afterwards. Out of my 10 instances, it happeneds maybe once every 2 days and I couldn't figure out why.
- Having only one core allocated to an instance seems to not let other executable use that core and will lead to massive freeze if you do not have enough core (oddly enough, 2 core seems fine as the OS will schedule everything).

## LDplayer issues

- Sometimes, when an instance is opened, it will have lost internet access (and thus not appear in adb devices list)
. When that happen, closing everything and rebooting the machine fix the issue. I found several people reporting the same online, such as [here on their discord](https://discord.com/channels/715095525979848783/864741116653731860/1128566336914735264).
- Having only one core allocated to an instance seems to not let other executable use that core and will lead to massive freeze if you do not have enough core (oddly enough, 2 core seems fine as the OS will schedule everything).

# Ideas to improve the program

- Make the setup less scary for non tech people
- Make settings configurable from the UI directly
- Make discord messages prettier
- Allow other emulators to be automated
- Rework the UI to be prettier

---

# Ideas for new programs

- Macro that waits for a specific wonder pick to appear and join it (for joining a wonder pick)
- Macro that add a list a friends, which retry until all are accepted (for joining a wonder pick)
- Macro that accept every incoming friend requests and kick previous people when full (for hosting a wonder pick)
- Macro that reroll an account up until getting X packs (for hosting wonder picks)
- Macro that take the free pack from the shop
- Macro that open one booster from a specific pack
- Macro that automatically do PvE battle