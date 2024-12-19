using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using System.Collections.Immutable;
using System.Diagnostics;

namespace NationsConverter.Extracts;

internal sealed class CoveredZoneBlockInfoExtract
{
    private readonly CGameCtnChallenge mapIn;
    private readonly ManualConversionSetModel conversionSet;
    private readonly bool isManiaPlanet;
    private readonly ILogger logger;

    public CoveredZoneBlockInfoExtract(CGameCtnChallenge mapIn, ManualConversionSetModel conversionSet, bool isManiaPlanet, ILogger logger)
    {
        this.mapIn = mapIn;
        this.conversionSet = conversionSet;
        this.isManiaPlanet = isManiaPlanet;
        this.logger = logger;
    }

    public ImmutableHashSet<CGameCtnBlock> Extract()
    {
        logger.LogInformation("Extracting covered zone blocks...");
        var watch = Stopwatch.StartNew();

        var groundPositions = new HashSet<Int3>();

        foreach (var (block, conversion) in conversionSet.GetBlockConversionPairs(mapIn))
        {
            if (conversion.ZoneHeight is not null)
            {
                continue;
            }

            PopulateGroundPositions(groundPositions, block, conversion);
        }

        var coveredZoneBlocks = ImmutableHashSet.CreateBuilder<CGameCtnBlock>();

        foreach (var (block, conversion) in conversionSet.GetBlockConversionPairs(mapIn))
        {
            if (!block.IsGround || conversion.ZoneHeight is null)
            {
                continue;
            }

            if (groundPositions.Contains(block.Coord + (0, isManiaPlanet ? 0 : 1, 0)))
            {
                coveredZoneBlocks.Add(block);
            }
        }

        logger.LogInformation("Extracted {Count} covered zone blocks ({ElapsedMilliseconds}ms).", coveredZoneBlocks.Count, watch.ElapsedMilliseconds);

        return coveredZoneBlocks.ToImmutable();
    }

    private static void PopulateGroundPositions(HashSet<Int3> groundPositions, CGameCtnBlock block, ManualConversionModel conversion)
    {
        // fallbacks should be less permissive in the future
        var units = conversion.GetProperty(block, x => x.Units, fallback: true) ?? [(0, 0, 0)];

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
            groundPositions.Add(block.Coord + unit - min);
        }
    }
}