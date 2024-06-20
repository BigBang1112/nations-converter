using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Plug;
using GBX.NET.Imaging.SkiaSharp;
using NationsConverterBuilder.Models;

namespace NationsConverterBuilder.Services;

internal sealed class InitStageService
{
    private readonly SetupService setupService;
    private readonly ItemMakerService itemMaker;
    private readonly ILogger<InitStageService> logger;

    private readonly string dataDirPath;
    private readonly string itemsDirPath;
    private readonly string? initItemsOutputPath;
    private readonly string? initMapsOutputPath;

    private static readonly string[] subCategories = ["Balanced", "Mod", "Modless"];

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

        initItemsOutputPath = config["InitItemsOutputPath"];
        initMapsOutputPath = config["InitMapsOutputPath"];
    }

    public async Task BuildAsync(CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>(setupService.Collections.Values.Count);

        foreach (var collection in setupService.Collections.Values)
        {
            setupService.SetupCollection(collection);

            tasks.Add(Parallel.ForEachAsync(subCategories, cancellationToken, (subCategory, cancellationToken) =>
            {
                var map = Gbx.ParseNode<CGameCtnChallenge>(Path.Combine(dataDirPath, "Base.Map.Gbx"));
                map.MapName = collection.DisplayName;
                map.ScriptMetadata!.Declare("NC2_IsConverted", true);

                var index = 0;

                RecurseBlockDirectories(collection.DisplayName, collection.BlockDirectories, map, subCategory, ref index);
                ProcessBlocks(collection.DisplayName, collection.RootBlocks, map, subCategory, ref index);

                if (!string.IsNullOrEmpty(initMapsOutputPath))
                {
                    Directory.CreateDirectory(Path.Combine(initMapsOutputPath, subCategory));
                    map.Save(Path.Combine(initMapsOutputPath, subCategory, $"{collection.DisplayName}.Map.Gbx"));
                }

                return ValueTask.CompletedTask;
            }));
        }

        await Task.WhenAll(tasks);
    }

    private void RecurseBlockDirectories(string collectionName, IDictionary<string, BlockDirectoryModel> dirs, CGameCtnChallenge map, string subCategory, ref int index)
    {
        foreach (var (dirName, dir) in dirs)
        {
            RecurseBlockDirectories(collectionName, dir.Directories, map, subCategory, ref index);
            ProcessBlocks(collectionName, dir.Blocks, map, subCategory, ref index);
        }
    }

    private void ProcessBlocks(string collectionName, IDictionary<string, BlockInfoModel> blocks, CGameCtnChallenge map, string subCategory, ref int index)
    {
        foreach (var (_, block) in blocks)
        {
            ProcessBlock(collectionName, map, block, subCategory, ref index);
        }
    }

    private void ProcessBlock(string collectionName, CGameCtnChallenge map, BlockInfoModel block, string subCategory, ref int index)
    {
        var node = (CGameCtnBlockInfo?)Gbx.ParseNode(block.GbxFilePath);

        if (node is null)
        {
            logger.LogError("Failed to parse block info for {BlockName}", block.Name);
            return;
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
}
