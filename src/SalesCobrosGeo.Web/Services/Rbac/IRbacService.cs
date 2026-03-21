namespace SalesCobrosGeo.Web.Services.Rbac;

/// <summary>
/// Servicio para gestión del sistema RBAC (Roles, Permisos, Asignaciones)
/// </summary>
public interface IRbacService
{
    // ───────────────────────────────────────────────────────────────────
    // ROLES
    // ───────────────────────────────────────────────────────────────────
    
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    Task<RoleDto?> GetRoleByIdAsync(int id);
    Task<RoleDto> CreateRoleAsync(CreateRoleDto dto);
    Task<RoleDto> UpdateRoleAsync(int id, UpdateRoleDto dto);
    Task<bool> DeleteRoleAsync(int id);
    
    // ───────────────────────────────────────────────────────────────────
    // PERMISSIONS
    // ───────────────────────────────────────────────────────────────────
    
    Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync();
    Task<IEnumerable<PermissionDto>> GetPermissionsByModuleAsync(int moduleId);
    Task<IEnumerable<ModuleDto>> GetAllModulesAsync();
    Task<IEnumerable<ActionDto>> GetAllActionsAsync();
    
    // ───────────────────────────────────────────────────────────────────
    // ROLE PERMISSIONS (Asignación Roles ↔ Permisos)
    // ───────────────────────────────────────────────────────────────────
    
    Task<IEnumerable<PermissionDto>> GetRolePermissionsAsync(int roleId);
    Task<bool> AssignPermissionToRoleAsync(int roleId, int permissionId, bool isGranted = true);
    Task<bool> RemovePermissionFromRoleAsync(int roleId, int permissionId);
    Task<bool> SetRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds);
    
    // ───────────────────────────────────────────────────────────────────
    // USER ROLES (Asignación Usuarios ↔ Roles)
    // ───────────────────────────────────────────────────────────────────
    
    Task<IEnumerable<UserRoleDto>> GetUserRolesAsync(string userName);
    Task<IEnumerable<UserRoleDto>> GetAllUserRolesAsync();
    Task<UserRoleDto> AssignRoleToUserAsync(AssignUserRoleDto dto);
    Task<bool> RemoveRoleFromUserAsync(int userRoleId);
    Task<bool> UpdateUserRoleAsync(int userRoleId, UpdateUserRoleDto dto);
    
    // ───────────────────────────────────────────────────────────────────
    // USER PERMISSIONS (Permisos custom por usuario)
    // ───────────────────────────────────────────────────────────────────
    
    Task<IEnumerable<UserPermissionDto>> GetUserCustomPermissionsAsync(string userName);
    Task<UserPermissionDto> GrantCustomPermissionAsync(GrantUserPermissionDto dto);
    Task<bool> RevokeCustomPermissionAsync(int userPermissionId);
    
    // ───────────────────────────────────────────────────────────────────
    // UTILITIES
    // ───────────────────────────────────────────────────────────────────
    
    Task<PermissionMatrixDto> GetRolePermissionMatrixAsync();
    Task<UserPermissionSummaryDto> GetUserEffectivePermissionsAsync(string userName);
    
    /// <summary>
    /// Verifica si un usuario tiene un permiso específico (consulta permisos efectivos)
    /// </summary>
    Task<bool> HasPermissionAsync(string userName, string permissionCode);
}

// ═══════════════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════════════

public record RoleDto(
    int Id,
    string Code,
    string Name,
    string Description,
    bool IsActive,
    bool IsSystem,
    int PermissionCount
);

public record CreateRoleDto(
    string Code,
    string Name,
    string Description,
    bool IsActive
);

public record UpdateRoleDto(
    string Name,
    string Description,
    bool IsActive
);

public record PermissionDto(
    int Id,
    string Code,
    string ModuleCode,
    string ModuleName,
    string ActionCode,
    string ActionName,
    string Description,
    bool IsActive
);

public record ModuleDto(
    int Id,
    string Code,
    string Name,
    string Description,
    bool IsActive
);

public record ActionDto(
    int Id,
    string Code,
    string Name,
    string Description,
    string Category,
    bool IsActive
);

public record UserRoleDto(
    int Id,
    string UserName,
    int RoleId,
    string RoleCode,
    string RoleName,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive,
    bool IsCurrentlyActive
);

public record AssignUserRoleDto(
    string UserName,
    int RoleId,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive
);

public record UpdateUserRoleDto(
    DateTime? EndDate,
    bool IsActive
);

public record UserPermissionDto(
    int Id,
    string UserName,
    int PermissionId,
    string PermissionCode,
    string PermissionDescription,
    bool IsGranted,
    DateTime? StartDate,
    DateTime? EndDate,
    DateTime? CreatedAt,
    bool IsCurrentlyValid
);

public record GrantUserPermissionDto(
    string UserName,
    int PermissionId,
    bool IsGranted,
    DateTime? StartDate,
    DateTime? EndDate
);

public record PermissionMatrixDto(
    IEnumerable<RoleDto> Roles,
    IEnumerable<PermissionDto> Permissions,
    Dictionary<int, List<int>> RolePermissionMap  // RoleId → List<PermissionId>
);

public record UserPermissionSummaryDto(
    string UserName,
    IEnumerable<RoleDto> ActiveRoles,
    IEnumerable<PermissionDto> PermissionsFromRoles,
    IEnumerable<UserPermissionDto> CustomPermissions,
    IEnumerable<string> EffectivePermissions
);
