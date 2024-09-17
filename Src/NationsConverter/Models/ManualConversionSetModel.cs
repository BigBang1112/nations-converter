using NationsConverterShared.Models;
using YamlDotNet.Serialization;

namespace NationsConverter.Models;

[YamlSerializable]
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
