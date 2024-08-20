using GBX.NET;
using GBX.NET.Engines.Game;

namespace NationsConverterShared.Models;

public sealed class ConversionModel : ConversionModifierModel
{
    public string PageName { get; set; } = "";
    public ConversionModifierModel? Ground { get; set; }
    public ConversionModifierModel? Air { get; set; }
    public int? ZoneHeight { get; set; }
    public string? Waypoint { get; set; }

    public T GetProperty<T>(Func<ConversionModel, ConversionModifierModel?> modifierFunc, Func<ConversionModifierModel, T?> propertyFunc)
        where T : struct
    {
        var modifier = modifierFunc(this) ?? throw new Exception("Modifier is not available.");

        if (propertyFunc(modifier) is T modifierValue)
        {
            return modifierValue;
        }

        if (propertyFunc(this) is T conversionValue)
        {
            return conversionValue;
        }

        throw new Exception("Property is null in both places.");
    }

    public T GetProperty<T>(Func<ConversionModel, ConversionModifierModel?> modifierFunc, Func<ConversionModifierModel, T> propertyFunc)
    {
        var modifier = modifierFunc(this) ?? throw new Exception("Modifier is not available.");

        if (propertyFunc(modifier) is T modifierValue)
        {
            return modifierValue;
        }

        if (propertyFunc(this) is T conversionValue)
        {
            return conversionValue;
        }

        throw new Exception("Property is null in both places.");
    }

    public T GetProperty<T>(CGameCtnBlock block, Func<ConversionModifierModel, T> propertyFunc)
    {
        return block.IsGround
            ? GetProperty(x => x.Ground, propertyFunc)
            : GetProperty(x => x.Air, propertyFunc);
    }
}
