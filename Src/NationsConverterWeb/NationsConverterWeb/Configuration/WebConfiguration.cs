using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using NationsConverterWeb.Authentication;
using System.Net;

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

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        // Figures out HTTPS behind proxies
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            foreach (var knownProxy in config.GetSection("KnownProxies").Get<string[]>() ?? [])
            {
                if (IPAddress.TryParse(knownProxy, out var ipAddress))
                {
                    options.KnownProxies.Add(ipAddress);
                    continue;
                }

                foreach (var hostIpAddress in Dns.GetHostAddresses(knownProxy))
                {
                    options.KnownProxies.Add(hostIpAddress);
                }
            }
        });
    }
}
