using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverterShared.Models;

namespace NationsConverter;

internal sealed class PlaceTransformationConverter : BlockConverter
{
    private readonly CGameCtnChallenge convertedMap;
    private readonly ILogger logger;

    public PlaceTransformationConverter(
        CGameCtnChallenge map,
        CGameCtnChallenge convertedMap,
        NationsConverterConfig config,
        ILogger logger) : base(map, config, logger)
    {
        this.convertedMap = convertedMap;
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

    protected override void ConvertBlock(CGameCtnBlock block, ConversionModel conversion)
    {
        if (conversion.Waypoint is not WaypointType.Start and not WaypointType.StartFinish)
        {
            return;
        }

        var blockCoordSize = conversion.GetProperty(block, x => x.Size);
        
        var pos = block.Direction switch
        {
            Direction.East => block.Coord + (blockCoordSize.Z, 0, 0),
            Direction.South => block.Coord + (blockCoordSize.X, 0, blockCoordSize.Z),
            Direction.West => block.Coord + (0, 0, blockCoordSize.X),
            _ => block.Coord
        };

        var rotRadians = -(int)block.Direction * MathF.PI / 2;
        
        var spawnPos = conversion.GetProperty(block, x => x.SpawnPos);

        logger.LogInformation("Placing transformation gate at {Pos} with rotation {Dir}...", pos, block.Direction);

        convertedMap.PlaceAnchoredObject(
            new($"GateGameplay{Environment}4m", 26, "Nadeo"),
                pos * BlockSize + spawnPos, (rotRadians, 0, 0));
    }
}
