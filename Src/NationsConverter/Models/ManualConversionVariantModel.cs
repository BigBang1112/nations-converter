namespace NationsConverter.Models;

public sealed class ManualConversionVariantModel
{
    public bool? NoItem { get; set; }
    public ManualConversionItemModel? Item { get; set; }
    public ManualConversionBlockModel? Block { get; set; }
}
