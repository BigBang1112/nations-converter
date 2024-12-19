using GBX.NET;

namespace NationsConverterShared.Models;

public sealed record ConversionClipModel
{
    public string? Name { get; init; }
    public Int3 Offset { get; init; }
    public Direction Dir { get; init; }
}
