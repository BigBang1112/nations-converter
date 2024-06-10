using GBX.NET;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using Microsoft.Extensions.Options;
using NationsConverterBuilder.Models;

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

    private static CPlugCrystal.Material CreateMaterial(MaterialModel materialModel)
    {
        var csts = materialModel.Color is null ? null : new CPlugMaterialUserInst.Cst[]
        {
            new()
            {
                U01 = "TargetColor",
                U02 = "Real",
                U03 = 3,
            }
        };

        var material = new CPlugMaterialUserInst
        {
            IsUsingGameMaterial = true,
            Link = $"Stadium\\Media\\{materialModel.Link}",
            SurfacePhysicId = (byte)(materialModel.Surface ?? CPlugSurface.MaterialId.Asphalt),
            TextureSizeInMeters = 1,
            Csts = csts,
            Color = materialModel.Color
        };
        material.CreateChunk<CPlugMaterialUserInst.Chunk090FD000>().Version = 11;
        material.CreateChunk<CPlugMaterialUserInst.Chunk090FD001>().Version = 5;
        material.CreateChunk<CPlugMaterialUserInst.Chunk090FD002>();
        return new CPlugCrystal.Material() { MaterialUserInst = material, MaterialName = "" };
    }

    private static CPlugCrystal.Material CreateMaterial()
    {
        return CreateMaterial(new()
        {
            Link = "Material\\PlatformTech",
            Surface = CPlugSurface.MaterialId.Asphalt
        });
    }

    public CPlugCrystal CreateCrystalFromSolid(CPlugSolid solid)
    {
        if (solid.Tree is not CPlugTree tree)
        {
            throw new ArgumentException("Solid must have a tree");
        }

        var groups = new List<CPlugCrystal.Part>();
        var positions = new List<Vec3>();
        var faces = new List<CPlugCrystal.Face>();
        var materials = new Dictionary<string, CPlugCrystal.Material>();
        var layers = new List<CPlugCrystal.Layer>();

        var collisionMat = CreateMaterial(new()
        {
            Link = "Editors\\MeshEditorMedia\\Materials\\Asphalt",
            Surface = CPlugSurface.MaterialId.Asphalt
        });
        var collisionIndicesOffset = 0;
        var collisionGroups = new List<CPlugCrystal.Part>();
        var collisionPositions = new List<Vec3>();
        var collisionFaces = new List<CPlugCrystal.Face>();

        var indicesOffset = 0;

        foreach (var t in GetAllChildren(tree))
        {
            if (t.Surface is CPlugSurface surface)
            {
                materials.TryAdd("_Collision", collisionMat);

                if (surface.Geom?.Surf is CPlugSurface.Mesh collisionMesh)
                {
                    var collisionGroup = new CPlugCrystal.Part() { Name = "part", U02 = 1, U03 = -1, U04 = -1 };
                    collisionGroups.Add(collisionGroup);

                    collisionPositions.AddRange(collisionMesh.Vertices);
                    collisionFaces.AddRange(collisionMesh.CookedTriangles?
                        .Select(tri => new CPlugCrystal.Face([
                            new(tri.U02.X + collisionIndicesOffset, default),
                            new(tri.U02.Y + collisionIndicesOffset, default),
                            new(tri.U02.Z + collisionIndicesOffset, default)
                        ],
                        collisionGroup,
                        collisionMat, // this material should be related to each surface material instead
                        null
                        )) ?? []);

                    collisionIndicesOffset += collisionMesh.Vertices.Length;
                }
                else
                {
                    logger.LogWarning("Unsupported collision surface type: {Type}", surface.Geom?.Surf?.GetType().Name);
                }
            }

            if (t.Visual is null)
            {
                continue;
            }

            if (t.Visual is not CPlugVisualIndexedTriangles visual)
            {
                logger.LogWarning("Unsupported visual type: {Type}", t.Visual?.GetType().Name);
                continue;
            }

            if (visual.IndexBuffer is null)
            {
                logger.LogWarning("Visual has no index buffer");
                continue;
            }

            var matName = t.ShaderFile is null ? null : GbxPath.GetFileNameWithoutExtension(t.ShaderFile.FilePath);

            var decal = default(CPlugCrystal.Material);
            CPlugCrystal.Material material;

            if (materials.TryGetValue(matName ?? "", out var mat))
            {
                material = mat;
            }
            else
            {
                if (matName is not null && initOptions.Value.Materials.TryGetValue(matName, out var matModel))
                {
                    if (matModel.Remove)
                    {
                        continue;
                    }

                    if (matModel.UvModifiers is not null)
                    {
                        uvModifierService.ApplyUvModifiers(visual, matModel.UvModifiers);
                    }

                    if (matModel.Decal is not null)
                    {
                        decal = CreateMaterial(new() { Link = matModel.Decal, Surface = CPlugSurface.MaterialId.NotCollidable });
                        materials.Add(Guid.NewGuid().ToString(), decal);
                    }

                    material = matModel.Link is null
                        ? CreateMaterial()
                        : CreateMaterial(matModel);
                }
                else
                {
                    material = CreateMaterial();
                }

                materials.Add(matName ?? "", material);
            }

            var decalMode = false;

            do
            {
                var group = new CPlugCrystal.Part { Name = "part", U02 = 1, U03 = -1, U04 = -1 };
                groups.Add(group);

                if (decalMode)
                {
                    var offset = decalMode ? 0.01f : 0;
                    positions.AddRange(visual.Vertices.Select(x => x.Position + (0, offset, 0)));
                }
                else
                {
                    positions.AddRange(visual.Vertices.Select(x => x.Position));
                }

                foreach (var indices in visual.IndexBuffer.Indices.Chunk(3))
                {
                    var verts = new CPlugCrystal.Vertex[indices.Length];
                    for (int i = 0; i < indices.Length; i++)
                    {
                        var uv = visual.TexCoords.FirstOrDefault()?.TexCoords[indices[i]].UV ?? (0, 0);
                        verts[i] = new CPlugCrystal.Vertex(indices[i] + indicesOffset, uv);
                    }

                    faces.Add(new CPlugCrystal.Face(
                        verts,
                        group,
                        decalMode ? decal : material,
                        null
                    ));
                }

                indicesOffset += visual.Vertices.Length;

                if (decal is not null)
                {
                    if (decalMode)
                    {
                        break;
                    }

                    decalMode = true;
                }
            }
            while (decalMode);
        }

        if (collisionFaces.Count > 0)
        {
            var collisionCrystal = new CPlugCrystal.Crystal
            {
                Version = 37,
                VisualLevels =
                [
                    new CPlugCrystal.VisualLevel { U01 = 4, U02 = 64 },
                    new CPlugCrystal.VisualLevel { U01 = 2, U02 = 128 },
                    new CPlugCrystal.VisualLevel { U01 = 1, U02 = 192 },
                ],
                IsEmbeddedCrystal = true,
                U01 = 4,
                Groups = collisionGroups.ToArray(),
                Positions = collisionPositions.ToArray(),
                Faces = collisionFaces.ToArray()
            };

            layers.Add(new CPlugCrystal.GeometryLayer
            {
                Ver = 2,
                GeometryVersion = 1,
                Crystal = collisionCrystal,
                LayerId = "Layer0",
                LayerName = "Collisions",
                Collidable = true,
                IsEnabled = true,
                IsVisible = false,
                U02 = [0]
            });
        }

        var crystal = new CPlugCrystal.Crystal
        {
            Version = 37,
            VisualLevels =
            [
                new CPlugCrystal.VisualLevel { U01 = 4, U02 = 64 },
                new CPlugCrystal.VisualLevel { U01 = 2, U02 = 128 },
                new CPlugCrystal.VisualLevel { U01 = 1, U02 = 192 },
            ],
            IsEmbeddedCrystal = true,
            U01 = 4,
            Groups = groups.ToArray(),
            Positions = positions.ToArray(),
            Faces = faces.ToArray()
        };

        layers.Add(new CPlugCrystal.GeometryLayer
        {
            Ver = 2,
            GeometryVersion = 1,
            Crystal = crystal,
            LayerId = "Layer1",
            LayerName = "Geometry",
            Collidable = collisionFaces.Count == 0,
            IsEnabled = true,
            IsVisible = true,
            U02 = [0]
        });

        var plugCrystal = new CPlugCrystal
        {
            Materials = materials.Values.ToArray(),
            Layers = layers
        };

        plugCrystal.CreateChunk<CPlugCrystal.Chunk09003003>().Version = 2;
        plugCrystal.CreateChunk<CPlugCrystal.Chunk09003005>();

        plugCrystal.CreateChunk<CPlugCrystal.Chunk09003006>().U01 = faces.SelectMany(x => x.Vertices)
            .Select(x => x.TexCoord)
            .ToArray();

        // lightmap data, matches *faced* indices count
        /*plugCrystal.CreateChunk<CPlugCrystal.Chunk09003006>().U01 = 
        [
            (0, 1), (1, 1), (1, 0), (0, 0),
            (1.27f, 2.27f), (2.27f, 2.27f), (2.27f, 1.27f), (1.27f, 1.27f),
            (0, 3.54f), (1, 3.54f), (1, 2.54f), (0, 2.54f),
            (0, 2.27f), (1, 2.27f), (1, 1.27f), (0, 1.27f),
            (2.54f, 1), (3.54f, 1), (3.54f, 0), (2.54f, 0),
            (1.27f, 1), (2.27f, 1), (2.27f, 0), (1.27f, 0)
        ];*/

        return plugCrystal;
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
