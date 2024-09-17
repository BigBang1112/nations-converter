using ByteSizeLib;
using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;

namespace NationsConverter;

internal sealed class CustomContentManager
{
    private const int ItemCollection = 26;

    private readonly ConcurrentDictionary<string, string> itemModelAuthors = [];
    private readonly HashSet<string> embeddedFilePaths = [];

    private readonly CGameCtnChallenge map;
    private readonly string runningDir;
    private readonly NationsConverterConfig config;
    private readonly ILogger logger;

    public CustomContentManager(CGameCtnChallenge map, string runningDir, NationsConverterConfig config, ILogger logger)
    {
        this.map = map;
        this.runningDir = runningDir;
        this.config = config;
        this.logger = logger;
    }

    public void PlaceItem(string itemModel, Vec3 pos, Vec3 rot)
    {
        // retrieve collection and login from the item model gbx, cache this item model in dictionary
        if (!itemModelAuthors.TryGetValue(itemModel, out var itemModelAuthor))
        {
            var itemPath = Path.Combine(runningDir, "UserData", "Items", itemModel);

            itemModelAuthor = File.Exists(itemPath)
                ? Gbx.ParseHeaderNode<CGameItemModel>(itemPath).Ident.Author
                : "akPfIM0aSzuHuaaDWptBbQ";

            // add file to hash set, so that it can be found + properly placed in the zip
            embeddedFilePaths.Add(Path.Combine("Items", itemModel));
        }

        map.PlaceAnchoredObject(new(itemModel.Replace('/', '\\'), ItemCollection, itemModelAuthor), pos, rot);
    }

    public CGameCtnBlock PlaceBlock(string blockModel, Int3 coord, Direction dir, bool isGround = false, byte variant = 0, byte subVariant = 0)
    {
        var block = map.PlaceBlock($"{blockModel.Replace('/', '\\')}.Block.Gbx_CustomBlock", coord, dir, isGround, variant, subVariant);

        embeddedFilePaths.Add(Path.Combine("Blocks", $"{blockModel}.Block.Gbx"));

        return block;
    }

    public CGameCtnBlock PlaceBlock(string blockModel, Vec3 pos, Vec3 rot, bool isGround = false, byte variant = 0, byte subVariant = 0)
    {
        var block = map.PlaceBlock($"{blockModel.Replace('/', '\\')}.Block.Gbx_CustomBlock", (-1, 0, -1), Direction.North, isGround, variant, subVariant);

        block.IsFree = true;
        block.AbsolutePositionInMap = pos;
        block.PitchYawRoll = rot;

        embeddedFilePaths.Add(Path.Combine("Blocks", $"{blockModel}.Block.Gbx"));

        return block;
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

        var actualEmbeddedItemsCount = 0;

        if (string.IsNullOrWhiteSpace(config.UserDataPack))
        {
            map.UpdateEmbeddedZipData(zip =>
            {
                foreach (var path in embeddedFilePaths)
                {
                    if (CreateEntryFromGbxFile(zip, path) is not null)
                    {
                        actualEmbeddedItemsCount++;
                    }
                }
            });

            LogFinishedEmbeddedItems(watch, actualEmbeddedItemsCount);

            return null;
        }

        var userDataPackFilePath = Path.Combine(runningDir, "UserData",
            config.UserDataPack.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            ? config.UserDataPack
            : config.UserDataPack + ".zip");

        if (!File.Exists(userDataPackFilePath))
        {
            throw new Exception("Specified user data pack does not exist.");
        }

        using var toEmbedZip = ZipFile.OpenRead(userDataPackFilePath);

        map.UpdateEmbeddedZipData(zip =>
        {
            foreach (var path in embeddedFilePaths)
            {
                var toEmbedEntry = toEmbedZip.GetEntry(path.Replace('\\', '/'));

                if (toEmbedEntry is null)
                {
                    if (CreateEntryFromGbxFile(zip, path) is not null)
                    {
                        actualEmbeddedItemsCount++;
                    }
                }
                else
                {
                    var entry = zip.CreateEntry(path, CompressionLevel.SmallestSize);
                    using var entryStream = entry.Open();
                    using var toEmbedEntryStream = toEmbedEntry.Open();
                    Gbx.Decompress(input: toEmbedEntryStream, output: entryStream);

                    actualEmbeddedItemsCount++;
                }
            }
        });

        if (actualEmbeddedItemsCount == 0)
        {
            map.EmbeddedZipData = null;
        }

        LogFinishedEmbeddedItems(watch, actualEmbeddedItemsCount);

        return userDataPackFilePath;
    }

    private ZipArchiveEntry? CreateEntryFromGbxFile(ZipArchive zip, string path)
    {
        var itemPath = Path.Combine(runningDir, "UserData", path);

        if (!File.Exists(itemPath))
        {
            return null;
        }

        var entry = zip.CreateEntry(path, CompressionLevel.SmallestSize);
        using var entryStream = entry.Open();
        using var fileStream = File.OpenRead(itemPath);
        Gbx.Decompress(input: fileStream, output: entryStream);

        return entry;
    }

    private void LogFinishedEmbeddedItems(Stopwatch watch, int actualEmbeddedItemsCount)
    {
        if (actualEmbeddedItemsCount == embeddedFilePaths.Count)
        {
            logger.LogInformation("Embedded {ActualEmbedded}/{ExpectedEmbedded} items in {ElapsedMilliseconds}ms.",
                actualEmbeddedItemsCount,
                embeddedFilePaths.Count,
                watch.ElapsedMilliseconds);
        }
        else
        {
            logger.LogWarning("Embedded {ActualEmbedded}/{ExpectedEmbedded} items in {ElapsedMilliseconds}ms.",
                actualEmbeddedItemsCount,
                embeddedFilePaths.Count,
                watch.ElapsedMilliseconds);
        }

        if (map.EmbeddedZipData is { Length: > 0 })
        {
            logger.LogInformation("Embedded size: {Size}", ByteSize.FromBytes(map.EmbeddedZipData.Length));
        }
        else
        {
            logger.LogWarning("No items were embedded.");
        }
    }
}
