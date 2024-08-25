using Microsoft.EntityFrameworkCore;

namespace NationsConverterWeb;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
