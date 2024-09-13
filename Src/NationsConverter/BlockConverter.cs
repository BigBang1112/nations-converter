using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Tool;
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

    public BlockConverter(CGameCtnChallenge map, NationsConverterConfig config, IComplexConfig complexConfig, ILogger logger)
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

        ConversionSet = complexConfig.Get<ConversionSetModel>("Generated/" + Environment);
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