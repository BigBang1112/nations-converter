using GBX.NET;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;

namespace NationsConverterBuilder.Services;

internal sealed class ItemMakerService
{
    private readonly ILogger<ItemMakerService> logger;

    public ItemMakerService(ILogger<ItemMakerService> logger)
    {
        this.logger = logger;
    }

    private static CPlugCrystal.Material CreateMaterial(string materialName, CPlugSurface.MaterialId surfaceId)
    {
        var material = new CPlugMaterialUserInst
        {
            IsUsingGameMaterial = true,
            Link = $"Stadium\\Media\\{materialName}",
            SurfacePhysicId = (byte)surfaceId,
            TextureSizeInMeters = 1
        };
        material.CreateChunk<CPlugMaterialUserInst.Chunk090FD000>().Version = 11;
        material.CreateChunk<CPlugMaterialUserInst.Chunk090FD001>().Version = 5;
        material.CreateChunk<CPlugMaterialUserInst.Chunk090FD002>();
        return new CPlugCrystal.Material() { MaterialUserInst = material, MaterialName = "" };
    }

    private static CPlugCrystal.Material CreateMaterial()
    {
        return CreateMaterial("Material\\PlatformTech", CPlugSurface.MaterialId.Asphalt);
    }

    public CPlugCrystal CreateCrystalFromSolid(CPlugSolid solid)
    {
        if (solid.Tree is not CPlugTree tree)
        {
            throw new ArgumentException("Solid must have a tree");
        }

        //
        
        
        /*var groupTest = new CPlugCrystal.Part { Name = "part", U02 = 1, U03 = -1, U04 = -1 };

        var crystalTest = new CPlugCrystal.Crystal
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
            Groups = [groupTest],
            Positions =
            [
                (-2, 0, -2),
                (-2, 0, 2),
                (2, 0, 2),
                (2, 0, -2),
                (-2, 4, -2),
                (-2, 4, 2),
                (2, 4, 2),
                (2, 4, -2)
            ],
            Faces =
            [
                new CPlugCrystal.Face([
                    new CPlugCrystal.Vertex(0, (4, 4)),
                    new CPlugCrystal.Vertex(3, (0, 4)),
                    new CPlugCrystal.Vertex(2, (0, 0)),
                    new CPlugCrystal.Vertex(1, (4, 0))
                ], groupTest, crystalMaterial, null),
                new CPlugCrystal.Face([
                    new CPlugCrystal.Vertex(5, (4, 4)),
                    new CPlugCrystal.Vertex(6, (0, 4)),
                    new CPlugCrystal.Vertex(7, (0, 0)),
                    new CPlugCrystal.Vertex(4, (4, 0))
                ], groupTest, crystalMaterial, null),
                new CPlugCrystal.Face([
                    new CPlugCrystal.Vertex(4, (4, 4)),
                    new CPlugCrystal.Vertex(7, (0, 4)),
                    new CPlugCrystal.Vertex(3, (0, 0)),
                    new CPlugCrystal.Vertex(0, (4, 0))
                ], groupTest, crystalMaterial, null),
                new CPlugCrystal.Face([
                    new CPlugCrystal.Vertex(5, (4, 4)),
                    new CPlugCrystal.Vertex(4, (0, 4)),
                    new CPlugCrystal.Vertex(0, (0, 0)),
                    new CPlugCrystal.Vertex(1, (4, 0))
                ], groupTest, crystalMaterial, null),
                new CPlugCrystal.Face([
                    new CPlugCrystal.Vertex(6, (4, 4)),
                    new CPlugCrystal.Vertex(5, (0, 4)),
                    new CPlugCrystal.Vertex(1, (0, 0)),
                    new CPlugCrystal.Vertex(2, (4, 0))
                ], groupTest, crystalMaterial, null),
                new CPlugCrystal.Face([
                    new CPlugCrystal.Vertex(7, (4, 4)),
                    new CPlugCrystal.Vertex(6, (0, 4)),
                    new CPlugCrystal.Vertex(2, (0, 0)),
                    new CPlugCrystal.Vertex(3, (4, 0))
                ], groupTest, crystalMaterial, null),
            ]
        };*/
        //

        var groups = new List<CPlugCrystal.Part>();
        var positions = new List<Vec3>();
        var faces = new List<CPlugCrystal.Face>();
        var materials = new Dictionary<string, CPlugCrystal.Material>();

        var indicesOffset = 0;

        foreach (var t in GetAllChildren(tree).Where(x => x.Visual is not null))
        {
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

            CPlugCrystal.Material material;

            if (materials.TryGetValue(matName ?? "", out var mat))
            {
                material = mat;
            }
            else
            {
                material = matName switch
                {
                    "SpeedAsphalt" => CreateMaterial("Material\\RoadTech", CPlugSurface.MaterialId.Asphalt),
                    "SpeedSandBorder" => CreateMaterial("Material_BlockCustom\\CustomSand", CPlugSurface.MaterialId.Sand),
                    _ => CreateMaterial()
                };

                materials.Add(matName ?? "", material);
            }

            var group = new CPlugCrystal.Part { Name = "part", U02 = 1, U03 = -1, U04 = -1 };
            groups.Add(group);

            positions.AddRange(visual.Vertices.Select(x => x.Position));

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
                    material,
                    null
                ));
            }

            indicesOffset += visual.Vertices.Length;
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

        var plugCrystal = new CPlugCrystal
        {
            Materials = materials.Values.ToArray(),
            Layers =
            [
                new CPlugCrystal.GeometryLayer
                {
                    Ver = 2,
                    GeometryVersion = 1,
                    Crystal = crystal,
                    LayerId = "Layer0",
                    LayerName = "Geometry",
                    Collidable = true,
                    IsEnabled = true,
                    IsVisible = true,
                    U02 = [0]
                }
            ]
        };

        plugCrystal.CreateChunk<CPlugCrystal.Chunk09003003>().Version = 2;
        plugCrystal.CreateChunk<CPlugCrystal.Chunk09003005>();

        plugCrystal.CreateChunk<CPlugCrystal.Chunk09003006>().U01 = faces.SelectMany(x => x.Vertices).Select(x => x.TexCoord).ToArray();

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
        item.CreateChunk<CGameItemModel.Chunk2E002009>().Version = 10; // will change
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
                foreach (var descendant in GetAllChildren(mip.Levels.First().Value))
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
