using GBX.NET;
using GBX.NET.Engines.Game;
using NationsConverter.Models;

namespace NationsConverter.Converters;

internal abstract class BlockConverterBase : EnvironmentConverterBase
{
    private readonly CGameCtnChallenge map;

    /// <summary>
    /// Block size in small units.
    /// </summary>
    protected Int3 BlockSize { get; }
    protected ManualConversionSetModel ConversionSet { get; }

    public BlockConverterBase(CGameCtnChallenge map, ManualConversionSetModel conversionSet)
        : base(map)
    {
        this.map = map;

        BlockSize = map.Collection.GetValueOrDefault().GetBlockSize();
        ConversionSet = conversionSet;
    }

    protected abstract void ConvertBlock(CGameCtnBlock block, ManualConversionModel conversion);

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