using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SalesCobrosGeo.Web.Security;

public static class AppPrincipalFactory
{
    public static ClaimsPrincipal BuildPrincipal(ApplicationUserSummary user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Username),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.DisplayName),
            new(ClaimTypes.Role, user.Role),
            new(AppClaimTypes.Theme, user.Theme),
            new(AppClaimTypes.DisplayRole, user.RoleLabel)
        };

        claims.AddRange(user.Permissions.Select(permission => new Claim(AppClaimTypes.Permission, permission)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
