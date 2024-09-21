using GBX.NET;
using GBX.NET.Engines.Game;

namespace NationsConverter.Converters;

internal sealed class MusicConverter : EnvironmentConverterBase
{
    private readonly CGameCtnChallenge convertedMap;
    private readonly NationsConverterConfig config;

    private const string Extension = "mux";

    public MusicConverter(CGameCtnChallenge map, CGameCtnChallenge convertedMap, NationsConverterConfig config)
        : base(map)
    {
        this.convertedMap = convertedMap;
        this.config = config;
    }

    public void Convert()
    {
        if (!config.IncludeMusic)
        {
            return;
        }

        var music = config.Music[Environment];

        convertedMap.CustomMusicPackDesc = new PackDesc(
            FilePath: $@"Media\Musics\NC2\{music}.{Extension}",
            Checksum: null,
            LocatorUrl: $"https://{config.HttpHost}/music/{music}.{Extension}");
    }
}
