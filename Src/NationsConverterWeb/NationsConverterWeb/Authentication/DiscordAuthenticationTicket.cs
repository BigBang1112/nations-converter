using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication.OAuth;
using NationsConverterWeb.Models;
using System.Security.Claims;

namespace NationsConverterWeb.Authentication;

internal static class DiscordAuthenticationTicket
{
    public static async Task OnCreatingTicketAsync(OAuthCreatingTicketContext context)
    {
        // add or update user in DB
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        if (context.Identity is null)
        {
            return;
        }

        var snowflakeIdStr = context.Identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!ulong.TryParse(snowflakeIdStr, out var snowflakeId))
        {
            return;
        }

        var username = context.Identity.FindFirst(ClaimTypes.Name)?.Value;

        if (username is null)
        {
            return;
        }

        var avatarHash = context.Identity.FindFirst(DiscordAuthenticationConstants.Claims.AvatarHash)?.Value;
        var globalName = context.Identity.FindFirst(DiscordAdditionalClaims.GlobalName)?.Value;

        var discordUser = await dbContext.DiscordUsers.FindAsync(snowflakeId);

        if (discordUser is null)
        {
            var user = new User { JoinedAt = DateTimeOffset.UtcNow };

            discordUser = new DiscordUser
            {
                Id = snowflakeId,
                Username = username,
                User = user,
                ConnectedAt = DateTimeOffset.UtcNow
            };

            user.DiscordUser = discordUser;

            user.IsAdmin = config.GetSection("Discord:Admins").Get<HashSet<string>>()?.Contains(snowflakeIdStr) == true;
            user.IsDeveloper = config.GetSection("Discord:Developers").Get<HashSet<string>>()?.Contains(snowflakeIdStr) == true;
            user.IsModeler = config.GetSection("Discord:Modelers").Get<HashSet<string>>()?.Contains(snowflakeIdStr) == true;

            await dbContext.Users.AddAsync(user);
            await dbContext.DiscordUsers.AddAsync(discordUser);
        }
        else
        {
            discordUser.Username = username;
        }

        discordUser.AvatarHash = avatarHash;
        discordUser.GlobalName = globalName;

        await dbContext.SaveChangesAsync();
    }
}
