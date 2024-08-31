namespace NationsConverterWeb.Models;

public sealed class ConverterSubCategory
{
    public required string Id { get; set; }

    public ICollection<Block> Blocks { get; set; } = [];
}
