using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Plug;
using GBX.NET.Imaging.SkiaSharp;
using NationsConverterShared.Converters.Json;
using NationsConverterBuilder.Models;
using NationsConverterShared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NationsConverterBuilder.Services;

internal sealed class InitStageService
{
    private readonly SetupService setupService;
    private readonly ItemMakerService itemMaker;
    private readonly ILogger<InitStageService> logger;

    private readonly string dataDirPath;
    private readonly string itemsDirPath;
    private readonly string sheetsDirPath;
    private readonly string? initItemsOutputPath;
    private readonly string? initMapsOutputPath;

    private static readonly string[] subCategories = ["Balanced", "Mod", "Modless"];

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static InitStageService()
    {
        jsonOptions.Converters.Add(new JsonInt3Converter());
    }

    public InitStageService(
        SetupService setupService,
        ItemMakerService itemMaker,
        IWebHostEnvironment hostEnvironment,
        IConfiguration config,
        ILogger<InitStageService> logger)
    {
        this.setupService = setupService;
        this.itemMaker = itemMaker;
        this.logger = logger;

        dataDirPath = Path.Combine(hostEnvironment.WebRootPath, "data");
        itemsDirPath = Path.Combine(hostEnvironment.WebRootPath, "items");
        sheetsDirPath = Path.Combine(hostEnvironment.WebRootPath, "sheets");

        initItemsOutputPath = config["InitItemsOutputPath"];
        initMapsOutputPath = config["InitMapsOutputPath"];
    }

    public async Task BuildAsync(CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>(setupService.Collections.Values.Count);

        foreach (var collection in setupService.Collections.Values)
        {
            setupService.SetupCollection(collection);

            tasks.Add(Parallel.ForEachAsync(subCategories, cancellationToken, async (subCategory, cancellationToken) =>
            {
                var displayName = collection.DisplayName;

                var map = Gbx.ParseNode<CGameCtnChallenge>(Path.Combine(dataDirPath, "Base.Map.Gbx"));
                map.MapName = displayName;
                map.ScriptMetadata!.Declare("MadeWithNationsConverter", true);
                map.ScriptMetadata!.Declare("NC2_IsConverted", true);
                map.ScriptMetadata!.Declare("NC2_ConvertedAt", DateTime.UtcNow.ToString("s"));

				var index = 0;

                var convs = new Dictionary<string, ConversionModel>();

                RecurseBlockDirectories(displayName, collection.BlockDirectories, map, subCategory, convs, ref index);
                ProcessBlocks(displayName, collection.RootBlocks, map, subCategory, convs, ref index);

                using (var sheetJsonStream = new FileStream(Path.Combine(sheetsDirPath, $"{displayName}.json"), FileMode.Create, FileAccess.Write, FileShare.Write, 4096, useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(sheetJsonStream, convs, jsonOptions, cancellationToken: cancellationToken);
                }

                if (!string.IsNullOrEmpty(initMapsOutputPath))
                {
                    Directory.CreateDirectory(Path.Combine(initMapsOutputPath, subCategory));
                    map.Save(Path.Combine(initMapsOutputPath, subCategory, $"{displayName}.Map.Gbx"));
                }
            }));
        }

        await Task.WhenAll(tasks);
    }

    private void RecurseBlockDirectories(
        string collectionName,
        IDictionary<string, BlockDirectoryModel> dirs,
        CGameCtnChallenge map,
        string subCategory,
        IDictionary<string, ConversionModel> convs,
        ref int index)
    {
        foreach (var (dirName, dir) in dirs)
        {
            RecurseBlockDirectories(collectionName, dir.Directories, map, subCategory, convs, ref index);
            ProcessBlocks(collectionName, dir.Blocks, map, subCategory, convs, ref index);
        }
    }

    private void ProcessBlocks(
        string collectionName,
        IDictionary<string, BlockInfoModel> blocks,
        CGameCtnChallenge map,
        string subCategory,
        IDictionary<string, ConversionModel> convs,
        ref int index)
    {
        foreach (var (name, block) in blocks)
        {
            var conv = ProcessBlock(collectionName, map, block, subCategory, ref index);

            if (conv is not null)
            {
                convs.Add(name, conv);
            }
        }
    }

    private ConversionModel? ProcessBlock(string collectionName, CGameCtnChallenge map, BlockInfoModel block, string subCategory, ref int index)
    {
        var node = (CGameCtnBlockInfo?)Gbx.ParseNode(block.GbxFilePath);

        if (node is null)
        {
            logger.LogError("Failed to parse block info for {BlockName}", block.Name);
            return null;
        }

        node.UpgradeIconToWebP();

        var pageName = string.IsNullOrWhiteSpace(node.PageName) ? "Other" : node.PageName;

        if (pageName[^1] is '/' or '\\')
        {
            pageName = pageName[..^1];
        }

        var dirPath = Path.Combine("NC2", "Solid", subCategory, "MM_Collision", collectionName, pageName, block.Name);

        if (node.GroundMobils is not null)
        {
            for (byte i = 0; i < node.GroundMobils.Length; i++)
            {
                var groundMobilSubVariants = node.GroundMobils[i];

                for (byte j = 0; j < groundMobilSubVariants.Length; j++)
                {
                    ProcessSubVariant(new()
                    {
                        Node = groundMobilSubVariants[j],
                        BlockInfo = node,
                        CollectionName = collectionName,
                        DirectoryPath = dirPath,
                        ModifierType = "Ground",
                        VariantIndex = i,
                        SubVariantIndex = j,
                        WebpData = node.IconWebP,
                        BlockName = block.Name,
                        SubCategory = subCategory
                    }, map, ref index);

                    index++;
                }
            }
        }

        if (node.AirMobils is not null)
        {
            for (byte i = 0; i < node.AirMobils.Length; i++)
            {
                var airMobilSubVariants = node.AirMobils[i];

                for (byte j = 0; j < airMobilSubVariants.Length; j++)
                {
                    ProcessSubVariant(new()
                    {
                        Node = airMobilSubVariants[j],
                        BlockInfo = node,
                        CollectionName = collectionName,
                        DirectoryPath = dirPath,
                        ModifierType = "Air",
                        VariantIndex = i,
                        SubVariantIndex = j,
                        WebpData = node.IconWebP,
                        BlockName = block.Name,
                        SubCategory = subCategory
                    }, map, ref index);

                    index++;
                }
            }
        }

        return GetBlockConversionModel(node, pageName);
    }

    private void ProcessSubVariant(SubVariantModel subVariant, CGameCtnChallenge map, ref int index)
    {
        if (subVariant.Node.Node?.Item?.Solid?.Tree is not CPlugSolid solid)
        {
            logger.LogError("Failed to get solid for {BlockName} {ModifierType} {VariantIndex} {SubVariantIndex}", subVariant.BlockName, subVariant.ModifierType, subVariant.VariantIndex, subVariant.SubVariantIndex);
            return;
        }

        Directory.CreateDirectory(Path.Combine(itemsDirPath, subVariant.DirectoryPath));

        var crystal = itemMaker.CreateCrystalFromSolid(solid, subVariant.SubCategory);
        var finalItem = itemMaker.Build(crystal, subVariant.WebpData, subVariant.BlockInfo.Ident.Collection.GetBlockSize());

        var itemPath = Path.Combine(subVariant.DirectoryPath, $"{subVariant.ModifierType}_{subVariant.VariantIndex}_{subVariant.SubVariantIndex}.Item.Gbx");

        finalItem.Save(Path.Combine(itemsDirPath, itemPath));

        if (!string.IsNullOrEmpty(initItemsOutputPath))
        {
            Directory.CreateDirectory(Path.Combine(initItemsOutputPath, subVariant.DirectoryPath));
            File.Copy(Path.Combine(itemsDirPath, itemPath), Path.Combine(initItemsOutputPath, itemPath), true);
        }

        map.PlaceAnchoredObject(
            new(itemPath.Replace('/', '\\'), 26, "akPfIM0aSzuHuaaDWptBbQ"),
                (index / 32 * 128, 64, index % 32 * 128), (0, 0, 0));
    }

    private static ConversionModel GetBlockConversionModel(CGameCtnBlockInfo node, string pageName)
    {
        var airUnits = node.AirBlockUnitInfos?.Select(x => x.RelativeOffset).ToArray() ?? [];
        var groundUnits = node.GroundBlockUnitInfos?.Select(x => x.RelativeOffset).ToArray() ?? [];

        var airSize = airUnits.Length > 0 ? new Int3(airUnits.Max(x => x.X) + 1, airUnits.Max(x => x.Y) + 1, airUnits.Max(x => x.Z) + 1) : default(Int3?);
        var groundSize = groundUnits.Length > 0 ? new Int3(groundUnits.Max(x => x.X) + 1, groundUnits.Max(x => x.Y) + 1, groundUnits.Max(x => x.Z) + 1) : default(Int3?);

        var airVariants = node.AirMobils?.Length ?? 0;
        var groundVariants = node.GroundMobils?.Length ?? 0;

        var airSubVariants = node.AirMobils?.Select(x => x.Length == 0 ? default(int?) : x.Length).ToArray() ?? [];
        var groundSubVariants = node.GroundMobils?.Select(x => x.Length == 0 ? default(int?) : x.Length).ToArray() ?? [];

        var airClips = GetConversionClipModels(node.AirBlockUnitInfos).ToArray();
        var groundClips = GetConversionClipModels(node.GroundBlockUnitInfos).ToArray();

        var commonUnits = airUnits.SequenceEqual(groundUnits) ? airUnits : null;
        var commonSize = airSize == groundSize ? airSize : null;
        var commonVariants = airVariants == groundVariants ? airVariants : default(int?);
        var commonSubVariants = airSubVariants.SequenceEqual(groundSubVariants) ? airSubVariants : null;
        var commonClips = airClips.SequenceEqual(groundClips) ? airClips : null;

        var airConvModel = default(ConversionModifierModel);
        var groundConvModel = default(ConversionModifierModel);

        if (airUnits.Length > 0)
        {
            airConvModel = new()
            {
                Units = commonUnits is null && airUnits.Length > 0 ? airUnits : null,
                Size = commonSize is null ? airSize : null,
                Variants = commonVariants is null ? airVariants : null,
                SubVariants = commonSubVariants is null && airSubVariants.Length > 0 ? airSubVariants : null,
                Clips = commonClips is null && airClips.Length > 0 ? airClips : null
            };
        }

        if (groundUnits.Length > 0)
        {
            groundConvModel = new()
            {
                Units = commonUnits is null && groundUnits.Length > 0 ? groundUnits : null,
                Size = commonSize is null ? groundSize : null,
                Variants = commonVariants is null ? groundVariants : null,
                SubVariants = commonSubVariants is null && groundSubVariants.Length > 0 ? groundSubVariants : null,
                Clips = commonClips is null && groundClips.Length > 0 ? groundClips : null
            };
        }

        return new ConversionModel
        {
            PageName = pageName,
            Units = commonUnits?.Length == 0 ? null : commonUnits,
            Size = commonSize,
            Variants = commonVariants,
            SubVariants = commonSubVariants?.Length == 0 ? null : commonSubVariants,
            Clips = commonClips?.Length == 0 ? null : commonClips,
            Air = airConvModel,
            Ground = groundConvModel
        };
    }

    private static IEnumerable<ConversionClipModel> GetConversionClipModels(CGameCtnBlockUnitInfo[]? blockUnitInfos)
    {
        if (blockUnitInfos is null)
        {
            yield break;
        }

        foreach (var unit in blockUnitInfos)
        {
            if (unit.Clips is null)
            {
                continue;
            }

            for (var i = 0; i < unit.Clips.Length; i++)
            {
                var clip = unit.Clips[i];
                if (clip.Node is not null)
                {
                    yield return new ConversionClipModel
                    {
                        Name = clip.Node.Ident.Id,
                        Offset = unit.RelativeOffset,
                        Dir = (Direction)i
                    };
                }
            }
        }
    }
}
