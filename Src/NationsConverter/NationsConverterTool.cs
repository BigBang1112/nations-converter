using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Script;
using System.Text;
using TmEssentials;

namespace NationsConverter;

public class NationsConverterTool
{
    private readonly Gbx<CGameCtnChallenge> gbxMap;
    private readonly CGameCtnChallenge map;

    public NationsConverterTool(Gbx<CGameCtnChallenge> gbxMap)
    {
        this.gbxMap = gbxMap;
        map = gbxMap.Node;
    }

    public CGameCtnChallenge ConvertMap()
    {
        var convertedMap = new CGameCtnChallenge
        {
            AnchoredObjects = new List<CGameCtnAnchoredObject>(),
            AuthorLogin = "akPfIM0aSzuHuaaDWptBbQ",
            AuthorNickname = "BigBang1112",
            AuthorZone = "World|Europe|Czechia|Jihoceský kraj",
            BlockStock = new CGameCtnCollectorList(),
            BuildVersion = "date=2024-04-30_16_52 git=127012-8c94a9edc65 GameVersion=3.3.0",
            ChallengeParameters = new CGameCtnChallengeParameters
            {
                MapType = "TrackMania\\TM_Race",
                TimeLimit = new TimeInt32(0, 1, 0)
            },
            ClipTriggerSize = (3, 1, 3),
            DayDuration = new TimeInt32(0, 5, 0),
            DecoBaseHeightOffset = 8,
            Decoration = new("48x48Screen155Day", 26, "Nadeo"),
            MapInfo = new($"{Convert.ToBase64String(Encoding.ASCII.GetBytes(Guid.NewGuid().ToString()))[..10]}{map.MapUid.Substring(9, 14)}NC2", 26, "akPfIM0aSzuHuaaDWptBbQ"),
            MapName = map.MapName,
            MapType = "TrackMania\\TM_Race",
            OffzoneTriggerSize = (3, 1, 3),
            ScriptMetadata = new CScriptTraitsMetadata
            {
                Traits = new Dictionary<string, CScriptTraitsMetadata.ScriptTrait>()
            },
            Size = (48, 40, 48),
            TitleId = "TMStadium",
            Xml = @"<header type=""map"" exever=""3.3.0"" exebuild=""2024-04-30_16_52"" title=""TMStadium"" lightmap=""0""><ident uid=""oSkQiOPge_EZhc81RXE44jk3VCa"" name=""Base"" author=""akPfIM0aSzuHuaaDWptBbQ"" authorzone=""World|Europe|Czechia|Jihoceský kraj""/><desc envir=""Stadium"" mood=""Day"" type=""Race"" maptype=""TrackMania\TM_Race"" mapstyle="""" validated=""0"" nblaps=""0"" displaycost=""203"" mod="""" hasghostblocks=""0"" /><playermodel id=""""/><times bronze=""-1"" silver=""-1"" gold=""-1"" authortime=""-1"" authorscore=""0""/><deps></deps></header>",
            CustomMusicPackDesc = PackDesc.Empty,
            ModPackDesc = PackDesc.Empty,
            Kind = CGameCtnChallenge.MapKind.InProgress,
            KindInHeader = CGameCtnChallenge.MapKind.InProgress,
            ThumbnailFarClipPlane = -1,
            ThumbnailNearClipPlane = -1,
            ThumbnailFov = 90
        };

        convertedMap.CreateChunk<CGameCtnChallenge.HeaderChunk03043002>().Version = 13;
        convertedMap.CreateChunk<CGameCtnChallenge.HeaderChunk03043003>().Version = 11;
        convertedMap.CreateChunk<CGameCtnChallenge.HeaderChunk03043004>().Version = 6;
        convertedMap.CreateChunk<CGameCtnChallenge.HeaderChunk03043005>();
        convertedMap.CreateChunk<CGameCtnChallenge.HeaderChunk03043007>().Version = 0; // 1 to include thumbnail
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
}
