using System.ComponentModel.DataAnnotations;

namespace NationsConverterWeb.Models;

public sealed class AssetRelease
{
    public int Id { get; set; }

    [Required]
    [StringLength(byte.MaxValue)]
    public required string Name { get; set; }

    [Required]
    public GameEnvironment Environment { get; set; } = default!;
    public string EnvironmentId { get; set; } = default!;

    [Required]
    public required byte[] Data { get; set; }

    [Required]
    public required DateTimeOffset ReleasedAt { get; set; }
}
