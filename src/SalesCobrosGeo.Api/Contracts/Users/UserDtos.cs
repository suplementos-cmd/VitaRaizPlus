using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Contracts.Users;

/// <summary>
/// DTO para retornar información de un usuario
/// </summary>
public sealed record UserDto(
    string UserName,
    string DisplayName,
    UserRole Role,
    bool IsActive,
    int ActiveRoleCount,
    int CustomPermissionCount
);

/// <summary>
/// DTO para crear un nuevo usuario
/// </summary>
public sealed record CreateUserDto(
    string UserName,
    string Password,
    string DisplayName,
    UserRole Role,
    bool IsActive = true
);

/// <summary>
/// DTO para actualizar un usuario existente
/// </summary>
public sealed record UpdateUserDto(
    string? NewPassword,
    string? DisplayName,
    UserRole? Role,
    bool? IsActive
);

/// <summary>
/// DTO detallado de usuario con roles y permisos RBAC
/// </summary>
public sealed record UserDetailDto(
    string UserName,
    string DisplayName,
    UserRole Role,
    bool IsActive,
    RbacUserSummary RbacSummary
);

/// <summary>
/// Resumen de RBAC para un usuario
/// </summary>
public sealed record RbacUserSummary(
    int TotalRoles,
    int ActiveRoles,
    int ExpiredRoles,
    int FutureRoles,
    int GrantedPermissions,
    int DeniedPermissions,
    string[] RoleCodes,
    string[] EffectivePermissionCodes
);
