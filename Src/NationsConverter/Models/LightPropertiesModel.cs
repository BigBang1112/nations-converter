using GBX.NET;

namespace NationsConverter.Models;

public sealed class LightPropertiesModel
{
    public Vec3 Color { get; set; } = new(1, 1, 1);
    public Vec3 Position { get; set; }
    public float Distance { get; set; }
    public float Intensity { get; set; }
    public bool NightOnly { get; set; }
    public float SpotInnerAngle { get; set; } = 40;
    public float SpotOuterAngle { get; set; } = 60;
}
