using GBX.NET;
using GBX.NET.Engines.Game;
using NationsConverter.Models;

namespace NationsConverter.Stages;

internal sealed class PylonStage : BlockStageBase
{
    private record PylonDefinition(string PylonName, ManualConversionModel Pylon, int Y);
    private readonly record struct PylonLimit(int Height, int PylonMask);
    private readonly record struct PylonPlacement(int Height, int Y);

    private readonly CGameCtnChallenge mapIn;
    private readonly CustomContentManager customContentManager;

    private readonly int baseHeight;

    /// <summary>
    /// Int3 is with Y = 0
    /// </summary>
    private readonly Dictionary<Int3, PylonDefinition> groundPositionsWithPylons = [];
    /// <summary>
    /// Int3 is with Y = 0
    /// </summary>
    private readonly HashSet<Int3> groundPositionsWithoutPylons = [];
    /// <summary>
    /// Int3 is with Y = 0
    /// </summary>
    private readonly Dictionary<Int3, PylonLimit> disallowedPylonPlacements = [];
    /// <summary>
    /// Int3 is with Y = 0
    /// </summary>
    private readonly Dictionary<(Int3 Coord, int PylonIndex), PylonPlacement> pylonPlacementHeights = [];

    private readonly HashSet<Int3> placedPylons = [];

    public PylonStage(
        CGameCtnChallenge mapIn,
        CGameCtnChallenge mapOut,
        ManualConversionSetModel conversionSet,
        CustomContentManager customContentManager) : base(mapIn, mapOut, conversionSet)
    {
        this.mapIn = mapIn;
        this.customContentManager = customContentManager;

        baseHeight = (conversionSet.Decorations
            .GetValueOrDefault($"{mapIn.Size.X}x{mapIn.Size.Y}x{mapIn.Size.Z}")?.BaseHeight ?? 0) + 1;
    }

    public override void Convert()
    {
        foreach (var (block, conversion) in ConversionSet.GetBlockConversionPairs(mapIn))
        {
            PopulateGroundPositionsWithAndWithoutPylons(block, conversion);
            PopulateDisallowedPylonPlacements(block, conversion);
        }

        base.Convert();

        /*var dirPath = string.IsNullOrWhiteSpace(conversion.PageName)
            ? blockName
            : Path.Combine(conversion.PageName, blockName);*/

        var pillarOffset = ConversionSet.PillarOffset;

        foreach (var ((coordY0, pylonIndex), placement) in pylonPlacementHeights)
        {
            var pylonDefinition = GetPylonDefinitionOrDefault(coordY0);
            var pylonConversion = pylonDefinition.Pylon;
            var dirPath = string.IsNullOrWhiteSpace(pylonConversion.PageName)
                ? pylonDefinition.PylonName
                : Path.Combine(pylonConversion.PageName, pylonDefinition.PylonName);
            var itemPath = Path.Combine(dirPath, $"Ground_{placement.Height - 1}_0.Item.Gbx");

            var pos = ((coordY0.X, pylonDefinition.Y, coordY0.Z) + CenterOffset) * BlockSize + (BlockSize.X / 2, 0, BlockSize.Z / 2);

            var dir = pylonIndex / 2;
            var itemDir = dir % 2;
            var side = pylonIndex % 2;

            var pylonOffset = pillarOffset - side * pillarOffset * 2;

            pos += (Direction)dir switch
            {
                Direction.North => (pylonOffset, 0, BlockSize.Z / 2),
                Direction.East => (-BlockSize.X / 2, 0, pylonOffset),
                Direction.South => (-pylonOffset, 0, -BlockSize.Z / 2),
                Direction.West => (BlockSize.X / 2, 0, -pylonOffset),
                _ => throw new ArgumentException("Invalid block direction")
            };

            // if the pos matches a pylon that was already placed, skip it
            if (!placedPylons.Add(pos))
            {
                continue;
            }

            var rotRadians = -itemDir * MathF.PI / 2;

            customContentManager.PlaceItem(itemPath, pos, (rotRadians, 0, 0));
        }
    }

    private void PopulateGroundPositionsWithAndWithoutPylons(CGameCtnBlock block, ManualConversionModel conversion)
    {
        if (conversion.Pylon is not null)
        {
            var pylonConversion = ConversionSet.Blocks[conversion.Pylon];
            groundPositionsWithPylons[block.Coord with { Y = 0 }] = new PylonDefinition(conversion.Pylon, pylonConversion, block.Coord.Y + 1);
            return;
        }

        if (conversion.ZoneHeight.HasValue)
        {
            groundPositionsWithoutPylons.Add(block.Coord with { Y = 0 });
        }
    }

    private void PopulateDisallowedPylonPlacements(CGameCtnBlock block, ManualConversionModel conversion)
    {
        if (conversion.ZoneHeight.HasValue)
        {
            return;
        }

        var acceptPylons = conversion.GetPropertyDefault(block, x => x.AcceptPylons);

        if (acceptPylons is null or { Length: 0 })
        {
            return;
        }

        var blockCoordSize = conversion.GetProperty(block, x => x.Size, fallback: true) - (1, 1, 1);
        var units = conversion.GetProperty(block, x => x.Units, fallback: true) ?? [(0, 0, 0)];

        for (var unitIndex = 0; unitIndex < units.Length; unitIndex++)
        {
            var offset = units[unitIndex];
            var alignedOffset = block.Direction switch
            {
                Direction.East => (-offset.Z + blockCoordSize.Z, offset.Y, offset.X),
                Direction.South => (-offset.X + blockCoordSize.X, offset.Y, -offset.Z + blockCoordSize.Z),
                Direction.West => (offset.Z, offset.Y, -offset.X + blockCoordSize.X),
                _ => offset
            };

            var coord = block.Coord + alignedOffset;

            disallowedPylonPlacements[block.Coord with { Y = 0 }] = new PylonLimit(block.Coord.Y, acceptPylons[unitIndex]);
        }
    }

    protected override void ConvertBlock(CGameCtnBlock block, ManualConversionModel conversion)
    {
        var placePylons = conversion.GetPropertyDefault(block, x => x.PlacePylons);

        if (placePylons is null or { Length: 0 })
        {
            return;
        }

        if (conversion.Road is not null)
        {
            var pylon = block.Variant switch
            {
                0 => 255,
                1 => 3,
                2 => 15,
                3 => 51,
                4 => 63,
                5 => 255,
                _ => throw new Exception("Invalid pylon variant")
            };

            TryAddPylons(block.Coord, block.Direction, pylon);

            return;
        }

        var blockCoordSize = conversion.GetProperty(block, x => x.Size, fallback: true) - (1, 1, 1);
        var units = conversion.GetProperty(block, x => x.Units, fallback: true) ?? [(0, 0, 0)];

        for (var unitIndex = 0; unitIndex < units.Length; unitIndex++)
        {
            var offset = units[unitIndex];
            var alignedOffset = block.Direction switch
            {
                Direction.East => (-offset.Z + blockCoordSize.Z, offset.Y, offset.X),
                Direction.South => (-offset.X + blockCoordSize.X, offset.Y, -offset.Z + blockCoordSize.Z),
                Direction.West => (offset.Z, offset.Y, -offset.X + blockCoordSize.X),
                _ => offset
            };

            var coord = block.Coord + alignedOffset;

            var pylon = placePylons[unitIndex];

            TryAddPylons(coord, block.Direction, pylon);
        }
    }

    private void TryAddPylons(Int3 coord, Direction direction, int pylonMask)
    {
        var coordY0 = coord with { Y = 0 };

        if (groundPositionsWithoutPylons.Contains(coordY0))
        {
            return;
        }

        if (disallowedPylonPlacements.TryGetValue(coordY0, out var pylonLimit))
        {
            // consider height of the block
            if ((pylonLimit.PylonMask & pylonMask) != pylonMask)
            {
                return;
            }
        }

        var pylonDefinition = GetPylonDefinitionOrDefault(coordY0);

        for (int pylonIndex = 0; pylonIndex < 8; pylonIndex++)
        {
            if ((pylonMask >> pylonIndex & 1) == 0)
            {
                continue;
            }

            var adjustedPylonIndex = (pylonIndex + (int)direction * 2) % 8;
            var key = (coordY0, adjustedPylonIndex);

            var newHeight = coord.Y - pylonDefinition.Y;

            if (newHeight <= 0)
            {
                continue;
            }

            if (!pylonPlacementHeights.TryGetValue(key, out var placement))
            {
                pylonPlacementHeights[key] = new PylonPlacement(newHeight, 0);
                continue;
            }

            if (newHeight > placement.Height)
            {
                pylonPlacementHeights[key] = new PylonPlacement(newHeight, 0);
            }
        }
    }

    private PylonDefinition GetPylonDefinitionOrDefault(Int3 coordY0)
    {
        if (groundPositionsWithPylons.TryGetValue(coordY0, out var pylonDefinition))
        {
            return pylonDefinition;
        }

        if (ConversionSet.DefaultZoneBlock is null)
        {
            throw new Exception("Default zone block not set");
        }

        var pylonName = ConversionSet.Blocks[ConversionSet.DefaultZoneBlock].Pylon ?? throw new Exception("Default zone block does not have pylon");
        var pylonConversion = ConversionSet.Blocks[pylonName];
        return new PylonDefinition(pylonName, pylonConversion, baseHeight);
    }
}
