using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverterShared.Models;
using System;

namespace NationsConverter;

internal abstract class BlockConverter
{
    private readonly CGameCtnChallenge map;
    private readonly NationsConverterConfig config;

    public BlockConverter(CGameCtnChallenge map, NationsConverterConfig config, ILogger logger)
    {
        this.map = map;
        this.config = config;
    }

    public abstract void ConvertBlock(CGameCtnBlock block, ConversionModel conversion, string environment, Int3 blockSize);

    public virtual void Convert()
    {
        var environment = map.GetEnvironment() switch
        {
            "Alpine" => "Snow",
            "Speed" => "Desert",
            _ => map.GetEnvironment()
        };

        var conversionSet = environment switch
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

        Convert(environment, conversionSet);
    }

    protected virtual void Convert(string environment, ConversionSetModel conversionSet)
    {
        var blockSize = map.Collection.GetValueOrDefault().GetBlockSize();

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

            ConvertBlock(block, conversion, environment, blockSize);
        }
    }
}