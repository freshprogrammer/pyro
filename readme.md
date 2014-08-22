##Pyro v0.5

###Controls (supports 360 controller)
- Arrow Keys, WASD - Move player, navigate Menu
- Esc - Pause
- Enter - menu interact

###Special Controls (Dev Mode Only)
- F1 - Toggle time based movement vs key based movement.
- F2 - Restart game
- 1 - Shorten trail length
- 2 - Extend trail length


###New Features & Changes
- Added basic AI that moves directly to fuel. AI option added to main menu.
- Added basic AI self preservation so it wont walk on fire unless it must.
- Updated score calculation. Each fuel scores you fuelCount*2. walking then decreases score by 1 each move up to fuelCount. Inventivenesses faster fuel collection. 
- Added separate high score tracking and storage for AI.
- Updated fire and fuel images
- Fire images now show player movement direction (Fixed bug in FGL rendering rotated cropped images)

###Build Notes
To build with fresh solution, you will need to manually link the FreshGameLibrary.dll included in the bin folder. You will also need the XNA SDK installed.

###Note
This was made for fun, not necessarily maintainability or long term reliability. Not clean code at all. I'm sure there is lots of remaining code from past projects and ideas from hacky conversion.
