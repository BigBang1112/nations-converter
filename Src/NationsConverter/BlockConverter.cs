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

    /// <summary>
    /// Block size in small units.
    /// </summary>
    protected Int3 BlockSize { get; }
    protected string Environment { get; }
    protected ConversionSetModel ConversionSet { get; }

    public BlockConverter(CGameCtnChallenge map, NationsConverterConfig config, ILogger logger)
    {
        this.map = map;
        this.config = config;

        BlockSize = map.Collection.GetValueOrDefault().GetBlockSize();

        Environment = map.GetEnvironment() switch
        {
            "Alpine" => "Snow",
            "Speed" => "Desert",
            _ => map.GetEnvironment()
        };

        ConversionSet = Environment switch
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

    protected abstract void ConvertBlock(CGameCtnBlock block, ConversionModel conversion);

    public virtual void Convert()
    {
        foreach (var block in map.GetBlocks())
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
}