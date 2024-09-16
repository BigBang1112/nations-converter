using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace NationsConverter;

internal sealed partial class DecorationConverter
{
    private readonly CGameCtnChallenge map;
    private readonly CGameCtnChallenge convertedMap;
    private readonly NationsConverterConfig config;
    private readonly CustomContentManager customContentManager;
    private readonly string environment;

    [GeneratedRegex(@"(Sunrise|Day|Sunset|Night)")]
    private static partial Regex MoodRegex();

    public DecorationConverter(
        CGameCtnChallenge map, 
        CGameCtnChallenge convertedMap, 
        NationsConverterConfig config, 
        CustomContentManager customContentManager,
        ILogger logger)
    {
        this.map = map;
        this.convertedMap = convertedMap;
        this.config = config;
        this.customContentManager = customContentManager;

        environment = map.GetEnvironment() switch
        {
            "Alpine" => "Snow",
            "Speed" => "Desert",
            _ => map.GetEnvironment()
        };
    }

    public void Convert()
    {
        var mood = MoodRegex().Match(map.Decoration.Id).Value;

        var mapBase = config.IncludeDecoration
            ? "NoStadium48x48"
            : "48x48Screen155";

        if (environment == "Island")
        {
            convertedMap.Size = new(90, 36, 90);
        }

        convertedMap.Decoration = new($"{mapBase}{mood}", 26, "Nadeo");

        if (config.IncludeDecoration)
        {
            var subCategory = "Modless";
            var dirPath = Path.Combine("NC2", "Solid", subCategory, "MM_Collision", environment, "Decorations");
            var itemPath = Path.Combine(dirPath, $"{map.Size.X}x{map.Size.Y}x{map.Size.Z}.Item.Gbx");

            customContentManager.PlaceItem(itemPath, (0, 0, 0), (0, 0, 0));
        }
    }
}
