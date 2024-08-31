namespace NationsConverterWeb.Models;

public sealed class GameEnvironment
{
    public required string Id { get; set; }

    public ICollection<Block> Blocks { get; set; } = [];
}
