##Pyro v0.1

###Controls (supports 360 controller)
- Arrow Keys, WASD - Move player, navigate Menu
- Esc - Pause
- Enter - menu interact

###Special controls
- F1 - toggle time based movement vs key based movement.


###New Features
- Added food that can be eaten - Eating food makes your tail longer
- Food spawn randomly (should be safe but tests were inconclusive)
- Added functionality for multiple food types to extent or shorten length
- Increased default move speed
- Fixed bug where walking over fire would make it linger forever
- New bug where you can mash keys and end up walking directly backwards
- New bug where food can fails to spawn or fails to be eaten. Lingers after being walked on. Disconnect between GameSlot and Gameobject

###Build Notes
To build with fresh solution, you will need to manually link the FreshGameLibrary.dll included in the bin folder.

###Note
This was made for fun, not necessarily maintainability or long term reliability. Not clean code at all. I'm sure there is lots of remaining code from past projects and ideas from hacky conversion.
