using GBX.NET;
using GBX.NET.Components;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
using GBX.NET.Imaging.SkiaSharp;
using NationsConverterBuilder.Models;
using System.Collections.Immutable;

namespace NationsConverterBuilder.Services;

internal sealed class SetupService
{
    private readonly string dataDirPath;
    private readonly string data2DirPath;

    public SetupService(IWebHostEnvironment hostEnvironment)
    {
        dataDirPath = Path.Combine(hostEnvironment.WebRootPath, "data");
        data2DirPath = Path.Combine(hostEnvironment.WebRootPath, "data2");
    }

    public Dictionary<string, CollectionModel> Collections { get; } = [];

    internal static readonly char[] separator = ['/', '\\'];

    public async Task SetupCollectionsAsync(CancellationToken cancellationToken = default)
    {
        foreach (var filePath in Directory.GetFiles(Path.Combine(dataDirPath, "Collections"), "*.Gbx"))
        {
            var collection = await Gbx.ParseNodeAsync<CGameCtnCollection>(filePath, cancellationToken: cancellationToken);
            
            if (string.IsNullOrEmpty(collection.Collection))
            {
                continue;
            }

            var decosFolderPath = Path.Combine(dataDirPath, collection.FolderDecoration ?? throw new Exception("FolderDecoration is null"));

            var decorations = new Dictionary<Int3, DecorationModel>();

            foreach (var decoFilePath in Directory.GetFiles(decosFolderPath, "*.Gbx"))
            {
                var deco = await Gbx.ParseNodeAsync<CGameCtnDecoration>(decoFilePath, cancellationToken: cancellationToken);

                if (deco.DecoSize is null || deco.DecoSize.SceneFile is null || decorations.ContainsKey(deco.DecoSize.Size))
                {
                    continue;
                }

                var scene3dPath = deco.DecoSize.SceneFile.GetFullPath();

                if (!File.Exists(scene3dPath))
                {
                    continue;
                }

                var scene3dHeader = Gbx.ParseHeader<CSceneLayout>(scene3dPath);

                if (scene3dHeader.RefTable is null)
                {
                    continue;
                }

                var sceneSolidFile = scene3dHeader.RefTable.Files
                    .FirstOrDefault(x => !x.FilePath.EndsWith("SkyDome.Solid.Gbx") && x.FilePath.EndsWith(".Solid.Gbx"));

                if (sceneSolidFile is null)
                {
                    continue;
                }

                var sceneSolidPath = GetFullFilePath(scene3dHeader.RefTable, sceneSolidFile);

                if (!File.Exists(sceneSolidPath))
                {
                    continue;
                }

                var sceneSolid = await Gbx.ParseNodeAsync<CPlugSolid>(sceneSolidPath, cancellationToken: cancellationToken);

                if (sceneSolid is null)
                {
                    continue;
                }

                decorations.Add(deco.DecoSize.Size, new DecorationModel
                {
                    Solid = sceneSolid,
                    BaseHeight = deco.DecoSize.BaseHeightBase,
                });
            }

            Collections.Add(collection.Collection, new CollectionModel
            {
                Id = collection.Collection,
                DisplayName = collection.DisplayName ?? collection.Collection,
                Node = collection,
                Decorations = decorations,
                BlockSize = ((int)collection.SquareSize, (int)collection.SquareHeight, (int)collection.SquareSize)
            });
        }
    }

    public void SetupCollection(CollectionModel collection)
    {
        if (collection.IsLoaded || collection.Node.FolderBlockInfo is null)
        {
            return;
        }

        var completeZoneList = collection.Node.CompleteListZoneList ?? [];
        var folderBlockInfoPaths = Directory.EnumerateFiles(Path.Combine(dataDirPath, collection.Node.FolderBlockInfo), "*.Gbx", SearchOption.AllDirectories);
        var stadium2 = default(CGameCtnCollection);
        var stadium2Blocks = default(ImmutableHashSet<string>);

        if (collection.DisplayName == "Stadium" && Directory.Exists(Path.Combine(data2DirPath, "Collections")))
        {
            stadium2 = Gbx.ParseNode<CGameCtnCollection>(Path.Combine(data2DirPath, "Collections", "Stadium.TMCollection.Gbx"));
            completeZoneList = completeZoneList
                .Concat(stadium2.CompleteListZoneList ?? [])
                .DistinctBy(x => x.File?.FilePath)
                .ToArray();
            stadium2Blocks = Directory.EnumerateFiles(Path.Combine(data2DirPath, collection.Node.FolderBlockInfo), "*.ED*.Gbx", SearchOption.AllDirectories)
                .Where(x => !x.EndsWith(".EDClip.Gbx"))
                .ToImmutableHashSet();
            folderBlockInfoPaths = folderBlockInfoPaths.Concat(stadium2Blocks);
        }

        foreach (var zone in completeZoneList)
        {
            switch (zone.Node)
            {
                case CGameCtnZoneFrontier frontier:
                    if (frontier.BlockInfoFrontier is not null)
                    {
                        collection.TerrainZones.Add(frontier.BlockInfoFrontier.Ident.Id, frontier);
                    }
                    break;
                case CGameCtnZoneFlat flat:
                    if (flat.BlockInfoFlat is not null)
                    {
                        collection.TerrainZones.Add(flat.BlockInfoFlat.Ident.Id, flat);
                    }
                    break;
            }
        }

        var modifiersFolder = Path.Combine(dataDirPath, collection.Id, "ConstructionDecorationTerrainModifier");

        if (Directory.Exists(modifiersFolder))
        {
            foreach (var modifierFile in Directory.EnumerateFiles(modifiersFolder, "*.Gbx"))
            {
                if (Gbx.ParseNode(modifierFile) is not CGameCtnDecorationTerrainModifier modifier)
                {
                    continue;
                }

                var name = modifier.IdName ?? string.Empty;

                if (name.StartsWith("TerrainModifier", StringComparison.OrdinalIgnoreCase) && name.Length > 15)
                {
                    name = name.Substring(15);
                }

                collection.TerrainModifiers.Add(name);
            }
        }

        // currently unused
        foreach (var modifier in collection.Node.ReplacementTerrainModifiers ?? [])
        {
            foreach (var mat in modifier.Node?.Remapping?.Fids?.Select(x => x.Name) ?? [])
            {
                collection.TerrainModifierMaterials.Add(mat);
            }
        }

        foreach (var blockInfoFilePath in folderBlockInfoPaths.AsParallel())
        {
            if (Gbx.ParseHeaderNode(blockInfoFilePath) is not CGameCtnBlockInfo blockInfoNode)
            {
                continue;
            }

            if (blockInfoNode is CGameCtnBlockInfoPylon)
            {
                collection.Pylon = blockInfoNode.Ident.Id;
            }

            var webpData = default(byte[]);

            if (blockInfoNode.Icon is not null || blockInfoNode.IconWebP is not null)
            {
                using var iconStream = new MemoryStream();
                blockInfoNode.ExportIcon(iconStream, SkiaSharp.SKEncodedImageFormat.Webp, 100);
                webpData = iconStream.ToArray();
            }

            var dirs = blockInfoNode.PageName?.Split(separator, StringSplitOptions.RemoveEmptyEntries) ?? [];
            var currentDirs = collection.BlockDirectories;

            var blockName = stadium2Blocks?.Contains(blockInfoFilePath) == true
                ? (Gbx.ParseNode(blockInfoFilePath) as CGameCtnBlockInfo)?.Ident.Id ?? throw new Exception("BlockInfo is null")
                : blockInfoNode.Ident.Id;

            if (dirs.Length == 0)
            {
                collection.RootBlocks.TryAdd(blockName, new BlockInfoModel
                {
                    Name = blockName,
                    NodeHeader = blockInfoNode,
                    GbxFilePath = blockInfoFilePath,
                    WebpIcon = webpData,
                    TerrainZone = collection.TerrainZones.GetValueOrDefault(blockName)
                });
                continue;
            }

            for (int i = 0; i < dirs.Length; i++)
            {
                var dir = dirs[i];

                var directory = currentDirs.GetOrAdd(dir, new BlockDirectoryModel
                {
                    Name = dir
                });

                if (i == dirs.Length - 1)
                {
                    directory.Blocks.TryAdd(blockName, new BlockInfoModel
                    {
                        Name = blockName,
                        NodeHeader = blockInfoNode,
                        GbxFilePath = blockInfoFilePath,
                        WebpIcon = webpData,
                        TerrainZone = collection.TerrainZones.GetValueOrDefault(blockName)
                    });
                }

                currentDirs = directory.Directories;
            }
        }

        collection.IsLoaded = true;
    }

    private string GetFilePath(GbxRefTable refTable, string filePath)
    {
        var ancestor = string.Concat(Enumerable.Repeat(".." + Path.DirectorySeparatorChar, refTable.AncestorLevel));

        return string.IsNullOrEmpty(refTable.FileSystemPath)
            ? Path.Combine(ancestor, filePath)
            : Path.Combine(refTable.FileSystemPath, ancestor, filePath);
    }

    private string GetFilePath(GbxRefTable refTable, UnlinkedGbxRefTableFile file) => GetFilePath(refTable, file.FilePath);
    public string GetFullFilePath(GbxRefTable refTable, UnlinkedGbxRefTableFile file) => Path.GetFullPath(GetFilePath(refTable, file));
}
