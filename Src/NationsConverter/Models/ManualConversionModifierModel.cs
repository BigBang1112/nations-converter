using GBX.NET;
using NationsConverterShared.Models;

namespace NationsConverter.Models;

public class ManualConversionModifierModel : ConversionModifierModel
{
    public HashSet<Direction>[]? ClipDirs { get; set; }
    public bool? NoItem { get; set; }
    public bool? NoTerrainModifier { get; set; }
    public ManualConversionBlockModel? Block { get; set; }
}
