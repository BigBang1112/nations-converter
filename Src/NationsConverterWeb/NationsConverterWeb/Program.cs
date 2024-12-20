using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using NationsConverterWeb;
using NationsConverterWeb.Authentication;
using NationsConverterWeb.Components;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Caching.Hybrid;
using NationsConverterWeb.BulkFixers;

GBX.NET.Gbx.LZO = new GBX.NET.LZO.MiniLZO();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/";
        options.AccessDeniedPath = "/";
    })
    .AddDiscord(options =>
    {
        options.ClientId = builder.Configuration["Discord:ClientId"] ?? throw new InvalidOperationException("Discord ClientId is missing");
        options.ClientSecret = builder.Configuration["Discord:ClientSecret"] ?? throw new InvalidOperationException("Discord ClientSecret is missing");
        options.AccessDeniedPath = "/";
        options.ClaimActions.MapJsonKey(DiscordAdditionalClaims.GlobalName, "global_name");

        options.Events.OnCreatingTicket = DiscordAuthenticationTicket.OnCreatingTicketAsync;
    });

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddDirectoryBrowser();

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

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    var connectionStr = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionStr, ServerVersion.AutoDetect(connectionStr));
});

builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformation>();

builder.Services.AddScoped<RevertMaterialPhysicsBulkFixer>();
builder.Services.AddScoped<CheckpointTerrainModifierBulkFixer>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddOpenTelemetry()
    .WithMetrics(options =>
    {
        options
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter();

        options.AddMeter("System.Net.Http");
    })
    .WithTracing(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.SetSampler<AlwaysOnSampler>();
        }

        options
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });
builder.Services.AddMetrics();

#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018

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

app.UseAuthentication();
app.UseAuthorization();

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

if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

app.MapGet("/login-discord", async (HttpContext context) =>
{
    await context.ChallengeAsync(DiscordAuthenticationDefaults.AuthenticationScheme, new()
    {
        RedirectUri = "/dashboard"
    });
}).AllowAnonymous();

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Redirect("/");
});

app.MapGet("/blockicon/{name}", async (
    HttpContext context,
    AppDbContext db,
    IWebHostEnvironment env,
    HybridCache cache,
    string? name) =>
{
    if (name is null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    try
    {
        var block = await cache.GetOrCreateAsync($"blockicon_{name}", async token =>
        {
            return await db.Blocks
                .Select(x => new { x.Name, x.IconWebp, x.CreatedAt })
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == name, token);
        }, new() { Expiration = TimeSpan.FromMinutes(5) }, cancellationToken: context.RequestAborted);

        if (block is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var lastModified = string.IsNullOrWhiteSpace(block.IconWebp)
            ? env.WebRootFileProvider.GetFileInfo(Path.Combine("img, bloc.webp")).LastModified
            : block.CreatedAt;

        var eTag = $"\"{lastModified.Ticks}\"";

        if (context.Request.Headers.TryGetValue("If-None-Match", out var requestEtag) && requestEtag == eTag)
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        if (context.Request.Headers.TryGetValue("If-Modified-Since", out var ifModifiedSince))
        {
            if (DateTime.TryParse(ifModifiedSince, out var modifiedSince) && modifiedSince >= lastModified)
            {
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                return;
            }
        }

        context.Response.ContentType = "image/webp";
        context.Response.Headers.ETag = eTag;
        context.Response.Headers.LastModified = lastModified.ToString("R");

        if (string.IsNullOrWhiteSpace(block.IconWebp))
        {
            await context.Response.SendFileAsync(Path.Combine(env.WebRootPath, "img", "bloc.webp"), context.RequestAborted);
            return;
        }

        var iconBytes = Convert.FromBase64String(block.IconWebp);

        await context.Response.Body.WriteAsync(iconBytes, context.RequestAborted);
    }
    catch (OperationCanceledException)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    }
});

app.MapGet("/assets/{name}", async (string name, AppDbContext db, CancellationToken cancellationToken) =>
{
    var release = await db.AssetReleases
        .AsNoTracking()
        .OrderByDescending(x => x.ReleasedAt)
        .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

    if (release is null)
    {
        return Results.NotFound();
    }

    return Results.File(release.Data, "application/zip", $"{release.Name}.zip", lastModified: release.ReleasedAt);
});

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(NationsConverterWeb.Client._Imports).Assembly);

app.Run();
