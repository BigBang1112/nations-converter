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

    private void PlaceItem(CGameCtnBlock block, ManualConversionModel conversion, string? overrideName = null, Direction? overrideDirection = null)
    {
        // fallbacks should be less permissive in the future
        var blockCoordSize = conversion.GetProperty(block, x => x.Size, fallback: true);
        var maxVariants = conversion.GetProperty(block, x => x.Variants, fallback: true);

        var variant = block.Variant.GetValueOrDefault();
        var subVariant = block.SubVariant.GetValueOrDefault();

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

        var pos = block.Coord + CenterOffset + direction switch
        {
            Direction.North => (0, 0, 0),
            Direction.East => (blockCoordSize.Z, 0, 0),
            Direction.South => (blockCoordSize.X, 0, blockCoordSize.Z),
            Direction.West => (0, 0, blockCoordSize.X),
            _ => throw new ArgumentException("Invalid block direction")
        };

        if (conversion.ZoneHeight.HasValue)
        {
            pos -= (0, conversion.ZoneHeight.Value, 0);
        }

        var rotRadians = -(int)direction * MathF.PI / 2;

        var blockModel = conversion.GetPropertyDefault(block, x => x.Block);
        if (blockModel is not null && !string.IsNullOrWhiteSpace(blockModel.Name))
        {
            mapOut.PlaceBlock(blockModel.Name, block.Coord + CenterOffset + (0, 8, 0), direction);
        }

        var noItem = conversion.GetPropertyDefault(block, x => x.NoItem);
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
                var useBaseTerrainModifier = conversion.GetPropertyDefault(block, x => x.UseBaseTerrainModifier);

                string terrainItemPath;
                if (terrainModifierZones.TryGetValue(block.Coord - (0, 1, 0), out var modifier))
                {
                    // if useBaseTerrainModifier, use different dirPath based on BlockTerrainModifiers
                    terrainItemPath = Path.Combine(dirPath, $"{modifier}_{variant}_{subVariant}.Item.Gbx");
                }
                else
                {
                    // if useBaseTerrainModifier, use different dirPath based on BlockTerrainModifiers
                    terrainItemPath = Path.Combine(dirPath, $"GroundDefault_{variant}_{subVariant}.Item.Gbx");
                }

                customContentManager.PlaceItem(terrainItemPath, pos * BlockSize, (rotRadians, 0, 0));
            }
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

                PlaceItem(block, ConversionSet.Blocks[block.Name], overrideDirection: dir);
            }
        }

        logger.LogInformation("Placed ground clip fillers.");
    }
}
