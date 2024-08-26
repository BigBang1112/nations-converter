using GBX.NET;

namespace NationsConverterShared.Models;

public class ConversionModifierModel
{
    public Int3[]? Units { get; set; }
    public Int3? Size { get; set; }
    public int? Variants { get; set; }
    public int[]? SubVariants { get; set; }
    public ConversionClipModel[]? Clips { get; set; }
    public Vec3? SpawnPos { get; set; }
}
