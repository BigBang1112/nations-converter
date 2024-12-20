﻿using GBX.NET;

namespace NationsConverterShared.Models;

public class ConversionModifierModel
{
    public Int3[]? Units { get; set; }
    public Int3? Size { get; set; }
    public int? Variants { get; set; }
    public int[]? SubVariants { get; set; }
    public ConversionClipModel[]? Clips { get; set; }
    public ConversionClipModel[]? Clips2 { get; set; }
    public Vec3? SpawnPos { get; set; }
    public Int2[]? WaterUnits { get; set; }
    public int[]? PlacePylons { get; set; }
    public int[]? AcceptPylons { get; set; }
    public Dictionary<string, Int3[]>? TerrainModifierUnits { get; set; }
}
