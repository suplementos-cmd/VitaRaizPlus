namespace SalesCobrosGeo.Web.Models.Administration;

/// <summary>
/// DTO compuesto para crear/editar usuario con asignaciones RBAC
/// unificadas en un solo guardado
/// </summary>
public sealed class UserWithRbacInput
{
    // ═══════════════════════════════════════════════════════════════════
    // DATOS BÁSICOS DEL USUARIO (heredados de UserAdminInput)
    // ═══════════════════════════════════════════════════════════════════
    public string? OriginalUsername { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Theme { get; set; } = "root";
    public string Role { get; set; } = "FULL";
    public string RoleLabel { get; set; } = "Acceso total";
    public bool IsActive { get; set; } = true;
    public bool TwoFactorEnabled { get; set; }
    public string? Password { get; set; }
    public List<string> Permissions { get; set; } = [];

    // ═══════════════════════════════════════════════════════════════════
    // ROLES RBAC (nuevo)
    // ═══════════════════════════════════════════════════════════════════
    /// <summary>
    /// Roles RBAC a asignar al usuario con fechas de vigencia
    /// </summary>
    public List<UserRoleAssignmentInput> RbacRoles { get; set; } = [];

    // ═══════════════════════════════════════════════════════════════════
    // PERMISOS CUSTOM (nuevo)
    // ═══════════════════════════════════════════════════════════════════
    /// <summary>
    /// Permisos personalizados (grants/denies) para el usuario con fechas de vigencia
    /// </summary>
    public List<UserPermissionGrantInput> CustomPermissions { get; set; } = [];

    /// <summary>
    /// Convierte a UserAdminInput (legacy) para compatibilidad con código existente
    /// </summary>
    public UserAdminInput ToBasicInput() => new()
    {
        OriginalUsername = OriginalUsername,
        Username = Username,
        DisplayName = DisplayName,
        Zone = Zone,
        Theme = Theme,
        Role = Role,
        RoleLabel = RoleLabel,
        IsActive = IsActive,
        TwoFactorEnabled = TwoFactorEnabled,
        Password = Password,
        Permissions = Permissions
    };
}

/// <summary>
/// Asignación de rol RBAC con vigencia temporal
/// </summary>
public sealed class UserRoleAssignmentInput
{
    public int RoleId { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

/// <summary>
/// Grant/Deny de permiso custom con vigencia temporal
/// </summary>
public sealed class UserPermissionGrantInput
{
    public int PermissionId { get; set; }
    public bool IsGranted { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

/// <summary>
/// ViewModel para el editor de usuario con catálogos RBAC incluidos
/// </summary>
public sealed record UserWithRbacEditViewModel(
    string Title,
    UserWithRbacInput Input,
    IReadOnlyList<string> AvailablePermissions,
    IReadOnlyList<string> AvailableRoles,
    IReadOnlyList<string> AvailableThemes,
    // Catálogos RBAC
    IReadOnlyList<Services.Rbac.RoleDto> RbacRoles,
    IReadOnlyList<Services.Rbac.PermissionDto> RbacPermissions,
    // Asignaciones actuales (para modo edición)
    IReadOnlyList<Services.Rbac.UserRoleDto> CurrentUserRoles,
    IReadOnlyList<Services.Rbac.UserPermissionDto> CurrentUserPermissions
);
