using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using System.Collections.Immutable;

namespace NationsConverter.Extracts;

internal sealed class TerrainModifierZoneExtract
{
    private readonly CGameCtnChallenge mapIn;
    private readonly ManualConversionSetModel conversionSet;
    private readonly ILogger logger;

    public TerrainModifierZoneExtract(
        CGameCtnChallenge mapIn,
        ManualConversionSetModel conversionSet,
        ILogger logger)
    {
        this.mapIn = mapIn;
        this.conversionSet = conversionSet;
        this.logger = logger;
    }

    public ImmutableDictionary<Int3, string> Extract()
    {
        var terrainModifierZones = ImmutableDictionary.CreateBuilder<Int3, string>();

        foreach (var block in mapIn.GetBlocks())
        {
            if (conversionSet.BlockTerrainModifiers.TryGetValue(block.Name, out var terrainModifier))
            {
                terrainModifierZones[block.Coord with { Y = 0 }] = terrainModifier;
                continue;
            }

            if (conversionSet.Environment != "Stadium")
            {
                continue;
            }

            if (!conversionSet.Blocks.TryGetValue(block.Name, out var conversion))
            {
                continue;
            }

            var terrainModifierUnits = conversion.GetPropertyDefault(block, x => x.TerrainModifierUnits);

            if (terrainModifierUnits is null || terrainModifierUnits.Count == 0)
            {
                continue;
            }

            if (!terrainModifierUnits.TryGetValue("Fabric", out var units))
            {
                continue;
            }

            SetFabricZones(terrainModifierZones, block, units, conversion.GetProperty(block, x => x.Size) - (1, 1, 1));
        }

        logger.LogInformation("Extracted {Count} terrain modifier zones.", terrainModifierZones.Count);

        return terrainModifierZones.ToImmutable();
    }

    private void SetFabricZones(
        ImmutableDictionary<Int3, string>.Builder terrainModifierZones, 
        CGameCtnBlock block, 
        Int3[] units, 
        Int3 size)
    {
        Span<Int3> alignedUnits = stackalloc Int3[units.Length];

        var min = new Int3(int.MaxValue, 0, int.MaxValue);

        for (int i = 0; i < units.Length; i++)
        {
            var unit = units[i];
            var alignedUnit = block.Direction switch
            {
                Direction.East => (-unit.Z + size.Z, unit.Y, unit.X),
                Direction.South => (-unit.X + size.X, unit.Y, -unit.Z + size.Z),
                Direction.West => (unit.Z, unit.Y, -unit.X + size.X),
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
            var finalUnit = (block.Coord + unit) with { Y = 0 };
            if (!terrainModifierZones.ContainsKey(finalUnit))
            {
                terrainModifierZones[finalUnit] = "Fabric";
            }
        }
    }
}