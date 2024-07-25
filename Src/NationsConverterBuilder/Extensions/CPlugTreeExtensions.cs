﻿using GBX.NET;
using GBX.NET.Components;
using GBX.NET.Engines.Plug;

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
        Func<GbxRefTableFile, CPlugMaterialUserInst?>? materialLinks = null,
        Action<GbxRefTableFile, int, Vec2[]>? uvModifiers = null,
        int lod = 0,
        ILogger? logger = null)
    {
        var groups = new List<CPlugCrystal.Part>();
        var positions = new List<Vec3>();
        var faces = new List<CPlugCrystal.Face>();
        var materials = new Dictionary<string, CPlugCrystal.Material>();
        var layers = new List<CPlugCrystal.Layer>();

        var indicesOffset = 0;

        foreach (var (t, loc) in GetAllChildren(tree, lod))
        {
            if (t.Visual is null)
            {
                continue;
            }

            if (t.ShaderFile is null)
            {
                logger?.LogWarning("Visual has no shader link, this is weird.");
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
                    var inst = materialLinks(t.ShaderFile);

                    if (inst is null)
                    {
                        continue;
                    }

                    material = new CPlugCrystal.Material() { MaterialUserInst = inst };
                }

                materials[matName] = material;
            }

            var uvSets = new Vec2[visual.TexCoords.Length][];

            for (var i = 0; i < visual.TexCoords.Length; i++)
            {
                var texCoordSet = visual.TexCoords[i].TexCoords;
                uvSets[i] = new Vec2[texCoordSet.Length];

                for (var j = 0; j < texCoordSet.Length; j++)
                {
                    uvSets[i][j] = texCoordSet[j].UV;
                }

                if (uvModifiers is not null)
                {
                    uvModifiers(t.ShaderFile, i, uvSets[i]);
                }
            }

            var group = new CPlugCrystal.Part { Name = "part", U02 = 1, U03 = -1, U04 = -1 };
            groups.Add(group);

            positions.AddRange(ApplyLocation(visual.Vertices.Select(x => x.Position), loc));

            foreach (var indices in visual.IndexBuffer.Indices.Chunk(3))
            {
                var verts = new CPlugCrystal.Vertex[indices.Length];
                for (int i = 0; i < indices.Length; i++)
                {
                    var uv = uvSets.Length == 0 ? (0, 0) : uvSets[0][indices[i]];
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

        layers.Add(new CPlugCrystal.GeometryLayer
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
        });

        var collisionLayer = CreateCollisionLayer(tree, "Layer1", logger);

        if (collisionLayer is not null)
        {
            // wtf, collision layer can have different collision surface types, so there will be more than 1 material
            layers.Add(collisionLayer);
            materials["_Collision"] = collisionLayer.Crystal!.Faces[0].Material!;
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
            location = Iso4.Identity;
        }

        if (tree.Children is null)
        {
            yield break;
        }

        foreach (var child in tree.Children)
        {
            var childLocation = child.Location.GetValueOrDefault(Iso4.Identity);

            var newLocation = MultiplyAddIso4(location, childLocation);

            if (child is CPlugTreeVisualMip mip)
            {
                var lodChild = GetLodTree(mip, lod);

                newLocation = MultiplyAddIso4(newLocation, lodChild.Location.GetValueOrDefault(Iso4.Identity));

                foreach (var descendant in GetAllChildren(lodChild, lod, newLocation))
                {
                    yield return descendant;
                }

                continue;
            }
            
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

    private static CPlugCrystal.GeometryLayer? CreateCollisionLayer(CPlugTree tree, string layerId, ILogger? logger)
    {
        var groups = new List<CPlugCrystal.Part>();
        var positions = new List<Vec3>();
        var faces = new List<CPlugCrystal.Face>();
        var materials = new Dictionary<CPlugSurface.MaterialId, CPlugCrystal.Material>();

        var indicesOffset = 0;

        foreach (var (child, location) in GetAllChildren(tree))
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
                var surfId = (CPlugSurface.MaterialId)material.SurfaceId;

                if (materials.TryGetValue(surfId, out var collisionMat))
                {
                    continue;
                }

                collisionMat = new CPlugCrystal.Material
                {
                    MaterialUserInst = CPlugMaterialUserInstExtensions.Create("Editors\\MeshEditorMedia\\Materials\\Asphalt", surfId)
                };

                materials.Add(surfId, collisionMat);
            }

            var group = new CPlugCrystal.Part() { Name = "part", U02 = 1, U03 = -1, U04 = -1 };
            groups.Add(group);

            positions.AddRange(ApplyLocation(collisionMesh.Vertices, location));
            faces.AddRange(collisionMesh.CookedTriangles?
                .Select(tri => new CPlugCrystal.Face([
                    new(tri.U02.X + indicesOffset, default),
                        new(tri.U02.Y + indicesOffset, default),
                        new(tri.U02.Z + indicesOffset, default)
                ],
                group,
                materials[(CPlugSurface.MaterialId)surface.Materials[tri.U03].SurfaceId],
                null
                )) ?? []);

            indicesOffset += collisionMesh.Vertices.Length;
        }

        if (groups.Count == 0)
        {
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

        return new CPlugCrystal.GeometryLayer
        {
            Ver = 2,
            GeometryVersion = 1,
            Crystal = collisionCrystal,
            LayerId = layerId,
            LayerName = "Collisions",
            Collidable = true,
            IsEnabled = true,
            IsVisible = false,
            U02 = [0]
        };
    }

    private static IEnumerable<Vec3> ApplyLocation(IEnumerable<Vec3> vertices, Iso4 location)
    {
        if (location == Iso4.Identity)
        {
            return vertices;
        }

        return vertices.Select(v => new Vec3(
            v.X * location.XX + v.Y * location.XY + v.Z * location.XZ + location.TX,
            v.X * location.YZ + v.Y * location.YY + v.Z * location.YZ + location.TY,
            v.X * location.ZX + v.Y * location.ZY + v.Z * location.ZZ + location.TZ
        ));
    }
}
