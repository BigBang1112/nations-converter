using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Tool;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using NationsConverterShared.Models;
using System.Collections.Immutable;

namespace NationsConverter;

internal sealed class TerrainModifierZoneExtract
{
    private readonly CGameCtnChallenge map;
    private readonly NationsConverterConfig config;
    private readonly ILogger logger;

    private readonly ManualConversionSetModel conversionSet;

    public TerrainModifierZoneExtract(
        CGameCtnChallenge map, 
        NationsConverterConfig config, 
        IComplexConfig complexConfig,
        ILogger logger)
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

        conversionSet = complexConfig.Get<ManualConversionSetModel>(Path.Combine("Manual", environment), cache: true)
            .Merge(complexConfig.Get<ConversionSetModel>(Path.Combine("Generated", environment), cache: true));
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