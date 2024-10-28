namespace NationsConverter.Models;

public sealed class ManualConversionVariantModel
{
    public bool? NoItem { get; set; }
    public bool? PlaceDefaultZone { get; set; }
    public ManualConversionItemModel? Item { get; set; }
    public ManualConversionBlockModel? Block { get; set; }
    public int? Variant { get; set; }
    public int? SubVariant { get; set; }
}
