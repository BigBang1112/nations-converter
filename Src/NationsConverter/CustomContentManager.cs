using ByteSizeLib;
using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using Microsoft.Extensions.Logging;
using NationsConverter.Stages;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;

namespace NationsConverter;

internal sealed class CustomContentManager : EnvironmentStageBase
{
    private const int ItemCollection = 26;

    private readonly ConcurrentDictionary<string, string> itemModelAuthors = [];
    private readonly HashSet<(string, string)> embeddedFilePaths = [];

    private readonly CGameCtnChallenge mapOut;
    private readonly string runningDir;
    private readonly NationsConverterConfig config;
    private readonly ILogger logger;

    private readonly string category;
    private readonly string subCategory;
    private readonly string technology;
    private readonly string rootFolderName;

    private const string NC2 = "NC2";

    public CustomContentManager(
        CGameCtnChallenge mapIn,
        CGameCtnChallenge mapOut,
        string runningDir,
        NationsConverterConfig config,
        uint seed,
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

        technology = Environment switch
        {
            "Stadium" => "MM",
            _ => "MM_Collision"
        };

        rootFolderName = config.CopyItems ? NC2 : $"{NC2}_{seed}";
    }

    public CGameCtnAnchoredObject PlaceItem(string itemModel, Vec3 pos, Vec3 rot, Vec3 pivot = default, bool modernized = false, string? technology = null, LightmapQuality lightmapQuality = LightmapQuality.Normal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemModel);

        var appliedSubCategory = modernized || category != "Crystal" || subCategory != "Modernized" ? subCategory : "Classic";

        var itemPath = Path.Combine(category, appliedSubCategory, technology ?? this.technology, Environment, itemModel);

        // retrieve author login (and collection in the future) from the item model gbx
        // cache this item model in dictionary
        if (!itemModelAuthors.TryGetValue(itemPath, out var itemModelAuthor))
        {
            var fullItemPath = Path.Combine(runningDir, "UserData", "Items", NC2, itemPath);

            itemModelAuthor = File.Exists(fullItemPath)
                ? Gbx.ParseHeaderNode<CGameItemModel>(fullItemPath).Ident.Author
                : "akPfIM0aSzuHuaaDWptBbQ";

            // add file to hash set, so that it can be found + properly placed in the zip
            embeddedFilePaths.Add(("Items", itemPath));
        }

        var item = mapOut.PlaceAnchoredObject(new(Path.Combine(rootFolderName, itemPath).Replace('/', '\\'), ItemCollection, itemModelAuthor), pos, rot, pivot);
        item.LightmapQuality = lightmapQuality;
        return item;
    }

    public CGameCtnBlock PlaceBlock(string blockModel, Int3 coord, Direction dir, bool isGround = false, byte variant = 0, byte subVariant = 0)
    {
        var blockPath = $"{blockModel.Replace('/', '\\')}.Block.Gbx";

        var block = mapOut.PlaceBlock($"{Path.Combine(rootFolderName, blockPath).Replace('/', '\\')}_CustomBlock", coord, dir, isGround, variant, subVariant);

        embeddedFilePaths.Add(("Blocks", blockPath));

        return block;
    }

    public CGameCtnBlock PlaceBlock(string blockModel, Vec3 pos, Vec3 rot, bool isGround = false, byte variant = 0, byte subVariant = 0)
    {
        var blockPath = $"{blockModel.Replace('/', '\\')}.Block.Gbx";

        var block = mapOut.PlaceBlock($"{Path.Combine(rootFolderName, blockPath).Replace('/', '\\')}_CustomBlock", (-1, 0, -1), Direction.North, isGround, variant, subVariant);

        block.IsFree = true;
        block.AbsolutePositionInMap = pos;
        block.PitchYawRoll = rot;

        embeddedFilePaths.Add(("Blocks", blockPath));

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
                foreach (var (embeddedType, remainingPath) in embeddedFilePaths)
                {
                    var loadPath = Path.Combine(embeddedType, NC2, remainingPath);
                    var embeddedPath = Path.Combine(embeddedType, rootFolderName, remainingPath);
                    if (CreateEntryFromGbxFile(zip, loadPath, embeddedPath) is null)
                    {
                        logger.LogWarning("File {Path} cannot be embedded because it does not exist.", remainingPath);
                    }
                    else
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
            foreach (var (embeddedType, remainingPath) in embeddedFilePaths)
            {
                var loadPath = Path.Combine(embeddedType, NC2, remainingPath);
                var embeddedPath = Path.Combine(embeddedType, rootFolderName, remainingPath);
                var toEmbedEntry = toEmbedZip.GetEntry(loadPath.Replace('\\', '/'));

                if (toEmbedEntry is null)
                {
                    if (CreateEntryFromGbxFile(zip, loadPath, embeddedPath) is null)
                    {
                        logger.LogWarning("File {Path} cannot be embedded because it does not exist.", remainingPath);
                    }
                    else
                    {
                        actualEmbeddedItemsCount++;
                    }

                    continue;
                }

                var entry = zip.CreateEntry(embeddedPath, CompressionLevel.SmallestSize);
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

    private ZipArchiveEntry? CreateEntryFromGbxFile(ZipArchive zip, string loadPath, string embeddedPath)
    {
        loadPath = Path.Combine(runningDir, "UserData", loadPath);

        if (!File.Exists(loadPath))
        {
            return null;
        }

        var entry = zip.CreateEntry(embeddedPath, CompressionLevel.SmallestSize);
        using var entryStream = entry.Open();
        using var fileStream = File.OpenRead(loadPath);
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

            if (logger.IsEnabled(LogLevel.Debug))
            {
                using var zip = mapOut.OpenReadEmbeddedZipData();

                logger.LogDebug("Embedded files:");

                foreach (var entry in zip.Entries.OrderByDescending(x => x.CompressedLength))
                {
                    var dirName = Path.GetFileName(Path.GetDirectoryName(entry.FullName));
                    var fileName = dirName is null ? entry.Name : Path.Combine(dirName, entry.Name);
                    logger.LogDebug("* {Path} ({Size})", fileName, ByteSize.FromBytes(entry.CompressedLength));
                }
            }
        }
        else
        {
            logger.LogWarning("No items were embedded.");
        }
    }
}
