# eden-unity-builder
Eden World Builder fan-made recreation for Android and PC

### Status:
The project is not actively developed anymore, so I'll just leave this repo here.
This is very buggy and has bad performance due to the unfinished voxel core.
Some code snippets are several years old.

Version of Unity to run is 2020.1.1f, should be ok on newer ones.

I will probably consider pull requests,
but only those that fix existing/refactoring code or add features from the ORIGINAL version.
Porting of Obj-C/C++ to C# is also accepted.

### Finished features:
- Base gameplay of the original game
- Simple blocky world with terrain generation
- Menu with online world list
- Ladders
- Block painting
- Sky painting
  
### Almost working features:
- Fire physics(after rewriting the voxel core its broken a bit)
- Sounds and music
- Support for worlds from Eden World Builder(Loading works but saving is broken, about that below)

### Unfinished features:
- Interactive blocks like liquids, portals
- Ramps and half blocks(for liquids)
- PC controls
- Mobs
- Teleport to the home and screenshot button

### About compatibility with World Builder:
You can now load any world from World Builder, however saving to original format is broken and needs to be finished.

### Useful links:
- Article about world format by Robert Munafo: https://mrob.com/pub/vidgames/eden-file-format.html
- Source code of World Builder(Objective-C/C++): https://github.com/phonkee/EdenWorldBuilder
