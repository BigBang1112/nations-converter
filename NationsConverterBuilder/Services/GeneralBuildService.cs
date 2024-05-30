using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Plug;
using NationsConverterBuilder.Models;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace NationsConverterBuilder.Services;

internal sealed class GeneralBuildService
{
    private readonly SetupService setupService;
    private readonly ItemMakerService itemMaker;
    private readonly string dataDirPath;

    public GeneralBuildService(SetupService setupService, ItemMakerService itemMaker, IWebHostEnvironment hostEnvironment)
    {
        this.setupService = setupService;
        this.itemMaker = itemMaker;

        dataDirPath = Path.Combine(hostEnvironment.WebRootPath, "data");
    }

    public async Task BuildAsync(CancellationToken cancellationToken = default)
    {
        await Parallel.ForEachAsync(setupService.Collections.Values, cancellationToken, async (collection, cancellationToken) =>
        {
            await setupService.SetupCollectionAsync(collection, cancellationToken);

            var map = Gbx.ParseNode<CGameCtnChallenge>(Path.Combine(dataDirPath, "Base.Map.Gbx"));
            map.MapName = collection.DisplayName;

            var index = 0;

            RecurseBlockDirectories(collection.DisplayName, collection.BlockDirectories, map, ref index);
            GenerateBlocks(collection.DisplayName, collection.RootBlocks, map, ref index);

            var mapOutput = Path.Combine("E:\\TrackmaniaUserData\\Maps\\NC2OUTPUT");
            Directory.CreateDirectory(mapOutput);
            map.Save(Path.Combine(mapOutput, $"{collection.DisplayName}.Map.Gbx"));
        });
    }

    private void RecurseBlockDirectories(string collectionName, IDictionary<string, BlockDirectoryModel> dirs, CGameCtnChallenge map, ref int index)
    {
        foreach (var (dirName, dir) in dirs)
        {
            RecurseBlockDirectories(collectionName, dir.Directories, map, ref index);
            GenerateBlocks(collectionName, dir.Blocks, map, ref index);
        }
    }

    private void GenerateBlocks(string collectionName, IDictionary<string, BlockInfoModel> blocks, CGameCtnChallenge map, ref int index)
    {
        foreach (var (name, block) in blocks)
        {
            block.Node = (CGameCtnBlockInfo)Gbx.ParseNode(block.GbxFilePath)!;

            var webpData = RawIconToWebpIcon(block.Node);

            var pageName = string.IsNullOrWhiteSpace(block.Node.PageName) ? "Other" : block.Node.PageName;

            if (pageName[^1] is '/' or '\\')
            {
                pageName = pageName[..^1];
            }

            var dirPath = Path.Combine("E:\\TrackmaniaUserData\\Items\\NC2OUTPUT", collectionName, pageName, name);

            for (int i = 0; i < block.Node.GroundMobils!.Length; i++)
            {
                var groundMobilSubVariants = block.Node.GroundMobils![i];

                for (int j = 0; j < groundMobilSubVariants.Length; j++)
                {
                    map.PlaceAnchoredObject(
                        new($"NC2OUTPUT\\{collectionName}\\{pageName.Replace('/', '\\')}\\{name}\\{name}_Ground_{i}_{j}.Item.Gbx", 26, "akPfIM0aSzuHuaaDWptBbQ"),
                        (index / 64 * 128, 64, index % 64 * 128), (0, 0, 0));
                    index++;

                    GenerateSubVariant(new()
                    {
                        Node = groundMobilSubVariants[j],
                        BlockInfo = block.Node,
                        CollectionName = collectionName,
                        DirectoryPath = dirPath,
                        ModifierType = "Ground",
                        VariantIndex = i,
                        SubVariantIndex = j,
                        WebpData = webpData,
                        BlockName = name
                    });
                }
            }

            for (int i = 0; i < block.Node.AirMobils!.Length; i++)
            {
                var airMobilSubVariants = block.Node.AirMobils![i];

                for (int j = 0; j < airMobilSubVariants.Length; j++)
                {
                    map.PlaceAnchoredObject(
                        new($"NC2OUTPUT\\{collectionName}\\{pageName.Replace('/', '\\')}\\{name}\\{name}_Air_{i}_{j}.Item.Gbx", 26, "akPfIM0aSzuHuaaDWptBbQ"),
                        (index / 64 * 128, 64, index % 64 * 128), (0, 0, 0));
                    index++;

                    GenerateSubVariant(new()
                    {
                        Node = airMobilSubVariants[j],
                        BlockInfo = block.Node,
                        CollectionName = collectionName,
                        DirectoryPath = dirPath,
                        ModifierType = "Air",
                        VariantIndex = i,
                        SubVariantIndex = j,
                        WebpData = webpData,
                        BlockName = name
                    });
                }
            }
        }
    }

    private static byte[]? RawIconToWebpIcon(CGameCtnBlockInfo blockInfo)
    {
        if (blockInfo.Icon is null)
        {
            return null;
        }

        int length = blockInfo.Icon.GetLength(0);
        int length2 = blockInfo.Icon.GetLength(1);
        int[] array = new int[length * length2];
        for (int i = 0; i < length2; i++)
        {
            for (int j = 0; j < length; j++)
            {
                array[i * length + j] = blockInfo.Icon[j, length2 - i - 1].ToArgb();
            }
        }

        var sKBitmap = new SKBitmap();
        var gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
        var info = new SKImageInfo(length, length2, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
        sKBitmap.InstallPixels(info, gcHandle.AddrOfPinnedObject(), info.RowBytes, delegate
        {
            gcHandle.Free();
        });
        var iconStream = new MemoryStream();
        sKBitmap?.Encode(iconStream, SKEncodedImageFormat.Webp, 100);
        return iconStream.ToArray();
    }

    private void GenerateSubVariant(SubVariantModel subVariant)
    {
        if (subVariant.Node.Node?.Item?.Solid?.Tree is not CPlugSolid solid)
        {
            return;
        }

        Directory.CreateDirectory(subVariant.DirectoryPath);

        var crystal = itemMaker.CreateCrystalFromSolid(solid);
        var finalItem = itemMaker.Build(crystal, subVariant.WebpData, subVariant.BlockInfo.Ident.Collection.GetBlockSize());

        finalItem.Save(Path.Combine(subVariant.DirectoryPath, $"{subVariant.BlockName}_{subVariant.ModifierType}_{subVariant.VariantIndex}_{subVariant.SubVariantIndex}.Item.Gbx"));
    }
}
