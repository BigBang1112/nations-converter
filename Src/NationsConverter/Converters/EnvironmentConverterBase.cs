using GBX.NET.Engines.Game;
using System.Collections.Frozen;

namespace NationsConverter.Converters;

internal abstract class EnvironmentConverterBase
{
    private static readonly FrozenDictionary<string, string> mapping = new Dictionary<string, string>()
    {
        ["Alpine"] = "Snow",
        ["Speed"] = "Desert"
    }.ToFrozenDictionary();

    protected string Environment { get; }

    protected EnvironmentConverterBase(CGameCtnChallenge map)
    {
        Environment = map.GetEnvironment();

        if (mapping.TryGetValue(Environment, out var newEnvironment))
        {
            Environment = newEnvironment;
        }
    }
}
