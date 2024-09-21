using GBX.NET.Engines.Game;
using GBX.NET.Tool;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using NationsConverterShared.Models;
using System.Diagnostics;

namespace NationsConverter.Extracts;

internal sealed class ConversionSetExtract
{
    private readonly CGameCtnChallenge map;
    private readonly IComplexConfig complexConfig;
    private readonly ILogger logger;

    public ConversionSetExtract(CGameCtnChallenge map, IComplexConfig complexConfig, ILogger logger)
    {
        this.map = map;
        this.complexConfig = complexConfig;
        this.logger = logger;
    }

    public ManualConversionSetModel Extract()
    {
        var environment = map.GetEnvironment() switch
        {
            "Alpine" => "Snow",
            "Speed" => "Desert",
            _ => map.GetEnvironment()
        };

        logger.LogInformation("Filling conversion set for {Environment} environment...", environment);
        var watch = Stopwatch.StartNew();

        var finalConversionSet = complexConfig.Get<ManualConversionSetModel>(Path.Combine("Manual", environment))
            .Fill(complexConfig.Get<ConversionSetModel>(Path.Combine("Generated", environment)));

        logger.LogInformation("Filled conversion set for {Environment} environment ({ElapsedMilliseconds}ms).", environment, watch.ElapsedMilliseconds);

        return finalConversionSet;
    }
}
