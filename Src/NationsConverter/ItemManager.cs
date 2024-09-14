using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;

namespace NationsConverter;

internal sealed class ItemManager
{
    private const int ItemCollection = 26;

    private readonly ConcurrentDictionary<string, string> itemModelAuthors = [];
    private readonly HashSet<string> embeddedFilePaths = [];

    private readonly CGameCtnChallenge map;
    private readonly string runningDir;
    private readonly ILogger logger;

    public ItemManager(CGameCtnChallenge map, string runningDir, ILogger logger)
    {
        this.map = map;
        this.runningDir = runningDir;
        this.logger = logger;
    }

    public void Place(string itemModel, Vec3 pos, Vec3 rot)
    {
        // retrieve collection and login from the item model gbx, cache this item model in dictionary
        if (!itemModelAuthors.TryGetValue(itemModel, out var itemModelAuthor))
        {
            var itemPath = Path.Combine(runningDir, "UserData", "Items", itemModel);
            
            itemModelAuthor = File.Exists(itemPath)
                ? Gbx.ParseHeaderNode<CGameItemModel>(itemPath).Ident.Author
                : "akPfIM0aSzuHuaaDWptBbQ";

            // add file to hash set, so that it can be found + properly placed in the zip
            embeddedFilePaths.Add(itemModel);
        }

        map.PlaceAnchoredObject(new(itemModel.Replace('/', '\\'), ItemCollection, itemModelAuthor), pos, rot);
    }

    public void EmbedData()
    {
        logger.LogInformation("Embedding item data...");

        var watch = Stopwatch.StartNew();

        map.UpdateEmbeddedZipData(zip =>
        {
            foreach (var itemModel in embeddedFilePaths)
            {
                var itemPath = Path.Combine(runningDir, "UserData", "Items", itemModel);

                if (!File.Exists(itemPath))
                {
                    continue;
                }

                var entry = zip.CreateEntry(Path.Combine("Items", itemModel), CompressionLevel.SmallestSize);
                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(itemPath);
                Gbx.Decompress(input: fileStream, output: entryStream);
            }
        });

        logger.LogInformation("Embedded item data in {ElapsedMilliseconds}ms.", watch.ElapsedMilliseconds);
    }
}
