Thank you for playtesting the NAP N++ randomizer client!


~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Should I backup my nprofile before playing this?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The client *shouldn't* hurt your main profile. As of writing this, 5 people have used the randomizer client, and it has not affected any of their profiles. That said, it never hurts to make a copy of your nprofile for any reason! 😉


~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
What can the client currently do?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

In its current state, the client does not randomize anything on its own. It can take in a provided "plando"* file, and shuffle the game based on that file. These files are simple text files that are easy to edit, so if you find the plando too difficult, you can easily change the starting values to make it easier. plando0.json was deliberately made to be difficult. XandoToaster plans to make subsequent plandos easier.

*"plando" is short for "planned randomizer," basically a randomizer experience built by hand


When a plando is run, it will shuffle the levels in the intro tab. You can get an item by beating a level, and you can get another by getting all the gold in a level. Not all levels will provide items.

The plando will also change the amount of time you start levels with, the amount of time gold is worth, and it will enforce a "maximum time" value for the timer.

Items you can get are new level unlocks, increases to each of these time values, and palette swap traps.

Level unlocks are done as "progressive episode unlocks", meaning, rather than unlocking a specific level, you will unlock the next level in an episode. A progressive episode unlock for an episode with all levels available will then unlock the episode to be played in episode view.

The goal of plando0 is to beat a single episode in episode view. After beating an episode, the client should display the text "Goal met!!!" Please report if it does not do this.


~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Do I need to launch the game before the client?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Open N++ before launching the randomizer client.
Close the client before closing N++.
(Don't worry if you forget this order. You'll see an error message or something if you get the order wrong, but nothing bad will happen.)


~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
How do I play a plando?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

In the client, there is a button at the top to browse your files.
Browse for one of the supplied plando files.
After selecting the file, click the "Load File" button.


~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
What is this "level grid"?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The grid that takes up the bulk of the space in the client represents the levels in the intro tab. After loading your plando file, you cannot play any episodes in episode view, and it is not clear in game which levels are available to play in level view.

A red square means that level is not available to be played.
A green square means that level is available to be played, and has not been beaten.
A blue square means that level has been beaten, but you have not gotten all the gold.
A gold square means that you have gotten all the gold on the level, and there is nothing left to do there.


~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

That's all you need to know to get started. Please report any feedback to XandoToaster!