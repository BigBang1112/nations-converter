using GBX.NET.Engines.Plug;

namespace NationsConverterBuilder.Models;

internal sealed class MaterialModel
{
    public string? Link { get; set; }
    public CPlugSurface.MaterialId? Surface { get; set; }
    public UvModifiersModel? UvModifiers { get; set; }
    public int[]? Color { get; set; }
    public bool Remove { get; set; }
    public string? Decal { get; set; }
    public UvModifiersModel? DecalUvModifiers { get; set; }
    public Dictionary<string, MaterialSubCategoryModel> SubCategories { get; set; } = [];
}
