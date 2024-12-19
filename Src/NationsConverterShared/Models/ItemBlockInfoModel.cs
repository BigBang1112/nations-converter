using GBX.NET;

namespace NationsConverterShared.Models;

public sealed class ItemBlockInfoModel
{
    public string? Modifier { get; set; }
    public int Variant { get; set; }
    public int SubVariant { get; set; }
    public Int3[]? Units { get; set; }
}
