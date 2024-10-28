using GBX.NET;

namespace NationsConverter.Models;

public sealed class ManualConversionItemModel
{
    public string? Name { get; set; }
    public float OffsetY { get; set; }
    public int Dir { get; set; }
    public Vec3 Pivot { get; set; }
}
