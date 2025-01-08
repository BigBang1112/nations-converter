using GBX.NET;

namespace NationsConverter.Models;

public sealed class ManualConversionItemModel
{
    public string? Name { get; set; }
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }
    public float OffsetZ { get; set; }
    public float Offset1Y { get; set; }
    public float Offset2Y { get; set; }
    public int Dir { get; set; }
    public Vec3 Pivot { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public bool AlwaysStaticObject { get; set; }
    public bool OnlyAir { get; set; }
    public bool OnlyGround { get; set; }

    /// <summary>
    /// If item from the Modernized sub-category should be used when Modernized is applied. False will always pick Classic. Only has effect in the Crystal category.
    /// </summary>
    public bool Modernized { get; set; }
}
