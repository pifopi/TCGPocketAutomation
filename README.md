# Pokemon TCG Pocket automation

Those are simple scripts to automate pokemon TCG pocket over adb and using open cv for template matching + discord message for logging what happened

# TODO list
- run adb devices -l to investigate no transport id error
- add CI
- make discord messages prettier
- make a setup guide
  - put adb, Bluestacks and LDPlayer in your PATH
  - activate ADB on every bluestack instance
  - activate ADB on every real phone instance + connect to it once
  - fill in the settings (settings.json) taking my file as an example
  - have an environnement variable named DISCORD_BOT_TOKEN with a discord bot token