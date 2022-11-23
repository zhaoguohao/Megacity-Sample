# Performance considerations

Here is a checklist you can use to get MegaCity to run better on your machine:

![Alt text](Assets/Tutorial/Textures/promo.jpg?raw=true "Megacity")

* Unity 2022.2.0b13 or a more recent version of 2022.2 is required.
* Download the recommended version from: 
* https://beta.unity3d.com/download/458a6ada3646/download.html
* unityhub://2022.2.0b13/458a6ada3646 
* In the Edit menu, Project Settings, Quality, select Medium or Low. Insane can be very taxing on the graphics card. You can also tweak the LOD Bias (lower it to trade quality for speed), more info here: https://docs.unity3d.com/Manual/class-QualitySettings.html
* In the Jobs menu, Burst, make sure that Enable Compilation is toggled on and that Safety Checks isn’t.
* In the Jobs menu, Edit > Preferences > Jobs, Leak Detection Level, make sure that Disabled is selected. Leak detection uses small GC allocations for debugging purposes and those will slow everything down.
* Look for the Subscene called "Player_Subscene" that contains the player prefab and the settings. 
* Streaming configuration component in the player prefab can be modified, everything outside of the Streaming Out radius has its high LOD streamed out in playmode, everything inside the In radius has it streamed in.

Editor tutorials:
* In the main menu go to Tutorials > Show tutorials.

Building a player:
* In Project Settings, Player, Backend, select IL2CPP. (make sure Windows Build Support is installed)
* Build in x86_64 (x86 isn’t supported) and ensure Development Build isn’t selected.
* In the main menu, go to Build Settings... / Build and Run.
* In Build Settings window press Build or Build and Run. 

Working with SubScenes:
* SubScenes in edit mode are a lot heavier, only edit a few at a time.
* Disable the selection outline in the Gizmos menus at the top of the scene view window.
* Disable auto-save prefab, some can take a long time to propagate changes.


