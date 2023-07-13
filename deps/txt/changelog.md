# Version 0.2.0.13
- Add support for disabling WebSockets on the background web browser, instead
  sending data to the process obtained using Dalamud IPC (Requires IINACT)
- Fix Cactbot URL not being editable if the URL exceeded 64 characters

# Version 0.2.0.12
- Fix reading from background browser install location for cactbot integration
  every frame when the config was open, rather than only when needed
- Change UI to communicate more clearly when the background web browser
  connection is disabled or not active
- Take measures to ensure the background web browser will not hang the game
  while the browser is starting
- Improve management of background threads and background web browser to ensure
  the plugin will load faster, and block the game from rendering for less time
  on plugin load
- Ensure background web browser process is always killed even when it is not
  responding when the plugin is given a chance to shutdown cleanly (sudden game
  crashes / hardware failure / bluescreens etc. prevent clean shutdowns)

# Version 0.2.0.11
- Fix cactbot timeline events not reading data correctly due to cactbot updates

# Version 0.2.0.10
- Ensure timeline events don't get left on screen should LMeter lose connection
  to the background web browser for any reason
- Add button to reset background web browser install location back to default
- Minor tweaks to Cactbot integration config presentation

# Version 0.2.0.9
- Add option to specify install location for background web browser for Cactbot
  integration
- Rework Cactbot integration config screen to be easier to read and understand
- Add better feedback into the state of the background web browser, to make
  debugging any unexpected errors easier
- Add better feedback into the state of the background connection, to make
  debugging any unexpected errors easier
- Add button to allow force killing of the background web browser

# Version 0.2.0.8
- Ensure plugin does not crash the game in the event of an unexpected issue
  launching the background web browser used for Cactbot integration
- Ensure Cactbot timeline events are rendered more accurately
- Cactbot timeline events now render using the same colors specified by Cactbot

# Version 0.2.0.7
- Add option to not automatically start background web browser, without forcing
  off Cactbot connections entirely

# Version 0.2.0.6
- Add option to disable audio prompts for Cactbot integration.
- Add option to selectively enable and disable rendering of Alert message
  popups, Alarm message popups, Info message popups, and Timeline event popups
  for Cactbot integration.
- Add option to render a text outline of user adjustable thickness around
  Alarm message popups, Alert message popups, and Info message popups to
  improve readability for Cactbot integration.
- Add option to selectively enable and disable printing of Alert messages,
  Alarm messages, and Info messages to in game chat (with appropriate colors)
  for Cactbot integration.
- Improve text rendering of Alarm messages, Alert messages, and Info messages
  to appear much less "pixel-ly" for Cactbot integration.
- The background web browser process used for Cactbot integration now:
  - Auto updates correctly (this was broken last version, sorry)
  - Will avoid starting when already running
  - Has better communication with the plugin regarding its startup state,
    allowing for easier tracking of any issues that may arise.
  - Should no longer crash the game if the user rapidly requests it restart
    manually.

# Version 0.2.0.5
- Ensure that a console window does not briefly show when launching the
  background browser used for the Cactbot feature.

# Version 0.2.0.4
- Add experimental option to display Cactbot data (alerts, timeline) as well as
  play sound effects.

# Version 0.2.0.3
- Add option to show the meter at all times in a duty, even when "Hide Outside
  Combat" is enabled.
- Add option to show meter in combat, even when "Hide Outside Duty" is enabled.

# Version 0.2.0.2
- Fix reading config file from pre 0.2.x.x incorrectly, hopefully for real this
  time

# Version 0.2.0.1
- Build against newest Dalamud to ensure patch 6.4 support (although there are
  no known issues with prior releases, and they may work fine.)
- Otherwise, this only promotes the previous update from testing to general
  release channel.

# Version 0.2.0.0 Release Candidate
- [WARNING] BACKUP YOUR CONFIG BEFORE UPDATING!

  This is a testing release to ensure this update doesn't break people's custom
  LMeter configs before pushing to more users. This should be considered an
  alpha release. Errors loading configs are not expected to happen, but in an
  abundance of caution, this release is being held from general availability
  until more user testing is done.

  On Windows, the LMeter config can be found at:
  `%APPDATA%\XIVLauncher\pluginConfigs\LMeter\LMeter.json`
  and on Linux / Mac OS it can be found in:
  `~/.xlcore/pluginConfigs/LMeter/LMeter.json`
- Add better connection status UI to make diagnosing errors during connection
  to ACT/IINACT easier to understand
- Add option to delay connecting to ACT/IINACT until after logging into a
  character
- Internal refactoring which should minorly improve performance, more to come
  in future updates.

# Version 0.1.9.0
- Add option to connect to IINACT using Dalamud IPC instead of using a
  WebSocket
- Improve subscription process over pre-releases to give more info during
  failure states
- Rename "Changelog" tab to "About / Changelog"
- Add git commit info into plugin before distribution, visible from the
  "About / Changelog" page
- Fix builds not being properly deterministic, aiding in transparency that the
  source code actually compiles to the build that users install.
- New logo

# Version 0.1.5.3
- Fix bug that that caused removal of custom added fonts.

# Version 0.1.5.2
- Added new text tags: effectivehealing, overheal, overhealpct, maxhitname,
  maxhitvalue
- Bars are now sorted by effective healing when the Healing sort mode is
  selected.
- Added option to use Job color for bar text color
- Fixed an issue with fonts on first time plugin load

# Version 0.1.5.1
- Fixed issue with auto-reconnect not working
- Fixed issue with name text tags
- Fixed issue with borders when Header is disabled
- Fixed issue with 'Return to Current Data' option
- Added new toggle option (/lm toggle <number> [on|off])

# Version 0.1.5.0
- Added Encounter history right-click context menu
- Added Rank text tag and Rank Text option under bar settings
- Fix problem with name text tags when using your name instead of YOU

# Version 0.1.4.3
- Fix potential crash with certain text tags
- Add position offsets for bar text
- Add option for borders only around bars (not header)

# Version 0.1.4.2
- Fix issue with ACT data not appearing in certain dungeons
- Improve logic for splitting encounters

# Version 0.1.4.1
- Fix potential plugin crash
- Fix bug with lock/click through
- Disable preview when config window is closed
- Force show meter when previewing

# Version 0.1.4.0
- Added advanced text-tag formatting (kilo-format and decimal-format)
- Text Format fields have been reset to default (please check out the new text
  tags!)
- Added text command to show/hide Meters (/lm toggle <number>)
- Added text command to toggle click-though for Meters (/lm ct <number>)
- Added option to hide Meter if ACT is not connected
- Added option to automatically attempt to reconnect to ACT
- Added option to add gaps between bars
- Added "Combat" job group to Visibility settings
- Fixed various bugs and improved performance

# Version 0.1.3.1
- Make auto-end disabled by default

# Version 0.1.3.0
- Add options to end ACT encounter when combat ends

# Version 0.1.2.0
- Update for Endwalker/Dalamud api5
- Add Reaper/Sage support
- Add Scrolling

# Version 0.1.1.0
- Fix sorting
- Fix bug with texture loading
- Fix default websocket address

# Version 0.1.0.0
- Created Plugin