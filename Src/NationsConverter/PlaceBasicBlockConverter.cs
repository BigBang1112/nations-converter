﻿using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverterShared.Models;

namespace NationsConverter;

internal sealed class PlaceBasicBlockConverter : BlockConverter
{
    private readonly CGameCtnChallenge convertedMap;
    private readonly HashSet<CGameCtnBlock> coveredZoneBlocks;
    private readonly ILogger logger;

    public PlaceBasicBlockConverter(
        CGameCtnChallenge map,
        CGameCtnChallenge convertedMap,
        NationsConverterConfig config,
        HashSet<CGameCtnBlock> coveredZoneBlocks,
        ILogger logger) : base(map, config, logger)
    {
        this.convertedMap = convertedMap;
        this.coveredZoneBlocks = coveredZoneBlocks;
        this.logger = logger;
    }

    protected override void ConvertBlock(CGameCtnBlock block, ConversionModel conversion)
    {
        if (coveredZoneBlocks.Contains(block))
        {
            return;
        }

        if (block.IsClip)
        {
            // Resolve later
            return;
        }

        // fallbacks should be less permissive in the future
        var blockCoordSize = conversion.GetProperty(block, x => x.Size, fallback: true);
        var maxVariants = conversion.GetProperty(block, x => x.Variants, fallback: true);

        if (block.Variant >= maxVariants)
        {
            throw new ArgumentException("Block variant exceeds max variants");
        }

        var maxSubVariants = conversion.GetProperty(block, x => x.SubVariants?[block.Variant.GetValueOrDefault()], fallback: true);

        if (maxSubVariants == 0)
        {
            logger.LogWarning("Block {BlockName} with variant {BlockVariant} has no sub variants defined. Skipping for now.", block.Name, block.Variant);
            return;
        }
        else if (block.SubVariant >= maxSubVariants)
        {
            throw new ArgumentException($"Block sub variant ({block.SubVariant}) exceeds max sub variants ({maxSubVariants})");
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

        var rotRadians = -(int)block.Direction * MathF.PI / 2;

        logger.LogInformation("Placing item ({BlockName}) at {Pos} with rotation {Dir}...", block.Name, pos, block.Direction);

        convertedMap.PlaceAnchoredObject(
            new(itemPath.Replace('/', '\\'), 26, "akPfIM0aSzuHuaaDWptBbQ"),
                pos * BlockSize, (rotRadians, 0, 0));
    }
}