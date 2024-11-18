using GBX.NET;
using GBX.NET.Engines.Game;
using NationsConverter.Models;

namespace NationsConverter.Stages;

internal abstract class BlockStageBase : EnvironmentStageBase
{
    private readonly CGameCtnChallenge mapIn;

    /// <summary>
    /// Block size in small units.
    /// </summary>
    protected Int3 BlockSize { get; }
    protected Int3 CenterOffset { get; }
    protected Int3 TotalOffset { get; }
    protected ManualConversionSetModel ConversionSet { get; }

    public BlockStageBase(CGameCtnChallenge mapIn, CGameCtnChallenge mapOut, ManualConversionSetModel conversionSet)
        : base(mapIn)
    {
        this.mapIn = mapIn;

        BlockSize = mapIn.Collection.GetValueOrDefault().GetBlockSize();
        if (Environment == "Stadium")
        {
            CenterOffset = new Int3((mapOut.Size.X - mapIn.Size.X) / 2, 0, (mapOut.Size.Z - mapIn.Size.Z) / 2);
        }
        TotalOffset = CenterOffset + (0, -mapIn.DecoBaseHeightOffset, 0);
        ConversionSet = conversionSet;
    }

    protected abstract void ConvertBlock(CGameCtnBlock block, ManualConversionModel conversion);

    public virtual void Convert()
    {
        foreach (var (block, conversion) in ConversionSet.GetBlockConversionPairs(mapIn))
        {
            ConvertBlock(block, conversion);
        }
    }
}