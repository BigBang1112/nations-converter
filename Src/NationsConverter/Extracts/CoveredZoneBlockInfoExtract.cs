using GBX.NET;
using GBX.NET.Engines.Game;
using NationsConverter.Models;
using System.Collections.Immutable;

namespace NationsConverter.Extracts;

internal sealed class CoveredZoneBlockInfoExtract
{
    private readonly CGameCtnChallenge map;
    private readonly ManualConversionSetModel conversionSet;

    public CoveredZoneBlockInfoExtract(CGameCtnChallenge map, ManualConversionSetModel conversionSet)
    {
        this.map = map;
        this.conversionSet = conversionSet;
    }

    public ImmutableHashSet<CGameCtnBlock> Extract()
    {
        var groundPositions = new HashSet<Int3>();

        foreach (var (block, conversion) in conversionSet.GetBlockConversionPairs(map))
        {
            if (conversion.ZoneHeight is not null)
            {
                continue;
            }

            PopulateGroundPositions(groundPositions, block, conversion);
        }

        var coveredZoneBlocks = ImmutableHashSet.CreateBuilder<CGameCtnBlock>();

        foreach (var (block, conversion) in conversionSet.GetBlockConversionPairs(map))
        {
            if (!block.IsGround || conversion.ZoneHeight is null)
            {
                continue;
            }

            if (groundPositions.Contains(block.Coord))
            {
                coveredZoneBlocks.Add(block);
            }
        }

        return coveredZoneBlocks.ToImmutable();
    }

    private static void PopulateGroundPositions(HashSet<Int3> groundPositions, CGameCtnBlock block, ManualConversionModel conversion)
    {
        // fallbacks should be less permissive in the future
        var units = conversion.GetProperty(block, x => x.Units, fallback: true) ?? [(0, 0, 0)];

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

            groundPositions.Add(block.Coord + alignedUnit - min - (0, 1, 0));
        }
    }
}