using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.Tool;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using System.Collections.Immutable;
using System.Diagnostics;

namespace NationsConverter.Stages;

internal sealed class PlaceBlockStage : BlockStageBase
{
    private readonly CGameCtnChallenge mapIn;
    private readonly CGameCtnChallenge mapOut;
    private readonly CustomContentManager customContentManager;
    private readonly ImmutableHashSet<CGameCtnBlock> coveredZoneBlocks;
    private readonly ImmutableDictionary<Int3, string> terrainModifierZones;
    private readonly bool isManiaPlanet;
    private readonly ILogger logger;

    private readonly Dictionary<string, string> reverseBlockTerrainModifiers;
    private readonly Dictionary<string, SkinInfoModel> skinMapping;

    private readonly Dictionary<Int3, CGameCtnBlock> clipBlocks = [];
    private readonly Dictionary<CGameCtnBlock, HashSet<Direction>> clipDirs = [];
    private readonly ILookup<Int3, CGameCtnBlock> blocksPerCoord;

    public PlaceBlockStage(
        CGameCtnChallenge mapIn,
        CGameCtnChallenge mapOut,
        ManualConversionSetModel conversionSet,
        CustomContentManager customContentManager,
        ImmutableHashSet<CGameCtnBlock> coveredZoneBlocks,
        ImmutableDictionary<Int3, string> terrainModifierZones,
        bool isManiaPlanet,
        IComplexConfig complexConfig,
        ILogger logger) : base(mapIn, mapOut, conversionSet, logger)
    {
        this.mapIn = mapIn;
        this.mapOut = mapOut;
        this.customContentManager = customContentManager;
        this.coveredZoneBlocks = coveredZoneBlocks;
        this.terrainModifierZones = terrainModifierZones;
        this.isManiaPlanet = isManiaPlanet;
        this.logger = logger;

        reverseBlockTerrainModifiers = ConversionSet.BlockTerrainModifiers.ToDictionary(x => x.Value, x => x.Key);
        skinMapping = complexConfig.Get<Dictionary<string, SkinInfoModel>>("Skins");

        blocksPerCoord = mapIn.GetBlocks().ToLookup(x => x.Coord);
    }

    public override void Convert()
    {
        logger.LogInformation("Gathering clip blocks (TMF)...");

        foreach (var clipBlock in mapIn.GetBlocks().Where(x => x.IsClip))
        {
            if (clipBlocks.TryAdd(clipBlock.Coord, clipBlock))
            {
                logger.LogInformation("Clip block at {Coord} of type {Name}", clipBlock.Coord, clipBlock.Name);
            }
            else
            {
                logger.LogInformation("Duplicate clip block at {Coord}?", clipBlock.Coord);
            }
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

        PlaceBlockConversion(block, conversion);

        TryPlaceClips(block, conversion);
    }

    private void PlaceBlockConversion(
        CGameCtnBlock block,
        ManualConversionModel conversion,
        string? overrideName = null,
        Direction? overrideDirection = null,
        ManualConversionBlockModel? overrideConversion = null,
        int? overrideVariant = null,
        int? overrideSubVariant = null)
    {
        var blockName = overrideName ?? block.Name;

        using var _ = logger.BeginScope("Block {BlockName}", blockName);

        logger.LogInformation("Placing block {BlockName} from {Coord}, {Direction} ...", blockName, block.Coord, block.Direction);

        // fallbacks should be less permissive in the future
        var blockCoordSize = conversion.GetProperty(block, x => x.Size, fallback: true);
        var maxVariants = conversion.GetProperty(block, x => x.Variants, fallback: true);

        var variant = overrideVariant ?? overrideConversion?.Variant ?? block.Variant;
        var subVariant = overrideSubVariant ?? overrideConversion?.SubVariant ?? block.SubVariant;

        if (conversion.Variant.HasValue)
        {
            variant = conversion.Variant.Value;
        }

        if (variant >= maxVariants)
        {
            logger.LogWarning("Block {BlockName} with variant {Variant} exceeds max variants ({MaxVariants}). Skipping for now.", blockName, variant, maxVariants);
            return;
        }

        if (block.IsClip)
        {
            subVariant = 0;
        }

        if (conversion.SubVariant.HasValue)
        {
            subVariant = conversion.SubVariant.Value;
        }

        var maxSubVariants = conversion.GetProperty(block, x => x.SubVariants?[variant], fallback: true);

        if (maxSubVariants == 0)
        {
            logger.LogWarning("Block {BlockName} with variant {BlockVariant} has no sub variants defined. Skipping for now.", blockName, variant);
            return;
        }
        else if (subVariant >= maxSubVariants)
        {
            throw new ArgumentException($"Block {blockName} sub variant ({subVariant}) exceeds max sub variants ({maxSubVariants})");
        }

        var dirPath = string.IsNullOrWhiteSpace(conversion.PageName)
            ? blockName
            : Path.Combine(conversion.PageName, blockName);

        var direction = overrideDirection ?? ((Direction)(((int)block.Direction + (overrideConversion?.Dir ?? 0)) % 4));
        var offset = new Int3();
        if (overrideConversion is not null)
        {
            offset = new Int3(
                overrideConversion.OffsetX,
                (int)overrideConversion.OffsetY + (isManiaPlanet ? overrideConversion.Offset2Y : overrideConversion.Offset1Y),
                overrideConversion.OffsetZ);
        }

        var blockModel = conversion.GetPropertyDefault(block, x => x.Block);
        if (blockModel is not null && !string.IsNullOrWhiteSpace(blockModel.Name))
        {
            PlaceBlockFromItemModel(block, direction, blockModel, blockCoordSize, conversion.Skin);
        }

        var blockModels = conversion.GetPropertyDefault(block, x => x.Blocks);
        if (blockModels is not null)
        {
            foreach (var b in blockModels)
            {
                PlaceBlockFromItemModel(block, direction, b, blockCoordSize, conversion.Skin);
            }
        }

        var conversionModels = conversion.GetPropertyDefault(block, x => x.Conversions);
        if (conversionModels is not null)
        {
            foreach (var c in conversionModels)
            {
                if (c is not null && !string.IsNullOrWhiteSpace(c.Name))
                {
                    PlaceBlockConversion(block, ConversionSet.Blocks[c.Name],
                        overrideName: c.Name,
                        overrideConversion: c,
                        overrideVariant: overrideConversion?.Variant);
                }
            }
        }

        var newCoord = block.Coord + TotalOffset + direction switch
        {
            Direction.North => (0, 0, 0),
            Direction.East => (blockCoordSize.Z, 0, 0),
            Direction.South => (blockCoordSize.X, 0, blockCoordSize.Z),
            Direction.West => (0, 0, blockCoordSize.X),
            _ => throw new ArgumentException("Invalid block direction")
        } + offset;

        if (conversion.ZoneHeight.HasValue)
        {
            newCoord -= (0, isManiaPlanet ? 1 : conversion.ZoneHeight.Value, 0);
        }

        var rot = -(int)direction;
        var rotRadians = rot * MathF.PI / 2;

        if (overrideConversion is null || !overrideConversion.NoItems)
        {
            var itemModel = conversion.GetPropertyDefault(block, x => x.Item);
            if (itemModel is not null)
            {
                PlaceItemFromItemModel(itemModel, variant, block.Coord, direction, blockCoordSize, block.IsGround);
            }

            var itemModels = conversion.GetPropertyDefault(block, x => x.Items);
            if (itemModels is not null)
            {
                foreach (var item in itemModels)
                {
                    PlaceItemFromItemModel(item, variant, block.Coord, direction, blockCoordSize, block.IsGround);
                }
            }
        }

        var conversionVariantsByBlock = conversion.GetPropertyDefault(block, x => x.ConversionVariantsByBlock);
        var variantModel = default(ManualConversionVariantModel);
        var disableTerrainModifier = false;

        if (conversionVariantsByBlock?.Count > 0)
        {
            variantModel = blocksPerCoord[block.Coord]
                .Select(b => conversionVariantsByBlock.GetValueOrDefault(b.Name)?.GetValueOrDefault(b.Variant))
                .FirstOrDefault(variant => variant is not null);

            if (variantModel is not null)
            {
                disableTerrainModifier = true; // temporary
            }
        }

        variantModel ??= conversion.GetPropertyDefault(block, x => x.ConversionVariants)?.GetValueOrDefault(variant);
        if (variantModel is not null)
        {
            if (variantModel.Item is not null)
            {
                PlaceItemFromItemModel(variantModel.Item, variant, block.Coord, direction, blockCoordSize, block.IsGround);
            }

            if (variantModel.Items is not null)
            {
                foreach (var item in variantModel.Items)
                {
                    PlaceItemFromItemModel(item, variant, block.Coord, direction, blockCoordSize, block.IsGround);
                }
            }

            if (variantModel.Block is not null)
            {
                PlaceBlockFromItemModel(block, direction, variantModel.Block, blockCoordSize, conversion.Skin);
            }

            if (variantModel.SubVariants?.TryGetValue(subVariant, out var subVariantModel) == true)
            {
                variantModel = subVariantModel;

                if (variantModel.Item is not null)
                {
                    PlaceItemFromItemModel(variantModel.Item, variant, block.Coord, direction, blockCoordSize, block.IsGround);
                }

                if (variantModel.Items is not null)
                {
                    foreach (var item in variantModel.Items)
                    {
                        PlaceItemFromItemModel(item, variant, block.Coord, direction, blockCoordSize, block.IsGround);
                    }
                }

                if (variantModel.Block is not null)
                {
                    PlaceBlockFromItemModel(block, direction, variantModel.Block, blockCoordSize, conversion.Skin);
                }
            }

            if (variantModel.Variant.HasValue)
            {
                variant = variantModel.Variant.Value;
            }

            if (variantModel.SubVariant.HasValue)
            {
                subVariant = variantModel.SubVariant.Value;
            }
        }

        var conversionModel = conversion.GetPropertyDefault(block, x => x.Conversion);
        if (conversionModel is not null && !string.IsNullOrWhiteSpace(conversionModel.Name))
        {
            PlaceBlockConversion(block, ConversionSet.Blocks[conversionModel.Name],
                overrideName: conversionModel.Name,
                overrideConversion: conversionModel,
                overrideVariant: variantModel?.Variant ?? overrideConversion?.Variant,
                overrideSubVariant: variantModel?.SubVariant ?? overrideConversion?.SubVariant);
        }

        var modifierType = (block.IsGround && !conversion.ForceAirItem) || conversion.ForceGroundItem ? "Ground" : "Air";

        var itemName = $"{modifierType}_{variant}_{subVariant}.Item.Gbx";
        var itemPath = Path.Combine(dirPath, itemName);

        var modernized = overrideConversion?.Modernized ?? conversion.Modernized;

        var noItem = overrideConversion?.NoItem ?? variantModel?.NoItem ?? conversion.GetPropertyDefault(block, x => x.NoItem);
        
        if (overrideConversion?.NoItems == true)
        {
            noItem = true;
        }

        if (!noItem)
        {
            logger.LogDebug("-- Placing primary item {ItemName} at adjusted coord {NewCoord}, {Direction} ...", itemName, newCoord, direction);

            var item = customContentManager.PlaceItem(itemPath, newCoord * BlockSize, (rotRadians, 0, 0), modernized: modernized, lightmapQuality: LightmapQuality.Highest, lightProperties: conversion.Lights);
            item.Color = conversion.Color ?? DifficultyColor.Default;

            if (conversion.Skin is not null)
            {
                if (conversion.Skin.RemapToColor?.Count > 0)
                {
                    var skinPath = block.Skin?.PackDesc?.FilePath;
                    item.Color = !string.IsNullOrEmpty(skinPath) && conversion.Skin.RemapToColor.TryGetValue(skinPath, out var color)
                        ? color
                        : conversion.Skin.FallbackColor;
                }
            }
        }

        // Place terrain-modifiable pieces
        if ((block.IsGround || conversion.AirModifiable) && conversion.Modifiable.GetValueOrDefault() && (conversion.NotModifiable is null || !conversion.NotModifiable.Contains((variant, subVariant))))
        {
            var noTerrainModifier = conversion.GetPropertyDefault(block, x => x.NoTerrainModifier);

            // temporary
            if (disableTerrainModifier)
            {
                noTerrainModifier = true;
            }

            if (!noTerrainModifier)
            {
                var placeDefaultZone = variantModel?.PlaceDefaultZone ?? conversion.GetPropertyDefault(block, x => x.PlaceDefaultZone);

                logger.LogDebug("-- Placing terrain modifier item ...");

                // This will work fine in 99% of cases, BUT
                // in TMF, when you place fabric on NON-0x0x0 rotated unit,
                // the fabric is not applied on the whole block
                // however with certain action combinations like undoing, this stays.
                // in TMF, on map reload, non 0x0x0 fabric is consistently not applied
                // so fabric is not yet quite exact here
                // So far, this system is needed ONLY for Stadium
                // 2025-06-30 -- one solution to this problem is to avoid getting modified by itself (the block that does the modification). this helps it a lot but is still not perfect
                //            -- it should be checked against real block units and not just 0x0x0!!
                var modifier = default(string);
                if (Environment == "Stadium")
                {
                    var units = conversion.GetProperty(block, x => x.Units, fallback: true)?.Where(x => x.Y == 0) ?? [];
                    foreach (var unit in units)
                    {
                        var alignedCoord = block.Coord + unit + block.Direction switch
                        {
                            Direction.East => (blockCoordSize.Z - 1, 0, 0),
                            Direction.South => (blockCoordSize.X - 1, 0, blockCoordSize.Z - 1),
                            Direction.West => (0, 0, blockCoordSize.X - 1),
                            _ => (0, 0, 0)
                        };

                        if (terrainModifierZones.TryGetValue(alignedCoord with { Y = 0 }, out modifier))
                        {
                            break;
                        }
                    }
                }
                else
                {
                    modifier = terrainModifierZones.GetValueOrDefault(block.Coord with { Y = 0 });
                }

                // if placeDefaultZone, place 1x1 pieces on all ground units at Y 0
                // otherwise just place item
                if (placeDefaultZone)
                {
                    string terrainItemPath;
                    if (modifier == "Fabric")
                    {
                        terrainItemPath = Path.Combine("Misc", "Fabric", "Ground.Item.Gbx");
                    }
                    else
                    {
                        var zoneBlockName = modifier is null
                            ? ConversionSet.DefaultZoneBlock ?? throw new InvalidOperationException("DefaultZoneBlock not set")
                            : reverseBlockTerrainModifiers.GetValueOrDefault(modifier, ConversionSet.DefaultZoneBlock ?? throw new InvalidOperationException("DefaultZoneBlock not set"));

                        var zoneConversion = ConversionSet.Blocks[zoneBlockName] ?? throw new InvalidOperationException("Zone block is null in conversion set");

                        var zoneDirPath = string.IsNullOrWhiteSpace(zoneConversion.PageName)
                            ? blockName
                            : Path.Combine(zoneConversion.PageName, zoneBlockName);
                        terrainItemPath = Path.Combine(zoneDirPath, "Ground_0_0.Item.Gbx");
                    }

                    var units = conversion.GetProperty(block, x => x.Units, fallback: true)?.ToArray() ?? [(0, 0, 0)];

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
                        logger.LogDebug("---- Placing default zone modifier item {ZoneBlockName} at adjusted coord {NewCoord} ...", terrainItemPath, newCoord + unit);

                        var alignedPos = block.Coord + unit - min + TotalOffset + (0, (conversion.ZoneHeight is null || isManiaPlanet ? -1 : 0) + offset.Y, 0);
                        customContentManager.PlaceItem(terrainItemPath, alignedPos * BlockSize, (0, 0, 0), modernized: modernized);
                    }
                }
                else
                {
                    var terrainItemName = modifier is null
                        ? $"GroundDefault_{variant}_{subVariant}.Item.Gbx"
                        : $"{modifier}_{variant}_{subVariant}.Item.Gbx";

                    logger.LogDebug("---- Placing terrain modifier item {TerrainItemName} ...", terrainItemName);

                    customContentManager.PlaceItem(Path.Combine(dirPath, terrainItemName), newCoord * BlockSize, (rotRadians, 0, 0), modernized: modernized);
                }
            }
        }

        logger.LogDebug("-- Completed {BlockName} placement.", blockName);
    }

    private void PlaceBlockFromItemModel(CGameCtnBlock block, Direction direction, ManualConversionBlockModel blockModel, Int3 blockSizeCoord, ManualConversionSkinModel? skinConversion)
    {
        if (string.IsNullOrWhiteSpace(blockModel.Name))
        {
            return;
        }

        var adjustedOffset = blockModel.IsRelativeOffset ? direction switch
        {
            Direction.North => (blockModel.OffsetX, 0, blockModel.OffsetZ),
            Direction.East => (blockSizeCoord.Z - blockModel.OffsetZ - 1, 0, blockModel.OffsetX),
            Direction.South => (blockSizeCoord.X - blockModel.OffsetX - 1, 0, blockSizeCoord.Z - blockModel.OffsetZ - 1),
            Direction.West => (blockModel.OffsetZ, 0, blockSizeCoord.X - blockModel.OffsetX - 1),
            _ => throw new ArgumentException("Invalid block direction")
        } : new Int3(0, 0, 0);

        var offsetY = blockModel.OffsetY + (isManiaPlanet ? blockModel.Offset2Y : blockModel.Offset1Y);
        var coord = block.Coord + TotalOffset + adjustedOffset + (0, 8 + (int)offsetY, 0);
        var dir = (Direction)(((int)direction + blockModel.Dir) % 4);

        logger.LogDebug("-- Placing TM2020 block {BlockName} at adjusted coord {NewCoord}, {Direction} ...", blockModel.Name, coord, dir);

        var additionalBlock = mapOut.PlaceBlock(
            blockModel.Name,
            coord,
            dir,
            blockModel.IsGround,
            (byte)blockModel.Variant.GetValueOrDefault(0));
        additionalBlock.Bit21 = blockModel.Bit21;

        if (blockModel.Name.Contains("Checkpoint"))
        {
            logger.LogInformation("Setting checkpoint properties ...");
            additionalBlock.WaypointSpecialProperty = new CGameWaypointSpecialProperty
            {
                Tag = "Checkpoint"
            };
            additionalBlock.WaypointSpecialProperty.CreateChunk<CGameWaypointSpecialProperty.Chunk2E009000>().Version = 2;
            additionalBlock.WaypointSpecialProperty.CreateChunk<CGameWaypointSpecialProperty.Chunk2E009001>();
        }

        if (blockModel.RotX != 0 || blockModel.RotY != 0 || blockModel.RotZ != 0)
        {
            additionalBlock.IsFree = true;
            additionalBlock.YawPitchRoll = (
                AdditionalMath.ToRadians(blockModel.RotX), 
                AdditionalMath.ToRadians(blockModel.RotY),
                AdditionalMath.ToRadians(blockModel.RotZ));
            additionalBlock.AbsolutePositionInMap = (block.Coord + TotalOffset + new Vec3(0, offsetY, 0)) * BlockSize;
        }

        if (skinConversion is not null)
        {
            additionalBlock.Skin = ConvertSkin(block);
        }
    }

    private CGameCtnBlockSkin? ConvertSkin(CGameCtnBlock block)
    {
        if (string.IsNullOrEmpty(block.Skin?.PackDesc?.FilePath))
        {
            return null;
        }

        if (!block.Skin.PackDesc.FilePath.StartsWith("Skins\\"))
        {
            return null;
        }

        var filePathToMatch = block.Skin.PackDesc.FilePath.Substring(@"Skins\".Length);

        CGameCtnBlockSkin skin;

        if (skinMapping.TryGetValue(filePathToMatch, out var skinInfo))
        {
            skin = new CGameCtnBlockSkin
            {
                PackDesc = new PackDesc
                {
                    FilePath = $@"Skins\{(string.IsNullOrEmpty(skinInfo.Primary) ? filePathToMatch : skinInfo.Primary)}",
                    LocatorUrl = skinInfo.PrimaryLocatorUrl
                },
                ForegroundPackDesc = new PackDesc
                {
                    FilePath = string.IsNullOrEmpty(skinInfo.Foreground) ? "" : $@"Skins\{skinInfo.Foreground}",
                    LocatorUrl = skinInfo.ForegroundLocatorUrl
                },
            };
        }
        else if (!string.IsNullOrWhiteSpace(block.Skin.PackDesc.LocatorUrl))
        {
            skin = new CGameCtnBlockSkin
            {
                PackDesc = new PackDesc
                {
                    FilePath = block.Skin.PackDesc.FilePath,
                    Checksum = block.Skin.PackDesc.Checksum,
                    LocatorUrl = block.Skin.PackDesc.LocatorUrl
                }
            };
        }
        else
        {
            return null;
        }

        skin.Text = "!4";
        skin.CreateChunk<CGameCtnBlockSkin.Chunk03059002>();
        skin.CreateChunk<CGameCtnBlockSkin.Chunk03059003>();

        return skin;
    }

    private void PlaceItemFromItemModel(
        ManualConversionItemModel itemModel,
        int variant,
        Int3 coord,
        Direction direction,
        Int3 blockCoordSize,
        bool isGround)
    {
        if (string.IsNullOrWhiteSpace(itemModel.Name))
        {
            return;
        }

        if (itemModel.OnlyGround && !isGround)
        {
            return;
        }

        if (itemModel.OnlyAir && isGround)
        {
            return;
        }

        var dir = ((int)direction + itemModel.Dir) % 4;

        var c = coord + TotalOffset + dir switch
        {
            0 => (0, 0, 0),
            1 => (blockCoordSize.Z, 0, 0),
            2 => (blockCoordSize.X, 0, blockCoordSize.Z),
            3 => (0, 0, blockCoordSize.X),
            _ => throw new ArgumentException("Invalid block direction")
        };

        var offsetY = itemModel.OffsetY + (isManiaPlanet ? itemModel.Offset2Y : itemModel.Offset1Y);

        var pos = c * BlockSize + dir switch
        {
            0 => new Vec3(itemModel.OffsetX, offsetY, itemModel.OffsetZ),
            1 => new Vec3(-itemModel.OffsetZ, offsetY, itemModel.OffsetX),
            2 => new Vec3(-itemModel.OffsetX, offsetY, -itemModel.OffsetZ),
            3 => new Vec3(itemModel.OffsetZ, offsetY, -itemModel.OffsetX),
            _ => throw new ArgumentException("Invalid block direction")
        };

        var rotXRadians = -dir * MathF.PI / 2;
        var rot = new Vec3(rotXRadians + AdditionalMath.ToRadians(itemModel.RotX), AdditionalMath.ToRadians(itemModel.RotY), AdditionalMath.ToRadians(itemModel.RotZ));
        var isOfficial = !itemModel.Name.Contains('/') && !itemModel.Name.Contains('\\');

        logger.LogDebug("-- Placing item {ItemName} (official: {IsOfficial}) at position {Pos}, {Rot} ...", itemModel.Name, isOfficial, pos, rot);

        if (isOfficial)
        {
            var item = mapOut.PlaceAnchoredObject(new(itemModel.Name, 26, "Nadeo"), pos, rot, itemModel.Pivot);
            item.BlockUnitCoord = new Byte3((byte)(pos.X / 32), (byte)(pos.Y / 8), (byte)(pos.Z / 32));
        }
        else
        {
            var name = itemModel.Name
                .Replace("{Variant}", variant.ToString())
                .Replace("{Modifier}", isGround ? "Ground" : "Air");
            customContentManager.PlaceItem(name, pos, rot, itemModel.Pivot, modernized: itemModel.Modernized, technology: itemModel.AlwaysStaticObject ? "SO" : null);
        }
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

        logger.LogInformation("-- Placing clips...");

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

            if (clipConversion is null)
            {
                logger.LogWarning("Clip {ClipName} is null in conversion set.", clip.Name);
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

            logger.LogInformation("---- Placing clip {ClipName} at adjusted coord {NewCoord}, {Direction} ...", clip.Name, clipBlock.Coord, dir);

            PlaceBlockConversion(clipBlock, clipConversion, overrideName: clip.Name, overrideDirection: dir, overrideVariant: 0);
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

            var modifier = terrainModifierZones.GetValueOrDefault(block.Coord with { Y = 0 });

            for (int i = 0; i < 4; i++)
            {
                var dir = (Direction)i;

                if (dirs.Contains(dir))
                {
                    continue;
                }

                // TODO condition Fabric in Stadium here

                logger.LogInformation("Placing ground clip filler {BlockName}, {Direction} ...", block.Name, dir);

                if (modifier == "Fabric")
                {
                    var terrainItemPath = Path.Combine("Misc", "Fabric", "Clip.Item.Gbx");
                    var newCoord = block.Coord + TotalOffset + dir switch
                    {
                        Direction.North => (0, 0, 0),
                        Direction.East => (1, 0, 0),
                        Direction.South => (1, 0, 1),
                        Direction.West => (0, 0, 1),
                        _ => throw new ArgumentException("Invalid block direction")
                    };
                    var rotRadians = -i * MathF.PI / 2;

                    customContentManager.PlaceItem(terrainItemPath, newCoord * BlockSize, (rotRadians, 0, 0));
                }
                else
                {
                    PlaceBlockConversion(block, ConversionSet.Blocks[block.Name] ?? throw new InvalidOperationException($"Conversion filler ({block.Name}) is null in conversion set"), overrideDirection: dir);
                }
            }
        }

        logger.LogInformation("Placed ground clip fillers.");
    }
}
