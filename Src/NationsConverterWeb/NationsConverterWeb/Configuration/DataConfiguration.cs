using Microsoft.EntityFrameworkCore;

namespace NationsConverterWeb.Configuration;

public static class DataConfiguration
{
    public static void AddDataServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            var connectionStr = config.GetConnectionString("DefaultConnection");
            options.UseMySql(connectionStr, ServerVersion.AutoDetect(connectionStr));
        });
    }

    public static void MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.Migrate();
        }
    }
}
