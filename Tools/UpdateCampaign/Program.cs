using GBX.NET;
using GBX.NET.LZO;
using GBX.NET.Engines.Game;
using ManiaAPI.NadeoAPI;
using ManiaAPI.NadeoAPI.Extensions.Gbx;
using Spectre.Console;
using System.Text.RegularExpressions;

Gbx.LZO = new Lzo();

using var http = new HttpClient();

var mapUidDict = new Dictionary<string, string>();
var newMapPaths = Directory.GetFiles(args[0], "*.Map.Gbx", SearchOption.AllDirectories);
AnsiConsole.MarkupLine($"Loading [green]{newMapPaths.Length}[/] maps...");

foreach (var newMapPath in newMapPaths)
{
    var map = Gbx.ParseNode<CGameCtnChallenge>(newMapPath);
    mapUidDict.Add(map.ScriptMetadata!.GetText("NC_OriginalMapUid") ?? throw new Exception("NC_OriginalMapUid must exist"), newMapPath);
}

AnsiConsole.MarkupLine($"[green]{mapUidDict.Count}[/] maps loaded");

using var ns = new NadeoServices(http, new());
using var nls = new NadeoLiveServices(http, new());

var login = AnsiConsole.Ask<string>("Enter Ubisoft Connect [green]login[/]:");

var password = AnsiConsole.Prompt(
    new TextPrompt<string>("Enter Ubisoft Connect [green]password[/]:")
        .PromptStyle("red")
        .Secret());

await ns.AuthorizeAsync(login, password, AuthorizationMethod.UbisoftAccount);
await nls.AuthorizeAsync(login, password, AuthorizationMethod.UbisoftAccount);

AnsiConsole.MarkupLine("[green]Authorization successful[/]");

var campaign = await nls.GetClubCampaignAsync(clubId: int.Parse(args[1]), campaignId: int.Parse(args[2]));
var mapInfos = await nls.GetMapInfosAsync(campaign.Campaign.Playlist.Select(x => x.MapUid));

foreach (var mapInfo in mapInfos)
{
    AnsiConsole.MarkupLine($"Downloading map [green]{mapInfo.Name}[/]");

    using var response = await http.GetAsync(mapInfo.DownloadUrl);
    var map = Gbx.ParseNode<CGameCtnChallenge>(await response.Content.ReadAsStreamAsync());

    var originalMapUid = map.ScriptMetadata!.GetText("NC_OriginalMapUid") ?? throw new Exception("NC_OriginalMapUid must exist");
    AnsiConsole.MarkupLine($"Original map UID: [green]{originalMapUid}[/]");

    if (!mapUidDict.TryGetValue(originalMapUid, out var newMapPath))
    {
        AnsiConsole.MarkupLine($"[red]Map with original UID {originalMapUid} not found[/]");
        continue;
    }

    var mapUidToApply = map.MapUid;
    AnsiConsole.MarkupLine($"Map UID to keep: [green]{mapUidToApply}[/]");

    var newMapGbx = Gbx.Parse<CGameCtnChallenge>(newMapPath);
    var newMap = newMapGbx.Node;

    AnsiConsole.MarkupLine($"Replacing map UID from [yellow]{newMap.MapUid}[/] to [green]{mapUidToApply}[/]");
    newMap.MapUid = mapUidToApply;

    newMap.Xml = MapUidInXmlRegex().Replace(newMap.Xml!, @$" uid=""{mapUidToApply}"" ");

    using var ms = new MemoryStream();
    newMapGbx.Save(ms);
    ms.Position = 0;

    AnsiConsole.MarkupLine($"Uploading map [green]{mapInfo.Name}[/]");

    await ns.UpdateMapAsync(mapInfo.MapId, ms, Path.GetFileName(newMapPath));
}

internal partial class Program
{
    [GeneratedRegex(@"\suid=""(.+?)""\s")]
    private static partial Regex MapUidInXmlRegex();
}