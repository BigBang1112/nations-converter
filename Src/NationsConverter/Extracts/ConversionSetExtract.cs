using GBX.NET.Engines.Game;
using GBX.NET.Tool;
using Microsoft.Extensions.Logging;
using NationsConverter.Converters;
using NationsConverter.Models;
using NationsConverterShared.Models;
using System.Diagnostics;

namespace NationsConverter.Extracts;

internal sealed class ConversionSetExtract : EnvironmentConverterBase
{
    private readonly IComplexConfig complexConfig;
    private readonly ILogger logger;

    private readonly string category;

    public ConversionSetExtract(CGameCtnChallenge mapIn, NationsConverterConfig config, IComplexConfig complexConfig, ILogger logger)
        : base(mapIn)
    {
        this.complexConfig = complexConfig;
        this.logger = logger;

        category = string.IsNullOrWhiteSpace(config.Category) ? Environment switch
        {
            "Stadium" => "Crystal",
            _ => "Solid"
        } : config.Category;
    }

    public ManualConversionSetModel Extract()
    {
        logger.LogInformation("Filling conversion set for {Environment} environment...", Environment);
        var watch = Stopwatch.StartNew();

        var finalConversionSet = complexConfig.Get<ManualConversionSetModel>(Path.Combine("Manual", category, Environment))
            .Fill(complexConfig.Get<ConversionSetModel>(Path.Combine("Generated", Environment)));

        logger.LogInformation("Filled conversion set for {Environment} environment ({ElapsedMilliseconds}ms).", Environment, watch.ElapsedMilliseconds);

        return finalConversionSet;
    }
}
