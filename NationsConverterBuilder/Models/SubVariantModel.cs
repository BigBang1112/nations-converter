using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Scene;

namespace NationsConverterBuilder.Models;

public sealed class SubVariantModel
{
    public required External<CSceneMobil> Node { get; init; }
    public required string CollectionName { get; init; }
    public required string DirectoryPath { get; init; }
    public required string ModifierType { get; init; }
    public required CGameCtnBlockInfo BlockInfo { get; init; }
    public required int VariantIndex { get; init; }
    public required int SubVariantIndex { get; init; }
    public required byte[]? WebpData { get; init; }
    public required string BlockName { get; init; }
}
