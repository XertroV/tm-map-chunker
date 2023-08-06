// See https://aka.ms/new-console-template for more information
using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.LZO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;

internal class Program
{
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return;
        }

        if (args[0] == "--help" || args[0] == "-h") {
            PrintHelp();
            return;
        }

        var mapFile = args[0];

        Console.WriteLine($"Chunking map: {mapFile}");

        if (args.Length > 1 && args.Length != 3 && args.Length != 5) {
            Console.WriteLine("error: incorrect args");
            PrintHelp();
            // AwaitEnter();
            return;
        }

        var width = 36;
        var depth = 36;
        if (args.Length >= 3) {
            width = Int32.Parse(args[1]);
            depth = Int32.Parse(args[2]);
        }
        int x = -1;
        int z = -1;
        if (args.Length == 5) {
            x = Int32.Parse(args[3]);
            z = Int32.Parse(args[4]);
        }

        GBX.NET.Lzo.SetLzo(typeof(GBX.NET.LZO.MiniLZO));

        CGameCtnChallenge map;
        try {
            map = GameBox.ParseNode<CGameCtnChallenge>(args[0]);
        } catch (Exception e) {
            Console.WriteLine($"Error: could not parse map:\n{e.ToString()}");
            throw e;
        }

        // map.MapName += "_resave";
        // map.Save(mapFile.Substring(0, mapFile.Length - 8) + "_resave.Map.Gbx");

        var chunkWholeMap = x < 0 || z < 0;
        var mapDims = map.Size!.Value;

        if (chunkWholeMap) {
            var currx = 0;
            while (currx < mapDims.X) {
                var currz = 0;
                while (currz < mapDims.Z) {
                    Console.WriteLine($"map EmbeddedData length: {map.EmbeddedData!.Count}");
                    CreateMapChunk(mapFile, currx, currz, width, depth);
                    currz += depth;
                }
                currx += width;
            }
        } else {
            CreateMapChunk(mapFile, x, z, width, depth);
        }
        Console.WriteLine($"Orig Map Size: {map.Size.ToString()}");

        return;
    }

    private static void CreateMapChunk(string mapFile, int currx, int currz, int width, int depth)
    {
        CGameCtnChallenge map = GameBox.Parse<CGameCtnChallenge>(mapFile);
        var initBlocksLen = map.Blocks!.Count;
        var initBBlocksLen = map.BakedBlocks!.Count;
        var initItemsLen = map.AnchoredObjects!.Count;
        map.Blocks = map.Blocks!.Where(b => BlockIsInRegion(b, currx, currz, width, depth)).ToList();
        map.BakedBlocks = map.BakedBlocks!.Where(b => BlockIsInRegion(b, currx, currz, width, depth)).ToList();
        map.AnchoredObjects = map.AnchoredObjects!.Where(x => IsInRegion(x.AbsolutePositionInMap, currx, currz, width, depth) || IsInRegion(x.BlockUnitCoord, currx, currz, width, depth)).ToList();
        // map.AnchoredObjects.Clear();
        // map.Blocks.Clear();
        // map.BakedBlocks.Clear();
        Console.WriteLine($"Region: {currx}, {currz}, {width}, {depth}");
        Console.WriteLine($"Blocks: orig: {initBlocksLen}, now: {map.Blocks!.Count}");
        Console.WriteLine($"BakedBlocks: orig: {initBBlocksLen}, now: {map.BakedBlocks!.Count}");
        Console.WriteLine($"AnchoredObjects: orig: {initItemsLen}, now: {map.AnchoredObjects!.Count}");

        // padding around the map to ensure all blocks have valid positions / locations
        Int3 offset = new Int3(6, 6, 6);

        foreach (var item in map.AnchoredObjects!)
        {
            ModPosition(item, currx, currz, width, depth, offset);
        }
        foreach (var item in map.Blocks!)
        {
            ModPosition(item, currx, currz, width, depth, offset);
        }
        foreach (var item in map.BakedBlocks!)
        {
            ModPosition(item, currx, currz, width, depth, offset);
        }
        map.Size = new Int3(width + offset.X * 2, map.Size!.Value.Y, depth + offset.Z * 2);
        var mapNameExtra = $" Chunk {currx} {currz}";
        map.MapName = map.MapName.Substring(0, Math.Min(map.MapName.Length, 40)) + "$z" + mapNameExtra;
        // var fileNameExtra = $"_Chunk_{currx}_{currz}";
        var outFile = mapFile.Substring(0, mapFile.Length - 8) + mapNameExtra + ".Map.Gbx";
        // v5 gives dictionary errors
        map.GetChunk<CGameCtnChallenge.Chunk03043040>()!.Version = 4;
        // map.MacroblockInstances = null;
        if (map.MacroblockInstances is not null) {
            map.MacroblockInstances!.Clear();
        }
        // map.MapUid += $"{currx}_{currz}_{width}_{depth}";
        map.Save(outFile);
        Console.WriteLine($"Wrote Map Chunk: {outFile} (size: {map.Size.ToString()})");
    }

    private static bool BlockIsInRegion(CGameCtnBlock b, float currx, float currz, float width, float depth)
    {
        return IsInRegion(b.Coord, currx, currz, width, depth)
            || (b.AbsolutePositionInMap is not null && IsInRegion(b.AbsolutePositionInMap.Value, currx, currz, width, depth));
    }

    private static void ModPosition(CGameCtnBlock block, int currx, int currz, int width, int depth, Int3 offset)
    {
        block.MacroblockReference = null;
        if (block.AbsolutePositionInMap is not null) {
            block.AbsolutePositionInMap = ModPosVec3(block.AbsolutePositionInMap.Value, currx, currz, width, depth, offset);
        }
        if (block.Coord.X >= 0 && block.Coord.Z >= 0) {
            block.Coord = ModCoord(block.Coord, currx, currz, width, depth, offset);
        }
    }

    private static Int3 ModCoord(Int3 coord, int currx, int currz, int width, int depth, Int3 offset)
    {
        return new Int3(coord.X - currx + offset.X, coord.Y, coord.Z - currz + offset.Z);
    }

    private static void ModPosition(CGameCtnAnchoredObject item, int currx, int currz, int width, int depth, Int3 offset)
    {
        item.MacroblockReference = null;
        item.SnappedOnBlock = null;
        item.SnappedOnGroup = null;
        item.SnappedOnItem = null;
        item.PlacedOnItem = null;
        item.BlockUnitCoord = ModCoord(item.BlockUnitCoord, currx, currz, width, depth, offset);
        item.AbsolutePositionInMap = ModPosVec3(item.AbsolutePositionInMap, currx, currz, width, depth, offset);
    }

    private static Vec3 ModPosVec3(Vec3 absolutePositionInMap, int currx, int currz, int width, int depth, Int3 offset)
    {
        return new Vec3(
            absolutePositionInMap.X - (currx - offset.X) * 32,
            absolutePositionInMap.Y,
            absolutePositionInMap.Z - (currz - offset.Z) * 32
        );
    }

    private static Byte3 ModCoord(Byte3 blockUnitCoord, int currx, int currz, int width, int depth, Int3 offset)
    {
        return new Byte3((byte)(blockUnitCoord.X - currx + offset.X), blockUnitCoord.Y, (byte)(blockUnitCoord.Z - currz + offset.Z));
    }

    private static bool IsInRegion(Byte3 blockUnitCoord, float currx, float currz, float width, float depth)
    {
        // Console.WriteLine($"Byte3 Coord: {blockUnitCoord.ToString()}");
        return currx <= blockUnitCoord.X && blockUnitCoord.X < (currx + width)
            && currz <= blockUnitCoord.Z && blockUnitCoord.Z < (currz + depth);
    }

    private static bool IsInRegion(Int3 blockUnitCoord, float currx, float currz, float width, float depth)
    {
        // Console.WriteLine($"Int3 Coord: {blockUnitCoord.ToString()}");
        return currx <= blockUnitCoord.X && blockUnitCoord.X < (currx + width)
            && currz <= blockUnitCoord.Z && blockUnitCoord.Z < (currz + depth);
    }

    private static bool IsInRegion(Vec3 pos, float currx, float currz, float width, float depth)
    {
        // Console.WriteLine($"Vec3 Coord: {pos.ToString()}");
        return currx <= pos.X / 32.0 && pos.X / 32.0 < currx + width
            && currz <= pos.Z / 32.0 && pos.Z / 32.0 < currz + depth;
    }

    private static void PrintHelp() {
            Console.WriteLine("usage: ./map-chunker MAP_FILE [WIDTH=36 DEPTH=36] [X Z]\n");
            Console.WriteLine("   --help, -h: show help.");
            Console.WriteLine("");

            Console.WriteLine("by default, the map will be chunked into 36x36 sized chunks (an extra 6 units are always added to each side as buffer so that blocks are always in valid positions).");
    }
}
