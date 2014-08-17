##Pyro v0.1

###Controls (supports 360 controller)
- Arrow Keys, WASD - Move player, navigate Menu
- Esc - Pause
- Enter - menu interact

###Special controls
- F1 - toggle time based movement vs key based movement.
- F2 - Restart game


###New Features
- New bug where food can fails to spawn or fails to be eaten. Lingers after being walked on. Disconnect between GameSlot and Gameobject. Might be spawning where the player is/was. PlayerSlot should be included in slots.

###Build Notes
To build with fresh solution, you will need to manually link the FreshGameLibrary.dll included in the bin folder.

###Note
This was made for fun, not necessarily maintainability or long term reliability. Not clean code at all. I'm sure there is lots of remaining code from past projects and ideas from hacky conversion.
