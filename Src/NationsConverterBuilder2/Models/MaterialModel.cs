using GBX.NET.Engines.Plug;

namespace NationsConverterBuilder2.Models;

internal sealed class MaterialModel
{
    public string? Link { get; set; }
    public CPlugSurface.MaterialId? Surface { get; set; }
    public UvModifiersModel? UvModifiers { get; set; }
    public int[]? Color { get; set; }
    public bool Remove { get; set; }
    public string? Decal { get; set; }
    public UvModifiersModel? DecalUvModifiers { get; set; }

    /// <summary>
    /// Make this material split the block variant into multiple item parts - especially terrain modifiers.
    /// Materials without modifiers will be combined into Ground, default material will be called GroundDefault, and the rest will be called according to the terrain modifier list.
    /// </summary>
    public Dictionary<string, string> Modifiers { get; set; } = [];

    public Dictionary<string, MaterialSubCategoryModel> SubCategories { get; set; } = [];
    
    public bool Stadium256 { get; set; }
}
