using GBX.NET.Engines.Game;
using System.Collections.Concurrent;

namespace NationsConverterBuilder.Models;

internal sealed class CollectionModel
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public required CGameCtnCollection Node { get; set; }
    public ConcurrentDictionary<string, BlockDirectoryModel> BlockDirectories { get; } = new();
    public ConcurrentDictionary<string, BlockInfoModel> RootBlocks { get; } = new();
    public bool IsLoaded { get; set; }
}
