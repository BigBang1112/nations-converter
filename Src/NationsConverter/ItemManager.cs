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
    private readonly NationsConverterConfig config;
    private readonly ILogger logger;

    public ItemManager(CGameCtnChallenge map, string runningDir, NationsConverterConfig config, ILogger logger)
    {
        this.map = map;
        this.runningDir = runningDir;
        this.config = config;
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

    /// <summary>
    /// Embeds item data into the map either from the user data folder or a user data pack zip inside the user data folder.
    /// </summary>
    /// <returns>The picked user data pack, or null if raw files were used.</returns>
    /// <exception cref="Exception"></exception>
    public string? EmbedData()
    {
        logger.LogInformation("Embedding item data...");

        var watch = Stopwatch.StartNew();

        var itemsPath = Path.Combine(runningDir, "UserData", "Items");
        var blocksPath = Path.Combine(runningDir, "UserData", "Blocks");
        var itemsOrBlocksHaveAtLeastOneFile = (Directory.Exists(itemsPath) && Directory.EnumerateFiles(itemsPath, "*.Item.Gbx", SearchOption.AllDirectories).Any())
            || (Directory.Exists(blocksPath) && Directory.EnumerateFiles(blocksPath, "*.Block.Gbx", SearchOption.AllDirectories).Any());

        string? userDataPackFilePath;

        if (itemsOrBlocksHaveAtLeastOneFile)
        {
            userDataPackFilePath = null;

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
        }
        else
        {
            userDataPackFilePath = config.UserDataPack is null
                ? (Directory.EnumerateFiles(Path.Combine(runningDir, "UserData"), "*.zip").SingleOrDefault() ?? throw new Exception("No available user data pack."))
                : Path.Combine(runningDir, "UserData", config.UserDataPack);

            using var toEmbedZip = ZipFile.OpenRead(userDataPackFilePath);

            map.UpdateEmbeddedZipData(zip =>
            {
                foreach (var itemModel in embeddedFilePaths)
                {
                    var itemPath = Path.Combine("Items", itemModel);

                    var toEmbedEntry = toEmbedZip.GetEntry(itemPath.Replace('\\', '/'));

                    if (toEmbedEntry is null)
                    {
                        continue;
                    }

                    var entry = zip.CreateEntry(Path.Combine("Items", itemModel), CompressionLevel.SmallestSize);
                    using var entryStream = entry.Open();
                    using var toEmbedEntryStream = toEmbedEntry.Open();
                    Gbx.Decompress(input: toEmbedEntryStream, output: entryStream);
                }
            });
        }

        logger.LogInformation("Embedded item data in {ElapsedMilliseconds}ms.", watch.ElapsedMilliseconds);

        return userDataPackFilePath;
    }
}
