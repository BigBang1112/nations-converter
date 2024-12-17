using GBX.NET.Tool;

namespace NationsConverter;

public class NationsConverterConfig : Config
{
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public string? Seed { get; set; }
    public bool IncludeMediaTracker { get; set; } = true;
    public bool IncludeDecoration { get; set; } = true;
    public bool UseNewWood { get; set; }
    public string[]? UserDataPackPriority { get; set; }
    public bool CopyItems { get; set; }
    public bool UniqueEmbeddedFolder { get; set; } = true;
    public string? UserDataFolder { get; set; }
    public string? HttpHost { get; set; } = "nc.gbx.tools";
    public bool IncludeMusic { get; set; } = true;
    public Dictionary<string, string> Music { get; set; } = new()
    {
        ["Snow"] = "https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/Snow_(RealnestBootleg).mux",
        ["Rally"] = "https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/Rally_(RealnestBootleg).mux",
        ["Desert"] = "https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/Desert_(Realnest&ThaumicTomBootleg).mux",
        ["Island"] = "https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/Island_(RealnestBootleg).mux",
        ["Bay"] = "https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/Bay_(RealnestBootleg).mux",
        ["Coast"] = "https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/Coast_(RealnestBootleg).mux",
    };
}
