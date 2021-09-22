# SolidMakerConverter
C# Project to Convert Cura Slicer files into Solidmaker/XVico X9 *.slm files.

You will need to use the Cura profile, The updated thumbnail create pytrhon script and the "filament profile" to generate a valid *.gcode file which the converter accepts.
The GCode file can then be dragged and dropped onto the SolidmakerConverter exe file and a *.slm file with the input files name will be created in the input files directory.

The outbut file supports preview image and should let you play somewhat with the slicer. Keep in mind The Solidmaker and X9 use M910, M920 and M930 to set up three tools each specifying speed and laser power setting which are selected by specifying a T value att the end of a G1 move.

You can find more GCode info for Solidmaker on Reddit: 
https://www.reddit.com/r/SolidMakerOwners/

Instruction Manual:
https://solidpre-1251753108.cos.na-siliconvalley.myqcloud.com/The%20Instructions%20of%20Solidmaker.pdf

Use at your own risk... ;)


