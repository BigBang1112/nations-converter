using Microsoft.EntityFrameworkCore;
using NationsConverterWeb.Models;

namespace NationsConverterWeb;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<DiscordUser> DiscordUsers { get; set; }
    public DbSet<Block> Blocks { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
