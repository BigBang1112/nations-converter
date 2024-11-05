namespace NationsConverterShared.Models;

public sealed class ConversionSetModel
{
    public string? Environment { get; set; }
    public string? DefaultZoneBlock { get; set; }
    public string? Pylon { get; set; }
    public Dictionary<string, ConversionDecorationModel> Decorations { get; set; } = [];
    public HashSet<string>? TerrainModifiers { get; set; }
    public SortedDictionary<string, ConversionModel> Blocks { get; set; } = [];
}
