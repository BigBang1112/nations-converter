﻿using GBX.NET;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using Microsoft.Extensions.Options;
using NationsConverterBuilder.Extensions;

namespace NationsConverterBuilder.Services;

internal sealed class ItemMakerService
{
    private readonly UvModifierService uvModifierService;
    private readonly IOptions<InitOptions> initOptions;
    private readonly ILogger<ItemMakerService> logger;

    public ItemMakerService(UvModifierService uvModifierService, IOptions<InitOptions> initOptions, ILogger<ItemMakerService> logger)
    {
        this.uvModifierService = uvModifierService;
        this.initOptions = initOptions;
        this.logger = logger;
    }

    public CPlugCrystal CreateCrystalFromSolid(CPlugSolid solid, string subCategory)
    {
        if (solid.Tree is not CPlugTree tree)
        {
            throw new ArgumentException("Solid must have a tree");
        }

        var matDict = new Dictionary<string, CPlugMaterialUserInst>();

        return tree.ToCrystal(matFile =>
        {
            var matName = GbxPath.GetFileNameWithoutExtension(matFile.FilePath);

            if (matDict.TryGetValue(matName, out var mat))
            {
                return mat;
            }

            if (!initOptions.Value.Materials.TryGetValue(matName, out var material))
            {
                return matDict[matName] = CPlugMaterialUserInstExtensions.Create();
            }

            if (material.Remove)
            {
                return null;
            }

            if (material.SubCategories.TryGetValue(subCategory, out var subCategoryMaterial))
            {
                if (subCategoryMaterial.Remove)
                {
                    return null;
                }

                if (!string.IsNullOrEmpty(subCategoryMaterial.Link))
                {
                    return matDict[matName] = CPlugMaterialUserInstExtensions.Create($"Stadium\\Media\\{subCategoryMaterial.Link}", material.Surface ?? CPlugSurface.MaterialId.Concrete, subCategoryMaterial.Color);
                }
            }

            if (!string.IsNullOrEmpty(material.Link))
            {
                return matDict[matName] = CPlugMaterialUserInstExtensions.Create($"Stadium\\Media\\{material.Link}", material.Surface ?? CPlugSurface.MaterialId.Concrete, material.Color);
            }

            return matDict[matName] = CPlugMaterialUserInstExtensions.Create();
        },
        (matFile, uvSetIndex, uvs) =>
        {
            var matName = GbxPath.GetFileNameWithoutExtension(matFile.FilePath);

            if (!initOptions.Value.Materials.TryGetValue(matName, out var material) || material.UvModifiers is null)
            {
                return;
            }

            uvModifierService.ApplyUvModifiers(uvs, material.UvModifiers);

            if (material.SubCategories.TryGetValue(subCategory, out var subCategoryMaterial))
            {
                if (subCategoryMaterial.UvModifiers is not null)
                {
                    uvModifierService.ApplyUvModifiers(uvs, subCategoryMaterial.UvModifiers);
                }
            }
        }, lod: 0, logger);
    }

    public CGameItemModel Build(CPlugCrystal plugCrystal, byte[]? webpData, Int3 blockSize)
    {
        var entityModelEdition = new CGameCommonItemEntityModelEdition
        {
            InventoryOccupation = 1000,
            ItemType = CGameCommonItemEntityModelEdition.EItemType.Ornament,
            MeshCrystal = plugCrystal
        };
        var chunk000 = entityModelEdition.CreateChunk<CGameCommonItemEntityModelEdition.Chunk2E026000>();
        chunk000.Version = 8;
        chunk000.U14 = Iso4.Identity;
        chunk000.U15 = true;
        entityModelEdition.CreateChunk<CGameCommonItemEntityModelEdition.Chunk2E026001>().Data = new byte[8];

        var placementParams = new CGameItemPlacementParam
        {
            Flags = 1,
            FlyVStep = blockSize.Y,
            GridSnapHOffset = blockSize.X / 2,
            GridSnapHStep = blockSize.X,
            GridSnapVStep = blockSize.Y,
            PivotPositions = [(blockSize.X / 2, 0, blockSize.X / 2)],
            PivotSnapDistance = -1,
            PlacementClass = new()
            {
                Version = 10,
                AlignToInterior = true,
                WorldDir = (0, 0, 1)
            }
        };
        placementParams.CreateChunk<CGameItemPlacementParam.Chunk2E020000>();
        placementParams.CreateChunk<CGameItemPlacementParam.Chunk2E020001>();
        placementParams.CreateChunk<CGameItemPlacementParam.Chunk2E020004>().Data = new byte[8];
        placementParams.CreateChunk<CGameItemPlacementParam.Chunk2E020005>();

        var item = new CGameItemModel
        {
            CatalogPosition = 1,
            Flags = (CGameCtnCollector.ECollectorFlags)16,
            Ident = new Ident("", 26, "akPfIM0aSzuHuaaDWptBbQ"),
            ItemType = CGameItemModel.EItemType.Ornament,
            ItemTypeE = CGameItemModel.EItemType.Ornament,
            NadeoSkinFids = new GBX.NET.Engines.MwFoundations.CMwNod[7],
            Name = "New Item",
            Description = "No Description",
            DefaultPlacement = placementParams,
            OrbitalPreviewAngle = 0.15f,
            OrbitalRadiusBase = -1,
            PageName = "Items",
            ProdState = CGameCtnCollector.EProdState.Release,
            EntityModelEdition = entityModelEdition,
            WaypointType = CGameItemModel.EWaypointType.None,
            IconWebP = webpData
        };
        item.CreateChunk<CGameCtnCollector.HeaderChunk2E001003>().Version = 8;
        var chunk004 = item.CreateChunk<CGameCtnCollector.HeaderChunk2E001004>();
        chunk004.U01 = 1;
        chunk004.IsHeavy = true;
        item.CreateChunk<CGameCtnCollector.HeaderChunk2E001006>();
        item.CreateChunk<CGameItemModel.HeaderChunk2E002000>();
        item.CreateChunk<CGameItemModel.HeaderChunk2E002001>();
        item.CreateChunk<CGameCtnCollector.Chunk2E001009>();
        item.CreateChunk<CGameCtnCollector.Chunk2E00100B>();
        item.CreateChunk<CGameCtnCollector.Chunk2E00100C>();
        item.CreateChunk<CGameCtnCollector.Chunk2E00100D>();
        item.CreateChunk<CGameCtnCollector.Chunk2E001010>().Version = 4;
        item.CreateChunk<CGameCtnCollector.Chunk2E001011>().Version = 1;
        item.CreateChunk<CGameCtnCollector.Chunk2E001012>().U02 = 1;
        item.CreateChunk<CGameItemModel.Chunk2E002008>();
        item.CreateChunk<CGameItemModel.Chunk2E002009>();
        item.CreateChunk<CGameItemModel.Chunk2E00200C>();
        item.CreateChunk<CGameItemModel.Chunk2E002012>();
        item.CreateChunk<CGameItemModel.Chunk2E002015>();
        item.CreateChunk<CGameItemModel.Chunk2E002019>().Version = 15;
        item.CreateChunk<CGameItemModel.Chunk2E00201A>();
        item.CreateChunk<CGameItemModel.Chunk2E00201C>().Version = 5;
        var chunk01E = item.CreateChunk<CGameItemModel.Chunk2E00201E>();
        chunk01E.Version = 7;
        chunk01E.U02 = -1;
        var chunk01F = item.CreateChunk<CGameItemModel.Chunk2E00201F>();
        chunk01F.Version = 12;
        chunk01F.U07 = -1;
        chunk01F.U09 = -1;
        chunk01F.U10 = -1;
        var chunk020 = item.CreateChunk<CGameItemModel.Chunk2E002020>();
        chunk020.Version = 3;
        chunk020.U03 = true;
        item.CreateChunk<CGameItemModel.Chunk2E002025>();
        item.CreateChunk<CGameItemModel.Chunk2E002026>();
        item.CreateChunk<CGameItemModel.Chunk2E002027>();

        return item;
    }

    private static IEnumerable<CPlugTree> GetAllChildren(CPlugTree tree)
    {
        if (tree.Children is null)
        {
            yield break;
        }

        foreach (var child in tree.Children)
        {
            if (child is CPlugTreeVisualMip mip)
            {
                foreach (var descendant in GetAllChildren(mip.Levels.OrderBy(x => x.Key).First().Value))
                {
                    yield return descendant;
                }

                continue;
            }

            yield return child;

            foreach (var descendant in GetAllChildren(child))
            {
                yield return descendant;
            }
        }
    }
}