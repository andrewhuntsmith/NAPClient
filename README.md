Thank you for playtesting the NAP N++ randomizer client!


~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Should I backup my nprofile before playing this?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The client *shouldn't* hurt your main profile. As of writing this, more than 5 people have used the randomizer client, and it has not affected any of their profiles. That said, it never hurts to make a copy of your nprofile for any reason! 😉


~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
What can the client currently do?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The client currently has two functions. (1) It can run a local plando file. (2) It can connect to an archipelago server.

1) Plandos: In its current state, the client does not randomize anything on its own. It can take in a provided "plando"* file, and shuffle the game based on that file. These files are simple text files that are easy to edit, so if you find the plando too difficult, you can easily change the starting values to make it easier. plando0.json was deliberately made to be difficult. XandoToaster plans to make subsequent plandos easier.

*"plando" is short for "planned randomizer," basically a randomizer experience built by hand


When a plando is run, it will shuffle the levels in the intro tab. You can get an item by beating a level, and you can get another by getting all the gold in a level. Not all levels will provide items.

The plando will also change the amount of time you start levels with, the amount of time gold is worth, and it will enforce a "maximum time" value for the timer.

Items you can get are new level unlocks, increases to each of these time values, and palette swap traps.

Level unlocks are done as "progressive episode unlocks", meaning, rather than unlocking a specific level, you will unlock the next level in an episode. A progressive episode unlock for an episode with all levels available will then unlock the episode to be played in episode view.

The goal of plando0 is to beat a single episode in episode view. After beating an episode, the client should display the text "Goal met!!!" Please report if it does not do this.


2) Archipelago: The client can also connect to an archipelago server. Learn more about archipelago randomizers in general at archipelago.gg.

Archipelago (often abbreviated as "AP") checks are currently slightly different from plando checks. AP randomizers will still be kept to the intro tab. Beating a level will always be a check. Beating an episode will always be a check.

Collecting all the gold in a level will not always be a check. In an AP randomizer, every level can have between zero and three challenges as checks. G++ can be a challenge check just like any other.

While playing a level, the client will display information about that level, including whether or not you've already beaten it, as well as what the challenges on the level are.


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
How do I play an AP randomizer?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

In the client, there is a button at the top to switch between plando setup and archipelago setup.
After pressing the "Archipelago Menu" button, enter the server information in the provided input fields.

If you are playing multiple N++ slots in a single randomizer, you can disconnect, change your slot name, reconnect, and the client should take care of everything.


~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
What is this "level grid"?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The grid that takes up the bulk of the space in the client represents the levels in the intro tab. After loading your plando file or joining an archipelago server, you cannot play any episodes in episode view, and it is not clear in game which levels are available to play in level view.

A red square means that level is not available to be played.
A green square means that level is available to be played, and has not been beaten.
A blue square means that level has been beaten, but you have not gotten all the gold.
A gold square means that you have gotten all the gold on the level (or all the checks in an archipelago randomizer), and there is nothing left to do there.


~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

That's all you need to know to get started. Please report any feedback to XandoToaster!
