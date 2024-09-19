namespace NationsConverterShared.Models;

public sealed class ConversionSetModel
{
    public string? DefaultZoneBlock { get; set; }
    public Dictionary<string, ConversionDecorationModel> Decorations { get; set; } = [];
    public HashSet<string>? TerrainModifiers { get; set; }
    public Dictionary<string, ConversionModel> Blocks { get; set; } = [];
}
