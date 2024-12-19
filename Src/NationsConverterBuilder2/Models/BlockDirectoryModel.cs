using System.Collections.Concurrent;

namespace NationsConverterBuilder2.Models;

internal sealed class BlockDirectoryModel
{
    public required string Name { get; set; }
    public ConcurrentDictionary<string, BlockDirectoryModel> Directories { get; } = new();
    public ConcurrentDictionary<string, BlockInfoModel> Blocks { get; } = new();
}
