using Microsoft.AspNetCore.CookiePolicy;
using NationsConverterWeb.Components;
using NationsConverterWeb.Endpoints;

namespace NationsConverterWeb.Configuration;

public static class MiddlewareConfiguration
{
    public static void UseMiddleware(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
            app.UseForwardedHeaders();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseForwardedHeaders();
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        // Response compression should be early
        if (!app.Environment.IsDevelopment())
        {
            app.UseResponseCompression();
        }

        // Cookie policy before authentication
        app.UseCookiePolicy(new CookiePolicyOptions
        {
            MinimumSameSitePolicy = SameSiteMode.Lax,
            Secure = CookieSecurePolicy.Always,
            HttpOnly = HttpOnlyPolicy.Always
        });

        // Authentication and authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Antiforgery after auth
        app.UseAntiforgery();

        app.MapStaticAssets();

        // Endpoint mapping
        app.MapEndpoints();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
    }
}
