namespace NationsConverterShared.Models;

public sealed class ConversionSetModel
{
    public Dictionary<string, ConversionDecorationModel> Decorations { get; set; } = [];
    public Dictionary<string, ConversionModel> Blocks { get; set; } = [];
}
