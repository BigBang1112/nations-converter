using GBX.NET.Engines.Game;

namespace NationsConverterBuilder.Models;

internal sealed class BlockInfoModel
{
    public required string Name { get; set; }
    public required CGameCtnBlockInfo Node { get; set; }
    public required byte[]? WebpIcon { get; set; }
}
