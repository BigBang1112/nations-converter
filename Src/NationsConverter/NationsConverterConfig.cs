using GBX.NET.Tool;
using YamlDotNet.Serialization;

namespace NationsConverter;

public class NationsConverterConfig : Config
{
    [YamlMember(Description = "Force a category, empty will let the converter pick the best option. Currently supported: Solid, Crystal")]
    public string? Category { get; set; }

    [YamlMember(Description = "Force a sub-category, empty will let the converter pick the best option. Currently supported: Modless in Solid; Modernized and Classic in Crystal")]
    public string? SubCategory { get; set; }

    [YamlMember(Description = "Randomization seed (currently used only for unique embedding folders). Non-numeric strings will be converted to numeric seed.")]
    public string? Seed { get; set; }

    [YamlMember(Description = "Adds the Nations Converter 2 + current stage items in the map's top left corner.")]
    public bool IncludeMediaTracker { get; set; } = true;

    [YamlMember(Description = "Adds decoration item and resizes the map accordingly. Currently does not affect Stadium maps.")]
    public bool IncludeDecoration { get; set; } = true;

    [YamlMember(Description = "Adds the Nations Converter 2 + current stage items in the map's top left corner.")]
    public bool IncludeMapWatermark { get; set; } = true;

    [YamlMember(Description = "Places invisible transformation gate on the start block.")]
    public bool PlaceTransformationGate { get; set; } = true;

    [YamlMember(Description = "Applies the default car from the map to the converted map. This won't make a difference without Editor++ installed.")]
    public bool ApplyDefaultCar { get; set; }

    [YamlMember(Description = "Keeps the map validated with its medal times. PLEASE use only if you're certain the map is gonna be possible to finish.")]
    public bool KeepMedalTimes { get; set; }

    [YamlMember(Description = "Uses the new wood physics added in November 2023.")]
    public bool UseNewWood { get; set; }

    [YamlMember(Description = "Currently unused.")]
    public string[]? UserDataPackPriority { get; set; }

    [YamlMember(Description = "Copies the items to your Items directory. Works only if UserDataFolder is set. Set this to true if you want to edit the item meshes after conversion.")]
    public bool CopyItems { get; set; }

    [YamlMember(Description = "Instead of using 'NC2' as the main folder for embedded items in the map, the folder will be named as 'NC2_{Seed}'. This will avoid item name collision issues, so disable this only if you know what you're doing.")]
    public bool UniqueEmbeddedFolder { get; set; } = true;
    
    [YamlMember(Description = "Optional path to the Trackmania user data folder, usually in your Documents folder. This won't affect output location of the map itself.")]
    public string? UserDataFolder { get; set; }
    
    [YamlMember(Description = "Currently unused.")]
    public string? HttpHost { get; set; } = "nc.gbx.tools";

    [YamlMember(Description = "Apply music to the map per environment according to 'Music' option.")]
    public bool IncludeMusic { get; set; } = true;

    [YamlMember(Description = "Pairs of environment and music download URLs.")]
    public Dictionary<string, string> Music { get; set; } = new()
    {
        ["Snow"] = "https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/Snow_(RealnestBootleg).mux",
        ["Rally"] = "https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/Rally_(RealnestBootleg).mux",
        ["Desert"] = "https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/Desert_(Realnest&ThaumicTomBootleg).mux",
        ["Island"] = "https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/Island_(RealnestBootleg).mux",
        ["Bay"] = "https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/Bay_(RealnestBootleg).mux",
        ["Coast"] = "https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/Coast_(RealnestBootleg).mux",
    };

    internal string GetUsedCategory(string environment)
    {
        return string.IsNullOrWhiteSpace(Category) ? environment switch
        {
            "Stadium" => "Crystal",
            _ => "Solid"
        } : Category;
    }

    internal string GetUsedSubCategory(string environment)
    {
        return string.IsNullOrWhiteSpace(SubCategory) ? environment switch
        {
            "Stadium" => "Modernized",
            _ => "Modless"
        } : SubCategory;
    }
}
