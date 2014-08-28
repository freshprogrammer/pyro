##Pyro v0.6

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
- Rewritten AI. Score based. Still only looking at nearest neighbour
- AI structure in place for depth scanning in next build.
- tweaked fuel spawning to be a little more random.
- Added code flags for zigzag AI and allowing player to walk on fire

###Build Notes
To build with fresh solution, you will need to manually link the FreshGameLibrary.dll included in the bin folder. You will also need the XNA SDK installed.

###Note
This was made for fun, not necessarily maintainability or long term reliability. Not clean code at all. I'm sure there is lots of remaining code from past projects and ideas from hacky conversion.
