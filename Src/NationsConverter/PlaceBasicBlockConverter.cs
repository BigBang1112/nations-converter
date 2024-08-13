using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverterShared.Models;

namespace NationsConverter;

internal sealed class PlaceBasicBlockConverter : BlockConverter
{
    private readonly CGameCtnChallenge convertedMap;
    private readonly ILogger logger;

    public PlaceBasicBlockConverter(
        CGameCtnChallenge map,
        CGameCtnChallenge convertedMap,
        NationsConverterConfig config,
        ILogger logger) : base(map, config, logger)
    {
        this.convertedMap = convertedMap;
        this.logger = logger;
    }

    protected override void ConvertBlock(CGameCtnBlock block, ConversionModel conversion)
    {
        Int3 blockCoordSize;
        int maxSubVariants;

        if (block.IsClip)
        {
            // Resolve later
            return;
        }

        if (block.IsGround)
        {
            blockCoordSize = conversion.GetProperty(x => x.Ground, x => x.Size);
            var maxVariants = conversion.GetProperty(x => x.Ground, x => x.Variants);

            if (block.Variant >= maxVariants)
            {
                throw new ArgumentException("Block variant exceeds max variants");
            }

            maxSubVariants = conversion.GetProperty(x => x.Ground, x => x.SubVariants?[block.Variant.GetValueOrDefault()]);
        }
        else
        {
            blockCoordSize = conversion.GetProperty(x => x.Air, x => x.Size);
            var maxVariants = conversion.GetProperty(x => x.Air, x => x.Variants);

            if (block.Variant >= maxVariants)
            {
                throw new ArgumentException("Block variant exceeds max variants");
            }

            maxSubVariants = conversion.GetProperty(x => x.Air, x => x.SubVariants?[block.Variant.GetValueOrDefault()]);
        }

        if (block.SubVariant >= maxSubVariants)
        {
            throw new ArgumentException("Block sub variant exceeds max sub variants");
        }

        var modifierType = block.IsGround ? "Ground" : "Air";

        var subCategory = "Modless";

        var dirPath = Path.Combine("NC2", "Solid", subCategory, "MM_Collision", Environment, conversion.PageName, block.Name);
        var itemPath = Path.Combine(dirPath, $"{modifierType}_{block.Variant.GetValueOrDefault()}_{block.SubVariant.GetValueOrDefault()}.Item.Gbx");

        var pos = block.Direction switch
        {
            Direction.East => block.Coord + (blockCoordSize.Z, 0, 0),
            Direction.South => block.Coord + (blockCoordSize.X, 0, blockCoordSize.Z),
            Direction.West => block.Coord + (0, 0, blockCoordSize.X),
            _ => block.Coord
        };

        if (conversion.ZoneHeight.HasValue)
        {
            pos -= (0, conversion.ZoneHeight.Value, 0);
        }

        var dir = -(int)block.Direction * MathF.PI / 2;

        logger.LogInformation("Placing item ({BlockName}) at {Pos} with rotation {Dir}...", block.Name, pos, dir);

        convertedMap.PlaceAnchoredObject(
            new(itemPath.Replace('/', '\\'), 26, "akPfIM0aSzuHuaaDWptBbQ"),
                pos * BlockSize, (dir, 0, 0));
    }
}
