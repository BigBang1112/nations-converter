namespace NationsConverterBuilder2.Models;

internal sealed class MaterialSubCategoryModel
{
    public string? Link { get; set; }
    public UvModifiersModel? UvModifiers { get; set; }
    public int[]? Color { get; set; }
    public bool Remove { get; set; }
    public string? Decal { get; set; }
    public UvModifiersModel? DecalUvModifiers { get; set; }
    public bool Stadium256 { get; set; }
}
