This program allows you to chunk a large map into smaller maps (36x36 by default). It might be useful when decorating a large map; you could chunk it, decorate the smaller parts, and save the deco as a macroblock to apply on the larger actual map.

The program will produce new map files called "NAME Chunk X Z.Map.Gbx" in the same directory as the map file.

Note: Each output map will include 6 blocks of empty padding around the map. This is necessary because, without this, blocks can be in invalid positions (where they poke outside the map).

### Usage:

Easy way: drop a .map.gbx file on top of the .exe file.

Advanced way: run this program from a CLI (like powershell or cmd).

CLI Arguments:

* path to map file
* width and height of chunk size
* coordinates for creating a specific chunk

Examples:
* `.\map-chunker.exe c:\users\xertrov\Documents\Trackmania\Maps\test-chunker-1.Map.gbx`
  * chunks a map into 36x36 maps, processes entire map
* `.\map-chunker.exe c:\users\xertrov\Documents\Trackmania\Maps\test-chunker-1.Map.gbx 64 32`
  * chunks a map into 64x32 maps, processes entire map
* `.\map-chunker.exe c:\users\xertrov\Documents\Trackmania\Maps\test-chunker-1.Map.gbx 64 32 11 17`
  * creates one chunk from a map of size 64x32 maps, starting at x=11 and z=17 (ending at x=74, z=48 inclusive)

Map in screenshot credit: Ski Freak
