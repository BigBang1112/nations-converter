using GBX.NET;

namespace NationsConverterBuilder.Models;

public sealed class ConversionModifierModel
{
    public Int3[]? Units { get; set; }
    public Int3? Size { get; set; }
    public int? Variants { get; set; }
    public int?[]? SubVariants { get; set; }
}
