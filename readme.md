##Pyro v0.4

###Controls (supports 360 controller)
- Arrow Keys, WASD - Move player, navigate Menu
- Esc - Pause
- Enter - menu interact

###Special Controls (Dev Mode Only)
- F1 - toggle time based movement vs key based movement.
- F2 - Restart game
- 1 - Shorten trail length
- 2 - Extend trail length


###New Features & Changes
- Fixed food spawning on player. Still watching for food failing to spawn. (known bug when screen is full)
- Shorted start tail length to 0.
- Added score & high score tracking (instance only)
- Added simple GUI to show score data.
- Added keys to shorten and extend tail if in developer mode.
- Added dead player when player hits trail. - programmer art
- High score is now tracked in the config file. high score is maintained through multiple sessions.

###Build Notes
To build with fresh solution, you will need to manually link the FreshGameLibrary.dll included in the bin folder. You will also need the XNA SDK installed.

###Note
This was made for fun, not necessarily maintainability or long term reliability. Not clean code at all. I'm sure there is lots of remaining code from past projects and ideas from hacky conversion.
