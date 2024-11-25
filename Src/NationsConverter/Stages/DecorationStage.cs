using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using System.Text.RegularExpressions;

namespace NationsConverter.Stages;

internal sealed partial class DecorationStage : EnvironmentStageBase
{
    private readonly CGameCtnChallenge mapIn;
    private readonly CGameCtnChallenge mapOut;
    private readonly ManualConversionSetModel conversionSet;
    private readonly NationsConverterConfig config;
    private readonly CustomContentManager customContentManager;
    private readonly ILogger logger;

    [GeneratedRegex(@"(Sunrise|Day|Sunset|Night)", RegexOptions.IgnoreCase)]
    private static partial Regex MoodRegex();

    public DecorationStage(
        CGameCtnChallenge mapIn,
        CGameCtnChallenge mapOut,
        ManualConversionSetModel conversionSet,
        NationsConverterConfig config,
        CustomContentManager customContentManager,
        ILogger logger) : base(mapIn)
    {
        this.mapIn = mapIn;
        this.mapOut = mapOut;
        this.conversionSet = conversionSet;
        this.config = config;
        this.customContentManager = customContentManager;
        this.logger = logger;
    }

    public void Convert()
    {
        var mood = MoodRegex().Match(mapIn.Decoration.Id).Value;

        if (string.IsNullOrEmpty(mood))
        {
            mood = "Day";
        }

        mood = string.Concat(mood[0].ToString().ToUpper(), mood.AsSpan(1));

        var mapBase = config.IncludeDecoration
            ? "NoStadium48x48"
            : "48x48Screen155";

        if (config.IncludeDecoration)
        {
            var blockSize = mapIn.Collection.GetValueOrDefault().GetBlockSize();
            mapOut.Size = new((int)(mapIn.Size.X * (blockSize.X / 32f)), 40, (int)(mapIn.Size.Z * (blockSize.Z / 32f)));
        }

        mapOut.Decoration = new($"{mapBase}{mood}", 26, "Nadeo");

        logger.LogInformation("Decoration: {Name}", mapOut.Decoration.Id);
        logger.LogInformation("Size: {Size}", mapOut.Size);

        if (config.IncludeDecoration)
        {
            var sizeStr = $"{mapIn.Size.X}x{mapIn.Size.Y}x{mapIn.Size.Z}";
            var itemPath = Path.Combine("Decorations", $"{sizeStr}.Item.Gbx");

            var yOffset = conversionSet.DecorationYOffset;
            if (conversionSet.Decorations.TryGetValue(sizeStr, out var deco))
            {
                yOffset += deco.YOffset;
            }

            customContentManager.PlaceItem(itemPath, (0, yOffset, 0), (0, 0, 0), lightmapQuality: LightmapQuality.Lowest);

            logger.LogInformation("Placed decoration item ({Size}).", sizeStr);
        }

        var size = config.IncludeDecoration ? mapOut.Size : mapIn.Size;
        var offset = new Int3((mapOut.Size.X - mapIn.Size.X) / 2, 0, (mapOut.Size.Z - mapIn.Size.Z) / 2);

        for (var x = 0; x < size.X; x++)
        {
            for (var z = 0; z < size.Z; z++)
            {
                var block = customContentManager.PlaceBlock(@"Misc\Void", (x, 9, z) + offset, Direction.North, isGround: true);
                block.LightmapQuality = LightmapQuality.Lowest;
            }
        }

        logger.LogInformation("Placed {SizeX}x{SizeZ} void.", size.X, size.Z);
    }
}
