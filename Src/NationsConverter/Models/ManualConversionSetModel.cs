using NationsConverterShared.Models;

namespace NationsConverter.Models;

public sealed class ManualConversionSetModel : ConversionSetModel
{
    public Dictionary<string, string> BlockTerrainModifiers { get; set; } = [];

    public ManualConversionSetModel Merge(ConversionSetModel conversionSet)
    {
        var merged = new ManualConversionSetModel
        {
            DefaultZoneBlock = DefaultZoneBlock ?? conversionSet.DefaultZoneBlock,
            Decorations = conversionSet.Decorations,
            TerrainModifiers = conversionSet.TerrainModifiers,
            Blocks = conversionSet.Blocks,
            BlockTerrainModifiers = BlockTerrainModifiers
        };

        // merge other properties in depth

        return merged;
    }
}
