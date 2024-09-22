using GBX.NET;
using NationsConverterShared.Models;

namespace NationsConverter.Models;

public class ManualConversionModifierModel : ConversionModifierModel
{
    public HashSet<Direction>[]? ClipDirs { get; set; }
}
