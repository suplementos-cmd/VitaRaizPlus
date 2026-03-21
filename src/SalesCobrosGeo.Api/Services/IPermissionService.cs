using SalesCobrosGeo.Api.Models.Security;

namespace SalesCobrosGeo.Api.Services;

/// <summary>
/// Servicio de gestión de permisos basado en RBAC
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Obtiene todos los permisos efectivos de un usuario (roles + custom)
    /// </summary>
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userName);
    
    /// <summary>
    /// Verifica si un usuario tiene un permiso específico
    /// </summary>
    Task<bool> HasPermissionAsync(string userName, string permissionCode);
    
    /// <summary>
    /// Evalúa un permiso con trazabilidad completa (para debugging/auditoría)
    /// </summary>
    Task<PermissionEvaluationResult> EvaluatePermissionAsync(string userName, string permissionCode);
    
    /// <summary>
    /// Obtiene los roles activos de un usuario
    /// </summary>
    Task<IEnumerable<Role>> GetUserActiveRolesAsync(string userName);
    
    /// <summary>
    /// Verifica si un usuario tiene alguno de los permisos especificados
    /// </summary>
    Task<bool> HasAnyPermissionAsync(string userName, params string[] permissionCodes);
    
    /// <summary>
    /// Verifica si un usuario tiene todos los permisos especificados
    /// </summary>
    Task<bool> HasAllPermissionsAsync(string userName, params string[] permissionCodes);
}
