using Microsoft.EntityFrameworkCore;
using NationsConverterWeb.Models;

namespace NationsConverterWeb;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<DiscordUser> DiscordUsers { get; set; }
    public DbSet<Block> Blocks { get; set; }
    public DbSet<BlockItem> BlockItems { get; set; }
    public DbSet<GameEnvironment> GameEnvironments { get; set; }
    public DbSet<ConverterCategory> ConverterCategories { get; set; }
    public DbSet<ConverterSubCategory> ConverterSubCategories { get; set; }
    public DbSet<ItemUpload> ItemUploads { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
