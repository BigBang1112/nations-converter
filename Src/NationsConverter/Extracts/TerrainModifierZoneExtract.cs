using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using System.Collections.Immutable;

namespace NationsConverter.Extracts;

internal sealed class TerrainModifierZoneExtract
{
    private readonly CGameCtnChallenge map;
    private readonly ManualConversionSetModel conversionSet;
    private readonly ILogger logger;

    public TerrainModifierZoneExtract(
        CGameCtnChallenge map,
        ManualConversionSetModel conversionSet,
        ILogger logger)
    {
        this.map = map;
        this.conversionSet = conversionSet;
        this.logger = logger;
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

        logger.LogInformation("Extracted {Count} terrain modifier zones.", terrainModifierZones.Count);

        return terrainModifierZones.ToImmutable();
    }
}