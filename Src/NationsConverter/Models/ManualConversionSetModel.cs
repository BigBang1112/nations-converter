using NationsConverterShared.Models;
using YamlDotNet.Serialization;

namespace NationsConverter.Models;

[YamlSerializable]
public sealed class ManualConversionSetModel
{
    public string? DefaultZoneBlock { get; set; }
    public Dictionary<string, string> BlockTerrainModifiers { get; set; } = [];
    public Dictionary<string, ManualConversionDecorationModel> Decorations { get; set; } = [];
    public HashSet<string>? TerrainModifiers { get; set; }
    public Dictionary<string, ManualConversionModel> Blocks { get; set; } = [];

    public ManualConversionSetModel Fill(ConversionSetModel conversionSet)
    {
        DefaultZoneBlock ??= conversionSet.DefaultZoneBlock;
        
        foreach (var (size, deco) in conversionSet.Decorations)
        {
            if (!Decorations.TryGetValue(size, out var manualDeco))
            {
                manualDeco = new ManualConversionDecorationModel();
                Decorations.Add(size, manualDeco);
            }

            manualDeco.BaseHeight ??= deco.BaseHeight;
        }

        TerrainModifiers ??= conversionSet.TerrainModifiers;

        foreach (var (block, conversion) in conversionSet.Blocks)
        {
            if (!Blocks.TryGetValue(block, out var manualConversion))
            {
                manualConversion = new ManualConversionModel();
                Blocks.Add(block, manualConversion);
            }

            manualConversion.PageName ??= conversion.PageName;

            if (conversion.Ground is not null)
            {
                manualConversion.Ground ??= new ManualConversionModifierModel();
                manualConversion.Ground.Units = conversion.Ground.Units;
                manualConversion.Ground.Size = conversion.Ground.Size;
                manualConversion.Ground.Variants = conversion.Ground.Variants;
                manualConversion.Ground.SubVariants = conversion.Ground.SubVariants;
                manualConversion.Ground.Clips = conversion.Ground.Clips;
                manualConversion.Ground.SpawnPos = conversion.Ground.SpawnPos;
            }

            if (conversion.Air is not null)
            {
                manualConversion.Air ??= new ManualConversionModifierModel();
                manualConversion.Air.Units = conversion.Air.Units;
                manualConversion.Air.Size = conversion.Air.Size;
                manualConversion.Air.Variants = conversion.Air.Variants;
                manualConversion.Air.SubVariants = conversion.Air.SubVariants;
                manualConversion.Air.Clips = conversion.Air.Clips;
                manualConversion.Air.SpawnPos = conversion.Air.SpawnPos;
            }

            manualConversion.Units = conversion.Units;
            manualConversion.Size = conversion.Size;
            manualConversion.Variants = conversion.Variants;
            manualConversion.SubVariants = conversion.SubVariants;
            manualConversion.Clips = conversion.Clips;
            manualConversion.SpawnPos = conversion.SpawnPos;
            manualConversion.ZoneHeight = conversion.ZoneHeight;
            manualConversion.Waypoint = conversion.Waypoint;
            manualConversion.Modifiable = conversion.Modifiable;
            manualConversion.NotModifiable = conversion.NotModifiable;
        }

        return this;
    }
}
