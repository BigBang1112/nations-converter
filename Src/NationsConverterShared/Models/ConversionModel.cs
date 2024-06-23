using GBX.NET;

namespace NationsConverterShared.Models;

public sealed class ConversionModel : ConversionModifierModel
{
    public string PageName { get; set; } = "";
    public ConversionModifierModel? Ground { get; set; }
    public ConversionModifierModel? Air { get; set; }
}
