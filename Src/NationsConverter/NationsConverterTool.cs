using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Tool;
using Microsoft.Extensions.Logging;
using NationsConverter.Stages;
using NationsConverter.Extracts;
using System.Text;
using TmEssentials;

namespace NationsConverter;

public class NationsConverterTool(Gbx<CGameCtnChallenge> gbxMapIn, IComplexConfig complexConfig, ILogger logger) : ITool,
    IProductive<Gbx<CGameCtnChallenge>>,
    IConfigurable<NationsConverterConfig>
{
    private const string BuildDate = "2024-07-02_14_35";
    private const string BuildGit = "127172-c8715502da1";
    private const string ExeVersion = "3.3.0";

    private readonly Gbx<CGameCtnChallenge> gbxMapIn = gbxMapIn;
    private readonly CGameCtnChallenge mapIn = gbxMapIn.Node;
    private readonly ILogger logger = logger;

    private readonly string runningDir = AppContext.BaseDirectory;

    public NationsConverterConfig Config { get; } = new();

    public Gbx<CGameCtnChallenge> Produce()
    {
        if (mapIn.Collection.GetValueOrDefault().Number == 26)
        {
            throw new InvalidOperationException("Map is already a TM2020 map");
        }

        using var http = new HttpClient();

        var mapOut = CreateBaseMap();

        var seed = uint.TryParse(Config.Seed, out var parsedSeed) ? parsedSeed
            : (uint)(Config.Seed?.GetHashCode() ?? Guid.NewGuid().GetHashCode());
        var random = new Random((int)seed);

        var isManiaPlanet = mapIn.Chunks.Any(c => c is CGameCtnChallenge.Chunk03043040);

        var customContentManager = new CustomContentManager(mapIn, mapOut, runningDir, Config, seed, logger);

        var conversionSetExtract = new ConversionSetExtract(mapIn, Config, complexConfig, logger);
        var conversionSet = conversionSetExtract.Extract();

        var coveredZoneBlockInfoExtract = new CoveredZoneBlockInfoExtract(mapIn, conversionSet, isManiaPlanet, logger);
        var coveredZoneBlocks = coveredZoneBlockInfoExtract.Extract();

        var terrainModifierZoneExtract = new TerrainModifierZoneExtract(mapIn, conversionSet, logger);
        var terrainModifierZones = terrainModifierZoneExtract.Extract();

        var placeBaseZoneStage = new PlaceBaseZoneStage(mapIn, mapOut, conversionSet, customContentManager, terrainModifierZones, logger);
        placeBaseZoneStage.Convert();

        var placeBlockStage = new PlaceBlockStage(mapIn, mapOut, conversionSet, customContentManager, coveredZoneBlocks, terrainModifierZones, isManiaPlanet, logger);
        placeBlockStage.Convert();

        var waterStage = new WaterStage(mapIn, mapOut, conversionSet, coveredZoneBlocks, isManiaPlanet, logger);
        waterStage.Convert();

        var pylonStage = new PylonStage(mapIn, mapOut, conversionSet, customContentManager);
        pylonStage.Convert();

        var decorationStage = new DecorationStage(mapIn, mapOut, conversionSet, Config, customContentManager, logger);
        decorationStage.Convert();

        var placeTransformationStage = new PlaceTransformationStage(mapIn, mapOut, conversionSet, customContentManager, logger);
        placeTransformationStage.Convert();

        var advertisementStage = new AdvertisementStage(mapOut);
        advertisementStage.Convert();

        customContentManager.EmbedData();

        mapOut.IsLapRace = mapIn.IsLapRace;
        mapOut.NbLaps = mapIn.NbLaps;

        var mediaTrackerStage = new MediaTrackerStage(mapIn, mapOut, Config);
        mediaTrackerStage.Convert();

        var musicStage = new MusicStage(mapIn, mapOut, Config, http, logger);
        musicStage.Convert();

        var metadataStage = new MetadataStage(mapIn, mapOut, Config, seed);
        metadataStage.Convert();

        if (Config.CopyItems)
        {
            var copyUserDataStage = new CopyUserDataStage(Config, mapOut, logger);
            copyUserDataStage.Copy();
        }

        if (!Config.UseNewWood)
        {
            mapOut.Chunks.Get<CGameCtnChallenge.Chunk03043022>()!.U01 = 7;
        }

        var fileNameWithoutExtension = gbxMapIn.FilePath is null
            ? TextFormatter.Deformat(mapIn.MapName)
            : GbxPath.GetFileNameWithoutExtension(gbxMapIn.FilePath);

        return new Gbx<CGameCtnChallenge>(mapOut)
        {
            FilePath = Path.Combine("Maps", "GbxTools", "NationsConverter", $"{fileNameWithoutExtension}.Map.Gbx")
        };
    }

    private CGameCtnChallenge CreateBaseMap()
    {
        var newMapUid = $"{Convert.ToBase64String(Encoding.ASCII.GetBytes(Guid.NewGuid().ToString()))[..10]}{mapIn.MapUid.Substring(9, 14)}NC2";
        var authorLogin = "akPfIM0aSzuHuaaDWptBbQ";
        var authorZone = "World|Europe|Czechia|Jihoceský kraj";
        var mapType = "TrackMania\\TM_Race"; // either Race, Arkady's Platform, or Zai's Stunt

        var mapOut = new CGameCtnChallenge
        {
            AnchoredObjects = [],
            AuthorLogin = authorLogin,
            AuthorNickname = "BigBang1112",
            AuthorZone = authorZone,
            Blocks = [],
            BlockStock = new CGameCtnCollectorList(),
            BuildVersion = $"date={BuildDate} git={BuildGit} GameVersion={ExeVersion}",
            ChallengeParameters = new CGameCtnChallengeParameters
            {
                MapType = mapType,
                TimeLimit = new TimeInt32(0, 1, 0)
            },
            ClipTriggerSize = (3, 1, 3),
            DayDuration = new TimeInt32(0, 5, 0),
            DecoBaseHeightOffset = 8,
            MapInfo = new(newMapUid, 26, authorLogin),
            MapName = mapIn.MapName,
            MapType = mapType,
            OffzoneTriggerSize = (3, 1, 3),
            Size = (48, 40, 48),
            TitleId = "TMStadium",
            Xml = $@"<header type=""map"" exever=""{ExeVersion}"" exebuild=""{BuildDate}"" title=""TMStadium"" lightmap=""0""><ident uid=""{newMapUid}"" name=""Base"" author=""{authorLogin}"" authorzone=""{authorZone}""/><desc envir=""Stadium"" mood=""Day"" type=""Race"" maptype=""{mapType}"" mapstyle="""" validated=""0"" nblaps=""0"" displaycost=""203"" mod="""" hasghostblocks=""0"" /><playermodel id=""""/><times bronze=""-1"" silver=""-1"" gold=""-1"" authortime=""-1"" authorscore=""0""/><deps></deps></header>",
            CustomMusicPackDesc = PackDesc.Empty,
            ModPackDesc = PackDesc.Empty,
            Kind = CGameCtnChallenge.MapKind.InProgress,
            KindInHeader = CGameCtnChallenge.MapKind.InProgress,
            ThumbnailFarClipPlane = -1,
            ThumbnailNearClipPlane = -1,
            ThumbnailPosition = mapIn.ThumbnailPosition,
            ThumbnailPitchYawRoll = mapIn.ThumbnailPitchYawRoll,
            ThumbnailFov = mapIn.ThumbnailFov,
            Thumbnail = mapIn.Thumbnail
        };

        mapOut.CreateChunk<CGameCtnChallenge.HeaderChunk03043002>().Version = 13;
        mapOut.CreateChunk<CGameCtnChallenge.HeaderChunk03043003>().Version = 11;
        mapOut.CreateChunk<CGameCtnChallenge.HeaderChunk03043004>().Version = 6;
        mapOut.CreateChunk<CGameCtnChallenge.HeaderChunk03043005>();
        mapOut.CreateChunk<CGameCtnChallenge.HeaderChunk03043007>().Version = 1; // 1 to include thumbnail
        mapOut.CreateChunk<CGameCtnChallenge.HeaderChunk03043008>().Version = 1;
        mapOut.CreateChunk<CGameCtnChallenge.Chunk0304300D>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043011>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043018>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043019>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk0304301F>().Version = 6;
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043022>().U01 = 1;
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043024>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043025>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043026>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043029>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk0304302A>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043034>();

        var oldThumbnailChunk = mapIn.Chunks.Get<CGameCtnChallenge.Chunk03043028>();
        if (oldThumbnailChunk is null)
        {
            mapOut.CreateChunk<CGameCtnChallenge.Chunk03043036>().U01 = 10;
        }
        else
        {
            mapOut.Chunks.Add(oldThumbnailChunk);
            mapOut.HasCustomCamThumbnail = true;
        }

        mapOut.CreateChunk<CGameCtnChallenge.Chunk0304303E>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043040>().Version = 4;
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043042>().Version = 1;
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043043>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043048>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043049>().Version = 2;
        mapOut.CreateChunk<CGameCtnChallenge.Chunk0304304B>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304304F>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043050>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043051>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043052>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043053>().Version = 3;
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043054>().Version = 1;
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043055>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043056>().Version = 3;
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043057>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043059>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk0304305A>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304305B>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304305C>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304305D>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk0304305F>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043060>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043061>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043062>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043063>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043064>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043065>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043067>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043068>().Version = 1;
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043069>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk0304306B>();
        mapOut.BlockStock.CreateChunk<CGameCtnCollectorList.Chunk0301B000>();
        mapOut.ChallengeParameters.CreateChunk<CGameCtnChallengeParameters.Chunk0305B001>();
        mapOut.ChallengeParameters.CreateChunk<CGameCtnChallengeParameters.Chunk0305B004>();
        mapOut.ChallengeParameters.CreateChunk<CGameCtnChallengeParameters.Chunk0305B008>();
        mapOut.ChallengeParameters.CreateChunk<CGameCtnChallengeParameters.Chunk0305B00A>();
        mapOut.ChallengeParameters.CreateChunk<CGameCtnChallengeParameters.Chunk0305B00D>();
        mapOut.ChallengeParameters.CreateChunk<CGameCtnChallengeParameters.Chunk0305B00E>();

        return mapOut;
    }
}
