namespace NationsConverterWeb.Models;

public sealed class DiscordUser
{
    public ulong Id { get; set; }
    public required string Username { get; set; }
    public string? GlobalName { get; set; }
    public string? AvatarHash { get; set; }

    public int UserId { get; set; } // Required foreign key property
    public User User { get; set; } = null!; // Required reference navigation to principal
}
