using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Web;
using TmEssentials;

namespace NationsConverter.Stages;

internal sealed class MusicStage : EnvironmentStageBase
{
    private readonly CGameCtnChallenge mapOut;
    private readonly NationsConverterConfig config;
    private readonly HttpClient http;
    private readonly ILogger logger;

    private const string Extension = "mux";

    private static readonly ConcurrentDictionary<string, bool> availableMusicUrls = [];

    public MusicStage(
        CGameCtnChallenge mapIn,
        CGameCtnChallenge mapOut,
        NationsConverterConfig config,
        HttpClient http,
        ILogger logger)
        : base(mapIn)
    {
        this.mapOut = mapOut;
        this.config = config;
        this.http = http;
        this.logger = logger;
    }

    public void Convert()
    {
        if (!config.IncludeMusic)
        {
            return;
        }

        var watch = Stopwatch.StartNew();

        if (!config.Music.TryGetValue(Environment, out var music))
        {
            logger.LogWarning("Music not set for environment {Environment}.", Environment);
            return;
        }

        var filePath = $@"Media\Musics\NC2\{music}.{Extension}";
        var locatorUrl = $"https://{config.HttpHost}/music/{HttpUtility.UrlPathEncode($"{music}.{Extension}").Replace("(", "%28").Replace(")", "%29")}";

        logger.LogInformation("Music set to {Music}!", music);
        logger.LogInformation("Locator URL: {LocatorUrl}", locatorUrl);

        mapOut.CustomMusicPackDesc = new PackDesc(filePath, Checksum: null, locatorUrl);

        if (availableMusicUrls.ContainsKey(locatorUrl))
        {
            return;
        }

        logger.LogInformation("Checking if music is available online...");

        using var response = http.HeadAsync(locatorUrl).GetAwaiter().GetResult();

        availableMusicUrls[locatorUrl] = response.IsSuccessStatusCode;

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Music is available online: status code {StatusCode} ({ElapsedMilliseconds}ms)", response.StatusCode, watch.ElapsedMilliseconds);
        }
        else
        {
            logger.LogWarning("Music is not available online: status code {StatusCode} ({ElapsedMilliseconds}ms)", response.StatusCode, watch.ElapsedMilliseconds);
        }
    }
}
