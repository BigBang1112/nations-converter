using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverterShared.Models;

namespace NationsConverter;

internal sealed class PlaceGroundConverter : BlockConverter
{
    private readonly CGameCtnChallenge map;

    private readonly bool[,] usedGrounds;

    public PlaceGroundConverter(
        CGameCtnChallenge map,
        CGameCtnChallenge convertedMap,
        NationsConverterConfig config,
        ILogger logger) : base(map, config, logger)
    {
        usedGrounds = new bool[map.Size.X, map.Size.Z];
        this.map = map;
    }

    protected override void ConvertBlock(CGameCtnBlock block, ConversionModel conversion)
    {
        if (block.IsGround)
        {
            usedGrounds[block.Coord.X, block.Coord.Z] = true;
        }
    }

    public override void Convert()
    {
        var baseHeight = ConversionSet.Decorations
            .GetValueOrDefault($"{map.Size.X}x{map.Size.Y}x{map.Size.Z}")?.BaseHeight ?? 0;

        base.Convert();


    }
}
