using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using NationsConverterShared.Models;

namespace NationsConverter.Converters;

internal sealed class PlaceTransformationConverter : BlockConverterBase
{
    private readonly CustomContentManager customContentManager;
    private readonly ILogger logger;

    public PlaceTransformationConverter(
        CGameCtnChallenge mapIn,
        ManualConversionSetModel conversionSet,
        CustomContentManager customContentManager,
        ILogger logger) : base(mapIn, conversionSet)
    {
        this.customContentManager = customContentManager;
        this.logger = logger;
    }

    public override void Convert()
    {
        if (Environment is not "Snow" and not "Rally" and not "Desert")
        {
            return;
        }

        // Nadeo PLS fix
        // convertedMap.PlayerModel = new($"Car{Environment}", 10003, "Nadeo");

        base.Convert();
    }

    protected override void ConvertBlock(CGameCtnBlock block, ManualConversionModel conversion)
    {
        if (conversion.Waypoint is not WaypointType.Start and not WaypointType.StartFinish)
        {
            return;
        }

        var blockCoordSize = conversion.GetProperty(block, x => x.Size);

        var spawnPos = conversion.GetProperty(block, x => x.SpawnPos);

        var pos = block.Direction switch
        {
            Direction.East => (block.Coord + (blockCoordSize.Z, 0, 0)) * BlockSize + new Vec3(-spawnPos.Z, spawnPos.Y, spawnPos.X),
            Direction.South => (block.Coord + (blockCoordSize.X, 0, blockCoordSize.Z)) * BlockSize + new Vec3(-spawnPos.X, spawnPos.Y, -spawnPos.Z),
            Direction.West => (block.Coord + (0, 0, blockCoordSize.X)) * BlockSize + new Vec3(spawnPos.Z, spawnPos.Y, -spawnPos.X),
            _ => block.Coord * BlockSize + spawnPos
        };

        var rotRadians = -(int)block.Direction * MathF.PI / 2;

        logger.LogInformation("Placing transformation gate at {Pos} with rotation {Dir}...", pos, block.Direction);

        var gateBlock = customContentManager.PlaceBlock($@"Misc\{Environment}HiddenGate", pos, (rotRadians, 0, 0));
        gateBlock.Bit21 = true;

        // Placing official transformation can be optional
        /*convertedMap.PlaceAnchoredObject(
            new($"GateGameplay{Environment}4m", 26, "Nadeo"),
                pos, (rotRadians, 0, 0));*/
    }
}
