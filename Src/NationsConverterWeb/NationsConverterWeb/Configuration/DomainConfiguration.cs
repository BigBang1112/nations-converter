using Microsoft.AspNetCore.Authentication;
using NationsConverterWeb.Authentication;
using NationsConverterWeb.BulkFixers;

namespace NationsConverterWeb.Configuration;

public static class DomainConfiguration
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddTransient<IClaimsTransformation, ClaimsTransformation>();

        services.AddScoped<RevertMaterialPhysicsBulkFixer>();
        services.AddScoped<CheckpointTerrainModifierBulkFixer>();
    }
}
