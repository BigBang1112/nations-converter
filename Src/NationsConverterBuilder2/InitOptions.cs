using NationsConverterBuilder2.Models;

namespace NationsConverterBuilder2;

internal sealed class InitOptions
{
    public HashSet<string> DisabledTerrainModifierBlocks { get; set; } = [];
    public HashSet<string> WaterZone { get; set; } = [];
    public HashSet<string> ObjectLinkAsItem { get; set; } = [];
    public Dictionary<string, MaterialModel> Materials { get; set; } = [];
}
