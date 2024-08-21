using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using NationsConverterShared.Models;
using System.Text.RegularExpressions;

namespace NationsConverter;

internal sealed partial class DecorationConverter
{
    private readonly CGameCtnChallenge map;
    private readonly CGameCtnChallenge convertedMap;
    private readonly NationsConverterConfig config;
    private readonly ILogger logger;

    private readonly string environment;
    private readonly ConversionSetModel conversionSet;

    [GeneratedRegex(@"(Sunrise|Day|Sunset|Night)")]
    private static partial Regex MoodRegex();

    public DecorationConverter(CGameCtnChallenge map, CGameCtnChallenge convertedMap, NationsConverterConfig config, ILogger logger)
    {
        this.map = map;
        this.convertedMap = convertedMap;
        this.config = config;
        this.logger = logger;

        environment = map.GetEnvironment() switch
        {
            "Alpine" => "Snow",
            "Speed" => "Desert",
            _ => map.GetEnvironment()
        };

        conversionSet = environment switch
        {
            "Snow" => config.Snow,
            "Rally" => config.Rally,
            "Desert" => config.Desert,
            "Island" => config.Island,
            "Bay" => config.Bay,
            "Coast" => config.Coast,
            "Stadium" => config.Stadium, // should not be always Solid category
            _ => throw new ArgumentException("Environment not supported")
        };
    }

    public void Convert()
    {
        var mood = MoodRegex().Match(map.Decoration.Id).Value;

        var mapBase = config.IncludeDecoration
            ? "NoStadium48x48"
            : "48x48Screen155";

        convertedMap.Decoration = new($"{mapBase}{mood}", 26, "Nadeo");

        if (config.IncludeDecoration)
        {
            var subCategory = "Modless";
            var dirPath = Path.Combine("NC2", "Solid", subCategory, "MM_Collision", environment, "Decorations");
            var itemPath = Path.Combine(dirPath, $"{map.Size.X}x{map.Size.Y}x{map.Size.Z}.Item.Gbx");

            convertedMap.PlaceAnchoredObject(
                new(itemPath.Replace('/', '\\'), 26, "akPfIM0aSzuHuaaDWptBbQ"),
                    (0, 0, 0), (0, 0, 0));
        }
    }
}
