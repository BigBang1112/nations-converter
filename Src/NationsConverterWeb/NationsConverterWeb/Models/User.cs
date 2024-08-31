using System.ComponentModel.DataAnnotations;

namespace NationsConverterWeb.Models;

public sealed class User
{
    public int Id { get; set; }
    public DiscordUser? DiscordUser { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsDeveloper { get; set; }
    public bool IsModeler { get; set; }

    [Required]
    public required DateTimeOffset JoinedAt { get; set; }

    public bool IsStaff => IsAdmin || IsDeveloper || IsModeler;
}
