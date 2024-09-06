using GBX.NET;
using GBX.NET.Comparers;
using GBX.NET.Components;
using GBX.NET.Engines.Graphic;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;

namespace NationsConverterBuilder.Extensions;

public static class CPlugTreeExtensions
{
    /// <summary>
    /// Creates a <see cref="CPlugCrystal"/> from <see cref="CPlugTree"/> and its children.
    /// </summary>
    /// <param name="tree">Tree.</param>
    /// <param name="materialLinks">Which <see cref="CPlugMaterialUserInst"/> should be used for referenced material in a tree. Same instance of <see cref="CPlugMaterialUserInst"/> can be reused. If null is returned, it removes the face. If this is null as a whole, only "default" material is used, known when creating an item from scratch.</param>
    /// <param name="lod">Level of detail picked for the crystal. If invalid value, the highest LOD is picked.</param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static CPlugCrystal ToCrystal(this CPlugTree tree,
        Func<CPlugMaterial?, GbxRefTableFile, CPlugMaterialUserInst?>? materialLinks = null,
        Action<GbxRefTableFile, int, Vec2[]>? uvModifiers = null,
        int lod = 0,
        CSceneObjectLink[]? objectLinks = null,
        Iso4? spawnLoc = null,
        int? mergeVerticesDigitThreshold = null,
        Func<GbxRefTableFile, CPlugMaterialUserInst?>? decalLinks = null,
        Action<GbxRefTableFile, int, Vec2[]>? decalUvModifiers = null,
        ILogger? logger = null)
    {
        var groups = new List<CPlugCrystal.Part>();
        var positions = new List<Vec3>();
        var faces = new List<CPlugCrystal.Face>();
        var materials = new Dictionary<string, CPlugCrystal.Material>();
        var layers = new List<CPlugCrystal.Layer>();

        var hasAnyLights = false;

        var positionsDict = mergeVerticesDigitThreshold.HasValue
            ? new Dictionary<Vec3, int>(new Vec3EqualityComparer(mergeVerticesDigitThreshold.Value))
            : [];

        var indicesOffset = 0;

        foreach (var (t, loc) in GetAllChildren(tree, lod).Append((tree, tree.Location.GetValueOrDefault(Iso4.Identity))))
        {
            if (!hasAnyLights && t is CPlugTreeLight)
            {
                hasAnyLights = true;
            }

            if (t.Visual is null)
            {
                continue;
            }

            if (t.ShaderFile is null)
            {
                logger?.LogWarning("Visual has no shader link, this is weird (crystal). Material has been probably directly embedded, it doesn't have a name.");
                continue;
            }

            if (t.Visual is CPlugVisualSprite)
            {
                continue;
            }

            if (t.Visual is not CPlugVisualIndexedTriangles visual)
            {
                logger?.LogWarning("Unsupported visual type: {Type}", t.Visual?.GetType().Name);
                continue;
            }

            if (visual.IndexBuffer is null)
            {
                logger?.LogWarning("Visual has no index buffer");
                continue;
            }

            var matName = materialLinks is null ? string.Empty : GbxPath.GetFileNameWithoutExtension(t.ShaderFile.FilePath);

            if (!materials.TryGetValue(matName, out var material))
            {
                if (materialLinks is null)
                {
                    material = new CPlugCrystal.Material
                    {
                        MaterialUserInst = CPlugMaterialUserInstExtensions.Create()
                    };
                }
                else
                {
                    var inst = materialLinks(t.Shader as CPlugMaterial, t.ShaderFile);

                    if (inst is null)
                    {
                        continue;
                    }

                    material = new CPlugCrystal.Material() { MaterialUserInst = inst };
                }

                materials[matName] = material;
            }

            var decalMaterialInst = decalLinks?.Invoke(t.ShaderFile);
            var decalMaterial = default(CPlugCrystal.Material);

            if (decalMaterialInst is not null)
            {
                if (!materials.TryGetValue(matName + "_Decal", out decalMaterial))
                {
                    decalMaterial = new CPlugCrystal.Material() { MaterialUserInst = decalMaterialInst };
                    materials[matName + "_Decal"] = decalMaterial;
                }
            }

            var meshMode = MeshMode.Default;

            // Add mesh pieces until break
            while (meshMode != MeshMode.None)
            {
                var uvSets = new Vec2[visual.TexCoords.Length][];

                for (var i = 0; i < visual.TexCoords.Length; i++)
                {
                    var texCoordSet = visual.TexCoords[i].TexCoords;
                    uvSets[i] = new Vec2[texCoordSet.Length];

                    for (var j = 0; j < texCoordSet.Length; j++)
                    {
                        uvSets[i][j] = texCoordSet[j].UV;
                    }

                    if (meshMode == MeshMode.Decal)
                    {
                        if (decalUvModifiers is null)
                        {
                            uvModifiers?.Invoke(t.ShaderFile, i, uvSets[i]);
                        }
                        else
                        {
                            decalUvModifiers(t.ShaderFile, i, uvSets[i]);
                        }
                    }
                    else
                    {
                        uvModifiers?.Invoke(t.ShaderFile, i, uvSets[i]);
                    }
                }

                var posOffset = meshMode == MeshMode.Decal ? new Vec3(0, 0.01f, 0) : new Vec3(0, 0, 0);

                var group = new CPlugCrystal.Part { Name = "part", U02 = 1, U03 = -1, U04 = -1 };
                groups.Add(group);

                // add all unique positions to the dictionary
                foreach (var pos in ApplyLocation(visual.Vertices.Select(x => x.Position), loc))
                {
                    if (!positionsDict.ContainsKey(pos + posOffset))
                    {
                        positionsDict[pos + posOffset] = positionsDict.Count;
                    }
                }

                foreach (var indices in visual.IndexBuffer.Indices.Chunk(3))
                {
                    var verts = new CPlugCrystal.Vertex[indices.Length];
                    for (int i = 0; i < indices.Length; i++)
                    {
                        var pos = ApplyLocation(visual.Vertices[indices[i]].Position, loc) + posOffset;
                        var index = positionsDict[pos];
                        var uv = uvSets.Length == 0 ? (0, 0) : uvSets[0][indices[i]];
                        verts[i] = new CPlugCrystal.Vertex(index, uv);
                    }

                    // skip degenerate triangles
                    if (verts[0].Index == verts[1].Index || verts[1].Index == verts[2].Index || verts[2].Index == verts[0].Index)
                    {
                        continue;
                    }

                    faces.Add(new CPlugCrystal.Face(
                        verts,
                        group,
                        meshMode == MeshMode.Decal ? decalMaterial : material,
                        null
                    ));
                }

                indicesOffset += visual.Vertices.Length;

                if (meshMode == MeshMode.Default && decalMaterial is not null)
                {
                    meshMode = MeshMode.Decal;
                    continue;
                }

                meshMode = MeshMode.None;
            }
        }

        foreach (var pos in positionsDict.OrderBy(x => x.Value))
        {
            positions.Add(pos.Key);
        }

        // fixing invalid meshes caused probably by completely zeroed out UVs (more degenerate triangles)
        foreach (var face in faces)
        {
            if (face.Vertices.Length != 3)
            {
                continue;
            }

            if (face.Vertices[0].TexCoord == (0, 0)
             && face.Vertices[1].TexCoord == (0, 0)
             && face.Vertices[2].TexCoord == (0, 0))
            {
                face.Vertices[0] = face.Vertices[0] with { TexCoord = (0, 1) };
                face.Vertices[1] = face.Vertices[1] with { TexCoord = (0, 0) };
                face.Vertices[2] = face.Vertices[2] with { TexCoord = (1, 0) };
            }
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
            LayerId = "Layer0",
            LayerName = "Geometry",
            Collidable = false,
            IsEnabled = true,
            IsVisible = true,
            CrystalEnabled = false,
            U02 = crystal.Groups.Select((_, i) => i).ToArray()
        });

        var collisionLayer = CreateCollisionLayer(tree, "Layer1", logger, isTrigger: false, out var newMaterials);

        if (collisionLayer is not null)
        {
            layers.Add(collisionLayer);

            var i = 0;
            foreach (var newMaterial in newMaterials)
            {
                materials.Add($"_Collision" + i, newMaterial);
                i++;
            }
        }

        foreach (var link in objectLinks ?? [])
        {
            if (link.Mobil?.Item?.Solid?.Tree is not CPlugSolid solid)
            {
                continue;
            }

            if (solid.Tree is not CPlugTree t)
            {
                continue;
            }

            var linkLayer = CreateCollisionLayer(t, "Layer2", logger, isTrigger: true, out var newMaterials2);

            if (linkLayer is null)
            {
                continue;
            }

            layers.Add(linkLayer);

            var i = 0;
            foreach (var newMaterial in newMaterials)
            {
                materials.Add($"_Trigger" + i, newMaterial);
                i++;
            }
        }

        if (spawnLoc.HasValue)
        {
            layers.Add(new CPlugCrystal.SpawnPositionLayer
            {
                Ver = 2,
                LayerId = "Layer3",
                LayerName = "Spawn",
                CrystalEnabled = false,
                IsEnabled = true,
                SpawnPosition = (spawnLoc.Value.TX, spawnLoc.Value.TY, spawnLoc.Value.TZ)
            });
        }

        if (hasAnyLights)
        {
            var lightLocPairs = new List<(CPlugLightUserModel, Iso4)>();

            foreach (var (t, loc) in GetAllChildren(tree, lod).Append((tree, tree.Location.GetValueOrDefault(Iso4.Identity))))
            {
                if (t is not CPlugTreeLight treeLight)
                {
                    continue;
                }

                if (treeLight.PlugLight is null)
                {
                    logger?.LogWarning("Tree light has no light instance");
                    continue;
                }

                if (treeLight.PlugLight.GxLightModel is not GxLight light)
                {
                    logger?.LogWarning("Light instance has no gx light");
                    continue;
                }

                var spot = light as GxLightSpot;

                var userLight = new CPlugLightUserModel
                {
                    Color = light.Color,
                    Distance = light.Intensity * 25,
                    Intensity = light.Intensity * 2,
                    //NightOnly
                    SpotInnerAngle = spot?.AngleInner ?? 40,
                    SpotOuterAngle = spot?.AngleOuter ?? 60,
                };
                userLight.CreateChunk<CPlugLightUserModel.Chunk090F9000>().Version = 1;

                lightLocPairs.Add((userLight, loc));
            }

            layers.Add(new CPlugCrystal.LightLayer
            {
                Ver = 2,
                LayerId = "Layer4",
                LayerName = "Light",
                CrystalEnabled = false,
                IsEnabled = true,
                Lights = lightLocPairs.Select(x => x.Item1).ToArray(),
                LightPositions = lightLocPairs.Select(x => new CPlugCrystal.LightPos { U01 = 0, U02 = x.Item2 }).ToArray()
            });
        }

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

    private static IEnumerable<(CPlugTree, Iso4)> GetAllChildren(CPlugTree tree, int lod = 0, Iso4 location = default)
    {
        if (location == default)
        {
            location = tree.Location.GetValueOrDefault(Iso4.Identity);
        }

        if (tree is CPlugTreeVisualMip mip)
        {
            var lodChild = GetLodTree(mip, lod);

            var newLocation = MultiplyAddIso4(location, lodChild.Location.GetValueOrDefault(Iso4.Identity));

            yield return (lodChild, newLocation);

            foreach (var descendant in GetAllChildren(lodChild, lod, newLocation))
            {
                yield return descendant;
            }
        }

        if (tree.Children is null)
        {
            yield break;
        }

        foreach (var child in tree.Children)
        {
            var childLocation = child.Location.GetValueOrDefault(Iso4.Identity);

            var newLocation = MultiplyAddIso4(location, childLocation);
            
            yield return (child, newLocation);

            foreach (var descendant in GetAllChildren(child, lod, newLocation))
            {
                yield return descendant;
            }
        }
    }

    private static Iso4 MultiplyAddIso4(Iso4 a, Iso4 b)
    {
        return new Iso4(
            a.XX * b.XX + a.XY * b.YX + a.XZ * b.ZX,
            a.XX * b.XY + a.XY * b.YY + a.XZ * b.ZY,
            a.XX * b.XZ + a.XY * b.YZ + a.XZ * b.ZZ,

            a.YX * b.XX + a.YY * b.YX + a.YZ * b.ZX,
            a.YX * b.XY + a.YY * b.YY + a.YZ * b.ZY,
            a.YX * b.XZ + a.YY * b.YZ + a.YZ * b.ZZ,

            a.ZX * b.XX + a.ZY * b.YX + a.ZZ * b.ZX,
            a.ZX * b.XY + a.ZY * b.YY + a.ZZ * b.ZY,
            a.ZX * b.XZ + a.ZY * b.YZ + a.ZZ * b.ZZ,

            a.TX + b.TX,
            a.TY + b.TY,
            a.TZ + b.TZ
        );
    }

    private static CPlugTree GetLodTree(CPlugTreeVisualMip mip, int lod)
    {
        return mip.Levels
            .OrderBy(x => x.FarZ)
            .Select(x => x.Tree)
            .ElementAtOrDefault(lod) ?? mip.Levels
                .OrderBy(x => x.FarZ)
                .First()
                .Tree;
    }

    private static IEnumerable<Vec3> ApplyLocation(IEnumerable<Vec3> vertices, Iso4 location)
    {
        if (location == Iso4.Identity)
        {
            return vertices;
        }

        return vertices.Select(v => ApplyLocation(v, location));
    }

    private static Vec3 ApplyLocation(Vec3 vertex, Iso4 location)
    {
        if (location == Iso4.Identity)
        {
            return vertex;
        }

        return new Vec3(
            vertex.X * location.XX + vertex.Y * location.XY + vertex.Z * location.XZ + location.TX,
            vertex.X * location.YZ + vertex.Y * location.YY + vertex.Z * location.YZ + location.TY,
            vertex.X * location.ZX + vertex.Y * location.ZY + vertex.Z * location.ZZ + location.TZ
        );
    }

    private static CPlugCrystal.Layer? CreateCollisionLayer(CPlugTree tree, string layerId, ILogger? logger, bool isTrigger, out IEnumerable<CPlugCrystal.Material> newMaterials)
    {
        var groups = new List<CPlugCrystal.Part>();
        var positions = new List<Vec3>();
        var faces = new List<CPlugCrystal.Face>();
        var materials = new Dictionary<(CPlugSurface.MaterialId, CPlugMaterialUserInst.GameplayId), CPlugCrystal.Material>();

        var indicesOffset = 0;

        foreach (var (child, location) in GetAllChildren(tree).Append((tree, tree.Location.GetValueOrDefault(Iso4.Identity))))
        {
            if (child.Surface is null)
            {
                continue;
            }

            if (child.Surface is not CPlugSurface surface)
            {
                logger?.LogWarning("Unsupported surface type: {Type}", child.Surface?.GetType().Name);
                continue;
            }

            if (surface.Geom?.Surf is not CPlugSurface.Mesh collisionMesh)
            {
                logger?.LogWarning("Unsupported collision surface type: {Type}", surface.Geom?.Surf?.GetType().Name);
                continue;
            }

            if (surface.Materials is null)
            {
                throw new Exception("Collision surface has no materials, which is weird");
            }

            foreach (var material in surface.Materials)
            {
                var surfIdSet = GetSurfaceIdSet(material);

                if (materials.TryGetValue(surfIdSet, out var collisionMat))
                {
                    continue;
                }

                var (surfId, gameplayId) = surfIdSet;

                var fullMaterialName = $@"Editors\MeshEditorMedia\Materials\{surfId}";

                collisionMat = new CPlugCrystal.Material
                {
                    MaterialUserInst = CPlugMaterialUserInstExtensions.Create(fullMaterialName, surfId, gameplayId)
                };

                materials.Add(surfIdSet, collisionMat);
            }

            var group = new CPlugCrystal.Part() { Name = "part", U02 = 1, U03 = -1, U04 = -1 };
            groups.Add(group);

            positions.AddRange(ApplyLocation(collisionMesh.Vertices, location));
            faces.AddRange(collisionMesh.CookedTriangles?
                .Select(tri =>
                {
                    var material = surface.Materials[tri.U03];

                    return new CPlugCrystal.Face([
                        new(tri.U02.X + indicesOffset, default),
                        new(tri.U02.Y + indicesOffset, default),
                        new(tri.U02.Z + indicesOffset, default)
                        ],
                        group,
                        materials[GetSurfaceIdSet(material)],
                        null
                    );
                }) ?? []);

            indicesOffset += collisionMesh.Vertices.Length;
        }

        if (groups.Count == 0)
        {
            newMaterials = [];
            return null;
        }

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
            Groups = groups.ToArray(),
            Positions = positions.ToArray(),
            Faces = faces.ToArray()
        };

        newMaterials = materials.Values;

        return isTrigger
            ? new CPlugCrystal.TriggerLayer
            {
                Ver = 2,
                Crystal = collisionCrystal,
                LayerId = layerId,
                LayerName = "Trigger",
                IsEnabled = true,
                CrystalEnabled = false,
            }
            : new CPlugCrystal.GeometryLayer
            {
                Ver = 2,
                GeometryVersion = 1,
                Crystal = collisionCrystal,
                LayerId = layerId,
                LayerName = "Collisions",
                Collidable = true,
                IsEnabled = true,
                IsVisible = false,
                CrystalEnabled = false,
                U02 = collisionCrystal.Groups.Select((_, i) => i).ToArray()
            };
    }

    private static (CPlugSurface.MaterialId, CPlugMaterialUserInst.GameplayId) GetSurfaceIdSet(CPlugSurface.SurfMaterial material)
    {
        var surfId = material.Material is null
            ? material.SurfaceId.GetValueOrDefault()
            : material.Material.SurfaceId;

        var gameplayId = surfId switch
        {
            CPlugSurface.MaterialId.Turbo_Deprecated => CPlugMaterialUserInst.GameplayId.Turbo,
            CPlugSurface.MaterialId.Turbo2_Deprecated => CPlugMaterialUserInst.GameplayId.Turbo2,
            CPlugSurface.MaterialId.TurboRoulette_Deprecated => CPlugMaterialUserInst.GameplayId.TurboRoulette,
            CPlugSurface.MaterialId.FreeWheeling_Deprecated => CPlugMaterialUserInst.GameplayId.FreeWheeling,
            _ => CPlugMaterialUserInst.GameplayId.None
        };

        if (gameplayId != 0)
        {
            surfId = CPlugSurface.MaterialId.Concrete;
        }

        return (surfId, gameplayId);
    }

    private enum MeshMode
    {
        None,
        Default,
        Decal
    }
}
