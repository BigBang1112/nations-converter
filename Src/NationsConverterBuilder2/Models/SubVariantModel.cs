using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Scene;

namespace NationsConverterBuilder2.Models;

internal sealed class SubVariantModel
{
    public required External<CSceneMobil>? Mobil { get; init; }
    public required CGameCtnBlockInfoMobil? Mobil2 { get; init; }
    public required string CollectionName { get; init; }
    public required string DirectoryPath { get; init; }
    public required string ModifierType { get; init; }
    public required CGameCtnBlockInfo BlockInfo { get; init; }
    public required int VariantIndex { get; init; }
    public required int SubVariantIndex { get; init; }
    public required byte[]? WebpData { get; init; }
    public required string BlockName { get; init; }
    public required string SubCategory { get; init; }
    public required string Technology { get; init; }
    public required string MapTechnology { get; init; }
    public required Int3[] Units { get; init; }
}
