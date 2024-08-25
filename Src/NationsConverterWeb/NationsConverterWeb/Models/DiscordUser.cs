using System.ComponentModel.DataAnnotations;

namespace NationsConverterWeb.Models;

public sealed class DiscordUser
{
    public ulong Id { get; set; }

    [Required]
    [StringLength(255)]
    public required string Username { get; set; }

    [StringLength(255)]
    public string? GlobalName { get; set; }

    [StringLength(255)]
    public string? AvatarHash { get; set; }

    public int UserId { get; set; } // Required foreign key property
    public User User { get; set; } = null!; // Required reference navigation to principal
}
