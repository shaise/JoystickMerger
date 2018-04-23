# JoystickMerger
Join two physical joysticks to one logical. Good for games that do not support more then one joystick.

This project was made specific for merging moded suncom joystick and throttle into one input device. 
(Project details at http://theseger.com/projects/2014/12/combine-multiple-game-controllers-single-logical-one/) But can easily be modified to suit other controllers.

The project is based on vJoy logical joystick driver (http://vjoystick.sourceforge.net/site/) and SharpDX - a c# wrapper for directx. (http://sharpdx.org/)

# Compilation:
1. Compile using Microsoft Visual Studio 2015 (Can use community edition)
2. copy vJoyInterface.dll from packages/x86 or packages/x64 to the executable folder
