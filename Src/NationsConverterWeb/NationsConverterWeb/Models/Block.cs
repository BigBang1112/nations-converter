using System.ComponentModel.DataAnnotations;

namespace NationsConverterWeb.Models;

public sealed class Block
{
    public int Id { get; set; }

    [Required]
    [StringLength(byte.MaxValue)]
    public required string Name { get; set; }

    [Required]
    [StringLength(short.MaxValue)]
    public required string PageName { get; set; }

    [Required]
    public required GameEnvironment Environment { get; set; }

    [Required]
    public required ConverterCategory Category { get; set; }

    [Required]
    public required ConverterSubCategory SubCategory { get; set; }

    [StringLength(short.MaxValue)]
    public string? Description { get; set; }

    public string? IconWebp { get; set; }

    public User? AssignedTo { get; set; }

    [Required]
    public required DateTimeOffset CreatedAt { get; set; }

    public ICollection<BlockItem> Items { get; set; } = [];
}
