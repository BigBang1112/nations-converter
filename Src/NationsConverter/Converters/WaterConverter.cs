using GBX.NET;
using GBX.NET.Engines.Game;
using NationsConverter.Models;
using System.Collections.Immutable;

namespace NationsConverter.Converters;

internal sealed class WaterConverter : BlockConverter
{
    private readonly CGameCtnChallenge convertedMap;
    private readonly ImmutableHashSet<CGameCtnBlock> coveredZoneBlocks;

    public WaterConverter(
        CGameCtnChallenge map,
        CGameCtnChallenge convertedMap,
        ManualConversionSetModel conversionSet,
        ImmutableHashSet<CGameCtnBlock> coveredZoneBlocks) : base(map, conversionSet)
    {
        this.convertedMap = convertedMap;
        this.coveredZoneBlocks = coveredZoneBlocks;
    }

    public override void Convert()
    {
        if (Environment is "Coast")
        {
            return;
        }

        base.Convert();

        foreach (var block in coveredZoneBlocks)
        {
            if (block.Variant is null || block.SubVariant is null)
            {
                continue;
            }

            if (!ConversionSet.Blocks.TryGetValue(block.Name, out var conversion))
            {
                continue;
            }

            ConvertBlock(block, conversion);
        }
    }

    protected override void ConvertBlock(CGameCtnBlock block, ManualConversionModel conversion)
    {
        // exception should be envs where water is base zone
        if (conversion.ZoneHeight is null)
        {
            return;
        }

        var waterUnits = conversion.GetPropertyDefault(block, m => m.WaterUnits);

        if (waterUnits is null || waterUnits.Length == 0)
        {
            return;
        }

        PlaceWater(convertedMap, block.Coord, BlockSize, ConversionSet.WaterHeight);
    }

    public static void PlaceWater(CGameCtnChallenge convertedMap, Int3 pos, Int3 blockSize, float waterHeight)
    {
        var waterSize = new Int3(blockSize.X / 32, 1, blockSize.Z / 32);

        for (var x = 0; x < waterSize.X; x++)
        {
            for (var z = 0; z < waterSize.Z; z++)
            {
                var blockWater = convertedMap.PlaceBlock("DecoWallWaterBase", (-1, 0, -1), Direction.North);
                blockWater.IsFree = true;
                blockWater.AbsolutePositionInMap = pos * blockSize + new Vec3(x * 32, waterHeight, z * 32);
            }
        }
    }
}
