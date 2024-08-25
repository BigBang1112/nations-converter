using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using NationsConverterWeb;
using NationsConverterWeb.Authentication;
using NationsConverterWeb.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/";
        options.AccessDeniedPath = "/thank-you";
    })
    .AddDiscord(options =>
    {
        options.ClientId = builder.Configuration["Discord:ClientId"] ?? throw new InvalidOperationException("Discord ClientId is missing");
        options.ClientSecret = builder.Configuration["Discord:ClientSecret"] ?? throw new InvalidOperationException("Discord ClientSecret is missing");
        options.AccessDeniedPath = "/";
        options.ClaimActions.MapJsonKey(DiscordAdditionalClaims.GlobalName, "global_name");

        options.Events.OnCreatingTicket = DiscordAuthenticationTicket.OnCreatingTicketAsync;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionStr = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionStr, ServerVersion.AutoDetect(connectionStr));
});

builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformation>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
}

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

app.UseStaticFiles();

app.MapGet("/login-discord", async (HttpContext context) =>
{
    await context.ChallengeAsync(DiscordAuthenticationDefaults.AuthenticationScheme, new()
    {
        RedirectUri = "/dashboard"
    });
});

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Redirect("/");
});

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(NationsConverterWeb.Client._Imports).Assembly);

app.Run();
