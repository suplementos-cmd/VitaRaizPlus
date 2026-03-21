using System.Security.Claims;

namespace SalesCobrosGeo.Web.Security;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// OBSOLETO: Los permisos ahora se validan exclusivamente via RBAC en RbacPermissionHandler
    /// Este método se mantiene solo para compatibilidad temporal con código legacy
    /// </summary>
    [Obsolete("Usar [Authorize(Policy = ...)] que valida permisos via RBAC automáticamente")]
    public static bool HasPermission(this ClaimsPrincipal user, string permission)
    {
        // Ya no validar permisos aquí - todo pasa por RbacPermissionHandler
        // Retornar false para forzar validación correcta via políticas
        return false;
    }

    public static string GetDisplayName(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.GivenName)
            ?? user.Identity?.Name
            ?? "Usuario";
    }

    public static string GetDisplayRole(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(AppClaimTypes.DisplayRole) ?? "Operativo";
    }

    public static string GetTheme(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(AppClaimTypes.Theme) ?? "root";
    }
}
