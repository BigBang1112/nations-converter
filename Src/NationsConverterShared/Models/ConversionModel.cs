using GBX.NET.Engines.Game;

namespace NationsConverterShared.Models;

public sealed class ConversionModel : ConversionModifierModel
{
    public string PageName { get; set; } = "";
    public ConversionModifierModel? Ground { get; set; }
    public ConversionModifierModel? Air { get; set; }
    public int? ZoneHeight { get; set; }
    public WaypointType? Waypoint { get; set; }

    public T GetProperty<T>(Func<ConversionModel, ConversionModifierModel?> modifierFunc, Func<ConversionModifierModel, T?> propertyFunc, Func<ConversionModel, ConversionModifierModel?>? fallbackModifierFunc = null)
        where T : struct
    {
        var modifier = modifierFunc(this)
            ?? fallbackModifierFunc?.Invoke(this) // This fallback may be better to log as lower level
            ?? throw new Exception("Modifier is not available.");

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

    public T GetProperty<T>(Func<ConversionModel, ConversionModifierModel?> modifierFunc, Func<ConversionModifierModel, T> propertyFunc, Func<ConversionModel, ConversionModifierModel?>? fallbackModifierFunc = null)
    {
        var modifier = modifierFunc(this)
            ?? fallbackModifierFunc?.Invoke(this) // This fallback may be better to log as lower level
            ?? throw new Exception("Modifier is not available.");

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

    public T GetProperty<T>(CGameCtnBlock block, Func<ConversionModifierModel, T?> propertyFunc, bool fallback = false)
        where T : struct
    {
        if (fallback)
        {
            return block.IsGround
                ? GetProperty(x => x.Ground, propertyFunc, x => x.Air)
                : GetProperty(x => x.Air, propertyFunc, x => x.Ground);
        }
        else
        {
            return block.IsGround
                ? GetProperty(x => x.Ground, propertyFunc)
                : GetProperty(x => x.Air, propertyFunc);
        }
    }

    public T GetProperty<T>(CGameCtnBlock block, Func<ConversionModifierModel, T> propertyFunc, bool fallback = false)
    {
        if (fallback)
        {
            return block.IsGround
                ? GetProperty(x => x.Ground, propertyFunc, x => x.Air)
                : GetProperty(x => x.Air, propertyFunc, x => x.Ground);
        }
        else
        {
            return block.IsGround
                ? GetProperty(x => x.Ground, propertyFunc)
                : GetProperty(x => x.Air, propertyFunc);
        }
    }
}
