using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace NationsConverterWeb.Models;

[Index(nameof(Name))]
public sealed class Block
{
    public int Id { get; set; }

    [Required]
    [StringLength(96)]
    public required string Name { get; set; }

    [Required]
    [StringLength(short.MaxValue)]
    public required string PageName { get; set; }

    [Required]
    public required GameEnvironment Environment { get; set; }
    public string EnvironmentId { get; set; } = default!;

    [Required]
    public required ConverterCategory Category { get; set; }
    public string CategoryId { get; set; } = default!;

    [Required]
    public required ConverterSubCategory SubCategory { get; set; }
    public string SubCategoryId { get; set; } = default!;

    [StringLength(short.MaxValue)]
    public string? Description { get; set; }

    [StringLength(ushort.MaxValue)]
    public string? IconWebp { get; set; }

    public User? AssignedTo { get; set; }
    public int? AssignedToId { get; set; }

    public DateTimeOffset? AssignedAt { get; set; }

    public bool HasUpload { get; set; }
    public bool IsDone { get; set; }

    [Required]
    public required DateTimeOffset CreatedAt { get; set; }

    public ICollection<BlockItem> Items { get; set; } = [];
}
