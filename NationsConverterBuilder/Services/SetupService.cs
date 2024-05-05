using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Imaging.SkiaSharp;
using NationsConverterBuilder.Models;

namespace NationsConverterBuilder.Services;

internal interface ISetupService
{
    Dictionary<string, CollectionModel> Collections { get; }

    Task SetupCollectionsAsync(CancellationToken cancellationToken = default);
    Task SetupCollectionAsync(CollectionModel collection, CancellationToken cancellationToken = default);
}

internal sealed class SetupService : ISetupService
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
            
            Collections.Add(collection.Collection, new CollectionModel
            {
                Id = collection.Collection,
                DisplayName = collection.DisplayName ?? collection.Collection,
                Node = collection
            });
        }
    }

    public async Task SetupCollectionAsync(CollectionModel collection, CancellationToken cancellationToken = default)
    {
        if (collection.IsLoaded || collection.Node.FolderBlockInfo is null)
        {
            return;
        }
        
        foreach (var blockInfoFilePath in Directory.EnumerateFiles(Path.Combine(dataDirPath, collection.Node.FolderBlockInfo), "*.Gbx", SearchOption.AllDirectories))
        {
            var blockInfoNode = (CGameCtnBlockInfo?)Gbx.ParseHeaderNode(blockInfoFilePath);

            if (blockInfoNode is null)
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
                    Node = blockInfoNode,
                    WebpIcon = webpData
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
                        Node = blockInfoNode,
                        WebpIcon = webpData
                    });
                }

                currentDirs = directory.Directories;
            }
        }

        collection.IsLoaded = true;
    }
}
