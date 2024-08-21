using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Script;
using GBX.NET.Tool;
using Microsoft.Extensions.Logging;
using System.Text;
using TmEssentials;

namespace NationsConverter;

public class NationsConverterTool(Gbx<CGameCtnChallenge> gbxMap, ILogger logger) : ITool,
    IProductive<Gbx<CGameCtnChallenge>>,
    IConfigurable<NationsConverterConfig>
{
    private const string BuildDate = "2024-07-02_14_35";
    private const string BuildGit = "127172-c8715502da1";
    private const string ExeVersion = "3.3.0";

    private readonly Gbx<CGameCtnChallenge> gbxMap = gbxMap;
    private readonly CGameCtnChallenge map = gbxMap.Node;
    private readonly ILogger logger = logger;

    public NationsConverterConfig Config { get; } = new();

    public Gbx<CGameCtnChallenge> Produce()
    {
        if (map.Collection.GetValueOrDefault().Number == 26)
        {
            throw new InvalidOperationException("Map is already a TM2020 map");
        }

        var convertedMap = CreateBaseMap();

        var placeBaseZoneConverter = new PlaceBaseZoneConverter(map, convertedMap, Config, logger);
        placeBaseZoneConverter.Convert();

        var coveredZoneBlockInfoExtract = new CoveredZoneBlockInfoExtract(map, Config, logger);
        var coveredZoneBlocks = coveredZoneBlockInfoExtract.Extract();

        var placeBasicBlockConverter = new PlaceBasicBlockConverter(map, convertedMap, Config, coveredZoneBlocks, logger);
        placeBasicBlockConverter.Convert();

        var placeTransformationConverter = new PlaceTransformationConverter(map, convertedMap, Config, logger);
        placeTransformationConverter.Convert();

        var decorationConverter = new DecorationConverter(map, convertedMap, Config, logger);
        decorationConverter.Convert();

        if (Config.CopyItems)
        {
            if (string.IsNullOrWhiteSpace(Config.UserDataFolder))
            {
                throw new InvalidOperationException("UserDataFolder is not set");
            }

            CopyDirectory("UserData", Config.UserDataFolder, true);
        }

        var fileNameWithoutExtension = gbxMap.FilePath is null
            ? TextFormatter.Deformat(map.MapName)
            : GbxPath.GetFileNameWithoutExtension(gbxMap.FilePath);

        return new Gbx<CGameCtnChallenge>(convertedMap)
        {
            FilePath = Path.Combine("Maps", "GbxTools", "NationsConverter", $"{fileNameWithoutExtension}.Map.Gbx")
        };
    }

    private CGameCtnChallenge CreateBaseMap()
    {
        var newMapUid = $"{Convert.ToBase64String(Encoding.ASCII.GetBytes(Guid.NewGuid().ToString()))[..10]}{map.MapUid.Substring(9, 14)}NC2";
        var authorLogin = "akPfIM0aSzuHuaaDWptBbQ";
        var authorZone = "World|Europe|Czechia|Jihoceský kraj";
        var mapType = "TrackMania\\TM_Race"; // either Race, Arkady's Platform, or Zai's Stunt

        var convertedMap = new CGameCtnChallenge
        {
            AnchoredObjects = new List<CGameCtnAnchoredObject>(),
            AuthorLogin = authorLogin,
            AuthorNickname = "BigBang1112",
            AuthorZone = authorZone,
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
            MapName = map.MapName,
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
            Thumbnail = map.Thumbnail
        };
        convertedMap.ScriptMetadata.Declare("MadeWithNationsConverter", true);
        convertedMap.ScriptMetadata.Declare("NC_OriginalAuthorLogin", map.AuthorLogin);
        convertedMap.ScriptMetadata.Declare("NC_OriginalAuthorNickname", map.AuthorNickname ?? string.Empty);
        convertedMap.ScriptMetadata.Declare("NC_OriginalMapUid", map.MapUid);
        convertedMap.ScriptMetadata.Declare("NC2_IsConverted", true);
        convertedMap.ScriptMetadata.Declare("NC2_ConvertedAt", DateTime.UtcNow.ToString("s"));
        convertedMap.ScriptMetadata.Declare("NC2_Version", "");
        convertedMap.ScriptMetadata.Declare("NC2_CLI_Version", "");
        convertedMap.ScriptMetadata.Declare("NC2_Web_Version", "");
        convertedMap.ScriptMetadata.Declare("NC2_GBXNET_Version", "");
        convertedMap.ScriptMetadata.Declare("NC2_Environment", map.GetEnvironment() switch
        {
            "Alpine" => "Snow",
            "Speed" => "Desert",
            _ => map.GetEnvironment()
        });

        convertedMap.CreateChunk<CGameCtnChallenge.HeaderChunk03043002>().Version = 13;
        convertedMap.CreateChunk<CGameCtnChallenge.HeaderChunk03043003>().Version = 11;
        convertedMap.CreateChunk<CGameCtnChallenge.HeaderChunk03043004>().Version = 6;
        convertedMap.CreateChunk<CGameCtnChallenge.HeaderChunk03043005>();
        convertedMap.CreateChunk<CGameCtnChallenge.HeaderChunk03043007>().Version = 1; // 1 to include thumbnail
        convertedMap.CreateChunk<CGameCtnChallenge.HeaderChunk03043008>().Version = 1;
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304300D>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043011>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043018>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043019>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304301F>().Version = 6;
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043022>().U01 = 1;
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043024>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043025>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043026>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043029>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304302A>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043034>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043036>().U01 = 10;
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304303E>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043040>().Version = 4;
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043042>().Version = 1;
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043043>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043044>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043048>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043049>().Version = 2;
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304304B>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304304F>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043050>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043051>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043052>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043053>().Version = 3;
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043054>().Version = 1;
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043055>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043056>().Version = 3;
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043057>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043059>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304305A>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304305B>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304305C>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304305D>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304305F>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043060>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043061>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043062>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043063>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043064>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043065>();
        //convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043067>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043068>().Version = 1;
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk03043069>();
        convertedMap.CreateChunk<CGameCtnChallenge.Chunk0304306B>();
        convertedMap.BlockStock.CreateChunk<CGameCtnCollectorList.Chunk0301B000>();
        convertedMap.ChallengeParameters.CreateChunk<CGameCtnChallengeParameters.Chunk0305B001>();
        convertedMap.ChallengeParameters.CreateChunk<CGameCtnChallengeParameters.Chunk0305B004>();
        convertedMap.ChallengeParameters.CreateChunk<CGameCtnChallengeParameters.Chunk0305B008>();
        convertedMap.ChallengeParameters.CreateChunk<CGameCtnChallengeParameters.Chunk0305B00A>();
        convertedMap.ChallengeParameters.CreateChunk<CGameCtnChallengeParameters.Chunk0305B00D>();
        convertedMap.ChallengeParameters.CreateChunk<CGameCtnChallengeParameters.Chunk0305B00E>();
        convertedMap.ScriptMetadata.CreateChunk<CScriptTraitsMetadata.Chunk11002000>().Version = 6;

        return convertedMap;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, overwrite: true);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}
