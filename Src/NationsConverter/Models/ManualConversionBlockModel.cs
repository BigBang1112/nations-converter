namespace NationsConverter.Models;

public sealed class ManualConversionBlockModel
{
    public string? Name { get; set; }
    public int? Variant { get; set; }
    public int? SubVariant { get; set; }
    public bool IsGround { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public int OffsetZ { get; set; }
    public bool NoItem { get; set; }
    public bool Bit21 { get; set; }
    public int Dir { get; set; }
}
