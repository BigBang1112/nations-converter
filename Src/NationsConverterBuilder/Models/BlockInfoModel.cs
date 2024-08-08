using GBX.NET.Engines.Game;

namespace NationsConverterBuilder.Models;

internal sealed class BlockInfoModel
{
    public required string Name { get; set; }
    public required CGameCtnBlockInfo NodeHeader { get; set; }
    public required string GbxFilePath { get; set; }
    public required byte[]? WebpIcon { get; set; }
    public required CGameCtnZone? TerrainZone { get; set; }
}
