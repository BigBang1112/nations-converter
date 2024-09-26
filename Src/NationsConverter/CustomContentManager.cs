using ByteSizeLib;
using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using Microsoft.Extensions.Logging;
using NationsConverter.Converters;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;

namespace NationsConverter;

internal sealed class CustomContentManager : EnvironmentConverterBase
{
    private const int ItemCollection = 26;

    private readonly ConcurrentDictionary<string, string> itemModelAuthors = [];
    private readonly HashSet<string> embeddedFilePaths = [];

    private readonly CGameCtnChallenge mapOut;
    private readonly string runningDir;
    private readonly NationsConverterConfig config;
    private readonly ILogger logger;

    private readonly string category;
    private readonly string subCategory;
    private readonly string technology;
    private readonly string baseItemPath;

    private const string NC2 = "NC2";

    public CustomContentManager(
        CGameCtnChallenge mapIn,
        CGameCtnChallenge mapOut,
        string runningDir,
        NationsConverterConfig config,
        ILogger logger) : base(mapIn)
    {
        this.mapOut = mapOut;
        this.runningDir = runningDir;
        this.config = config;
        this.logger = logger;

        category = string.IsNullOrWhiteSpace(config.Category) ? Environment switch
        {
            "Stadium" => "Crystal",
            _ => "Solid"
        } : config.Category;

        subCategory = string.IsNullOrWhiteSpace(config.SubCategory) ? Environment switch
        {
            "Stadium" => "Modernized",
            _ => "Modless"
        } : config.SubCategory;

        technology = "MM_Collision";

        baseItemPath = Path.Combine(NC2, category, subCategory, technology, Environment);
    }

    public void PlaceItem(string itemModel, Vec3 pos, Vec3 rot)
    {
        var itemPath = Path.Combine(baseItemPath, itemModel);

        // retrieve author login (and collection in the future) from the item model gbx
        // cache this item model in dictionary
        if (!itemModelAuthors.TryGetValue(itemPath, out var itemModelAuthor))
        {
            var fullItemPath = Path.Combine(runningDir, "UserData", "Items", itemPath);

            itemModelAuthor = File.Exists(fullItemPath)
                ? Gbx.ParseHeaderNode<CGameItemModel>(fullItemPath).Ident.Author
                : "akPfIM0aSzuHuaaDWptBbQ";

            // add file to hash set, so that it can be found + properly placed in the zip
            embeddedFilePaths.Add(Path.Combine("Items", itemPath));
        }

        mapOut.PlaceAnchoredObject(new(itemPath.Replace('/', '\\'), ItemCollection, itemModelAuthor), pos, rot);
    }

    public CGameCtnBlock PlaceBlock(string blockModel, Int3 coord, Direction dir, bool isGround = false, byte variant = 0, byte subVariant = 0)
    {
        var blockPath = Path.Combine(NC2, $"{blockModel}.Block.Gbx").Replace('/', '\\');

        var block = mapOut.PlaceBlock($"{blockPath}_CustomBlock", coord, dir, isGround, variant, subVariant);

        embeddedFilePaths.Add(Path.Combine("Blocks", blockPath));

        return block;
    }

    public CGameCtnBlock PlaceBlock(string blockModel, Vec3 pos, Vec3 rot, bool isGround = false, byte variant = 0, byte subVariant = 0)
    {
        var blockPath = Path.Combine(NC2, $"{blockModel}.Block.Gbx").Replace('/', '\\');

        var block = mapOut.PlaceBlock($"{blockPath}_CustomBlock", (-1, 0, -1), Direction.North, isGround, variant, subVariant);

        block.IsFree = true;
        block.AbsolutePositionInMap = pos;
        block.PitchYawRoll = rot;

        embeddedFilePaths.Add(Path.Combine("Blocks", blockPath));

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
            mapOut.UpdateEmbeddedZipData(zip =>
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

        mapOut.UpdateEmbeddedZipData(zip =>
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

                    continue;
                }

                var entry = zip.CreateEntry(path, CompressionLevel.SmallestSize);
                using var entryStream = entry.Open();
                using var toEmbedEntryStream = toEmbedEntry.Open();
                Gbx.Decompress(input: toEmbedEntryStream, output: entryStream);

                actualEmbeddedItemsCount++;
            }
        });

        if (actualEmbeddedItemsCount == 0)
        {
            mapOut.EmbeddedZipData = null;
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

        if (mapOut.EmbeddedZipData is { Length: > 0 })
        {
            logger.LogInformation("Embedded size: {Size}", ByteSize.FromBytes(mapOut.EmbeddedZipData.Length));
        }
        else
        {
            logger.LogWarning("No items were embedded.");
        }
    }
}
