using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Web;
using TmEssentials;

namespace NationsConverter.Converters;

internal sealed class MusicConverter : EnvironmentConverterBase
{
    private readonly CGameCtnChallenge mapOut;
    private readonly NationsConverterConfig config;
    private readonly HttpClient http;
    private readonly ILogger logger;

    private const string Extension = "mux";

    public MusicConverter(
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

        var music = config.Music[Environment];
        var filePath = $@"Media\Musics\NC2\{music}.{Extension}";
        var locatorUrl = $"https://{config.HttpHost}/music/{HttpUtility.UrlPathEncode($"{music}.{Extension}").Replace("(", "%28").Replace(")", "%29")}";

        logger.LogInformation("Music set to {Music}!", music);
        logger.LogInformation("Locator URL: {LocatorUrl}", locatorUrl);

        mapOut.CustomMusicPackDesc = new PackDesc(filePath, Checksum: null, locatorUrl);

        logger.LogInformation("Checking if music is available online...");

        using var response = http.HeadAsync(locatorUrl).GetAwaiter().GetResult();
        
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
