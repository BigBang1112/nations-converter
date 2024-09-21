using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using System.Text.RegularExpressions;

namespace NationsConverter;

internal sealed partial class DecorationConverter
{
    private readonly CGameCtnChallenge map;
    private readonly CGameCtnChallenge convertedMap;
    private readonly ManualConversionSetModel conversionSet;
    private readonly NationsConverterConfig config;
    private readonly CustomContentManager customContentManager;
    private readonly ILogger logger;

    [GeneratedRegex(@"(Sunrise|Day|Sunset|Night)")]
    private static partial Regex MoodRegex();

    public DecorationConverter(
        CGameCtnChallenge map, 
        CGameCtnChallenge convertedMap,
        ManualConversionSetModel conversionSet,
        NationsConverterConfig config,
        CustomContentManager customContentManager,
        ILogger logger)
    {
        this.map = map;
        this.convertedMap = convertedMap;
        this.conversionSet = conversionSet;
        this.config = config;
        this.customContentManager = customContentManager;
        this.logger = logger;
    }

    public void Convert()
    {
        var mood = MoodRegex().Match(map.Decoration.Id).Value;

        var mapBase = config.IncludeDecoration
            ? "NoStadium48x48"
            : "48x48Screen155";

        if (conversionSet.Environment == "Island")
        {
            convertedMap.Size = new(90, 36, 90);
        }

        convertedMap.Decoration = new($"{mapBase}{mood}", 26, "Nadeo");

        logger.LogInformation("Decoration: {Name}", convertedMap.Decoration.Id);
        logger.LogInformation("Size: {Size}", convertedMap.Size);

        if (config.IncludeDecoration)
        {
            var sizeStr = $"{map.Size.X}x{map.Size.Y}x{map.Size.Z}";
            var subCategory = "Modless";
            var dirPath = Path.Combine("NC2", "Solid", subCategory, "MM_Collision", conversionSet.Environment, "Decorations");
            var itemPath = Path.Combine(dirPath, $"{sizeStr}.Item.Gbx");

            var yOffset = conversionSet.DecorationYOffset;
            if (conversionSet.Decorations.TryGetValue(sizeStr, out var deco))
            {
                yOffset += deco.YOffset;
            }

            customContentManager.PlaceItem(itemPath, (0, yOffset, 0), (0, 0, 0));

            logger.LogInformation("Placed decoration item ({Size}).", sizeStr);
        }
    }
}
