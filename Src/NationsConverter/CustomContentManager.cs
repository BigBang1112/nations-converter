using ByteSizeLib;
using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
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
    private readonly Dictionary<string, LightPropertiesModel[]> itemLights = [];

    private readonly CGameCtnChallenge mapOut;
    private readonly string runningDir;
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
        this.logger = logger;

        category = config.GetUsedCategory(Environment);
        subCategory = config.GetUsedSubCategory(Environment);

        technology = Environment switch
        {
            "Stadium" => "MM",
            _ => "MM_Collision"
        };

        rootFolderName = config.UniqueEmbeddedFolder ? $"{NC2}_{seed}" : NC2;
    }

    public CGameCtnAnchoredObject PlaceItem(string itemModel, Vec3 pos, Vec3 rot, Vec3 pivot = default, bool modernized = false, string? technology = null, LightmapQuality lightmapQuality = LightmapQuality.Normal, LightPropertiesModel[]? lightProperties = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemModel);

        var appliedSubCategory = modernized || category != "Crystal" || subCategory != "Modernized" ? subCategory : "Classic";

        var itemPath = Path.Combine(category, appliedSubCategory, technology ?? this.technology, Environment, itemModel);

        return PlaceItemRaw(itemPath, pos, rot, pivot, lightmapQuality, lightProperties);
    }

    public CGameCtnAnchoredObject PlaceItemRaw(string itemPath, Vec3 pos, Vec3 rot, Vec3 pivot = default, LightmapQuality lightmapQuality = LightmapQuality.Normal, LightPropertiesModel[]? lightProperties = null)
    {
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

        if (lightProperties?.Length > 0)
        {
            // so that you can match it in CreateEntryFromLocalGbxFile/CreateEntryFromZippedGbxFile
            itemLights.TryAdd(Path.Combine("Items", rootFolderName, itemPath), lightProperties);
        }

        var item = mapOut.PlaceAnchoredObject(new(Path.Combine(rootFolderName, itemPath.Replace("�", "")).Replace('/', '\\'), ItemCollection, itemModelAuthor), pos, rot, pivot);
        item.BlockUnitCoord = new Byte3((byte)(pos.X / 32), (byte)(pos.Y / 8), (byte)(pos.Z / 32));
        item.LightmapQuality = lightmapQuality;
        return item;
    }

    public CGameCtnBlock PlaceBlock(string blockModel, Int3 coord, Direction dir, bool isGround = false, byte variant = 0, byte subVariant = 0)
    {
        var blockPath = $"{blockModel}.Block.Gbx";

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
        block.YawPitchRoll = rot;

        embeddedFilePaths.Add(("Blocks", blockPath));

        return block;
    }

    public void EmbedData()
    {
        logger.LogInformation("Embedding item data...");

        var watch = Stopwatch.StartNew();

        var userDataPath = Path.Combine(runningDir, "UserData");

        var zipStreams = Directory.GetFiles(userDataPath, "*.nc2")
            .Select(x => (Path.GetFileNameWithoutExtension(x), ZipFile.OpenRead(x)))
            .ToDictionary(x => x.Item1, x => x.Item2);

        var actualEmbeddedItemsCount = 0;

        mapOut.UpdateEmbeddedZipData(zip =>
        {
            foreach (var (embeddedType, remainingPath) in embeddedFilePaths)
            {
                var loadPath = Path.Combine(embeddedType, NC2, remainingPath);
                var embeddedPath = Path.Combine(embeddedType, rootFolderName, remainingPath.Replace("�", ""));

                var loadPathForEntry = loadPath.Replace('\\', '/');
                var toEmbedEntry = zipStreams.Values
                    .Select(x => x.GetEntry(loadPathForEntry))
                    .FirstOrDefault(x => x is not null);

                if (toEmbedEntry is not null)
                {
                    CreateEntryFromZippedGbxFile(zip, toEmbedEntry, embeddedPath);
                    actualEmbeddedItemsCount++;
                }
                else if (CreateEntryFromLocalGbxFile(zip, loadPath, embeddedPath) is not null)
                {
                    actualEmbeddedItemsCount++;
                }
                else
                {
                    logger.LogWarning("File {Path} cannot be embedded because it does not exist.", remainingPath);
                }
            }
        });

        if (actualEmbeddedItemsCount == 0)
        {
            mapOut.EmbeddedZipData = null;
        }

        LogFinishedEmbeddedItems(watch, actualEmbeddedItemsCount);

        foreach (var zip in zipStreams.Values)
        {
            zip.Dispose();
        }
    }

    private ZipArchiveEntry? CreateEntryFromLocalGbxFile(ZipArchive zip, string loadPath, string embeddedPath)
    {
        loadPath = Path.Combine(runningDir, "UserData", loadPath);

        if (!File.Exists(loadPath))
        {
            return null;
        }

        var entry = zip.CreateEntry(embeddedPath, CompressionLevel.SmallestSize);
        using var entryStream = entry.Open();
        using var fileStream = File.OpenRead(loadPath);

        if (!TryInjectLights(fileStream, entryStream, embeddedPath))
        {
            Gbx.Decompress(input: fileStream, output: entryStream);
        }

        return entry;
    }

    private ZipArchiveEntry CreateEntryFromZippedGbxFile(ZipArchive zip, ZipArchiveEntry entryFrom, string embeddedPath)
    {
        var entry = zip.CreateEntry(embeddedPath, CompressionLevel.SmallestSize);
        using var entryStream = entry.Open();
        using var toEmbedEntryStream = entryFrom.Open();

        if (!TryInjectLights(toEmbedEntryStream, entryStream, embeddedPath))
        {
            Gbx.Decompress(input: toEmbedEntryStream, output: entryStream);
        }

        return entry;
    }

    private bool TryInjectLights(Stream sourceItemGbxStream, Stream outputItemGbxStream, string embeddedPath)
    {
        if (!itemLights.TryGetValue(embeddedPath, out var lightProperties))
        {
            return false;
        }

        // 1. open item gbx fully (issue with "Failed to read compressed data" if not using MemoryStream)
        using var ms = new MemoryStream();
        sourceItemGbxStream.CopyTo(ms);
        ms.Position = 0;

        Gbx<CGameItemModel> itemGbx;

        try
        { 
            itemGbx = Gbx.Parse<CGameItemModel>(ms);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse item model Gbx for light injection: {Path}", embeddedPath);
            throw;
        }

        // 2. add light layer according to lightProperties
        if (itemGbx.Node.EntityModelEdition is not CGameCommonItemEntityModelEdition { MeshCrystal: not null } entityEdition)
        {
            return false;
        }

        var lightLayer = entityEdition.MeshCrystal
            .Layers
            .OfType<CPlugCrystal.LightLayer>()
            .FirstOrDefault();

        if (lightLayer is null)
        {
            lightLayer = new CPlugCrystal.LightLayer
            {
                Ver = 2,
                LayerName = "Light",
                LayerId = "Layer4",
                IsEnabled = true
            };
            entityEdition.MeshCrystal.Layers.Add(lightLayer);
        }

        var lights = lightProperties.Select(props =>
        {
            var userLight = new CPlugLightUserModel
            {
                Color = props.Color,
                Distance = props.Distance,
                Intensity = props.Intensity,
                NightOnly = props.NightOnly,
                SpotInnerAngle = props.SpotInnerAngle,
                SpotOuterAngle = props.SpotOuterAngle,
            };
            userLight.CreateChunk<CPlugLightUserModel.Chunk090F9000>().Version = 1;
            return userLight;
        });

        var prevLightCount = lightLayer.Lights?.Length ?? 0;

        var lightPositions = lightProperties.Select((x, i) => new CPlugCrystal.LightPos
        {
            U01 = i + prevLightCount, // index of the light
            U02 = new(0, 0, 0, 0, 0, 0, 0, 0, 0, x.Position.X, x.Position.Y, x.Position.Z)
        });

        lightLayer.Lights = (lightLayer.Lights ?? []).Concat(lights).ToArray();
        lightLayer.LightPositions = (lightLayer.LightPositions ?? []).Concat(lightPositions).ToArray();

        // 3. write uncompressed to outputItemGbxStream
        itemGbx.BodyCompression = GbxCompression.Uncompressed;
        itemGbx.Save(outputItemGbxStream);

        return true;
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
