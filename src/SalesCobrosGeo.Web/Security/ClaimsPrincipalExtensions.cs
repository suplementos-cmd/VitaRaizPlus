using System.Security.Claims;

namespace SalesCobrosGeo.Web.Security;

public static class ClaimsPrincipalExtensions
{
    public static bool HasPermission(this ClaimsPrincipal user, string permission)
    {
        return user.IsInRole(AppRoles.Full) || user.HasClaim(AppClaimTypes.Permission, permission);
    }

    public static string GetDisplayName(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.GivenName)
            ?? user.Identity?.Name
            ?? "Usuario";
    }

    public static string GetDisplayRole(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(AppClaimTypes.DisplayRole)
            ?? (user.IsInRole(AppRoles.Full) ? "Acceso total" : "Operativo");
    }

    public static string GetTheme(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(AppClaimTypes.Theme) ?? "root";
    }
}
