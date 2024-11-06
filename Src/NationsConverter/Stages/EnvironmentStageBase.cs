using GBX.NET.Engines.Game;
using System.Collections.Frozen;

namespace NationsConverter.Stages;

internal abstract class EnvironmentStageBase
{
    private static readonly FrozenDictionary<string, string> mapping = new Dictionary<string, string>()
    {
        ["Alpine"] = "Snow",
        ["Speed"] = "Desert"
    }.ToFrozenDictionary();

    protected string Environment { get; }

    protected EnvironmentStageBase(CGameCtnChallenge mapIn)
    {
        Environment = mapIn.GetEnvironment();

        if (mapping.TryGetValue(Environment, out var newEnvironment))
        {
            Environment = newEnvironment;
        }
    }
}
