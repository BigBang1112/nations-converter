using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverterShared.Models;

namespace NationsConverter;

internal sealed class PlaceGroundConverter : BlockConverter
{
    private readonly CGameCtnChallenge map;
    private readonly CGameCtnChallenge convertedMap;

    private readonly int baseHeight;
    private readonly bool[,] occupiedZone;

    public PlaceGroundConverter(
        CGameCtnChallenge map,
        CGameCtnChallenge convertedMap,
        NationsConverterConfig config,
        ILogger logger) : base(map, config, logger)
    {
        this.map = map;
        this.convertedMap = convertedMap;

        occupiedZone = new bool[map.Size.X, map.Size.Z];

        baseHeight = ConversionSet.Decorations
            .GetValueOrDefault($"{map.Size.X}x{map.Size.Y}x{map.Size.Z}")?.BaseHeight ?? 0;
    }

    protected override void ConvertBlock(CGameCtnBlock block, ConversionModel conversion)
    {
        if (!block.IsGround)
        {
            return;
        }

        // If block is of zone type (ZoneHeight not null) it is automatically considered occupied
        // Block's height does not matter - tested on TMUnlimiter
        if (conversion.ZoneHeight.HasValue)
        {
            occupiedZone[block.Coord.X, block.Coord.Z] = true;
            return;
        }

        // TODO: Set more occupied places where there are ground blocks already by using block units
    }

    public override void Convert()
    {
        if (string.IsNullOrEmpty(ConversionSet.DefaultZoneBlock))
        {
            return;
        }

        base.Convert();

        var conversion = ConversionSet.Blocks[ConversionSet.DefaultZoneBlock];

        var subCategory = "Modless";
        var dirPath = Path.Combine("NC2", "Solid", subCategory, "MM_Collision", Environment, conversion.PageName, ConversionSet.DefaultZoneBlock);
        var itemPath = Path.Combine(dirPath, "Ground_0_0.Item.Gbx");

        for (var x = 0; x < occupiedZone.GetLength(0); x++)
        {
            for (var z = 0; z < occupiedZone.GetLength(1); z++)
            {
                if (occupiedZone[x, z])
                {
                    continue;
                }

                var pos = new Int3(x, baseHeight, z);

                convertedMap.PlaceAnchoredObject(
                    new(itemPath.Replace('/', '\\'), 26, "akPfIM0aSzuHuaaDWptBbQ"),
                        pos * BlockSize, (0, 0, 0));
            }
        }
    }
}
