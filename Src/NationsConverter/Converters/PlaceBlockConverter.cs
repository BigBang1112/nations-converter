using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using System.Collections.Immutable;
using System.Diagnostics;

namespace NationsConverter.Converters;

internal sealed class PlaceBlockConverter : BlockConverterBase
{
    private readonly CGameCtnChallenge mapIn;
    private readonly CGameCtnChallenge mapOut;
    private readonly CustomContentManager customContentManager;
    private readonly ImmutableHashSet<CGameCtnBlock> coveredZoneBlocks;
    private readonly ImmutableDictionary<Int3, string> terrainModifierZones;
    private readonly ILogger logger;

    private readonly Dictionary<string, string> reverseBlockTerrainModifiers;

    private readonly Dictionary<Int3, CGameCtnBlock> clipBlocks = [];
    private readonly Dictionary<CGameCtnBlock, HashSet<Direction>> clipDirs = [];

    public PlaceBlockConverter(
        CGameCtnChallenge mapIn,
        CGameCtnChallenge mapOut,
        ManualConversionSetModel conversionSet,
        CustomContentManager customContentManager,
        ImmutableHashSet<CGameCtnBlock> coveredZoneBlocks,
        ImmutableDictionary<Int3, string> terrainModifierZones,
        ILogger logger) : base(mapIn, mapOut, conversionSet)
    {
        this.mapIn = mapIn;
        this.mapOut = mapOut;
        this.customContentManager = customContentManager;
        this.coveredZoneBlocks = coveredZoneBlocks;
        this.terrainModifierZones = terrainModifierZones;
        this.logger = logger;

        reverseBlockTerrainModifiers = ConversionSet.BlockTerrainModifiers.ToDictionary(x => x.Value, x => x.Key);
    }

    public override void Convert()
    {
        foreach (var clipBlock in mapIn.GetBlocks().Where(x => x.IsClip))
        {
            clipBlocks.Add(clipBlock.Coord, clipBlock);
        }

        logger.LogInformation("Placing blocks...");
        var watch = Stopwatch.StartNew();

        base.Convert();

        logger.LogInformation("Placed blocks ({ElapsedMilliseconds}ms).", watch.ElapsedMilliseconds);

        PlaceGroundClipFillers();
    }

    protected override void ConvertBlock(CGameCtnBlock block, ManualConversionModel conversion)
    {
        if (coveredZoneBlocks.Contains(block))
        {
            return;
        }

        if (block.IsClip)
        {
            return;
        }

        PlaceItem(block, conversion);

        TryPlaceClips(block, conversion);
    }

    private void PlaceItem(
        CGameCtnBlock block, 
        ManualConversionModel conversion, 
        string? overrideName = null, 
        Direction? overrideDirection = null, 
        ManualConversionBlockModel? overrideConversion = null)
    {
        // fallbacks should be less permissive in the future
        var blockCoordSize = conversion.GetProperty(block, x => x.Size, fallback: true);
        var maxVariants = conversion.GetProperty(block, x => x.Variants, fallback: true);

        var variant = overrideConversion?.Variant ?? block.Variant.GetValueOrDefault();
        var subVariant = overrideConversion?.SubVariant ?? block.SubVariant.GetValueOrDefault();

        if (variant >= maxVariants)
        {
            throw new ArgumentException("Block variant exceeds max variants");
        }

        if (block.IsClip)
        {
            subVariant = 0;
        }

        var blockName = overrideName ?? block.Name;

        var maxSubVariants = conversion.GetProperty(block, x => x.SubVariants?[variant], fallback: true);

        if (maxSubVariants == 0)
        {
            logger.LogWarning("Block {BlockName} with variant {BlockVariant} has no sub variants defined. Skipping for now.", blockName, variant);
            return;
        }
        else if (subVariant >= maxSubVariants)
        {
            throw new ArgumentException($"Block sub variant ({subVariant}) exceeds max sub variants ({maxSubVariants})");
        }

        var modifierType = block.IsGround ? "Ground" : "Air";

        var dirPath = string.IsNullOrWhiteSpace(conversion.PageName)
            ? blockName
            : Path.Combine(conversion.PageName, blockName);

        var itemPath = Path.Combine(dirPath, $"{modifierType}_{variant}_{subVariant}.Item.Gbx");

        var direction = overrideDirection ?? block.Direction;
        var offset = new Int3(0, overrideConversion?.OffsetY ?? 0, 0);

        var blockModel = conversion.GetPropertyDefault(block, x => x.Block);
        if (blockModel is not null && !string.IsNullOrWhiteSpace(blockModel.Name))
        {
            var additionalBlock = mapOut.PlaceBlock(blockModel.Name, block.Coord + CenterOffset + (0, 8 + blockModel.OffsetY, 0), direction, blockModel.IsGround, (byte)blockModel.Variant);
            additionalBlock.Bit21 = block.Bit21;
        }

        var conversionModel = conversion.GetPropertyDefault(block, x => x.Conversion);
        if (conversionModel is not null && !string.IsNullOrWhiteSpace(conversionModel.Name))
        {
            PlaceItem(block, ConversionSet.Blocks[conversionModel.Name],
                overrideName: conversionModel.Name,
                overrideConversion: conversionModel);
        }

        var pos = block.Coord + CenterOffset + direction switch
        {
            Direction.North => (0, 0, 0),
            Direction.East => (blockCoordSize.Z, 0, 0),
            Direction.South => (blockCoordSize.X, 0, blockCoordSize.Z),
            Direction.West => (0, 0, blockCoordSize.X),
            _ => throw new ArgumentException("Invalid block direction")
        } + offset;

        if (conversion.ZoneHeight.HasValue)
        {
            pos -= (0, conversion.ZoneHeight.Value, 0);
        }

        var rotRadians = -(int)direction * MathF.PI / 2;

        var itemModel = conversion.GetPropertyDefault(block, x => x.Item);
        if (itemModel is not null)
        {
            PlaceItemFromItemModel(itemModel, variant, block.Coord, direction, blockCoordSize);
        }

        var itemModels = conversion.GetPropertyDefault(block, x => x.Items);
        if (itemModels is not null)
        {
            foreach (var item in itemModels)
            {
                PlaceItemFromItemModel(item, variant, block.Coord, direction, blockCoordSize);
            }
        }

        var conversionVariants = conversion.GetPropertyDefault(block, x => x.ConversionVariants);
        if (conversionVariants?.TryGetValue(variant, out var variantModel) == true && variantModel is not null)
        {
            if (variantModel.Item is not null)
            {
                PlaceItemFromItemModel(variantModel.Item, variant, block.Coord, direction, blockCoordSize);
            }
        }

        var noItem = overrideConversion?.NoItem ?? conversion.GetPropertyDefault(block, x => x.NoItem);
        if (!noItem)
        {
            logger.LogInformation("Placing item ({BlockName}) at {Pos} with rotation {Dir}...", blockName, pos, direction);
            customContentManager.PlaceItem(itemPath, pos * BlockSize, (rotRadians, 0, 0));
        }

        // Place terrain-modifiable pieces
        if (block.IsGround && conversion.Modifiable.GetValueOrDefault() && (conversion.NotModifiable is null || !conversion.NotModifiable.Contains((variant, subVariant))))
        {
            var noTerrainModifier = conversion.GetPropertyDefault(block, x => x.NoTerrainModifier);

            if (!noTerrainModifier)
            {
                var placeDefaultZone = conversion.GetPropertyDefault(block, x => x.PlaceDefaultZone);

                // This will work fine in 99% of cases, BUT
                // in TMF, when you place fabric on NON-0x0x0 rotated unit,
                // the fabric is not applied on the whole block
                // however with certain action combinations like undoing, this stays.
                // in TMF, on map reload, non 0x0x0 fabric is consistently not applied
                // so fabric is not yet quite exact here
                // So far, this system is needed ONLY for Stadium
                var modifier = default(string);
                if (Environment == "Stadium")
                {
                    var units = conversion.GetProperty(block, x => x.Units)?.Where(x => x.Y == 0) ?? [];
                    foreach (var unit in units)
                    {
                        var alignedCoord = block.Coord + unit + block.Direction switch
                        {
                            Direction.East => (blockCoordSize.Z - 1, 0, 0),
                            Direction.South => (blockCoordSize.X - 1, 0, blockCoordSize.Z - 1),
                            Direction.West => (0, 0, blockCoordSize.X - 1),
                            _ => (0, 0, 0)
                        };

                        if (terrainModifierZones.TryGetValue(alignedCoord - (0, 1, 0), out modifier))
                        {
                            break;
                        }
                    }
                }
                else
                {
                    modifier = terrainModifierZones.GetValueOrDefault(block.Coord - (0, 1, 0));
                }

                // if useBaseTerrainModifier, place 1x1 pieces on all ground units at Y 0
                // otherwise just place item
                if (placeDefaultZone)
                {
                    string zoneBlockName;
                    ManualConversionModel zoneConversion;

                    if (modifier is null)
                    {
                        zoneBlockName = ConversionSet.DefaultZoneBlock ?? throw new InvalidOperationException("DefaultZoneBlock not set");
                        zoneConversion = ConversionSet.Blocks[zoneBlockName];
                    }
                    else
                    {
                        zoneBlockName = reverseBlockTerrainModifiers.GetValueOrDefault(modifier, ConversionSet.DefaultZoneBlock ?? throw new InvalidOperationException("DefaultZoneBlock not set"));
                        zoneConversion = ConversionSet.Blocks[zoneBlockName];
                    }

                    var zoneDirPath = string.IsNullOrWhiteSpace(zoneConversion.PageName)
                        ? blockName
                        : Path.Combine(zoneConversion.PageName, zoneBlockName);
                    var terrainItemPath = Path.Combine(zoneDirPath, "Ground_0_0.Item.Gbx");

                    var units = conversion.GetProperty(block, x => x.Units)?.ToArray() ?? [(0, 0, 0)];

                    var alignedUnits = new Int3[units.Length];

                    var min = new Int3(int.MaxValue, 0, int.MaxValue);

                    for (int i = 0; i < units.Length; i++)
                    {
                        var unit = units[i];
                        var alignedUnit = block.Direction switch
                        {
                            Direction.East => (-unit.Z, unit.Y, unit.X),
                            Direction.South => (-unit.X, unit.Y, -unit.Z),
                            Direction.West => (unit.Z, unit.Y, -unit.X),
                            _ => unit
                        };

                        if (alignedUnit.X < min.X)
                        {
                            min = min with { X = alignedUnit.X };
                        }

                        if (alignedUnit.Z < min.Z)
                        {
                            min = min with { Z = alignedUnit.Z };
                        }

                        alignedUnits[i] = alignedUnit;
                    }

                    foreach (var unit in alignedUnits.Where(x => x.Y == 0))
                    {
                        var alignedPos = block.Coord + unit - min + CenterOffset + (0, (conversion.ZoneHeight is null ? -1 : 0) + offset.Y, 0);
                        customContentManager.PlaceItem(terrainItemPath, alignedPos * BlockSize, (0, 0, 0));
                    }
                }
                else
                {
                    var terrainItemPath = modifier is null
                        ? Path.Combine(dirPath, $"GroundDefault_{variant}_{subVariant}.Item.Gbx")
                        : Path.Combine(dirPath, $"{modifier}_{variant}_{subVariant}.Item.Gbx");
                    customContentManager.PlaceItem(terrainItemPath, pos * BlockSize, (rotRadians, 0, 0));
                }

            }
        }
    }

    private void PlaceItemFromItemModel(ManualConversionItemModel itemModel, int variant, Int3 coord, Direction direction, Int3 blockCoordSize)
    {
        if (string.IsNullOrWhiteSpace(itemModel.Name))
        {
            return;
        }

        var dir = (((int)direction + itemModel.Dir) % 4);

        var pos = coord + CenterOffset + dir switch
        {
            0 => (0, 0, 0),
            1 => (blockCoordSize.Z, 0, 0),
            2 => (blockCoordSize.X, 0, blockCoordSize.Z),
            3 => (0, 0, blockCoordSize.X),
            _ => throw new ArgumentException("Invalid block direction")
        };

        var rotRadians = -dir * MathF.PI / 2;

        var name = itemModel.Name.Replace("{Variant}", variant.ToString());
        customContentManager.PlaceItem(name, pos * BlockSize + new Vec3(0, itemModel.OffsetY, 0), (rotRadians, 0, 0));
    }

    private bool TryPlaceClips(CGameCtnBlock block, ManualConversionModel conversion)
    {
        var clips = conversion.GetPropertyDefault(block, x => x.Clips);
        
        if (clips is null || clips.Length == 0)
        {
            return false;
        }

        if (conversion.Road is not null)
        {
            return false;
        }

        var blockCoordSize = conversion.GetProperty(block, x => x.Size, fallback: true) - (1, 1, 1);

        foreach (var clip in clips)
        {
            if (clip.Name is null)
            {
                continue;
            }

            var offset = clip.Offset;
            var alignedOffset = block.Direction switch
            {
                Direction.East => (-offset.Z + blockCoordSize.Z, offset.Y, offset.X),
                Direction.South => (-offset.X + blockCoordSize.X, offset.Y, -offset.Z + blockCoordSize.Z),
                Direction.West => (offset.Z, offset.Y, -offset.X + blockCoordSize.X),
                _ => offset
            };

            Int3 clipPop = (Direction)(((int)block.Direction + (int)clip.Dir) % 4) switch
            {
                Direction.North => (0, 0, 1),
                Direction.East => (-1, 0, 0),
                Direction.South => (0, 0, -1),
                Direction.West => (1, 0, 0),
                _ => throw new ArgumentException("Invalid clip direction")
            };
            var coordMatch = block.Coord + alignedOffset + clipPop;

            if (!clipBlocks.TryGetValue(coordMatch, out var clipBlock))
            {
                continue;
            }

            if (!ConversionSet.Blocks.TryGetValue(clip.Name, out var clipConversion))
            {
                logger.LogWarning("Clip {ClipName} not found in conversion set.", clip.Name);
                continue;
            }

            var dir = (Direction)(((int)block.Direction + (int)clip.Dir + 2) % 4);

            if (!clipDirs.TryGetValue(clipBlock, out var dirs))
            {
                dirs = [];
                clipDirs.Add(clipBlock, dirs);
            }
            dirs.Add(dir);

            // TODO condition Fabric in Stadium here

            PlaceItem(clipBlock, clipConversion, overrideName: clip.Name, overrideDirection: dir);
        }

        return clipDirs is { Count: > 0 };
    }

    private void PlaceGroundClipFillers()
    {
        logger.LogInformation("Placing ground clip fillers...");

        foreach (var (block, dirs) in clipDirs)
        {
            if (!block.IsGround)
            {
                continue;
            }

            for (int i = 0; i < 4; i++)
            {
                var dir = (Direction)i;

                if (dirs.Contains(dir))
                {
                    continue;
                }

                // TODO condition Fabric in Stadium here

                PlaceItem(block, ConversionSet.Blocks[block.Name], overrideDirection: dir);
            }
        }

        logger.LogInformation("Placed ground clip fillers.");
    }
}
