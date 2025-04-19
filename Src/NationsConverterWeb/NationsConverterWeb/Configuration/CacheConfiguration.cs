namespace NationsConverterWeb.Configuration;

public static class CacheConfiguration
{
    public static void AddCacheServices(this IServiceCollection services)
    {
        services.AddHybridCache();
    }
}
