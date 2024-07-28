using GBX.NET.Components;
using GBX.NET.Engines.Plug;
using GBX.NET;

namespace NationsConverterBuilder.Extensions;

public static class CPlugSolidExtensions
{
    public static CPlugStaticObjectModel ToStaticObject(this CPlugSolid solid,
        Func<GbxRefTableFile, CPlugMaterialUserInst?>? materialLinks = null,
        Action<GbxRefTableFile, int, Vec2[]>? uvModifiers = null,
        int lod = 0,
        ILogger? logger = null)
    {
        if (solid.Tree is not CPlugTree tree)
        {
            throw new ArgumentException("Solid has no tree");
        }

        var materials = new Dictionary<string, CPlugSolid2Model.Material>();
        var materialIndices = new Dictionary<CPlugSolid2Model.Material, int>();
        var visuals = new List<CPlugVisualIndexedTriangles>();
        var shadedGeoms = new List<CPlugSolid2Model.ShadedGeom>();

        foreach (var (t, loc) in GetAllChildren(tree, lod).Append((tree, tree.Location.GetValueOrDefault(Iso4.Identity))))
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
                    material = new CPlugSolid2Model.Material
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

                    material = new CPlugSolid2Model.Material() { MaterialUserInst = inst };
                }

                materials[matName] = material;
            }

            if (!materialIndices.TryGetValue(material, out var matIndex))
            {
                matIndex = materialIndices.Count;
                materialIndices[material] = matIndex;
            }

            // This is mutating, BlockInfo instance needs to be reparsed if this object is needed later
            ApplyLocation(visual.Vertices, loc);

            var shadedGeom = new CPlugSolid2Model.ShadedGeom
            {
                Lod = 1,
                MaterialIndex = matIndex,
                U01 = -1,
                VisualIndex = visuals.Count
            };
            shadedGeoms.Add(shadedGeom);

            visuals.Add(visual);
        }

        var solid2 = new CPlugSolid2Model
        {
            CustomMaterials = materials.Values.ToArray(),
            FileWriteTime = DateTime.Now,
            MaterialsFolderName = @"Stadium\Media\Material\",
            VisCstType = 1,
            Visuals = visuals.ToArray(),
        };
        var chunk000 = solid2.CreateChunk<CPlugSolid2Model.Chunk090BB000>();
        chunk000.Version = 32;
        chunk000.U05 = 1;
        chunk000.U07 = 1;
        chunk000.U15 = 1;
        chunk000.U16 = 1;

        return new CPlugStaticObjectModel
        {
            Version = 3,
            IsMeshCollidable = true,
            Mesh = solid2
        };
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

    private static void ApplyLocation(CPlugVisual3D.Vertex[] vertices, Iso4 location)
    {
        if (location == Iso4.Identity)
        {
            return;
        }

        for (var i = 0; i < vertices.Length; i++)
        {
            var v = vertices[i];

            vertices[i] = v with
            {
                Position = new Vec3(
                    v.Position.X * location.XX + v.Position.Y * location.XY + v.Position.Z * location.XZ + location.TX,
                    v.Position.X * location.YZ + v.Position.Y * location.YY + v.Position.Z * location.YZ + location.TY,
                    v.Position.X * location.ZX + v.Position.Y * location.ZY + v.Position.Z * location.ZZ + location.TZ
                )
            };
        }
    }
}
