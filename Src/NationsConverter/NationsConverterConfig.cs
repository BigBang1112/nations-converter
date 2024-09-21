using GBX.NET.Tool;

namespace NationsConverter;

public class NationsConverterConfig : Config
{
    public bool CopyItems { get; set; } = true;
    public string? UserDataFolder { get; set; }
    public bool IncludeDecoration { get; set; }
    public bool UseNewWood { get; set; }
    public string? UserDataPack { get; set; } = "InProgress";
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
