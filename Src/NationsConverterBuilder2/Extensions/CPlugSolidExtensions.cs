using GBX.NET.Components;
using GBX.NET.Engines.Plug;
using GBX.NET;
using GBX.NET.Serialization.Chunking;

namespace NationsConverterBuilder2.Extensions;

public static class CPlugSolidExtensions
{
    public static CPlugStaticObjectModel ToStaticObject(this CPlugSolid solid,
        Func<CPlugMaterial?, GbxRefTableFile, CPlugMaterialUserInst?>? materialLinks = null,
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
                logger?.LogWarning("Visual has no shader link, this is weird (static object). Material has been probably directly embedded, it doesn't have a name.");
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
                        MaterialUserInst = CPlugMaterialUserInstExtensions.Create("PlatformTech")
                    };
                }
                else
                {
                    var inst = materialLinks(t.Shader as CPlugMaterial, t.ShaderFile);

                    if (inst is null)
                    {
                        continue;
                    }

                    if (inst.Link?.StartsWith(@"Stadium\Media\Material\") == true)
                    {
                        inst.Link = inst.Link.Substring(@"Stadium\Media\Material\".Length);
                        inst.IsUsingGameMaterial = false;
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

            /*var vertexStream = new CPlugVertexStream
            {
                Positions = ApplyLocation(visual.Vertices.Select(v => v.Position), loc).ToArray(),
            };
            var vertexChunk000 = vertexStream.CreateChunk<CPlugVertexStream.Chunk09056000>();
            vertexChunk000.Version = 1;
            vertexChunk000.U01 = true;

            foreach (var field in typeof(CPlugVertexStream).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
            {
                switch (field.Name)
                {
                    case "count":
                        field.SetValue(vertexStream, vertexStream.Positions.Length);
                        break;
                    case "flags":
                        field.SetValue(vertexStream, 3u);
                        break;
                    case "dataDecls":
                        var decls = new List<CPlugVertexStream.DataDecl>
                        {
                            new()
                            {
                                Flags1 = 0xA00400
                            }
                        };

                        if (visual.TexCoords.Length > 0)
                        {
                            decls.Add(new()
                            {
                                Flags1 = 0x20A0020A,
                                Flags2 = 64,
                                Offset = 16
                            });
                        }

                        field.SetValue(vertexStream, decls.ToArray());
                        break;
                    case "uvs":
                        var uvs = (SortedDictionary<int, Vec2[]>)field.GetValue(vertexStream)!;
                        for (var i = 0; i < visual.TexCoords.Length; i++)
                        {
                            uvs[i] = visual.TexCoords[i].TexCoords.Select(v => v.UV).ToArray();
                            uvModifiers?.Invoke(t.ShaderFile, matIndex, uvs[i]);
                        }
                        field.SetValue(vertexStream, uvs);
                        break;
                }
            }*/

            // This will mutate CPlugVisual, BlockInfo instance needs to be reparsed if this object is needed later
            ApplyLocation(visual.Vertices, loc);
            /*visual.Vertices = [];
            visual.VertexStreams = [vertexStream];*/
            //visual.Flags = 168;

            // Uses different index storing method
            visual.IndexBuffer.Chunks.Remove<CPlugIndexBuffer.Chunk09057000>();
            visual.IndexBuffer.CreateChunk<CPlugIndexBuffer.Chunk09057001>();

            visual.Chunks.Remove<CPlugVisual.Chunk09006004>();
            visual.Chunks.Remove<CPlugVisual.Chunk0900600E>();
            visual.CreateChunk<CPlugVisual.Chunk0900600F>().Version = 4; // TMT version
            visual.CreateChunk<CPlugVisual.Chunk09006010>();

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
            ShadedGeoms = shadedGeoms.ToArray(),
            /*PreLightGenerator = new() // crashes more often
            {
                Version = 1,
                U01 = 1,
                U02 = 8.016032f, // can change
                U03 = true,
                U04 = 0.001f,
                U05 = 0.001f,
                U06 = 0.999f,
                U07 = 0.995f,
                U08 = 3.4028235E+38f,
                U09 = 3.4028235E+38f,
                U10 = -3.4028235E+38f,
                U11 = -3.4028235E+38f,
            }*/
        };
        var chunk000 = solid2.CreateChunk<CPlugSolid2Model.Chunk090BB000>();
        chunk000.Version = 32;
        chunk000.U05 = 1;
        chunk000.U07 = 1;
        chunk000.U15 = 1;
        chunk000.U16 = 1;
        solid2.Chunks.Add(new SkippableChunk(0x090BB002) { Data = new byte[8] });

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
