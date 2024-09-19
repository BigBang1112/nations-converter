using GBX.NET.Engines.Game;
using GBX.NET.Tool;
using NationsConverter.Models;
using NationsConverterShared.Models;

namespace NationsConverter.Extracts;

internal sealed class ConversionSetExtract
{
    private readonly CGameCtnChallenge map;
    private readonly IComplexConfig complexConfig;

    public ConversionSetExtract(CGameCtnChallenge map, IComplexConfig complexConfig)
    {
        this.map = map;
        this.complexConfig = complexConfig;
    }

    public ManualConversionSetModel Extract()
    {
        var environment = map.GetEnvironment() switch
        {
            "Alpine" => "Snow",
            "Speed" => "Desert",
            _ => map.GetEnvironment()
        };

        return complexConfig.Get<ManualConversionSetModel>(Path.Combine("Manual", environment))
            .Fill(complexConfig.Get<ConversionSetModel>(Path.Combine("Generated", environment)));
    }
}
