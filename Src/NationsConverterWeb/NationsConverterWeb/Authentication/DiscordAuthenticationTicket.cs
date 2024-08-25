using Microsoft.AspNetCore.Authentication.OAuth;

namespace NationsConverterWeb.Authentication;

internal static class DiscordAuthenticationTicket
{
    public static async Task OnCreatingTicketAsync(OAuthCreatingTicketContext context)
    {
        // add or update user in DB
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
    }
}
