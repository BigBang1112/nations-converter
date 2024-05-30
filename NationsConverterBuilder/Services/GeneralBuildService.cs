using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
using GBX.NET.Imaging.SkiaSharp;
using NationsConverterBuilder.Models;
using SkiaSharp;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace NationsConverterBuilder.Services;

internal sealed class GeneralBuildService
{
    private readonly SetupService setupService;
    private readonly ItemMakerService itemMaker;

    public GeneralBuildService(SetupService setupService, ItemMakerService itemMaker)
    {
        this.setupService = setupService;
        this.itemMaker = itemMaker;
    }

    public async Task BuildAsync(CancellationToken cancellationToken = default)
    {
        await Parallel.ForEachAsync(setupService.Collections.Values, cancellationToken, async (collection, cancellationToken) =>
        {
            await setupService.SetupCollectionAsync(collection, cancellationToken);

            RecurseBlockDirectories(collection.DisplayName, collection.BlockDirectories);
            GenerateBlocks(collection.DisplayName, collection.RootBlocks);
        });
    }

    private void RecurseBlockDirectories(string collectionName, IDictionary<string, BlockDirectoryModel> dirs)
    {
        foreach (var (dirName, dir) in dirs)
        {
            RecurseBlockDirectories(collectionName, dir.Directories);
            GenerateBlocks(collectionName, dir.Blocks);
        }
    }

    private void GenerateBlocks(string collectionName, IDictionary<string, BlockInfoModel> blocks)
    {
        foreach (var (name, block) in blocks)
        {
            block.Node = (CGameCtnBlockInfo)Gbx.ParseNode(block.GbxFilePath)!;

            var webpData = RawIconToWebpIcon(block.Node);

            var dirPath = string.IsNullOrWhiteSpace(block.Node.PageName)
                ? Path.Combine("E:\\TrackmaniaUserData\\Items\\NC2OUTPUT", collectionName, "Other", name)
                : Path.Combine("E:\\TrackmaniaUserData\\Items\\NC2OUTPUT", collectionName, block.Node.PageName, name);

            for (int i = 0; i < block.Node.GroundMobils!.Length; i++)
            {
                var groundMobilSubVariants = block.Node.GroundMobils![i];

                for (int j = 0; j < groundMobilSubVariants.Length; j++)
                {
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
