using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using System.Collections.Immutable;
using System.Diagnostics;

namespace NationsConverter.Stages;

internal sealed class WaterStage : BlockStageBase
{
    private readonly CGameCtnChallenge mapOut;
    private readonly ImmutableHashSet<CGameCtnBlock> coveredZoneBlocks;
    private readonly bool isManiaPlanet;
    private readonly ILogger logger;

    public WaterStage(
        CGameCtnChallenge mapIn,
        CGameCtnChallenge mapOut,
        ManualConversionSetModel conversionSet,
        ImmutableHashSet<CGameCtnBlock> coveredZoneBlocks,
        bool isManiaPlanet,
        ILogger logger) : base(mapIn, mapOut, conversionSet)
    {
        this.mapOut = mapOut;
        this.coveredZoneBlocks = coveredZoneBlocks;
        this.isManiaPlanet = isManiaPlanet;
        this.logger = logger;
    }

    public override void Convert()
    {
        if (Environment is "Coast")
        {
            logger.LogWarning("Water in Coast environment is not supported yet.");
            return;
        }

        logger.LogInformation("Placing water...");
        var watch = Stopwatch.StartNew();

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

        logger.LogInformation("Placed water ({ElapsedMilliseconds}ms).", watch.ElapsedMilliseconds);
    }

    protected override void ConvertBlock(CGameCtnBlock block, ManualConversionModel conversion)
    {
        // exception should be envs where water is base zone
        if (conversion.ZoneHeight is null)
        {
            return;
        }

        var waterUnits = conversion.GetPropertyDefault(block, m => m.WaterUnits);

        if (waterUnits?.Length > 0)
        {
            PlaceWater(mapOut, block.Coord + TotalOffset + (0, conversion.WaterOffsetY + (isManiaPlanet ? -1 : 0), 0), BlockSize, ConversionSet.WaterHeight);
        }
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
