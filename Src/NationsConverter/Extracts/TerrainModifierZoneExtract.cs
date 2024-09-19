using GBX.NET;
using GBX.NET.Engines.Game;
using NationsConverter.Models;
using System.Collections.Immutable;

namespace NationsConverter.Extracts;

internal sealed class TerrainModifierZoneExtract
{
    private readonly CGameCtnChallenge map;
    private readonly ManualConversionSetModel conversionSet;

    public TerrainModifierZoneExtract(
        CGameCtnChallenge map,
        ManualConversionSetModel conversionSet)
    {
        this.map = map;
        this.conversionSet = conversionSet;
    }

    public ImmutableDictionary<Int3, string> Extract()
    {
        var terrainModifierZones = ImmutableDictionary.CreateBuilder<Int3, string>();

        foreach (var block in map.GetBlocks())
        {
            if (block.Variant is null || block.SubVariant is null)
            {
                continue;
            }

            if (conversionSet.BlockTerrainModifiers.TryGetValue(block.Name, out var terrainModifier))
            {
                terrainModifierZones[block.Coord] = terrainModifier;
            }
        }

        return terrainModifierZones.ToImmutable();
    }
}