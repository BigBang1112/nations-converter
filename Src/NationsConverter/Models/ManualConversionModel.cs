using GBX.NET;
using GBX.NET.Engines.Game;
using NationsConverterShared.Models;
using YamlDotNet.Serialization;

namespace NationsConverter.Models;

[YamlSerializable]
public sealed class ManualConversionModel : ManualConversionModifierModel
{
    public string? PageName { get; set; }
    public ManualConversionModifierModel? Ground { get; set; }
    public ManualConversionModifierModel? Air { get; set; }
    public int? ZoneHeight { get; set; }
    public WaypointType? Waypoint { get; set; }
    public bool? Modifiable { get; set; }
    public HashSet<Int2>? NotModifiable { get; set; }

    public T GetProperty<T>(Func<ManualConversionModel, ManualConversionModifierModel?> modifierFunc, Func<ManualConversionModifierModel, T?> propertyFunc, Func<ManualConversionModel, ManualConversionModifierModel?>? fallbackModifierFunc = null)
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

    public T GetProperty<T>(Func<ManualConversionModel, ManualConversionModifierModel?> modifierFunc, Func<ManualConversionModifierModel, T> propertyFunc, Func<ManualConversionModel, ManualConversionModifierModel?>? fallbackModifierFunc = null)
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

    public T GetProperty<T>(CGameCtnBlock block, Func<ManualConversionModifierModel, T?> propertyFunc, bool fallback = false)
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

    public T GetProperty<T>(CGameCtnBlock block, Func<ManualConversionModifierModel, T> propertyFunc, bool fallback = false)
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
