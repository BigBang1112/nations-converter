using GBX.NET;

namespace NationsConverterBuilder.Models;

public sealed class ConversionModel
{
    public string PageName { get; set; } = "";
    public Int3[]? Units { get; set; }
    public Int3? Size { get; set; }
    public int? Variants { get; set; }
    public int?[]? SubVariants { get; set; }
    public ConversionModifierModel? Ground { get; set; }
    public ConversionModifierModel? Air { get; set; }
}
