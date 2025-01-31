﻿using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using System.Collections.Immutable;
using System.Diagnostics;

namespace NationsConverter.Stages;

internal sealed class PlaceBaseZoneStage : BlockStageBase
{
    private readonly CGameCtnChallenge mapIn;
    private readonly CGameCtnChallenge mapOut;
    private readonly CustomContentManager customContentManager;
    private readonly ImmutableDictionary<Int3, string> terrainModifierZones;
    private readonly ILogger logger;
    private readonly int baseHeight;
    private readonly bool[,] occupiedZone;

    public PlaceBaseZoneStage(
        CGameCtnChallenge mapIn,
        CGameCtnChallenge mapOut,
        ManualConversionSetModel conversionSet,
        CustomContentManager customContentManager,
        ImmutableDictionary<Int3, string> terrainModifierZones,
        ILogger logger) : base(mapIn, mapOut, conversionSet, logger)
    {
        this.mapIn = mapIn;
        this.mapOut = mapOut;
        this.customContentManager = customContentManager;
        this.terrainModifierZones = terrainModifierZones;
        this.logger = logger;

        occupiedZone = new bool[mapIn.Size.X, mapIn.Size.Z];

        baseHeight = ConversionSet.Decorations
            .GetValueOrDefault($"{mapIn.Size.X}x{mapIn.Size.Y}x{mapIn.Size.Z}")?.BaseHeight ?? 0;
    }

    protected override void ConvertBlock(CGameCtnBlock block, ManualConversionModel conversion)
    {
        // If block is of zone type (ZoneHeight not null) it is automatically considered occupied
        // Block's height does not matter - tested on TMUnlimiter
        if (conversion.ZoneHeight.HasValue)
        {
            occupiedZone[block.Coord.X, block.Coord.Z] = true;
            return;
        }

        // If block has ground variant, base ground is considered occupied if one of its units is at the base height + 1
        // THX TOMEK0055 FOR ALL THE HELP!!!
        // fallbacks should be less permissive in the future
        var units = conversion.GetProperty(block, x => x.Units, fallback: true) ?? [(0, 0, 0)];

        Span<Int3> alignedUnits = stackalloc Int3[units.Length];

        var min = new Int3(int.MaxValue, 0, int.MaxValue);

        for (int i = 0; i < units.Length; i++)
        {
            var unit = units[i];
            var alignedUnit = block.Direction switch
            {
                Direction.East => (-unit.Z, unit.Y, unit.X),
                Direction.South => (-unit.X, unit.Y, -unit.Z),
                Direction.West => (unit.Z, unit.Y, -unit.X),
                _ => unit
            };

            if (alignedUnit.X < min.X)
            {
                min = min with { X = alignedUnit.X };
            }

            if (alignedUnit.Z < min.Z)
            {
                min = min with { Z = alignedUnit.Z };
            }

            alignedUnits[i] = alignedUnit;
        }

        foreach (var unit in alignedUnits)
        {
            var pos = block.Coord + unit - min;

            if (occupiedZone.GetLength(0) <= pos.X || occupiedZone.GetLength(1) <= pos.Z)
            {
                continue;
            }

            if (!occupiedZone[pos.X, pos.Z])
            {
                occupiedZone[pos.X, pos.Z] = pos.Y == baseHeight + 1 + mapIn.DecoBaseHeightOffset;
            }
        }
    }

    public override void Convert()
    {
        if (string.IsNullOrEmpty(ConversionSet.DefaultZoneBlock))
        {
            return;
        }

        logger.LogInformation("Placing zone in empty spots...");
        var watch = Stopwatch.StartNew();

        base.Convert();

        var conversion = ConversionSet.Blocks[ConversionSet.DefaultZoneBlock];

        var dirPath = string.IsNullOrWhiteSpace(conversion.PageName)
            ? ConversionSet.DefaultZoneBlock
            : Path.Combine(conversion.PageName, ConversionSet.DefaultZoneBlock);
        var itemPath = Path.Combine(dirPath, "Ground_0_0.Item.Gbx");

        for (var x = 0; x < occupiedZone.GetLength(0); x++)
        {
            for (var z = 0; z < occupiedZone.GetLength(1); z++)
            {
                if (occupiedZone[x, z])
                {
                    continue;
                }

                var pos = new Int3(x, baseHeight, z) + CenterOffset;

                if (Environment == "Stadium" && terrainModifierZones.TryGetValue((x, 0, z), out var terrainModifier))
                {
                    if (terrainModifier == "Fabric")
                    {
                        customContentManager.PlaceItem(Path.Combine("Misc", "Fabric", "Ground.Item.Gbx"), pos * BlockSize, (0, 0, 0), modernized: conversion.Modernized);
                        continue;
                    }
                }

                customContentManager.PlaceItem(itemPath, pos * BlockSize, (0, 0, 0), modernized: conversion.Modernized);

                if (conversion.ZoneHeight.HasValue)
                {
                    var waterUnits = conversion.GetPropertyDefault(x => x.Ground, x => x.WaterUnits);

                    if (waterUnits is { Length: > 0 })
                    {
                        WaterStage.PlaceWater(mapOut, pos, BlockSize, ConversionSet.WaterHeight);
                    }
                }
            }
        }

        logger.LogInformation("Placed zone in empty spots ({ElapsedMilliseconds}ms).", watch.ElapsedMilliseconds);
    }
}
