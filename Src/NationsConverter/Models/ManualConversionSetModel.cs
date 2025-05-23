﻿using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverterShared.Models;
using YamlDotNet.Serialization;

namespace NationsConverter.Models;

[YamlSerializable]
public sealed class ManualConversionSetModel
{
    private string? environment;

    public string Environment
    {
        get => string.IsNullOrWhiteSpace(environment)
            ? throw new InvalidOperationException("Environment cannot be null, empty, or white space") : environment;
        set => environment = value;
    }

    private readonly HashSet<string> blockNamesNotFound = [];

    public string? DefaultZoneBlock { get; set; }
    public Dictionary<string, string> BlockTerrainModifiers { get; set; } = [];
    public float DecorationYOffset { get; set; }
    public Dictionary<string, ManualConversionDecorationModel> Decorations { get; set; } = [];
    public HashSet<string>? TerrainModifiers { get; set; }
    public float WaterHeight { get; set; }
    public Dictionary<string, ManualConversionModel> Blocks { get; set; } = [];
    public int PillarOffset { get; set; }

    public ManualConversionSetModel Fill(ConversionSetModel conversionSet)
    {
        environment ??= conversionSet.Environment;
        DefaultZoneBlock ??= conversionSet.DefaultZoneBlock;
        
        foreach (var (size, deco) in conversionSet.Decorations)
        {
            if (!Decorations.TryGetValue(size, out var manualDeco))
            {
                manualDeco = new ManualConversionDecorationModel();
                Decorations.Add(size, manualDeco);
            }

            manualDeco.BaseHeight ??= deco.BaseHeight;
        }

        TerrainModifiers ??= conversionSet.TerrainModifiers;

        foreach (var (block, conversion) in conversionSet.Blocks)
        {
            if (!Blocks.TryGetValue(block, out var manualConversion) || manualConversion is null)
            {
                manualConversion = new ManualConversionModel();
                Blocks[block] = manualConversion;
            }

            manualConversion ??= new ManualConversionModel();
            manualConversion.PageName ??= conversion.PageName;

            if (conversion.Ground is not null)
            {
                manualConversion.Ground ??= new ManualConversionModifierModel();
                manualConversion.Ground.Units = conversion.Ground.Units;
                manualConversion.Ground.Size = conversion.Ground.Size;
                manualConversion.Ground.Variants = conversion.Ground.Variants;
                manualConversion.Ground.SubVariants = conversion.Ground.SubVariants;
                manualConversion.Ground.Clips = conversion.Ground.Clips;
                manualConversion.Ground.SpawnPos = conversion.Ground.SpawnPos;
                manualConversion.Ground.WaterUnits = conversion.Ground.WaterUnits;
                manualConversion.Ground.PlacePylons = conversion.Ground.PlacePylons;
                manualConversion.Ground.AcceptPylons = conversion.Ground.AcceptPylons;
                manualConversion.Ground.TerrainModifierUnits = conversion.Ground.TerrainModifierUnits;
            }

            if (conversion.Air is not null)
            {
                manualConversion.Air ??= new ManualConversionModifierModel();
                manualConversion.Air.Units = conversion.Air.Units;
                manualConversion.Air.Size = conversion.Air.Size;
                manualConversion.Air.Variants = conversion.Air.Variants;
                manualConversion.Air.SubVariants = conversion.Air.SubVariants;
                manualConversion.Air.Clips = conversion.Air.Clips;
                manualConversion.Air.SpawnPos = conversion.Air.SpawnPos;
                manualConversion.Air.WaterUnits = conversion.Air.WaterUnits;
                manualConversion.Air.PlacePylons = conversion.Air.PlacePylons;
                manualConversion.Air.AcceptPylons = conversion.Air.AcceptPylons;
                manualConversion.Air.TerrainModifierUnits = conversion.Air.TerrainModifierUnits;
            }

            manualConversion.Units = conversion.Units;
            manualConversion.Size = conversion.Size;
            manualConversion.Variants = conversion.Variants;
            manualConversion.SubVariants = conversion.SubVariants;
            manualConversion.Clips = conversion.Clips;
            manualConversion.SpawnPos = conversion.SpawnPos;
            manualConversion.ZoneHeight = conversion.ZoneHeight;
            manualConversion.Pylon = conversion.Pylon;
            manualConversion.Waypoint = conversion.Waypoint;
            manualConversion.Modifiable ??= conversion.Modifiable;
            manualConversion.NotModifiable ??= conversion.NotModifiable;
            manualConversion.WaterUnits = conversion.WaterUnits;
            manualConversion.Road = conversion.Road;
            manualConversion.PlacePylons = conversion.PlacePylons;
            manualConversion.AcceptPylons = conversion.AcceptPylons;
            manualConversion.TerrainModifierUnits = conversion.TerrainModifierUnits;

            if (conversion.Skin is not null)
            {
                manualConversion.Skin ??= new ManualConversionSkinModel();
            }
        }

        return this;
    }

    public IEnumerable<KeyValuePair<CGameCtnBlock, ManualConversionModel>> GetBlockConversionPairs(CGameCtnChallenge map, ILogger logger)
    {
        foreach (var block in map.GetBlocks())
        {
            if (Blocks.TryGetValue(block.Name, out var conversion) && conversion is not null)
            {
                yield return KeyValuePair.Create(block, conversion);
            }
            else if (blockNamesNotFound.Add(block.Name))
            {
                logger.LogWarning("Block {BlockName} not found in conversion set!", block.Name);
            }
        }
    }
}
