﻿using GBX.NET;
using GBX.NET.Components;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
using Microsoft.Extensions.Options;
using NationsConverterBuilder.Extensions;
using NationsConverterShared.Models;
using System.Text.Json;

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

    public CPlugCrystal CreateCrystalFromSolid(CPlugSolid solid, CSceneObjectLink[]? objectLinks, Iso4? spawnLoc, string subCategory, string? modifier = null, Func<CPlugTree, bool>? skipTreeWhen = null)
    {
        if (solid.Tree is not CPlugTree tree)
        {
            throw new ArgumentException("Solid must have a tree");
        }

        var matDict = new Dictionary<string, CPlugMaterialUserInst>();

        return tree.ToCrystal((mat, matFile) =>
        {
            return GetMaterialLink(mat, matFile, matDict, subCategory, modifier);
        },
        (matFile, uvSetIndex, uvs) =>
        {
            ApplyUvModifiers(matFile, uvSetIndex, uvs, subCategory);
        },
        lod: 0,
        objectLinks,
        spawnLoc,
        mergeVerticesDigitThreshold: 3,
        matFile => GetDecalLink(matFile, matDict, subCategory),
        (matFile, uvSetIndex, uvs) =>
        {
            ApplyDecalUvModifiers(matFile, uvSetIndex, uvs, subCategory);
        },
        skipTreeWhen,
        noAdditions: modifier is not null,
        logger);
    }

    public CPlugStaticObjectModel CreateStaticObjectFromSolid(CPlugSolid solid, string subCategory)
    {
        var matDict = new Dictionary<string, CPlugMaterialUserInst>();

        return solid.ToStaticObject((mat, matFile) =>
        {
            return GetMaterialLink(mat, matFile, matDict, subCategory, modifier: null);
        },
        (matFile, uvSetIndex, uvs) =>
        {
            ApplyUvModifiers(matFile, uvSetIndex, uvs, subCategory);
        },
        lod: 0, logger);
    }

    private CPlugMaterialUserInst? GetMaterialLink(
        CPlugMaterial? mat,
        GbxRefTableFile matFile,
        Dictionary<string, CPlugMaterialUserInst> matDict,
        string subCategory,
        string? modifier)
    {
        var matName = GbxPath.GetFileNameWithoutExtension(matFile.FilePath);

        if (modifier is null && matDict.TryGetValue(matName, out var matInst))
        {
            return matInst;
        }

        if (!initOptions.Value.Materials.TryGetValue(matName, out var material))
        {
            return matDict[matName] = CPlugMaterialUserInstExtensions.Create();
        }

        if (modifier is not null && material.Modifiers.TryGetValue(modifier, out var modifierMaterial))
        {
            material = initOptions.Value.Materials[modifierMaterial];

            if (matDict.TryGetValue(modifierMaterial, out var matModifierInst))
            {
                return matModifierInst;
            }
        }

        if (material.Remove)
        {
            return null;
        }

        var surface = mat?.SurfaceId ?? material.Surface ?? CPlugSurface.MaterialId.Concrete;

        if (material.SubCategories.TryGetValue(subCategory, out var subCategoryMaterial))
        {
            if (subCategoryMaterial.Remove)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(subCategoryMaterial.Link))
            {
                return matDict[matName] = CPlugMaterialUserInstExtensions.Create($"Stadium\\Media\\{subCategoryMaterial.Link}", surface, color: subCategoryMaterial.Color);
            }
        }

        if (!string.IsNullOrEmpty(material.Link))
        {
            return matDict[matName] = CPlugMaterialUserInstExtensions.Create($"Stadium\\Media\\{material.Link}", surface, color: material.Color);
        }

        return matDict[matName] = CPlugMaterialUserInstExtensions.Create();
    }

    private void ApplyUvModifiers(GbxRefTableFile matFile, int uvSetIndex, Vec2[] uvs, string subCategory)
    {
        var matName = GbxPath.GetFileNameWithoutExtension(matFile.FilePath);

        if (!initOptions.Value.Materials.TryGetValue(matName, out var material))
        {
            return;
        }

        if (material.SubCategories.TryGetValue(subCategory, out var subCategoryMaterial) && subCategoryMaterial.UvModifiers is not null)
        {
            uvModifierService.ApplyUvModifiers(uvs, subCategoryMaterial.UvModifiers);
        }

        if (material.UvModifiers is not null)
        {
            uvModifierService.ApplyUvModifiers(uvs, material.UvModifiers);
        }
    }

    private void ApplyDecalUvModifiers(GbxRefTableFile matFile, int uvSetIndex, Vec2[] uvs, string subCategory)
    {
        var matName = GbxPath.GetFileNameWithoutExtension(matFile.FilePath);

        if (!initOptions.Value.Materials.TryGetValue(matName, out var material))
        {
            return;
        }

        if (material.SubCategories.TryGetValue(subCategory, out var subCategoryMaterial) && subCategoryMaterial.DecalUvModifiers is not null)
        {
            uvModifierService.ApplyUvModifiers(uvs, subCategoryMaterial.DecalUvModifiers);
        }

        if (material.DecalUvModifiers is not null)
        {
            uvModifierService.ApplyUvModifiers(uvs, material.DecalUvModifiers);
        }
    }

    private CPlugMaterialUserInst? GetDecalLink(
        GbxRefTableFile matFile,
        Dictionary<string, CPlugMaterialUserInst> matDict,
        string subCategory)
    {
        var matName = GbxPath.GetFileNameWithoutExtension(matFile.FilePath);

        if (!initOptions.Value.Materials.TryGetValue(matName, out var material))
        {
            return null;
        }

        matName += "_Decal";

        if (material.SubCategories.TryGetValue(subCategory, out var subCategoryMaterial))
        {
            if (!string.IsNullOrEmpty(subCategoryMaterial.Decal))
            {
                if (matDict.TryGetValue(matName, out var mat))
                {
                    return mat;
                }

                return matDict[matName] = CPlugMaterialUserInstExtensions.Create($"Stadium\\Media\\{subCategoryMaterial.Decal}", CPlugSurface.MaterialId.NotCollidable, color: subCategoryMaterial.Color);
            }
        }

        if (!string.IsNullOrEmpty(material.Decal))
        {
            if (matDict.TryGetValue(matName, out var mat))
            {
                return mat;
            }

            return matDict[matName] = CPlugMaterialUserInstExtensions.Create($"Stadium\\Media\\{material.Decal}", CPlugSurface.MaterialId.NotCollidable, color: material.Color);
        }

        return null;
    }

    public CGameItemModel Build(CPlugCrystal plugCrystal, byte[]? webpData, Int3 blockSize, string name, ItemInfoModel itemInfo)
    {
        var item = CreateGenericItem(webpData, blockSize, name, itemInfo);

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

        item.EntityModelEdition = entityModelEdition;

        return item;
    }

    public CGameItemModel Build(CPlugStaticObjectModel staticObject, byte[]? webpData, Int3 blockSize, string name, ItemInfoModel itemInfo)
    {
        var item = CreateGenericItem(webpData, blockSize, name, itemInfo);

        var entityModel = new CGameCommonItemEntityModel
        {
            StaticObject = staticObject
        };
        var chunk000 = entityModel.CreateChunk<CGameCommonItemEntityModel.Chunk2E027000>();
        chunk000.Version = 4;
        chunk000.U03 = Iso4.Identity;
        chunk000.U12 = Iso4.Identity;

        item.EntityModel = entityModel;

        return item;
    }

    private CGameItemModel CreateGenericItem(byte[]? webpData, Int3 blockSize, string name, ItemInfoModel itemInfo)
    {
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
            Name = name,
            Description = JsonSerializer.Serialize(itemInfo, AppJsonContext.Default.ItemInfoModel),
            DefaultPlacement = placementParams,
            OrbitalPreviewAngle = 0.15f,
            OrbitalRadiusBase = -1,
            PageName = "Items",
            ProdState = CGameCtnCollector.EProdState.Release,
            WaypointType = CGameItemModel.EWaypointType.None,
            IconWebP = webpData ?? [
                0x52, 0x49, 0x46, 0x46, 0xEC, 0x03, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50,
                0x56, 0x50, 0x38, 0x58, 0x0A, 0x00, 0x00, 0x00, 0x18, 0x00, 0x00, 0x00,
                0x3F, 0x00, 0x00, 0x3F, 0x00, 0x00, 0x56, 0x50, 0x38, 0x4C, 0xEE, 0x02,
                0x00, 0x00, 0x2F, 0x3F, 0xC0, 0x0F, 0x10, 0x95, 0x48, 0xB3, 0x6D, 0xAB,
                0x6D, 0x33, 0xCF, 0x7F, 0x2A, 0xA0, 0x4A, 0x33, 0x72, 0xEF, 0xBE, 0xE3,
                0x72, 0x11, 0x42, 0xBF, 0x92, 0x7C, 0x05, 0x3F, 0x77, 0x2D, 0x11, 0x13,
                0x64, 0xDB, 0xDC, 0x5F, 0xF3, 0x9D, 0xC7, 0xFC, 0xCB, 0xB5, 0x6D, 0xED,
                0xD8, 0xAB, 0xFF, 0xFD, 0xBE, 0xD8, 0xB6, 0x6D, 0x5E, 0x80, 0x6D, 0x9B,
                0x95, 0x2B, 0x27, 0x65, 0x2A, 0x5F, 0x80, 0x93, 0xCA, 0xCE, 0xF1, 0xE9,
                0x6C, 0xDB, 0xB6, 0xFD, 0xFB, 0xFF, 0xD7, 0x04, 0x84, 0xAF, 0x6D, 0xAB,
                0xD0, 0x3A, 0x96, 0x86, 0x6B, 0xC3, 0x47, 0x3A, 0x4E, 0x10, 0x2D, 0x32,
                0x06, 0x16, 0xD4, 0x64, 0x99, 0xCF, 0x78, 0xAF, 0x7C, 0xA7, 0x99, 0x33,
                0xD4, 0x8E, 0x08, 0x1C, 0x23, 0x5A, 0x61, 0x94, 0xA5, 0x56, 0xD2, 0xE4,
                0xC2, 0xB1, 0xD4, 0x53, 0x89, 0xBF, 0x99, 0x70, 0x29, 0x41, 0x80, 0xE4,
                0xB7, 0xE2, 0x94, 0x61, 0xBC, 0x32, 0xD9, 0x6A, 0x35, 0x46, 0xF2, 0xCD,
                0x68, 0x2E, 0xF4, 0x2F, 0x2D, 0x2B, 0xDC, 0xE3, 0xBC, 0xE4, 0xAA, 0x99,
                0xBC, 0x0F, 0x88, 0xA4, 0x6A, 0xC9, 0x4B, 0xE3, 0xDE, 0x90, 0x32, 0xAB,
                0xFF, 0x58, 0x73, 0x9F, 0x8C, 0xB6, 0x82, 0xB4, 0xCC, 0xC1, 0xCC, 0xEB,
                0xDC, 0xFF, 0x4C, 0xC3, 0xFB, 0xF0, 0x08, 0x91, 0xC0, 0x20, 0xF9, 0xAF,
                0xBD, 0xA6, 0x1D, 0xB4, 0xA6, 0x8D, 0xB1, 0xF5, 0xC3, 0x28, 0x24, 0xA6,
                0x4A, 0x4F, 0x61, 0x7B, 0xCA, 0x6A, 0xC2, 0x2B, 0x86, 0x9E, 0xC9, 0xC9,
                0x13, 0x44, 0x24, 0xD0, 0x2B, 0x5E, 0x29, 0xD6, 0xAC, 0xED, 0x15, 0x9E,
                0x15, 0xA6, 0x63, 0x44, 0x53, 0x56, 0x59, 0xE9, 0x52, 0x98, 0x58, 0x3A,
                0x95, 0xFE, 0x4D, 0xEE, 0x4F, 0xA6, 0x91, 0xC9, 0x99, 0xE7, 0x10, 0x09,
                0x8C, 0x92, 0x9F, 0xCA, 0x9B, 0x43, 0xA6, 0x0C, 0x89, 0x95, 0x2E, 0x13,
                0xA4, 0x26, 0x86, 0xCA, 0xDA, 0xA8, 0xC2, 0xC6, 0xDC, 0x8D, 0x84, 0x2F,
                0x4C, 0x6D, 0x64, 0x72, 0xF6, 0x79, 0x44, 0x02, 0xA3, 0x40, 0xAB, 0xFC,
                0xA2, 0xD9, 0x30, 0x36, 0x8E, 0x88, 0x9A, 0x20, 0x3B, 0x32, 0xCA, 0x52,
                0x1B, 0x65, 0x5A, 0xCE, 0x74, 0xEA, 0xE5, 0x44, 0x25, 0x93, 0xDA, 0x75,
                0x0E, 0x25, 0x4A, 0xC5, 0x25, 0xCD, 0x74, 0x65, 0x7A, 0x85, 0xCD, 0x18,
                0xA9, 0x9E, 0xD4, 0xB4, 0x20, 0x40, 0xD8, 0x96, 0x71, 0x92, 0xFB, 0x86,
                0xAB, 0x65, 0xE2, 0xD0, 0x05, 0x91, 0x54, 0x2B, 0x79, 0x63, 0x3C, 0x69,
                0x69, 0x1B, 0x1A, 0x50, 0x69, 0x5A, 0x27, 0x37, 0x24, 0xF3, 0x2E, 0xF7,
                0x3F, 0xD3, 0xF0, 0x76, 0xEA, 0x86, 0x48, 0x60, 0x90, 0x2A, 0xB5, 0x77,
                0x8D, 0x21, 0x75, 0x98, 0xB1, 0x4C, 0xE5, 0xDB, 0xB1, 0x2B, 0x22, 0x81,
                0x52, 0x10, 0xFB, 0xA7, 0x8C, 0x5C, 0x2C, 0x28, 0x68, 0x51, 0x9F, 0x88,
                0x90, 0x31, 0xAC, 0x64, 0x27, 0xEF, 0x89, 0x68, 0x90, 0xD0, 0x8E, 0x44,
                0x3A, 0x23, 0x0B, 0xD9, 0xC2, 0x5E, 0x20, 0x31, 0x76, 0x5B, 0x99, 0x43,
                0x25, 0xBD, 0x14, 0xEF, 0x52, 0x51, 0x72, 0x5F, 0x0B, 0xE4, 0xCC, 0xBD,
                0xAC, 0x60, 0x2A, 0x16, 0x24, 0xF8, 0x94, 0x48, 0x11, 0x46, 0xB2, 0x94,
                0x6D, 0x89, 0x75, 0xB7, 0xB2, 0x92, 0x7B, 0x2F, 0xC1, 0x0F, 0x2E, 0xB9,
                0xA8, 0x30, 0x83, 0xAD, 0xEC, 0x25, 0xBA, 0xEC, 0xAE, 0x73, 0xB1, 0xA0,
                0xE1, 0xD9, 0x58, 0x2A, 0xCF, 0xB1, 0xAC, 0x4A, 0x17, 0xDD, 0x77, 0x2B,
                0x98, 0x8A, 0x15, 0x09, 0x4D, 0x70, 0xC9, 0xC5, 0xCA, 0x42, 0xB6, 0x12,
                0x9B, 0xBE, 0x2E, 0x65, 0x0C, 0x1A, 0x24, 0xB8, 0xE1, 0x92, 0x89, 0x84,
                0x29, 0x6C, 0x64, 0x2F, 0xD1, 0xC7, 0x2E, 0xCC, 0xC0, 0x8A, 0x0A, 0xF5,
                0x0A, 0x63, 0x45, 0x95, 0x15, 0x2C, 0x65, 0x27, 0xD1, 0xDF, 0xBD, 0xAC,
                0x64, 0x82, 0x72, 0x6C, 0x6C, 0x9D, 0xB5, 0x51, 0x1B, 0x3F, 0xEF, 0xD5,
                0x45, 0xAF, 0xA1, 0x16, 0x7E, 0xDE, 0x1B, 0x55, 0x07, 0x5A, 0xC0, 0x6C,
                0x38, 0x03, 0xEF, 0xC1, 0x5F, 0xD0, 0xE8, 0x0B, 0xF5, 0xF0, 0x1E, 0x9C,
                0xB9, 0xBD, 0xEA, 0x04, 0x28, 0xA0, 0x19, 0x0C, 0x87, 0x3D, 0x70, 0x0B,
                0xBE, 0x80, 0x7A, 0x1F, 0xA0, 0x01, 0xBE, 0x84, 0x5B, 0xB0, 0x1B, 0x86,
                0x07, 0x28, 0x0D, 0x46, 0xE1, 0x12, 0x0A, 0xE8, 0x02, 0x53, 0xE0, 0x08,
                0x3C, 0x07, 0x7F, 0x43, 0x5D, 0x33, 0xBF, 0xAF, 0xFB, 0xE7, 0xF2, 0x80,
                0x22, 0x38, 0xBF, 0x43, 0x0B, 0xE8, 0x03, 0x8B, 0xE1, 0x32, 0x7C, 0x0C,
                0x7F, 0x43, 0xA3, 0x2B, 0x82, 0x7F, 0xE0, 0xE3, 0xCF, 0x15, 0xFA, 0x40,
                0xCB, 0x7B, 0x68, 0x16, 0x4A, 0xD0, 0x02, 0xC6, 0xC0, 0x5E, 0x78, 0x00,
                0xDF, 0x74, 0x2E, 0x3A, 0xF8, 0xF6, 0xD6, 0xC3, 0xE8, 0x17, 0x94, 0x82,
                0x8F, 0x17, 0xE8, 0x06, 0x33, 0xE1, 0x28, 0xBC, 0x0C, 0xFF, 0x75, 0xE7,
                0x75, 0x7D, 0x80, 0xA3, 0x30, 0x13, 0xBA, 0x41, 0x31, 0x18, 0x85, 0xBF,
                0xD0, 0x0A, 0x06, 0xC0, 0x0A, 0xB8, 0x02, 0x9F, 0xC3, 0x7F, 0x74, 0x8C,
                0x7A, 0xF8, 0xFC, 0xF1, 0x7B, 0xFD, 0x04, 0xCF, 0x2F, 0x1F, 0x98, 0x08,
                0x47, 0xE0, 0x31, 0xF8, 0x9E, 0x62, 0xA4, 0x0E, 0x1E, 0x87, 0x23, 0x30,
                0x01, 0x5A, 0x41, 0x79, 0x30, 0x8A, 0x36, 0xDE, 0xA1, 0x17, 0xCC, 0x87,
                0x63, 0xF0, 0x2A, 0xBC, 0x76, 0x83, 0xF9, 0xFD, 0x3D, 0xB4, 0xF6, 0x0E,
                0x6D, 0x60, 0xC0, 0xF5, 0x79, 0x0F, 0x5F, 0xDB, 0x45, 0x58, 0x49, 0x46,
                0xD8, 0x00, 0x00, 0x00, 0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00,
                0x06, 0x00, 0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
                0x00, 0x00, 0x1A, 0x01, 0x05, 0x00, 0x01, 0x00, 0x00, 0x00, 0x56, 0x00,
                0x00, 0x00, 0x1B, 0x01, 0x05, 0x00, 0x01, 0x00, 0x00, 0x00, 0x5E, 0x00,
                0x00, 0x00, 0x28, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00,
                0x00, 0x00, 0x31, 0x01, 0x02, 0x00, 0x11, 0x00, 0x00, 0x00, 0x66, 0x00,
                0x00, 0x00, 0x69, 0x87, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x78, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x60, 0x00, 0x00, 0x00, 0x01, 0x00,
                0x00, 0x00, 0x60, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x70, 0x61,
                0x69, 0x6E, 0x74, 0x2E, 0x6E, 0x65, 0x74, 0x20, 0x35, 0x2E, 0x30, 0x2E,
                0x31, 0x33, 0x00, 0x00, 0x05, 0x00, 0x00, 0x90, 0x07, 0x00, 0x04, 0x00,
                0x00, 0x00, 0x30, 0x32, 0x33, 0x30, 0x01, 0xA0, 0x03, 0x00, 0x01, 0x00,
                0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0xA0, 0x04, 0x00, 0x01, 0x00,
                0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x03, 0xA0, 0x04, 0x00, 0x01, 0x00,
                0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x05, 0xA0, 0x04, 0x00, 0x01, 0x00,
                0x00, 0x00, 0xBA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00,
                0x01, 0x00, 0x02, 0x00, 0x04, 0x00, 0x00, 0x00, 0x52, 0x39, 0x38, 0x00,
                0x02, 0x00, 0x07, 0x00, 0x04, 0x00, 0x00, 0x00, 0x30, 0x31, 0x30, 0x30,
                0x00, 0x00, 0x00, 0x00
            ]
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
}
