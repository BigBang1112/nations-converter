using GBX.NET;
using NationsConverterShared.Models;

namespace NationsConverter.Models;

public class ManualConversionSkinModel : ConversionSkinModel
{
    public Dictionary<string, DifficultyColor>? RemapToColor { get; set; }
    public DifficultyColor FallbackColor { get; set; }
}
