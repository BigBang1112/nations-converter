using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using NationsConverterWeb.Authentication;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using NationsConverterWeb.Components;
using NationsConverterWeb.Endpoints;

namespace NationsConverterWeb.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/";
                options.AccessDeniedPath = "/";
            })
            .AddDiscord(options =>
            {
                options.ClientId = config["Discord:ClientId"] ?? throw new InvalidOperationException("Discord ClientId is missing");
                options.ClientSecret = config["Discord:ClientSecret"] ?? throw new InvalidOperationException("Discord ClientSecret is missing");
                options.AccessDeniedPath = "/";
                options.ClaimActions.MapJsonKey(DiscordAdditionalClaims.GlobalName, "global_name");

                options.Events.OnCreatingTicket = DiscordAuthenticationTicket.OnCreatingTicketAsync;
            });

        services.AddCascadingAuthenticationState();

        services.AddDirectoryBrowser();

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    public static void UseAuthMiddleware(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
    }

    public static void UseSecurityMiddleware(this WebApplication app)
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDir);

        /*app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(dataDir),
            RequestPath = "/data",
            ServeUnknownFileTypes = true
        });*/

        app.UseDirectoryBrowser(new DirectoryBrowserOptions
        {
            FileProvider = new PhysicalFileProvider(dataDir),
            RequestPath = "/data"
        });

        app.UseStaticFiles(new StaticFileOptions()
        {
            ContentTypeProvider = new FileExtensionContentTypeProvider
            {
                Mappings = { [".mux"] = "application/octet-stream" }
            }
        });

        app.UseCookiePolicy(new CookiePolicyOptions
        {
            MinimumSameSitePolicy = SameSiteMode.Lax,
            Secure = CookieSecurePolicy.Always,
            HttpOnly = HttpOnlyPolicy.Always
        });

        if (!app.Environment.IsDevelopment())
        {
            app.UseResponseCompression();
        }
    }

    public static void UseEndpointMiddleware(this WebApplication app)
    {
        app.MapEndpoints();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
    }
}
