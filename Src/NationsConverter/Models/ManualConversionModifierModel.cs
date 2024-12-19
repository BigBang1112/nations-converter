using GBX.NET;
using NationsConverterShared.Models;

namespace NationsConverter.Models;

public class ManualConversionModifierModel : ConversionModifierModel
{
    public HashSet<Direction>[]? ClipDirs { get; set; }
    public bool? NoItem { get; set; }
    public bool? NoTerrainModifier { get; set; }
    public bool? PlaceDefaultZone { get; set; }
    public ManualConversionBlockModel? Block { get; set; }
    public ManualConversionBlockModel[]? Blocks { get; set; }
    public ManualConversionItemModel? Item { get; set; }
    public ManualConversionItemModel[]? Items { get; set; }
    public ManualConversionBlockModel? Conversion { get; set; }
    public ManualConversionBlockModel[]? Conversions { get; set; }
    public Dictionary<int, ManualConversionVariantModel>? ConversionVariants { get; set; }
    public LightPropertiesModel[]? Lights { get; set; }
}
