namespace NationsConverterWeb.Models;

public sealed class ConverterCategory
{
    public required string Id { get; set; }

    public ICollection<Block> Blocks { get; set; } = [];
}
