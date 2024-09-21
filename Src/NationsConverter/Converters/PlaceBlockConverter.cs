using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using System.Collections.Immutable;

namespace NationsConverter.Converters;

internal sealed class PlaceBlockConverter : BlockConverterBase
{
    private readonly CustomContentManager customContentManager;
    private readonly ImmutableHashSet<CGameCtnBlock> coveredZoneBlocks;
    private readonly ImmutableDictionary<Int3, string> terrainModifierZones;
    private readonly ILogger logger;

    public PlaceBlockConverter(
        CGameCtnChallenge map,
        ManualConversionSetModel conversionSet,
        CustomContentManager customContentManager,
        ImmutableHashSet<CGameCtnBlock> coveredZoneBlocks,
        ImmutableDictionary<Int3, string> terrainModifierZones,
        ILogger logger) : base(map, conversionSet)
    {
        this.customContentManager = customContentManager;
        this.coveredZoneBlocks = coveredZoneBlocks;
        this.terrainModifierZones = terrainModifierZones;
        this.logger = logger;
    }

    protected override void ConvertBlock(CGameCtnBlock block, ManualConversionModel conversion)
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

        var dirPath = string.IsNullOrWhiteSpace(conversion.PageName)
            ? Path.Combine("NC2", "Solid", subCategory, "MM_Collision", Environment, block.Name)
            : Path.Combine("NC2", "Solid", subCategory, "MM_Collision", Environment, conversion.PageName, block.Name);

        var variant = block.Variant.GetValueOrDefault();
        var subVariant = block.SubVariant.GetValueOrDefault();

        var itemPath = Path.Combine(dirPath, $"{modifierType}_{variant}_{subVariant}.Item.Gbx");

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

        customContentManager.PlaceItem(itemPath, pos * BlockSize, (rotRadians, 0, 0));

        // Place terrain-modifiable pieces
        if (block.IsGround && conversion.Modifiable.GetValueOrDefault() && (conversion.NotModifiable is null || !conversion.NotModifiable.Contains((variant, subVariant))))
        {
            var terrainItemPath = terrainModifierZones.TryGetValue(block.Coord - (0, 1, 0), out var modifier)
                ? Path.Combine(dirPath, $"{modifier}_{variant}_{subVariant}.Item.Gbx")
                : Path.Combine(dirPath, $"GroundDefault_{variant}_{subVariant}.Item.Gbx");

            customContentManager.PlaceItem(terrainItemPath, pos * BlockSize, (rotRadians, 0, 0));
        }
    }
}
