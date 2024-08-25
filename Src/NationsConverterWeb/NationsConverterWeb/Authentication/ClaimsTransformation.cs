using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace NationsConverterWeb.Authentication;

public class ClaimsTransformation : IClaimsTransformation
{
    private readonly IConfiguration config;

    public ClaimsTransformation(IConfiguration config)
    {
        this.config = config;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
        {
            return Task.FromResult(principal);
        }

        var snowflake = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (snowflake is null)
        {
            return Task.FromResult(principal);
        }

        var admins = config.GetSection("Discord:Admins").Get<HashSet<string>>();

        if (admins?.Contains(snowflake) == true)
        {
            identity.AddClaim(new(ClaimTypes.Role, "Admin"));
        }

        var developers = config.GetSection("Discord:Developers").Get<HashSet<string>>();

        if (developers?.Contains(snowflake) == true)
        {
            identity.AddClaim(new(ClaimTypes.Role, "Developer"));
        }

        var modelers = config.GetSection("Discord:Modelers").Get<HashSet<string>>();

        if (modelers?.Contains(snowflake) == true)
        {
            identity.AddClaim(new(ClaimTypes.Role, "Modeler"));
        }

        return Task.FromResult(principal);
    }
}