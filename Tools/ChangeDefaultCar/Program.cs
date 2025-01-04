using GBX.NET;
using GBX.NET.LZO;
using GBX.NET.Engines.Game;
using ManiaAPI.NadeoAPI;
using ManiaAPI.NadeoAPI.Extensions.Gbx;
using Spectre.Console;

Gbx.LZO = new Lzo();

var carName = AnsiConsole.Ask<string>("Enter [green]car name[/] (empty for StadiumCar):", "");

using var http = new HttpClient();

using var ns = new NadeoServices(http);
using var nls = new NadeoLiveServices(http);

var login = AnsiConsole.Ask<string>("Enter Ubisoft Connect [green]login[/]:");

var password = AnsiConsole.Prompt(
    new TextPrompt<string>("Enter Ubisoft Connect [green]password[/]:")
        .PromptStyle("red")
        .Secret());

await ns.AuthorizeAsync(login, password, AuthorizationMethod.UbisoftAccount);
await nls.AuthorizeAsync(login, password, AuthorizationMethod.UbisoftAccount);

AnsiConsole.MarkupLine("[green]Authorization successful[/]");

var campaign = await nls.GetClubCampaignAsync(clubId: int.Parse(args[0]), campaignId: int.Parse(args[1]));
var mapInfos = await nls.GetMapInfosAsync(campaign.Campaign.Playlist.Select(x => x.MapUid));

foreach (var mapInfo in mapInfos)
{
    AnsiConsole.MarkupLine($"Downloading map [green]{mapInfo.Name}[/]");

    using var response = await http.GetAsync(mapInfo.DownloadUrl);

    if (response.Content.Headers.ContentDisposition?.FileName is null)
    {
        AnsiConsole.MarkupLine("[red]Map has no file name, skipping[/]");
        continue;
    }

    using var stream = await response.Content.ReadAsStreamAsync();
    var mapGbx = Gbx.Parse<CGameCtnChallenge>(stream);

    mapGbx.Node.PlayerModel = new(carName, "", "");

    using var ms = new MemoryStream();
    mapGbx.Save(ms);
    ms.Position = 0;

    AnsiConsole.MarkupLine($"Uploading map [green]{mapInfo.Name}[/]");

    await ns.UpdateMapAsync(mapInfo.MapId, ms, response.Content.Headers.ContentDisposition.FileName);
}