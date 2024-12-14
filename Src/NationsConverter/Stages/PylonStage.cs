using GBX.NET;
using GBX.NET.Engines.Game;
using NationsConverter.Models;

namespace NationsConverter.Stages;

internal sealed class PylonStage : BlockStageBase
{
    private readonly CustomContentManager customContentManager;

    private readonly int baseHeight;
    private readonly Dictionary<(Int3, int), int> pylons = [];
    private readonly HashSet<Int3> placedPylons = [];

    public PylonStage(
        CGameCtnChallenge mapIn,
        CGameCtnChallenge mapOut,
        ManualConversionSetModel conversionSet,
        CustomContentManager customContentManager) : base(mapIn, mapOut, conversionSet)
    {
        this.customContentManager = customContentManager;

        baseHeight = (conversionSet.Decorations
            .GetValueOrDefault($"{mapIn.Size.X}x{mapIn.Size.Y}x{mapIn.Size.Z}")?.BaseHeight ?? 0) + 1;
    }

    public override void Convert()
    {
        base.Convert();

        if (string.IsNullOrWhiteSpace(ConversionSet.Pylon))
        {
            return;
        }

        var blockName = ConversionSet.Pylon;
        var conversion = ConversionSet.Blocks[blockName];

        var dirPath = string.IsNullOrWhiteSpace(conversion.PageName)
            ? blockName
            : Path.Combine(conversion.PageName, blockName);

        var pillarOffset = ConversionSet.PillarOffset;

        foreach (var ((coord, pylonIndex), height) in pylons)
        {
            var itemPath = Path.Combine(dirPath, $"Ground_{height - 1}_0.Item.Gbx");

            // baseHeight should be adjusted to actual height on the ground
            var pos = ((coord.X, baseHeight, coord.Z) + CenterOffset) * BlockSize + (BlockSize.X / 2, 0, BlockSize.Z / 2);

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

            AddPylons(block.Coord, block.Direction, pylon);

            return;
        }

        var blockCoordSize = conversion.GetProperty(block, x => x.Size, fallback: true) - (1, 1, 1);
        var units = conversion.GetProperty(block, x => x.Units, fallback: true) ?? [(0, 0, 0)];

        for (int unitIndex = 0; unitIndex < units.Length; unitIndex++)
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

            AddPylons(coord, block.Direction, pylon);
        }
    }

    private void AddPylons(Int3 coord, Direction direction, int pylon)
    {
        for (int pylonIndex = 0; pylonIndex < 8; pylonIndex++)
        {
            if ((pylon >> pylonIndex & 1) == 0)
            {
                continue;
            }

            var adjustedPylonIndex = (pylonIndex + (int)direction * 2) % 8;
            var key = (coord with { Y = 0 }, adjustedPylonIndex);

            // baseHeight should be adjusted to actual height on the ground
            var newHeight = coord.Y - baseHeight;

            if (newHeight <= 0)
            {
                continue;
            }

            if (!pylons.TryGetValue(key, out var height))
            {
                pylons[key] = newHeight;
                continue;
            }

            if (newHeight > height)
            {
                pylons[key] = newHeight;
            }
        }
    }
}
