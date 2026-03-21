using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SalesCobrosGeo.Api.Services;

namespace SalesCobrosGeo.Api.Security;

/// <summary>
/// Extension methods para ClaimsPrincipal con verificación de permisos RBAC
/// </summary>
public static class ClaimsPrincipalPermissionExtensions
{
    /// <summary>
    /// Verifica si el usuario tiene un permiso específico
    /// </summary>
    public static async Task<bool> HasPermissionAsync(
        this ClaimsPrincipal user, 
        IPermissionService permissionService,
        string permissionCode)
    {
        if (user?.Identity?.Name == null)
            return false;

        return await permissionService.HasPermissionAsync(user.Identity.Name, permissionCode);
    }

    /// <summary>
    /// Verifica si el usuario tiene alguno de los permisos especificados
    /// </summary>
    public static async Task<bool> HasAnyPermissionAsync(
        this ClaimsPrincipal user,
        IPermissionService permissionService,
        params string[] permissionCodes)
    {
        if (user?.Identity?.Name == null)
            return false;

        return await permissionService.HasAnyPermissionAsync(user.Identity.Name, permissionCodes);
    }

    /// <summary>
    /// Verifica si el usuario tiene todos los permisos especificados
    /// </summary>
    public static async Task<bool> HasAllPermissionsAsync(
        this ClaimsPrincipal user,
        IPermissionService permissionService,
        params string[] permissionCodes)
    {
        if (user?.Identity?.Name == null)
            return false;

        return await permissionService.HasAllPermissionsAsync(user.Identity.Name, permissionCodes);
    }

    /// <summary>
    /// Verifica si el usuario puede realizar una acción en un módulo
    /// Ejemplo: CanPerformAction("sales", "create") verifica "sales:create"
    /// </summary>
    public static async Task<bool> CanPerformActionAsync(
        this ClaimsPrincipal user,
        IPermissionService permissionService,
        string module,
        string action)
    {
        if (user?.Identity?.Name == null)
            return false;

        var permissionCode = $"{module}:{action}";
        return await permissionService.HasPermissionAsync(user.Identity.Name, permissionCode);
    }

    /// <summary>
    /// Obtiene todos los permisos del usuario
    /// </summary>
    public static async Task<IEnumerable<string>> GetPermissionsAsync(
        this ClaimsPrincipal user,
        IPermissionService permissionService)
    {
        if (user?.Identity?.Name == null)
            return Array.Empty<string>();

        return await permissionService.GetUserPermissionsAsync(user.Identity.Name);
    }
}

/// <summary>
/// Helper para obtener IPermissionService desde HttpContext
/// </summary>
public static class HttpContextPermissionExtensions
{
    /// <summary>
    /// Obtiene el servicio de permisos desde el HttpContext
    /// </summary>
    public static IPermissionService? GetPermissionService(this HttpContext httpContext)
    {
        return httpContext.RequestServices.GetService<IPermissionService>();
    }

    /// <summary>
    /// Verifica permiso usando el usuario actual y el servicio del context
    /// </summary>
    public static async Task<bool> UserHasPermissionAsync(
        this HttpContext httpContext,
        string permissionCode)
    {
        var permissionService = httpContext.GetPermissionService();
        if (permissionService == null || httpContext.User?.Identity?.Name == null)
            return false;

        return await permissionService.HasPermissionAsync(
            httpContext.User.Identity.Name, 
            permissionCode);
    }
}

/// <summary>
/// Constantes de permisos para facilitar uso en código
/// </summary>
public static class AppPermissionCodes
{
    // Dashboard
    public const string DashboardView = "dashboard:view";
    public const string DashboardExport = "dashboard:export";

    // Sales
    public const string SalesView = "sales:view";
    public const string SalesCreate = "sales:create";
    public const string SalesUpdate = "sales:update";
    public const string SalesDelete = "sales:delete";
    public const string SalesExport = "sales:export";
    public const string SalesApprove = "sales:approve";
    public const string SalesCancel = "sales:cancel";
    public const string SalesOwn = "sales:own";
    public const string SalesZone = "sales:zone";
    public const string SalesAll = "sales:all";

    // Collections
    public const string CollectionsView = "collections:view";
    public const string CollectionsCreate = "collections:create";
    public const string CollectionsUpdate = "collections:update";
    public const string CollectionsDelete = "collections:delete";
    public const string CollectionsExport = "collections:export";
    public const string CollectionsOwn = "collections:own";
    public const string CollectionsZone = "collections:zone";
    public const string CollectionsAll = "collections:all";

    // Maintenance
    public const string MaintenanceView = "maintenance:view";
    public const string MaintenanceUpdate = "maintenance:update";

    // Admin
    public const string AdminView = "admin:view";
    public const string AdminCreate = "admin:create";
    public const string AdminUpdate = "admin:update";
    public const string AdminDelete = "admin:delete";
    public const string AdminManage = "admin:manage";

    // Reports
    public const string ReportsView = "reports:view";
    public const string ReportsExport = "reports:export";
    public const string ReportsPrint = "reports:print";

    // Clients
    public const string ClientsView = "clients:view";
    public const string ClientsCreate = "clients:create";
    public const string ClientsUpdate = "clients:update";
    public const string ClientsDelete = "clients:delete";
    public const string ClientsZone = "clients:zone";
    public const string ClientsAll = "clients:all";

    // Products
    public const string ProductsView = "products:view";
    public const string ProductsCreate = "products:create";
    public const string ProductsUpdate = "products:update";
    public const string ProductsDelete = "products:delete";

    // Zones
    public const string ZonesView = "zones:view";
    public const string ZonesCreate = "zones:create";
    public const string ZonesUpdate = "zones:update";
    public const string ZonesDelete = "zones:delete";
}
