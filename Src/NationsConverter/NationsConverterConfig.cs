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
        ["Snow"] = "Snow (Realnest Bootleg)",
        ["Rally"] = "Rally (Realnest Bootleg)",
        ["Desert"] = "Desert (Realnest & ThaumicTom Bootleg)",
        ["Island"] = "Island (Realnest Bootleg)",
        ["Bay"] = "Bay (Realnest Bootleg)",
        ["Coast"] = "Coast (Realnest Bootleg)",
    };
}
