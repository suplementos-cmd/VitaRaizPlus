namespace SalesCobrosGeo.Api.Models.Security;

/// <summary>
/// Módulo del sistema (Dashboard, Ventas, Cobros, etc.)
/// </summary>
public record Module
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

/// <summary>
/// Acción disponible en el sistema (VIEW, CREATE, UPDATE, DELETE, etc.)
/// </summary>
public record ActionModel
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty; // READ, WRITE, WORKFLOW, SCOPE, ADMIN
    public bool IsActive { get; init; }
}

/// <summary>
/// Permiso = Módulo + Acción (ejemplo: "sales:create", "dashboard:view")
/// </summary>
public record Permission
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;  // formato: "{module}:{action}"
    public int ModuleId { get; init; }
    public int ActionId { get; init; }
    public string Description { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

/// <summary>
/// Rol dinámico del sistema
/// </summary>
public record Role
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsSystem { get; init; }  // true = no se puede eliminar
}

/// <summary>
/// Mapeo de roles a permisos (N:M)
/// </summary>
public record RolePermission
{
    public int Id { get; init; }
    public int RoleId { get; init; }
    public int PermissionId { get; init; }
    public bool IsGranted { get; init; }  // true = otorga, false = deniega explícitamente
}

/// <summary>
/// Asignación de roles a usuarios con fechas de vigencia
/// </summary>
public record UserRole
{
    public int Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int RoleId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool IsActive { get; init; }
    
    /// <summary>
    /// Verifica si el rol está actualmente activo según las fechas
    /// </summary>
    public bool IsCurrentlyActive() => 
        IsActive && 
        StartDate <= DateTime.UtcNow && 
        (EndDate == null || EndDate.Value >= DateTime.UtcNow);
}

/// <summary>
/// Permisos custom por usuario (override de roles)
/// </summary>
public record UserPermission
{
    public int Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int PermissionId { get; init; }
    public bool IsGranted { get; init; }  // true = agregar permiso, false = quitar permiso
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    
    /// <summary>
    /// Verifica si el permiso custom está actualmente válido según las fechas
    /// </summary>
    public bool IsCurrentlyValid() =>
        (StartDate == null || StartDate.Value <= DateTime.UtcNow) &&
        (EndDate == null || EndDate.Value >= DateTime.UtcNow);
}

/// <summary>
/// Resultado de evaluación de permisos con trazabilidad
/// </summary>
public class PermissionEvaluationResult
{
    public string UserName { get; set; } = string.Empty;
    public string PermissionCode { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
    public List<string> GrantedBy { get; set; } = new();
    public List<string> DeniedBy { get; set; } = new();
    public string? Reason { get; set; }
}
