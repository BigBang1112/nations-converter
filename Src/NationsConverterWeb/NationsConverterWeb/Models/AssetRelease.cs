using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace NationsConverterWeb.Models;

[Index(nameof(Name))]
public sealed class AssetRelease
{
    public int Id { get; set; }

    [Required]
    [StringLength(64)]
    public required string Name { get; set; }

    [Required]
    public GameEnvironment Environment { get; set; } = default!;
    public string EnvironmentId { get; set; } = default!;

    [Required]
    public required byte[] Data { get; set; }

    [Required]
    public required DateTimeOffset ReleasedAt { get; set; }
}
