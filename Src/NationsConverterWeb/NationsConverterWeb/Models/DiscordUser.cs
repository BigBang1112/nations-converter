using System.ComponentModel.DataAnnotations;

namespace NationsConverterWeb.Models;

public sealed class DiscordUser
{
    public ulong Id { get; set; }

    [Required]
    [StringLength(byte.MaxValue)]
    public required string Username { get; set; }

    [StringLength(byte.MaxValue)]
    public string? GlobalName { get; set; }

    [StringLength(byte.MaxValue)]
    public string? AvatarHash { get; set; }

    [Required]
    public required DateTimeOffset ConnectedAt { get; set; }

    public int UserId { get; set; } // Required foreign key property
    public User User { get; set; } = null!; // Required reference navigation to principal
}
