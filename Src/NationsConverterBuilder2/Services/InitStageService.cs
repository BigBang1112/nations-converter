﻿using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Plug;
using GBX.NET.Imaging.SkiaSharp;
using NationsConverterShared.Converters.Json;
using NationsConverterBuilder2.Models;
using NationsConverterShared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using GBX.NET.Engines.GameData;
using Microsoft.Extensions.Options;

namespace NationsConverterBuilder2.Services;

internal sealed class InitStageService
{
    private readonly SetupService setupService;
    private readonly ItemMakerService itemMaker;
    private readonly IOptions<InitOptions> initOptions;
    private readonly ILogger<InitStageService> logger;

    private readonly string dataDirPath;
    private readonly string itemsDirPath;
    private readonly string sheetsDirPath;
    private readonly string? initItemsOutputPath;
    private readonly string? initMapsOutputPath;
    private readonly string? initVersion;

    private static readonly string[] subCategories = ["Modless"];

    private static readonly object PadLock = new();

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private static readonly AppJsonContext jsonContext;

    static InitStageService()
    {
        jsonOptions.Converters.Add(new JsonInt2Converter());
        jsonOptions.Converters.Add(new JsonInt3Converter());
        jsonOptions.Converters.Add(new JsonVec3Converter());
        jsonContext = new(jsonOptions);
    }

    public InitStageService(
        SetupService setupService,
        ItemMakerService itemMaker,
        IWebHostEnvironment hostEnvironment,
        IConfiguration config,
        IOptions<InitOptions> initOptions,
        ILogger<InitStageService> logger)
    {
        this.setupService = setupService;
        this.itemMaker = itemMaker;
        this.initOptions = initOptions;
        this.logger = logger;

        dataDirPath = Path.Combine(hostEnvironment.WebRootPath, "data");
        itemsDirPath = Path.Combine(hostEnvironment.WebRootPath, "items");
        sheetsDirPath = Path.Combine(hostEnvironment.WebRootPath, "sheets");

        initItemsOutputPath = config["InitItemsOutputPath"];
        initMapsOutputPath = config["InitMapsOutputPath"];
        initVersion = config["InitStageVersion"];
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
                
                var decorationConvs = new Dictionary<string, ConversionDecorationModel>();

                foreach (var (size, deco) in collection.Decorations)
                {
                    ProcessDecoration(collection, deco, subCategory, size);

                    decorationConvs.Add($"{size.X}x{size.Y}x{size.Z}", new ConversionDecorationModel
                    {
                        BaseHeight = deco.BaseHeight
                    });
                }

                var baseMap = default(CGameCtnChallenge);

                if (File.Exists(Path.Combine(dataDirPath, "Base.Map.Gbx")))
                {
                    baseMap = Gbx.ParseNode<CGameCtnChallenge>(Path.Combine(dataDirPath, "Base.Map.Gbx"));
                    baseMap.MapName = displayName;
                    baseMap.ScriptMetadata!.Declare("MadeWithNationsConverter", true);
                    baseMap.ScriptMetadata!.Declare("NC2_IsConverted", true);
                    baseMap.ScriptMetadata!.Declare("NC2_ConvertedAt", DateTime.UtcNow.ToString("s"));
                }

				var index = 0;

                var convs = new SortedDictionary<string, ConversionModel>();

                RecurseBlockDirectories(displayName, collection.BlockDirectories, baseMap, subCategory, convs, ref index);
                ProcessBlocks(displayName, collection.RootBlocks, baseMap, subCategory, convs, ref index);

                var defaultZone = (collection.Node.DefaultZone as CGameCtnZoneFlat)?.BlockInfoFlat?.Ident.Id;
                
                var convSet = new ConversionSetModel
                {
                    Blocks = convs,
                    Decorations = decorationConvs,
                    TerrainModifiers = collection.TerrainModifiers.Count == 0 ? null : collection.TerrainModifiers,
                    DefaultZoneBlock = defaultZone,
                    Environment = collection.DisplayName,
                    Pylon = collection.Pylon
                };

                await using (var sheetJsonStream = new FileStream(Path.Combine(sheetsDirPath, $"{displayName}.json"), FileMode.Create, FileAccess.Write, FileShare.Write, 4096, useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(sheetJsonStream, convSet, jsonContext.ConversionSetModel, cancellationToken: cancellationToken);
                }

                if (!string.IsNullOrEmpty(initMapsOutputPath))
                {
                    Directory.CreateDirectory(Path.Combine(initMapsOutputPath, subCategory));
                    baseMap?.Save(Path.Combine(initMapsOutputPath, subCategory, $"{displayName}.Map.Gbx"));
                }
            }));
        }

        await Task.WhenAll(tasks);
    }

    private void RecurseBlockDirectories(
        string collectionName,
        IDictionary<string, BlockDirectoryModel> dirs,
        CGameCtnChallenge? baseMap,
        string subCategory,
        IDictionary<string, ConversionModel> convs,
        ref int index)
    {
        foreach (var (dirName, dir) in dirs)
        {
            RecurseBlockDirectories(collectionName, dir.Directories, baseMap, subCategory, convs, ref index);
            ProcessBlocks(collectionName, dir.Blocks, baseMap, subCategory, convs, ref index);
        }
    }

    private void ProcessBlocks(
        string collectionName,
        IDictionary<string, BlockInfoModel> blocks,
        CGameCtnChallenge? baseMap,
        string subCategory,
        IDictionary<string, ConversionModel> convs,
        ref int index)
    {
        foreach (var (name, block) in blocks)
        {
            try
            {
                var conv = ProcessBlock(collectionName, baseMap, block, subCategory, ref index);

                if (conv is not null)
                {
                    convs.Add(name, conv);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process block {BlockName}", block.Name);
            }
        }
    }

    private static readonly string[] technologies = ["MM_Collision"];

    private ConversionModel? ProcessBlock(string collectionName, CGameCtnChallenge? baseMap, BlockInfoModel block, string subCategory, ref int index)
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

        var isTerrainModifiable = false;
        var notModifiable = new HashSet<Int2>();
        var mapTechnology = "MM_Collision";

        CGameCtnBlockInfo nodeForMesh;
        try
        {
            nodeForMesh = setupService.MeshOverrideBlockInfoPaths.TryGetValue(block.Name, out var path)
                ? (CGameCtnBlockInfo?)Gbx.ParseNode(path) ?? node
                : node;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse mesh override block info for {BlockName}", block.Name);
            nodeForMesh = node;
        }

        foreach (var technology in technologies)
        {
            var dirPath = Path.Combine("NC2", "Solid", subCategory, technology, collectionName, pageName, block.Name);

            if (node.GroundMobils is not null)
            {
                var groundUnits = node.GroundBlockUnitInfos?.Select(x => x.RelativeOffset).ToArray() ?? [];

                for (byte i = 0; i < node.GroundMobils.Length; i++)
                {
                    var groundMobilSubVariants = node.GroundMobils[i];

                    for (byte j = 0; j < groundMobilSubVariants.Length; j++)
                    {
                        ProcessSubVariant(new()
                        {
                            Mobil = groundMobilSubVariants[j],
                            Mobil2 = null,
                            BlockInfo = node,
                            CollectionName = collectionName,
                            DirectoryPath = dirPath,
                            ModifierType = "Ground",
                            VariantIndex = i,
                            SubVariantIndex = j,
                            WebpData = node.IconWebP,
                            BlockName = block.Name,
                            SubCategory = subCategory,
                            Technology = technology,
                            MapTechnology = mapTechnology,
                            Units = groundUnits
                        }, baseMap, ref index, out var isModifiable);

                        if (technology == mapTechnology)
                        {
                            index++;
                        }

                        isTerrainModifiable |= isModifiable;

                        if (!isModifiable)
                        {
                            notModifiable.Add((i, j));
                        }
                    }
                }
            }

            if (node.AirMobils is not null)
            {
                var airUnits = node.AirBlockUnitInfos?.Select(x => x.RelativeOffset).ToArray() ?? [];
                
                for (byte i = 0; i < node.AirMobils.Length; i++)
                {
                    var airMobilSubVariants = node.AirMobils[i];

                    for (byte j = 0; j < airMobilSubVariants.Length; j++)
                    {
                        ProcessSubVariant(new()
                        {
                            Mobil = airMobilSubVariants[j],
                            Mobil2 = null,
                            BlockInfo = node,
                            CollectionName = collectionName,
                            DirectoryPath = dirPath,
                            ModifierType = "Air",
                            VariantIndex = i,
                            SubVariantIndex = j,
                            WebpData = node.IconWebP,
                            BlockName = block.Name,
                            SubCategory = subCategory,
                            Technology = technology,
                            MapTechnology = mapTechnology,
                            Units = airUnits
                        }, baseMap, ref index, out var isModifiable);

                        if (technology == mapTechnology)
                        {
                            index++;
                        }

                        isTerrainModifiable |= isModifiable;
                    }
                }
            }

            if (nodeForMesh.VariantBaseGround is not null)
            {
                var groundUnits = nodeForMesh.GroundBlockUnitInfos?.Select(x => x.RelativeOffset).ToArray() ?? [];
                for (byte i = 0; i < nodeForMesh.VariantBaseGround.Mobils?.Length; i++)
                {
                    var groundMobilSubVariants = nodeForMesh.VariantBaseGround.Mobils[i];
                    for (byte j = 0; j < groundMobilSubVariants.Length; j++)
                    {
                        ProcessSubVariant(new()
                        {
                            Mobil = null,
                            Mobil2 = groundMobilSubVariants[j],
                            BlockInfo = node,
                            CollectionName = collectionName,
                            DirectoryPath = dirPath,
                            ModifierType = "Ground",
                            VariantIndex = i,
                            SubVariantIndex = j,
                            WebpData = node.IconWebP,
                            BlockName = block.Name,
                            SubCategory = subCategory,
                            Technology = technology,
                            MapTechnology = mapTechnology,
                            Units = groundUnits
                        }, baseMap, ref index, out var isModifiable);
                        if (technology == mapTechnology)
                        {
                            index++;
                        }
                        isTerrainModifiable |= isModifiable;
                        if (!isModifiable)
                        {
                            notModifiable.Add((i, j));
                        }
                    }
                }
            }

            if (nodeForMesh.AdditionalVariantsGround is not null)
            {
                var groundUnits = nodeForMesh.GroundBlockUnitInfos?.Select(x => x.RelativeOffset).ToArray() ?? [];
                foreach (var variant in nodeForMesh.AdditionalVariantsGround)
                {
                    for (byte i = 0; i < variant.Mobils?.Length; i++)
                    {
                        var groundMobilSubVariants = variant.Mobils[i];
                        for (byte j = 0; j < groundMobilSubVariants.Length; j++)
                        {
                            ProcessSubVariant(new()
                            {
                                Mobil = null,
                                Mobil2 = groundMobilSubVariants[j],
                                BlockInfo = node,
                                CollectionName = collectionName,
                                DirectoryPath = dirPath,
                                ModifierType = $"Ground_{variant.Name?.Replace(' ', '_') ?? "Unnamed"}",
                                VariantIndex = i,
                                SubVariantIndex = j,
                                WebpData = node.IconWebP,
                                BlockName = block.Name,
                                SubCategory = subCategory,
                                Technology = technology,
                                MapTechnology = mapTechnology,
                                Units = groundUnits
                            }, baseMap, ref index, out var isModifiable);
                        }
                    }
                }
            }

            if (nodeForMesh.VariantBaseAir is not null)
            {
                var airUnits = nodeForMesh.AirBlockUnitInfos?.Select(x => x.RelativeOffset).ToArray() ?? [];
                
                for (byte i = 0; i < nodeForMesh.VariantBaseAir.Mobils?.Length; i++)
                {
                    var airMobilSubVariants = nodeForMesh.VariantBaseAir.Mobils[i];
                    for (byte j = 0; j < airMobilSubVariants.Length; j++)
                    {
                        ProcessSubVariant(new()
                        {
                            Mobil = null,
                            Mobil2 = airMobilSubVariants[j],
                            BlockInfo = node,
                            CollectionName = collectionName,
                            DirectoryPath = dirPath,
                            ModifierType = "Air",
                            VariantIndex = i,
                            SubVariantIndex = j,
                            WebpData = node.IconWebP,
                            BlockName = block.Name,
                            SubCategory = subCategory,
                            Technology = technology,
                            MapTechnology = mapTechnology,
                            Units = airUnits
                        }, baseMap, ref index, out var isModifiable);
                        if (technology == mapTechnology)
                        {
                            index++;
                        }
                        isTerrainModifiable |= isModifiable;
                    }
                }
            }

            if (nodeForMesh.AdditionalVariantsAir is not null)
            {
                var airUnits = nodeForMesh.AirBlockUnitInfos?.Select(x => x.RelativeOffset).ToArray() ?? [];

                foreach (var variant in nodeForMesh.AdditionalVariantsAir)
                {
                    for (byte i = 0; i < variant.Mobils?.Length; i++)
                    {
                        var airMobilSubVariants = variant.Mobils[i];
                        for (byte j = 0; j < airMobilSubVariants.Length; j++)
                        {
                            ProcessSubVariant(new()
                            {
                                Mobil = null,
                                Mobil2 = airMobilSubVariants[j],
                                BlockInfo = node,
                                CollectionName = collectionName,
                                DirectoryPath = dirPath,
                                ModifierType = $"Air_{variant.Name?.Replace(' ', '_') ?? "Unnamed"}",
                                VariantIndex = i,
                                SubVariantIndex = j,
                                WebpData = node.IconWebP,
                                BlockName = block.Name,
                                SubCategory = subCategory,
                                Technology = technology,
                                MapTechnology = mapTechnology,
                                Units = airUnits
                            }, baseMap, ref index, out var isModifiable);
                        }
                    }
                }
            }
        }

        return GetBlockConversionModel(node, pageName, block.TerrainZone, isTerrainModifiable, notModifiable);
    }

    private void ProcessSubVariant(SubVariantModel subVariant, CGameCtnChallenge? baseMap, ref int index, out bool isTerrainModifiable)
    {
        var solid = subVariant.Mobil?.Node?.Item?.Solid?.Tree is CPlugSolid s ? s : subVariant.Mobil2?.SolidFid;

        if (solid is null)
        {
            logger.LogError("Failed to get solid for {BlockName} {ModifierType} {VariantIndex} {SubVariantIndex}", subVariant.BlockName, subVariant.ModifierType, subVariant.VariantIndex, subVariant.SubVariantIndex);
            isTerrainModifiable = false;
            return;
        }

        var spawnLoc = subVariant.ModifierType == "Air"
            ? subVariant.BlockInfo.SpawnLocAir
            : subVariant.BlockInfo.SpawnLocGround;

        Directory.CreateDirectory(Path.Combine(itemsDirPath, subVariant.DirectoryPath));

        var modifierMaterials = GetModifierMaterials(solid);
        var modifierTypes = new List<string> { subVariant.ModifierType };
        isTerrainModifiable = !initOptions.Value.DisabledTerrainModifierBlocks.Contains(subVariant.BlockName)
            && modifierMaterials.Count > 0 && subVariant.ModifierType == "Ground";
        if (isTerrainModifiable)
        {
            modifierTypes.AddRange(modifierMaterials.Values.Distinct());
        }

        foreach (var modifierType in modifierTypes)
        {
            var itemInfo = new ItemInfoModel
            {
                Block = new()
                {
                    Modifier = modifierType,
                    Variant = subVariant.VariantIndex,
                    SubVariant = subVariant.SubVariantIndex,
                    Units = subVariant.Units,
                },
                InitVersion = initVersion
            };

            CGameItemModel finalItem;
            List<CGameItemModel>? linkItems = null;
            switch (subVariant.Technology)
            {
                case "MM_Collision":
                    var usesObjectLinkAsItem = initOptions.Value.ObjectLinkAsItem.Contains(subVariant.BlockName);
                    if (usesObjectLinkAsItem && modifierType is "Ground" or "Air")
                    {
                        foreach (var link in subVariant.Mobil?.Node?.ObjectLink ?? [])
                        {
                            if (link.Mobil?.Item?.Solid?.Tree is not CPlugSolid linkSolid)
                            {
                                continue;
                            }

                            var linkCrystal = itemMaker.CreateCrystalFromSolid(linkSolid, null, null, subVariant.SubCategory,
                                null,
                                skipTreeWhen: tree =>
                                {
                                    if (modifierMaterials.Count == 0
                                        || subVariant.ModifierType != "Ground"
                                        || tree.ShaderFile is null
                                        || initOptions.Value.DisabledTerrainModifierBlocks.Contains(subVariant.BlockName))
                                    {
                                        return false;
                                    }

                                    var matName = GbxPath.GetFileNameWithoutExtension(tree.ShaderFile.FilePath);

                                    // skip materials that are not part of the current modifier type
                                    if (modifierMaterials.TryGetValue(matName, out var materialModifier))
                                    {
                                        return modifierType == "Ground";
                                    }

                                    return modifierType != "Ground";
                                });

                            var linkItem = itemMaker.Build(linkCrystal, subVariant.WebpData, subVariant.BlockInfo.Ident.Collection.GetBlockSize(), subVariant.BlockName, itemInfo);
                            linkItems ??= [];
                            linkItems.Add(linkItem);
                        }
                    }

                    var crystal = itemMaker.CreateCrystalFromSolid(solid, usesObjectLinkAsItem ? null : subVariant.Mobil?.Node?.ObjectLink, spawnLoc, subVariant.SubCategory,
                            modifierType is "Ground" or "Air" ? null : modifierType,
                            skipTreeWhen: tree =>
                            {
                                if (modifierMaterials.Count == 0
                                    || subVariant.ModifierType != "Ground"
                                    || tree.ShaderFile is null
                                    || initOptions.Value.DisabledTerrainModifierBlocks.Contains(subVariant.BlockName))
                                {
                                    return false;
                                }

                                var matName = GbxPath.GetFileNameWithoutExtension(tree.ShaderFile.FilePath);

                                // skip materials that are not part of the current modifier type
                                if (modifierMaterials.TryGetValue(matName, out var materialModifier))
                                {
                                    return modifierType == "Ground";
                                }

                                return modifierType != "Ground";
                            });
                    finalItem = itemMaker.Build(crystal, subVariant.WebpData, subVariant.BlockInfo.Ident.Collection.GetBlockSize(), subVariant.BlockName, itemInfo);
                    break;
                case "Solid2":
                    // Solid2 still in development
                    if (modifierType is not "Ground" and not "Air")
                    {
                        continue;
                    }
                    var staticObject = itemMaker.CreateStaticObjectFromSolid(solid, subVariant.SubCategory);
                    finalItem = itemMaker.Build(staticObject, subVariant.WebpData, subVariant.BlockInfo.Ident.Collection.GetBlockSize(), subVariant.BlockName, itemInfo);
                    break;
                default:
                    //logger.LogError("Unsupported technology {Technology}", subVariant.Technology);
                    return;
            }

            if (modifierType is "Ground" or "Air")
            {
                finalItem.WaypointType = (CGameItemModel.EWaypointType)(int)subVariant.BlockInfo.WayPointType;
            }

            var itemPath = Path.Combine(subVariant.DirectoryPath, $"{modifierType}_{subVariant.VariantIndex}_{subVariant.SubVariantIndex}.Item.Gbx");

            finalItem.Save(Path.Combine(itemsDirPath, itemPath));

            if (!string.IsNullOrEmpty(initItemsOutputPath))
            {
                Directory.CreateDirectory(Path.Combine(initItemsOutputPath, subVariant.DirectoryPath));
                File.Copy(Path.Combine(itemsDirPath, itemPath), Path.Combine(initItemsOutputPath, itemPath), true);
            }

            if (subVariant.Technology == subVariant.MapTechnology && modifierType is "Air" or "Ground" or "GroundDefault")
            {
                baseMap?.PlaceAnchoredObject(
                    new(itemPath.Replace('/', '\\'), 26, "akPfIM0aSzuHuaaDWptBbQ"),
                        (index / 32 * 128, 64, index % 32 * 128), (0, 0, 0));
            }

            for (int i = 0; i < linkItems?.Count; i++)
            {
                var linkItem = linkItems[i];

                var linkItemPath = Path.Combine(subVariant.DirectoryPath, $"{modifierType}Link{i}_{subVariant.VariantIndex}_{subVariant.SubVariantIndex}.Item.Gbx");
                
                linkItem.Save(Path.Combine(itemsDirPath, linkItemPath));

                if (!string.IsNullOrEmpty(initItemsOutputPath))
                {
                    Directory.CreateDirectory(Path.Combine(initItemsOutputPath, subVariant.DirectoryPath));
                    File.Copy(Path.Combine(itemsDirPath, linkItemPath), Path.Combine(initItemsOutputPath, linkItemPath), true);
                }
            }
        }
    }

    private ConversionModel GetBlockConversionModel(CGameCtnBlockInfo node, string pageName, CGameCtnZone? zone, bool isTerrainModifiable, HashSet<Int2> notModifiable)
    {
        var airUnits = (node.AirBlockUnitInfos ?? node.VariantBaseAir?.BlockUnitModels)?
            .Select(x => x.RelativeOffset).ToArray() ?? [];
        var groundUnits = (node.GroundBlockUnitInfos ?? node.VariantBaseGround?.BlockUnitModels)?
            .Select(x => x.RelativeOffset).ToArray() ?? [];

        var airSize = airUnits.Length > 0 ? new Int3(airUnits.Max(x => x.X) + 1, airUnits.Max(x => x.Y) + 1, airUnits.Max(x => x.Z) + 1) : default(Int3?);
        var groundSize = groundUnits.Length > 0 ? new Int3(groundUnits.Max(x => x.X) + 1, groundUnits.Max(x => x.Y) + 1, groundUnits.Max(x => x.Z) + 1) : default(Int3?);

        var airVariants = node.AirMobils?.Length ?? node.VariantBaseAir?.Mobils?.Length ?? 0;
        var groundVariants = node.GroundMobils?.Length ?? node.VariantBaseGround?.Mobils?.Length ?? 0;

        var airSubVariants = (node.AirMobils?.Select(x => x.Length)
            ?? node.VariantBaseAir?.Mobils?.Select(x => x.Length))?.ToArray() ?? [];
        var groundSubVariants = (node.GroundMobils?.Select(x => x.Length)
            ?? node.VariantBaseGround?.Mobils?.Select(x => x.Length))?.ToArray() ?? [];

        var airClips = GetConversionClipModels(node.AirBlockUnitInfos ?? node.VariantBaseAir?.BlockUnitModels, tm2Clips: false).ToArray();
        var groundClips = GetConversionClipModels(node.GroundBlockUnitInfos ?? node.VariantBaseGround?.BlockUnitModels, tm2Clips: false).ToArray();

        var airClips2 = GetConversionClipModels(node.VariantBaseAir?.BlockUnitModels, tm2Clips: true).ToArray();
        var groundClips2 = GetConversionClipModels(node.VariantBaseGround?.BlockUnitModels, tm2Clips: true).ToArray();

        Vec3? airSpawnPos = node.WayPointType
            is CGameCtnBlockInfo.EWayPointType.Start
            or CGameCtnBlockInfo.EWayPointType.StartFinish
            or CGameCtnBlockInfo.EWayPointType.Checkpoint ? (node.SpawnLocAir.GetValueOrDefault().TX, node.SpawnLocAir.GetValueOrDefault().TY, node.SpawnLocAir.GetValueOrDefault().TZ) : null;
        Vec3? groundSpawnPos = node.WayPointType
            is CGameCtnBlockInfo.EWayPointType.Start
            or CGameCtnBlockInfo.EWayPointType.StartFinish
            or CGameCtnBlockInfo.EWayPointType.Checkpoint ? (node.SpawnLocGround.GetValueOrDefault().TX, node.SpawnLocGround.GetValueOrDefault().TY, node.SpawnLocGround.GetValueOrDefault().TZ) : null;

        var airWaterUnits = (node.AirBlockUnitInfos ?? node.VariantBaseAir?.BlockUnitModels)?
            .Where(x => initOptions.Value.WaterZone.Contains(x.Chunks.Get<CGameCtnBlockUnitInfo.Chunk03036001>()!.U01 ?? ""))
            .Select(x => new Int2(x.RelativeOffset.X, x.RelativeOffset.Z))
            .Distinct().ToArray() ?? [];
        var groundWaterUnits = (node.GroundBlockUnitInfos ?? node.VariantBaseGround?.BlockUnitModels)?
            .Where(x => initOptions.Value.WaterZone.Contains(x.Chunks.Get<CGameCtnBlockUnitInfo.Chunk03036001>()!.U01 ?? ""))
            .Select(x => new Int2(x.RelativeOffset.X, x.RelativeOffset.Z))
            .Distinct().ToArray() ?? [];

        var airPlacePylons = node.AirBlockUnitInfos?.Select(x => x.PlacePylons).ToArray() ?? [];
        if (airPlacePylons.All(x => x == 0)) airPlacePylons = null;
        var groundPlacePylons = node.GroundBlockUnitInfos?.Select(x => x.PlacePylons).ToArray() ?? [];
        if (groundPlacePylons.All(x => x == 0)) groundPlacePylons = null;

        var airAcceptPylons = node.AirBlockUnitInfos?.Select(x => x.AcceptPylons).ToArray() ?? [];
        if (airAcceptPylons.All(x => x == 255)) airAcceptPylons = null;
        var groundAcceptPylons = node.GroundBlockUnitInfos?.Select(x => x.AcceptPylons).ToArray() ?? [];
        if (groundAcceptPylons.All(x => x == 255)) groundAcceptPylons = null;

        var airTerrainModifierUnits = node.AirBlockUnitInfos?
            .Where(x => !string.IsNullOrWhiteSpace(x.TerrainModifierId))
            .GroupBy(x => x.TerrainModifierId!)
            .ToDictionary(x => x.Key, x => x.Select(x => x.RelativeOffset).ToArray()) ?? [];
        var groundTerrainModifierUnits = node.GroundBlockUnitInfos?
            .Where(x => !string.IsNullOrWhiteSpace(x.TerrainModifierId))
            .GroupBy(x => x.TerrainModifierId!)
            .ToDictionary(x => x.Key, x => x.Select(x => x.RelativeOffset).ToArray()) ?? [];

        var commonUnits = airUnits.SequenceEqual(groundUnits) ? airUnits : null;
        var commonSize = airSize == groundSize ? airSize : null;
        var commonVariants = airVariants == groundVariants ? airVariants : default(int?);
        var commonSubVariants = airSubVariants.SequenceEqual(groundSubVariants) ? airSubVariants : null;
        var commonClips = airClips.SequenceEqual(groundClips) ? airClips : null;
        var commonClips2 = airClips2.SequenceEqual(groundClips2) ? airClips2 : null;
        var commonSpawnPos = airSpawnPos == groundSpawnPos ? airSpawnPos : null;
        var commonWaterUnits = airWaterUnits.SequenceEqual(groundWaterUnits) ? airWaterUnits : null;
        var commonPlacePylons = groundPlacePylons is not null && airPlacePylons?.SequenceEqual(groundPlacePylons) == true ? airPlacePylons : null;
        var commonAcceptPylons = groundAcceptPylons is not null && airAcceptPylons?.SequenceEqual(groundAcceptPylons) == true ? airAcceptPylons : null;
        var commonTerrainModifierUnits =
            airTerrainModifierUnits.Keys.Count == groundTerrainModifierUnits.Keys.Count &&
            airTerrainModifierUnits.Keys.All(k => groundTerrainModifierUnits.ContainsKey(k) && airTerrainModifierUnits[k].SequenceEqual(groundTerrainModifierUnits[k]))
            ? airTerrainModifierUnits
            : null;

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
                Clips = commonClips is null && airClips.Length > 0 ? airClips : null,
                Clips2 = commonClips2 is null && airClips2.Length > 0 ? airClips2 : null,
                SpawnPos = commonSpawnPos is null ? airSpawnPos : null,
                WaterUnits = commonWaterUnits is null && airWaterUnits.Length > 0 ? airWaterUnits : null,
                PlacePylons = commonPlacePylons is null ? airPlacePylons : null,
                AcceptPylons = commonAcceptPylons is null ? airAcceptPylons : null,
                TerrainModifierUnits = commonTerrainModifierUnits is null && airTerrainModifierUnits.Count > 0 ? airTerrainModifierUnits : null
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
                Clips = commonClips is null && groundClips.Length > 0 ? groundClips : null,
                Clips2 = commonClips2 is null && groundClips2.Length > 0 ? groundClips2 : null,
                SpawnPos = commonSpawnPos is null ? groundSpawnPos : null,
                WaterUnits = commonWaterUnits is null && groundWaterUnits.Length > 0 ? groundWaterUnits : null,
                PlacePylons = commonPlacePylons is null ? groundPlacePylons : null,
                AcceptPylons = commonAcceptPylons is null ? groundAcceptPylons : null,
                TerrainModifierUnits = commonTerrainModifierUnits is null && groundTerrainModifierUnits.Count > 0 ? groundTerrainModifierUnits : null
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
            Clips2 = commonClips2?.Length == 0 ? null : commonClips2,
            Air = airConvModel,
            Ground = groundConvModel,
            ZoneHeight = zone?.Height,
            Pylon = (zone as CGameCtnZoneFlat)?.BlockInfoPylon?.Ident.Id,
            Waypoint = node.WayPointType is CGameCtnBlockInfo.EWayPointType.None ? null : node.WayPointType switch
            {
                CGameCtnBlockInfo.EWayPointType.Start => WaypointType.Start,
                CGameCtnBlockInfo.EWayPointType.StartFinish => WaypointType.StartFinish,
                CGameCtnBlockInfo.EWayPointType.Checkpoint => WaypointType.Checkpoint,
                CGameCtnBlockInfo.EWayPointType.Finish => WaypointType.Finish,
                _ => null
            },
            SpawnPos = commonSpawnPos,
            Modifiable = isTerrainModifiable ? true : null,
            NotModifiable = isTerrainModifiable && notModifiable.Count > 0 ? notModifiable : null,
            WaterUnits = commonWaterUnits?.Length == 0 ? null : commonWaterUnits,
            Road = node is CGameCtnBlockInfoRoad ? new() : null,
            PlacePylons = commonPlacePylons,
            AcceptPylons = commonAcceptPylons,
            TerrainModifierUnits = commonTerrainModifierUnits?.Count == 0 ? null : commonTerrainModifierUnits,
            TM2 = node.Chunks.Get<CGameCtnBlockInfo.Chunk0304E023>() is not null ? true : null,
            Skin = node.Chunks.Get<CPlugGameSkin.HeaderChunk090F4000>() is null ? null : new()
        };
    }

    private IEnumerable<ConversionClipModel> GetConversionClipModels(CGameCtnBlockUnitInfo[]? blockUnitInfos, bool tm2Clips)
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
                if (unit.Clips[i].Node is not CGameCtnBlockInfoClip clip)
                {
                    continue;
                }

                var isTM2Clip = setupService.Stadium2Clips.Contains(clip.Ident.Id);
                if ((!tm2Clips && isTM2Clip) || (tm2Clips && !isTM2Clip))
                {
                    continue;
                }

                yield return new ConversionClipModel
                {
                    Name = clip.Ident.Id,
                    Offset = unit.RelativeOffset,
                    Dir = (Direction)i
                };
            }
        }
    }

    private void ProcessDecoration(CollectionModel collection, DecorationModel decoration, string subCategory, Int3 decoSize)
    {
        if (decoration.Solid is null)
        {
            return;
        }

        var dirPath = Path.Combine("NC2", "Solid", subCategory, "MM_Collision", collection.DisplayName, "Decorations");

        Directory.CreateDirectory(Path.Combine(itemsDirPath, dirPath));

        var itemInfo = new ItemInfoModel
        {
            Deco = new()
            {
                Size = decoSize
            },
            InitVersion = initVersion
        };

        var crystal = itemMaker.CreateCrystalFromSolid(decoration.Solid, null, null, subCategory);
        var finalItem = itemMaker.Build(crystal, decoration.WebpIcon, collection.BlockSize, name: $"{decoSize.X}x{decoSize.Y}x{decoSize.Z}", itemInfo);

        var itemPath = Path.Combine(dirPath, $"{decoSize.X}x{decoSize.Y}x{decoSize.Z}.Item.Gbx");

        finalItem.Save(Path.Combine(itemsDirPath, itemPath));

        if (!string.IsNullOrEmpty(initItemsOutputPath))
        {
            Directory.CreateDirectory(Path.Combine(initItemsOutputPath, dirPath));
            File.Copy(Path.Combine(itemsDirPath, itemPath), Path.Combine(initItemsOutputPath, itemPath), true);
        }
    }

    private Dictionary<string, string> GetModifierMaterials(CPlugSolid solid)
    {
        var modifierMaterials = new Dictionary<string, string>();

        foreach (var treeMaterialName in ((CPlugTree?)solid.Tree)?.GetAllChildren(includeVisualMipLevels: true)
            .Select(x => GbxPath.GetFileNameWithoutExtension(x.ShaderFile?.FilePath)) ?? [])
        {
            if (treeMaterialName is null || !initOptions.Value.Materials.TryGetValue(treeMaterialName, out var material))
            {
                continue;
            }

            if (material.Modifiers.Count == 0)
            {
                continue;
            }

            modifierMaterials[treeMaterialName] = "GroundDefault";

            foreach (var (modifier, modifierMaterialName) in material.Modifiers)
            {
                modifierMaterials[modifierMaterialName] = modifier;
            }

            // here it could do break; ?
        }

        return modifierMaterials;
    }
}
