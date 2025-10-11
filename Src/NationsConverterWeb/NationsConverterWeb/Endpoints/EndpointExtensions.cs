using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace NationsConverterWeb.Endpoints;

public static class EndpointExtensions
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
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
                    ? env.WebRootFileProvider.GetFileInfo(Path.Combine("img", "bloc.webp")).LastModified
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

            return Results.File(release.Data, "application/zip", $"{release.Name}.nc2", lastModified: release.ReleasedAt);
        });
    }
}
