# Version 2.0.1
- Removed double haptic impulse on replay buffer when there's less than a second of delay

# Version 2.0.0
- Switched from ModUI to the new UIFramework for configuration
- Fixed the mod sometimes creating a duplicate websocket client when saving the configuration
- Fixed Haptic feedback only giving a very short impulse, independently from the configured duration
- Fixed audio feedback not working (it's been months I know, but it works now)
- Moved audio files directly to UserData, so the user can replace them with their own if they want to
- Added an option to configure audio feedback volume
- Added an option to delay replay buffer saving when pressing the buttons

# Version 1.3.0
- Partially fixed the mod for v0.5

# Version 1.2.1
- Fixed IsRecordingActive() inconsistency with paused state

# Version 1.2.0
- Semi-fixed for 0.4.2 (audio doesn't work)

# Version 1.1.0
- Added screenshot support
- Added sound effects as an additional feedback option
- Reduced the delay between the button press and the feedback

# Version 1.0.2
- Added missing configuration step to README.md
- Fixed some API methods not being declared as static
- Fixed some requests being seen as failed (despite being successful)
- Fixed the mod appearing twice in ModUI (it was a version mismatch)

# Version 1.0.1
- Fixed embedded image in README.md

# Version 1.0.0
- Created

# Help And Other Resources
Get help and find other resources in the Modding Discord:
https://discord.gg/fsbcnZgzfa
