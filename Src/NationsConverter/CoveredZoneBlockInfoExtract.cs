﻿using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using NationsConverterShared.Models;

namespace NationsConverter;

internal sealed class CoveredZoneBlockInfoExtract
{
    private readonly CGameCtnChallenge map;
    private readonly NationsConverterConfig config;
    private readonly ILogger logger;

    private readonly ConversionSetModel conversionSet;

    public CoveredZoneBlockInfoExtract(CGameCtnChallenge map, NationsConverterConfig config, ILogger logger)
    {
        this.map = map;
        this.config = config;
        this.logger = logger;

        var environment = map.GetEnvironment() switch
        {
            "Alpine" => "Snow",
            "Speed" => "Desert",
            _ => map.GetEnvironment()
        };

        conversionSet = environment switch
        {
            "Snow" => config.Snow,
            "Rally" => config.Rally,
            "Desert" => config.Desert,
            "Island" => config.Island,
            "Bay" => config.Bay,
            "Coast" => config.Coast,
            "Stadium" => config.Stadium, // should not be always Solid category
            _ => throw new ArgumentException("Environment not supported")
        };
    }

    public HashSet<CGameCtnBlock> Extract()
    {
        var groundPositions = new HashSet<Int3>();

        foreach (var block in map.GetBlocks())
        {
            if (block.Variant is null || block.SubVariant is null)
            {
                continue;
            }

            if (!conversionSet.Blocks.TryGetValue(block.Name, out var conversion))
            {
                continue;
            }

            if (conversion.ZoneHeight is not null)
            {
                continue;
            }

            PopulateGroundPositions(groundPositions, block, conversion);
        }

        var coveredZoneBlocks = new HashSet<CGameCtnBlock>();

        foreach (var block in map.GetBlocks())
        {
            if (block.Variant is null || block.SubVariant is null)
            {
                continue;
            }

            if (!conversionSet.Blocks.TryGetValue(block.Name, out var conversion))
            {
                continue;
            }

            if (!block.IsGround || conversion.ZoneHeight is null)
            {
                continue;
            }

            if (groundPositions.Contains(block.Coord))
            {
                coveredZoneBlocks.Add(block);
            }
        }

        return coveredZoneBlocks;
    }

    private void PopulateGroundPositions(HashSet<Int3> groundPositions, CGameCtnBlock block, ConversionModel conversion)
    {
        var units = (block.IsGround
            ? conversion.GetProperty(x => x.Ground, x => x.Units)
            : conversion.GetProperty(x => x.Air, x => x.Units)) ?? [(0, 0, 0)];

        Span<Int3> alignedUnits = stackalloc Int3[units.Length];

        var min = new Int3(int.MaxValue, 0, int.MaxValue);

        for (int i = 0; i < units.Length; i++)
        {
            var unit = units[i];
            var alignedUnit = block.Direction switch
            {
                Direction.East => (-unit.Z, unit.Y, unit.X),
                Direction.South => (-unit.X, unit.Y, -unit.Z),
                Direction.West => (unit.Z, unit.Y, -unit.X),
                _ => unit
            };

            // instead of this min solution, it could be poss to just add half of the block size floored
            if (alignedUnit.X < min.X)
            {
                min = min with { X = alignedUnit.X };
            }

            if (alignedUnit.Z < min.Z)
            {
                min = min with { Z = alignedUnit.Z };
            }

            alignedUnits[i] = alignedUnit;
        }

        foreach (var unit in alignedUnits)
        {
            groundPositions.Add(block.Coord + unit - min - (0, 1, 0));
        }
    }
}