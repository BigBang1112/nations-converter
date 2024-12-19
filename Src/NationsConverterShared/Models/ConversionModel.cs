using GBX.NET;

namespace NationsConverterShared.Models;

public class ConversionModel : ConversionModifierModel
{
    public string PageName { get; set; } = "";
    public ConversionModifierModel? Ground { get; set; }
    public ConversionModifierModel? Air { get; set; }
    public int? ZoneHeight { get; set; }
    public string? Pylon { get; set; }
    public WaypointType? Waypoint { get; set; }
    public bool? Modifiable { get; set; }
    public HashSet<Int2>? NotModifiable { get; set; }
    public ConversionRoadModel? Road { get; set; }
    public ConversionSkinModel? Skin { get; set; }
    public bool? TM2 { get; set; }
}
