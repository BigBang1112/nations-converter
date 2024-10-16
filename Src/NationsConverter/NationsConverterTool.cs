using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Script;
using GBX.NET.Tool;
using Microsoft.Extensions.Logging;
using NationsConverter.Converters;
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

        var customContentManager = new CustomContentManager(mapIn, mapOut, runningDir, Config, logger);

        var conversionSetExtract = new ConversionSetExtract(mapIn, Config, complexConfig, logger);
        var conversionSet = conversionSetExtract.Extract();

        var placeBaseZoneConverter = new PlaceBaseZoneConverter(mapIn, mapOut, conversionSet, customContentManager, logger);
        placeBaseZoneConverter.Convert();

        var coveredZoneBlockInfoExtract = new CoveredZoneBlockInfoExtract(mapIn, conversionSet, logger);
        var coveredZoneBlocks = coveredZoneBlockInfoExtract.Extract();

        var terrainModifierZoneExtract = new TerrainModifierZoneExtract(mapIn, conversionSet, logger);
        var terrainModifierZones = terrainModifierZoneExtract.Extract();

        var placeBlockConverter = new PlaceBlockConverter(mapIn, mapOut, conversionSet, customContentManager, coveredZoneBlocks, terrainModifierZones, logger);
        placeBlockConverter.Convert();

        var waterConverter = new WaterConverter(mapIn, mapOut, conversionSet, coveredZoneBlocks, logger);
        waterConverter.Convert();

        var pylonConverter = new PylonConverter(mapIn, mapOut, conversionSet, customContentManager);
        pylonConverter.Convert();

        var placeTransformationConverter = new PlaceTransformationConverter(mapIn, mapOut, conversionSet, customContentManager, logger);
        placeTransformationConverter.Convert();

        var decorationConverter = new DecorationConverter(mapIn, mapOut, conversionSet, Config, customContentManager, logger);
        decorationConverter.Convert();

        var userDataPackFilePath = customContentManager.EmbedData();

        var musicConverter = new MusicConverter(mapIn, mapOut, Config, http, logger);
        musicConverter.Convert();

        if (Config.CopyItems)
        {
            var copyUserDataConverter = new CopyUserDataConverter(Config, runningDir, userDataPackFilePath);
            copyUserDataConverter.Copy();
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
            AnchoredObjects = new List<CGameCtnAnchoredObject>(),
            AuthorLogin = authorLogin,
            AuthorNickname = "BigBang1112",
            AuthorZone = authorZone,
            Blocks = new List<CGameCtnBlock>(),
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
            ScriptMetadata = new CScriptTraitsMetadata(),
            Size = (48, 40, 48),
            TitleId = "TMStadium",
            Xml = $@"<header type=""map"" exever=""{ExeVersion}"" exebuild=""{BuildDate}"" title=""TMStadium"" lightmap=""0""><ident uid=""{newMapUid}"" name=""Base"" author=""{authorLogin}"" authorzone=""{authorZone}""/><desc envir=""Stadium"" mood=""Day"" type=""Race"" maptype=""{mapType}"" mapstyle="""" validated=""0"" nblaps=""0"" displaycost=""203"" mod="""" hasghostblocks=""0"" /><playermodel id=""""/><times bronze=""-1"" silver=""-1"" gold=""-1"" authortime=""-1"" authorscore=""0""/><deps></deps></header>",
            CustomMusicPackDesc = PackDesc.Empty,
            ModPackDesc = PackDesc.Empty,
            Kind = CGameCtnChallenge.MapKind.InProgress,
            KindInHeader = CGameCtnChallenge.MapKind.InProgress,
            ThumbnailFarClipPlane = -1,
            ThumbnailNearClipPlane = -1,
            ThumbnailFov = 90,
            Thumbnail = mapIn.Thumbnail
        };
        mapOut.ScriptMetadata.Declare("MadeWithNationsConverter", true);
        mapOut.ScriptMetadata.Declare("NC_OriginalAuthorLogin", mapIn.AuthorLogin);
        mapOut.ScriptMetadata.Declare("NC_OriginalAuthorNickname", mapIn.AuthorNickname ?? string.Empty);
        mapOut.ScriptMetadata.Declare("NC_OriginalMapUid", mapIn.MapUid);
        mapOut.ScriptMetadata.Declare("NC2_IsConverted", true);
        mapOut.ScriptMetadata.Declare("NC2_ConvertedAt", DateTime.UtcNow.ToString("s"));
        mapOut.ScriptMetadata.Declare("NC2_Version", "");
        mapOut.ScriptMetadata.Declare("NC2_CLI_Version", "");
        mapOut.ScriptMetadata.Declare("NC2_Web_Version", "");
        mapOut.ScriptMetadata.Declare("NC2_GBXNET_Version", "");
        mapOut.ScriptMetadata.Declare("NC2_Environment", mapIn.GetEnvironment() switch
        {
            "Alpine" => "Snow",
            "Speed" => "Desert",
            _ => mapIn.GetEnvironment()
        });
        mapOut.ScriptMetadata.Declare("NC2_PreAlpha", true);

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
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043036>().U01 = 10;
        mapOut.CreateChunk<CGameCtnChallenge.Chunk0304303E>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043040>().Version = 4;
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043042>().Version = 1;
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043043>();
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043044>();
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
        mapOut.ScriptMetadata.CreateChunk<CScriptTraitsMetadata.Chunk11002000>().Version = 6;

        return mapOut;
    }
}
