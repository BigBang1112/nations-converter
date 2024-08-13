using GBX.NET;
using GBX.NET.Components;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
using GBX.NET.Imaging.SkiaSharp;
using NationsConverterBuilder.Models;

namespace NationsConverterBuilder.Services;

internal sealed class SetupService
{
    private readonly string dataDirPath;

    public SetupService(IWebHostEnvironment hostEnvironment)
    {
        dataDirPath = Path.Combine(hostEnvironment.WebRootPath, "data");
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

        foreach (var zone in collection.Node.CompleteListZoneList ?? [])
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

        var folderBlockInfoPath = Path.Combine(dataDirPath, collection.Node.FolderBlockInfo);

        foreach (var blockInfoFilePath in Directory.EnumerateFiles(folderBlockInfoPath, "*.Gbx", SearchOption.AllDirectories).AsParallel())
        {
            if (Gbx.ParseHeaderNode(blockInfoFilePath) is not CGameCtnBlockInfo blockInfoNode)
            {
                continue;
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

            if (dirs.Length == 0)
            {
                collection.RootBlocks.TryAdd(blockInfoNode.Ident.Id, new BlockInfoModel
                {
                    Name = blockInfoNode.Ident.Id,
                    NodeHeader = blockInfoNode,
                    GbxFilePath = blockInfoFilePath,
                    WebpIcon = webpData,
                    TerrainZone = collection.TerrainZones.GetValueOrDefault(blockInfoNode.Ident.Id)
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
                    directory.Blocks.TryAdd(blockInfoNode.Ident.Id, new BlockInfoModel
                    {
                        Name = blockInfoNode.Ident.Id,
                        NodeHeader = blockInfoNode,
                        GbxFilePath = blockInfoFilePath,
                        WebpIcon = webpData,
                        TerrainZone = collection.TerrainZones.GetValueOrDefault(blockInfoNode.Ident.Id)
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
