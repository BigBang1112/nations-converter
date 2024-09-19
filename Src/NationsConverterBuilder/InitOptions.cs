using NationsConverterBuilder.Models;

namespace NationsConverterBuilder;

internal sealed class InitOptions
{
    public HashSet<string> DisabledTerrainModifierBlocks { get; set; } = [];
    public HashSet<string> WaterZone { get; set; } = [];
    public Dictionary<string, MaterialModel> Materials { get; set; } = [];
}
