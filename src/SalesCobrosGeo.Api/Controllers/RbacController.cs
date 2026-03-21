using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Data;
using SalesCobrosGeo.Api.Services;

namespace SalesCobrosGeo.Api.Controllers;

/// <summary>
/// API Controller para gestión RBAC (Roles, Permisos, Asignaciones)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RbacController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly ExcelDataService _excelService;
    private readonly ILogger<RbacController> _logger;

    public RbacController(
        IPermissionService permissionService,
        ExcelDataService excelService,
        ILogger<RbacController> logger)
    {
        _permissionService = permissionService;
        _excelService = excelService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════
    // ROLES
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtener todos los roles
    /// </summary>
    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
    {
        try
        {
            var rolesData = await _excelService.ReadSheetAsync("Roles");
            var roles = rolesData.Select(row => new RoleDto(
                GetInt(row, "Id"),
                GetString(row, "Code"),
                GetString(row, "Name"),
                GetString(row, "Description"),
                GetBool(row, "IsActive") ?? true,
                GetBool(row, "IsSystem") ?? false,
                0 // PermissionCount se calcula después
            )).ToList();

            // Calcular cantidad de permisos por rol
            var rolePermissionsData = await _excelService.ReadSheetAsync("RolePermissions");
            var permissionCounts = rolePermissionsData
                .Where(rp => GetBool(rp, "IsGranted") ?? true)
                .GroupBy(rp => GetInt(rp, "RoleId"))
                .ToDictionary(g => g.Key, g => g.Count());

            roles = roles.Select(r => r with 
            { 
                PermissionCount = permissionCounts.GetValueOrDefault(r.Id, 0) 
            }).ToList();

            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo roles");
            return StatusCode(500, new { error = "Error obteniendo roles" });
        }
    }

    /// <summary>
    /// Obtener rol por ID
    /// </summary>
    [HttpGet("roles/{id:int}")]
    public async Task<ActionResult<RoleDto>> GetRoleById(int id)
    {
        try
        {
            var rolesData = await _excelService.ReadSheetAsync("Roles");
            var role = rolesData.FirstOrDefault(r => GetInt(r, "Id") == id);

            if (role == null)
                return NotFound(new { error = $"Rol {id} no encontrado" });

            var permissionsData = await _excelService.ReadSheetAsync("RolePermissions");
            var permissionCount = permissionsData
                .Count(rp => GetInt(rp, "RoleId") == id && (GetBool(rp, "IsGranted") ?? true));

            var roleDto = new RoleDto(
                GetInt(role, "Id"),
                GetString(role, "Code"),
                GetString(role, "Name"),
                GetString(role, "Description"),
                GetBool(role, "IsActive") ?? true,
                GetBool(role, "IsSystem") ?? false,
                permissionCount
            );

            return Ok(roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo rol {RoleId}", id);
            return StatusCode(500, new { error = "Error obteniendo rol" });
        }
    }

    /// <summary>
    /// Crear nuevo rol
    /// </summary>
    [HttpPost("roles")]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleDto dto)
    {
        try
        {
            var rolesData = await _excelService.ReadSheetAsync("Roles");
            
            // Validar que el código no exista
            if (rolesData.Any(r => GetString(r, "Code").Equals(dto.Code, StringComparison.OrdinalIgnoreCase)))
                return Conflict(new { error = $"Ya existe un rol con código '{dto.Code}'" });

            // Obtener nuevo ID
            var newId = rolesData.Any() ? rolesData.Max(r => GetInt(r, "Id")) + 1 : 1;

            var newRole = new Dictionary<string, object?>
            {
                ["Id"] = newId,
                ["Code"] = dto.Code,
                ["Name"] = dto.Name,
                ["Description"] = dto.Description,
                ["IsActive"] = dto.IsActive,
                ["IsSystem"] = false
            };

            await _excelService.AppendRowAsync("Roles", newRole);

            var roleDto = new RoleDto(
                newId,
                dto.Code,
                dto.Name,
                dto.Description,
                dto.IsActive,
                false,
                0
            );

            _logger.LogInformation("Rol creado: {RoleCode}", dto.Code);
            return CreatedAtAction(nameof(GetRoleById), new { id = newId }, roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando rol {@Dto}", dto);
            return StatusCode(500, new { error = "Error creando rol" });
        }
    }

    /// <summary>
    /// Actualizar rol existente
    /// </summary>
    [HttpPut("roles/{id:int}")]
    public async Task<ActionResult<RoleDto>> UpdateRole(int id, [FromBody] UpdateRoleDto dto)
    {
        try
        {
            var rolesData = await _excelService.ReadSheetAsync("Roles");
            var role = rolesData.FirstOrDefault(r => GetInt(r, "Id") == id);

            if (role == null)
                return NotFound(new { error = $"Rol {id} no encontrado" });

            // No permitir editar roles de sistema
            if (GetBool(role, "IsSystem") ?? false)
                return BadRequest(new { error = "No se pueden editar roles del sistema" });

            // Actualizar usando UpdateRowsAsync
            await _excelService.UpdateRowsAsync("Roles", 
                r => GetInt(r, "Id") == id,
                r => {
                    r["Name"] = dto.Name;
                    r["Description"] = dto.Description;
                    r["IsActive"] = dto.IsActive;
                });

            var roleDto = new RoleDto(
                id,
                GetString(role, "Code"),
                dto.Name,
                dto.Description,
                dto.IsActive,
                false,
                0
            );

            _logger.LogInformation("Rol actualizado: {RoleId}", id);
            return Ok(roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando rol {RoleId}", id);
            return StatusCode(500, new { error = "Error actualizando rol" });
        }
    }

    /// <summary>
    /// Eliminar rol (solo si no es de sistema y no tiene usuarios asignados)
    /// </summary>
    [HttpDelete("roles/{id:int}")]
    public async Task<ActionResult> DeleteRole(int id)
    {
        try
        {
            var rolesData = await _excelService.ReadSheetAsync("Roles");
            var role = rolesData.FirstOrDefault(r => GetInt(r, "Id") == id);

            if (role == null)
                return NotFound(new { error = $"Rol {id} no encontrado" });

            // No permitir eliminar roles de sistema
            if (GetBool(role, "IsSystem") ?? false)
                return BadRequest(new { error = "No se pueden eliminar roles del sistema" });

            // Verificar que no tenga usuarios asignados
            var userRolesData = await _excelService.ReadSheetAsync("UserRoles");
            if (userRolesData.Any(ur => GetInt(ur, "RoleId") == id && (GetBool(ur, "IsActive") ?? true)))
                return BadRequest(new { error = "No se puede eliminar el rol porque tiene usuarios asignados" });

            // Eliminar usando DeleteRowsAsync
            await _excelService.DeleteRowsAsync("Roles", r => GetInt(r, "Id") == id);

            _logger.LogInformation("Rol eliminado: {RoleId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando rol {RoleId}", id);
            return StatusCode(500, new { error = "Error eliminando rol" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // PERMISSIONS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtener todos los permisos
    /// </summary>
    [HttpGet("permissions")]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetAllPermissions()
    {
        try
        {
            var permissionsData = await _excelService.ReadSheetAsync("Permissions");
            var modulesData = await _excelService.ReadSheetAsync("Modules");
            var actionsData = await _excelService.ReadSheetAsync("Actions");

            var moduleDict = modulesData.ToDictionary(m => GetInt(m, "Id"), m => (Code: GetString(m, "Code"), Name: GetString(m, "Name")));
            var actionDict = actionsData.ToDictionary(a => GetInt(a, "Id"), a => (Code: GetString(a, "Code"), Name: GetString(a, "Name")));

            var permissions = permissionsData.Select(p => {
                var modId = GetInt(p, "ModuleId");
                var actId = GetInt(p, "ActionId");
                var mod = moduleDict.GetValueOrDefault(modId, (Code: "unknown", Name: "Unknown"));
                var act = actionDict.GetValueOrDefault(actId, (Code: "unknown", Name: "Unknown"));
                return new PermissionDto(
                    GetInt(p, "Id"),
                    GetString(p, "Code"),
                    mod.Code,
                    mod.Name,
                    act.Code,
                    act.Name,
                    GetString(p, "Description"),
                    GetBool(p, "IsActive") ?? true
                );
            }).ToList();

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo permisos");
            return StatusCode(500, new { error = "Error obteniendo permisos" });
        }
    }

    /// <summary>
    /// Obtener módulos del sistema
    /// </summary>
    [HttpGet("modules")]
    public async Task<ActionResult<IEnumerable<ModuleDto>>> GetAllModules()
    {
        try
        {
            var modulesData = await _excelService.ReadSheetAsync("Modules");
            var modules = modulesData.Select(m => new ModuleDto(
                GetInt(m, "Id"),
                GetString(m, "Code"),
                GetString(m, "Name"),
                GetString(m, "Description"),
                GetBool(m, "IsActive") ?? true
            )).ToList();

            return Ok(modules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo módulos");
            return StatusCode(500, new { error = "Error obteniendo módulos" });
        }
    }

    /// <summary>
    /// Obtener acciones del sistema
    /// </summary>
    [HttpGet("actions")]
    public async Task<ActionResult<IEnumerable<ActionDto>>> GetAllActions()
    {
        try
        {
            var actionsData = await _excelService.ReadSheetAsync("Actions");
            var actions = actionsData.Select(a => new ActionDto(
                GetInt(a, "Id"),
                GetString(a, "Code"),
                GetString(a, "Name"),
                GetString(a, "Description"),
                GetString(a, "Category"),
                GetBool(a, "IsActive") ?? true
            )).ToList();

            return Ok(actions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo acciones");
            return StatusCode(500, new { error = "Error obteniendo acciones" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // ROLE PERMISSIONS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtener permisos de un rol
    /// </summary>
    [HttpGet("roles/{roleId:int}/permissions")]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetRolePermissions(int roleId)
    {
        try
        {
            var rolePermissionsData = await _excelService.ReadSheetAsync("RolePermissions");
            var permissionIds = rolePermissionsData
                .Where(rp => GetInt(rp, "RoleId") == roleId && (GetBool(rp, "IsGranted") ?? true))
                .Select(rp => GetInt(rp, "PermissionId"))
                .ToHashSet();

            var permissionsData = await _excelService.ReadSheetAsync("Permissions");
            var modulesData = await _excelService.ReadSheetAsync("Modules");
            var actionsData = await _excelService.ReadSheetAsync("Actions");

            var moduleDict = modulesData.ToDictionary(m => GetInt(m, "Id"), m => (Code: GetString(m, "Code"), Name: GetString(m, "Name")));
            var actionDict = actionsData.ToDictionary(a => GetInt(a, "Id"), a => (Code: GetString(a, "Code"), Name: GetString(a, "Name")));

            var permissions = permissionsData
                .Where(p => permissionIds.Contains(GetInt(p, "Id")))
                .Select(p => {
                    var modId = GetInt(p, "ModuleId");
                    var actId = GetInt(p, "ActionId");
                    var mod = moduleDict.GetValueOrDefault(modId, (Code: "unknown", Name: "Unknown"));
                    var act = actionDict.GetValueOrDefault(actId, (Code: "unknown", Name: "Unknown"));
                    return new PermissionDto(
                        GetInt(p, "Id"),
                        GetString(p, "Code"),
                        mod.Code,
                        mod.Name,
                        act.Code,
                        act.Name,
                        GetString(p, "Description"),
                        GetBool(p, "IsActive") ?? true
                    );
                }).ToList();

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo permisos del rol {RoleId}", roleId);
            return StatusCode(500, new { error = "Error obteniendo permisos del rol" });
        }
    }

    /// <summary>
    /// Asignar un permiso a un rol
    /// </summary>
    [HttpPost("roles/{roleId:int}/permissions")]
    public async Task<ActionResult> AssignPermissionToRole(int roleId, [FromBody] AssignPermissionDto dto)
    {
        try
        {
            // Validar que el rol existe
            var rolesData = await _excelService.ReadSheetAsync("Roles");
            var roleExists = rolesData.Any(r => GetInt(r, "Id") == roleId);
            if (!roleExists)
            {
                return NotFound(new { error = $"Rol {roleId} no encontrado" });
            }

            // Validar que el permiso existe
            var permissionsData = await _excelService.ReadSheetAsync("Permissions");
            var permissionExists = permissionsData.Any(p => GetInt(p, "Id") == dto.PermissionId);
            if (!permissionExists)
            {
                return NotFound(new { error = $"Permiso {dto.PermissionId} no encontrado" });
            }

            // Verificar si ya existe la asignación
            var rolePermissionsData = await _excelService.ReadSheetAsync("RolePermissions");
            var existingAssignment = rolePermissionsData.FirstOrDefault(rp =>
                GetInt(rp, "RoleId") == roleId &&
                GetInt(rp, "PermissionId") == dto.PermissionId
            );

            if (existingAssignment != null)
            {
                // Actualizar IsGranted si ya existe
                await _excelService.UpdateRowsAsync(
                    "RolePermissions",
                    row => GetInt(row, "RoleId") == roleId && GetInt(row, "PermissionId") == dto.PermissionId,
                    row => row["IsGranted"] = dto.IsGranted
                );
            }
            else
            {
                // Crear nueva asignación
                var maxId = rolePermissionsData.Any() 
                    ? rolePermissionsData.Max(rp => GetInt(rp, "Id")) 
                    : 0;

                var newAssignment = new Dictionary<string, object?>
                {
                    ["Id"] = maxId + 1,
                    ["RoleId"] = roleId,
                    ["PermissionId"] = dto.PermissionId,
                    ["IsGranted"] = dto.IsGranted,
                    ["CreatedAt"] = DateTime.UtcNow,
                    ["UpdatedAt"] = DateTime.UtcNow
                };

                await _excelService.AppendRowAsync("RolePermissions", newAssignment);
            }

            _logger.LogInformation("Permiso {PermissionId} asignado al rol {RoleId} (Granted: {IsGranted})", 
                dto.PermissionId, roleId, dto.IsGranted);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asignando permiso {PermissionId} al rol {RoleId}", dto.PermissionId, roleId);
            return StatusCode(500, new { error = "Error asignando permiso al rol" });
        }
    }

    /// <summary>
    /// Remover un permiso de un rol
    /// </summary>
    [HttpDelete("roles/{roleId:int}/permissions/{permissionId:int}")]
    public async Task<ActionResult> RemovePermissionFromRole(int roleId, int permissionId)
    {
        try
        {
            await _excelService.DeleteRowsAsync(
                "RolePermissions",
                row => GetInt(row, "RoleId") == roleId && GetInt(row, "PermissionId") == permissionId
            );

            _logger.LogInformation("Permiso {PermissionId} removido del rol {RoleId}", permissionId, roleId);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removiendo permiso {PermissionId} del rol {RoleId}", permissionId, roleId);
            return StatusCode(500, new { error = "Error removiendo permiso del rol" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // MATRIX
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtener matriz completa de rol-permisos (Role Permission Matrix)
    /// </summary>
    [HttpGet("permission-matrix")]
    public async Task<ActionResult<PermissionMatrixDto>> GetPermissionMatrix()
    {
        try
        {
            // Obtener roles
            var rolesData = await _excelService.ReadSheetAsync("Roles");
            var roles = rolesData.Select(r => new RoleDto(
                GetInt(r, "Id"),
                GetString(r, "Code"),
                GetString(r, "Name"),
                GetString(r, "Description"),
                GetBool(r, "IsActive") ?? true,
                GetBool(r, "IsSystem") ?? false,
                0
            )).ToList();

            // Obtener permisos
            var permissionsData = await _excelService.ReadSheetAsync("Permissions");
            var modulesData = await _excelService.ReadSheetAsync("Modules");
            var actionsData = await _excelService.ReadSheetAsync("Actions");

            var moduleDict = modulesData.ToDictionary(m => GetInt(m, "Id"), m => (Code: GetString(m, "Code"), Name: GetString(m, "Name")));
            var actionDict = actionsData.ToDictionary(a => GetInt(a, "Id"), a => (Code: GetString(a, "Code"), Name: GetString(a, "Name")));

            var permissions = permissionsData.Select(p => {
                var modId = GetInt(p, "ModuleId");
                var actId = GetInt(p, "ActionId");
                var mod = moduleDict.GetValueOrDefault(modId, (Code: "unknown", Name: "Unknown"));
                var act = actionDict.GetValueOrDefault(actId, (Code: "unknown", Name: "Unknown"));
                return new PermissionDto(
                    GetInt(p, "Id"),
                    GetString(p, "Code"),
                    mod.Code,
                    mod.Name,
                    act.Code,
                    act.Name,
                    GetString(p, "Description"),
                    GetBool(p, "IsActive") ?? true
                );
            }).ToList();

            // Obtener mapeo de permisos por rol
            var rolePermissionsData = await _excelService.ReadSheetAsync("RolePermissions");
            var rolePermissionMap = rolePermissionsData
                .Where(rp => GetBool(rp, "IsGranted") ?? true)
                .GroupBy(rp => GetInt(rp, "RoleId"))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(rp => GetInt(rp, "PermissionId")).ToList()
                );

            var matrix = new PermissionMatrixDto(
                roles,
                permissions,
                rolePermissionMap
            );

            return Ok(matrix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo matriz de permisos");
            return StatusCode(500, new { error = "Error obteniendo matriz de permisos" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // USER ROLES
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtener todas las asignaciones de roles a usuarios
    /// </summary>
    [HttpGet("user-roles")]
    public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetAllUserRoles()
    {
        try
        {
            var userRolesData = await _excelService.ReadSheetAsync("UserRoles");
            var rolesData = await _excelService.ReadSheetAsync("Roles");

            var rolesDict = rolesData.ToDictionary(
                r => GetInt(r, "Id"),
                r => (Code: GetString(r, "Code"), Name: GetString(r, "Name"))
            );

            var now = DateTime.UtcNow;
            var userRoles = userRolesData.Select(ur =>
            {
                var roleId = GetInt(ur, "RoleId");
                var role = rolesDict.GetValueOrDefault(roleId, (Code: "Unknown", Name: "Unknown"));
                var isActive = GetBool(ur, "IsActive") ?? true;
                var startDate = GetDateTime(ur, "StartDate") ?? DateTime.MinValue;
                var endDate = GetDateTime(ur, "EndDate");
                var isCurrentlyActive = isActive && startDate <= now && (endDate == null || endDate >= now);

                return new UserRoleDto(
                    GetInt(ur, "Id"),
                    GetString(ur, "UserName"),
                    roleId,
                    role.Code,
                    role.Name,
                    startDate,
                    endDate,
                    isActive,
                    isCurrentlyActive
                );
            }).ToList();

            return Ok(userRoles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo user roles");
            return StatusCode(500, new { error = "Error obteniendo user roles" });
        }
    }

    /// <summary>
    /// Asignar rol a un usuario
    /// </summary>
    [HttpPost("user-roles")]
    public async Task<ActionResult<UserRoleDto>> CreateUserRole([FromBody] AssignUserRoleDto dto)
    {
        try
        {
            _logger.LogInformation("API CreateUserRole called: {@Dto}", dto);
            
            var userRolesData = await _excelService.ReadSheetAsync("UserRoles");
            _logger.LogInformation("Current UserRoles count: {Count}", userRolesData.Count);
            
            // Obtener nuevo ID
            var newId = userRolesData.Any() ? userRolesData.Max(ur => GetInt(ur, "Id")) + 1 : 1;
            _logger.LogInformation("New UserRole ID will be: {NewId}", newId);

            var newUserRole = new Dictionary<string, object?>
            {
                ["Id"] = newId,
                ["UserName"] = dto.UserName,
                ["RoleId"] = dto.RoleId,
                ["StartDate"] = dto.StartDate,
                ["EndDate"] = dto.EndDate,
                ["IsActive"] = dto.IsActive
            };

            await _excelService.AppendRowAsync("UserRoles", newUserRole);
            _logger.LogInformation("AppendRowAsync completed for UserRoles");

            // Obtener información del rol
            var rolesData = await _excelService.ReadSheetAsync("Roles");
            var role = rolesData.FirstOrDefault(r => GetInt(r, "Id") == dto.RoleId);
            var roleCode = role != null ? GetString(role, "Code") : "Unknown";
            var roleName = role != null ? GetString(role, "Name") : "Unknown";

            var now = DateTime.UtcNow;
            var isCurrentlyActive = dto.IsActive && dto.StartDate <= now && (dto.EndDate == null || dto.EndDate >= now);

            var userRoleDto = new UserRoleDto(
                newId,
                dto.UserName,
                dto.RoleId,
                roleCode,
                roleName,
                dto.StartDate,
                dto.EndDate,
                dto.IsActive,
                isCurrentlyActive
            );

            _logger.LogInformation("UserRole successfully created: Id={Id}, {UserName} -> {RoleCode} (IsActive={IsActive}, IsCurrentlyActive={IsCurrentlyActive})", 
                newId, dto.UserName, roleCode, dto.IsActive, isCurrentlyActive);
            return CreatedAtAction(nameof(GetAllUserRoles), null, userRoleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando user role {@Dto}", dto);
            return StatusCode(500, new { error = "Error creando user role" });
        }
    }

    /// <summary>
    /// Actualizar asignación de rol a usuario
    /// </summary>
    [HttpPut("user-roles/{id:int}")]
    public async Task<ActionResult<UserRoleDto>> UpdateUserRole(int id, [FromBody] UpdateUserRoleDto dto)
    {
        try
        {
            var userRolesData = await _excelService.ReadSheetAsync("UserRoles");
            var userRole = userRolesData.FirstOrDefault(ur => GetInt(ur, "Id") == id);

            if (userRole == null)
                return NotFound(new { error = $"UserRole {id} no encontrado" });

            // Actualizar usando UpdateRowsAsync
            await _excelService.UpdateRowsAsync("UserRoles",
                ur => GetInt(ur, "Id") == id,
                ur =>
                {
                    ur["EndDate"] = dto.EndDate;
                    ur["IsActive"] = dto.IsActive;
                });

            // Obtener información del rol
            var roleId = GetInt(userRole, "RoleId");
            var rolesData = await _excelService.ReadSheetAsync("Roles");
            var role = rolesData.FirstOrDefault(r => GetInt(r, "Id") == roleId);
            var roleCode = role != null ? GetString(role, "Code") : "Unknown";
            var roleName = role != null ? GetString(role, "Name") : "Unknown";

            var userName = GetString(userRole, "UserName");
            var startDate = GetDateTime(userRole, "StartDate") ?? DateTime.MinValue;
            var now = DateTime.UtcNow;
            var isCurrentlyActive = dto.IsActive && startDate <= now && (dto.EndDate == null || dto.EndDate >= now);

            var userRoleDto = new UserRoleDto(
                id,
                userName,
                roleId,
                roleCode,
                roleName,
                startDate,
                dto.EndDate,
                dto.IsActive,
                isCurrentlyActive
            );

            _logger.LogInformation("UserRole actualizado: {UserRoleId}", id);
            return Ok(userRoleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando user role {UserRoleId}", id);
            return StatusCode(500, new { error = "Error actualizando user role" });
        }
    }

    /// <summary>
    /// Eliminar asignación de rol a usuario
    /// </summary>
    [HttpDelete("user-roles/{id:int}")]
    public async Task<ActionResult<bool>> DeleteUserRole(int id)
    {
        try
        {
            var userRolesData = await _excelService.ReadSheetAsync("UserRoles");
            var userRole = userRolesData.FirstOrDefault(ur => GetInt(ur, "Id") == id);

            if (userRole == null)
                return NotFound(new { error = $"UserRole {id} no encontrado" });

            // Eliminar usando DeleteRowsAsync
            await _excelService.DeleteRowsAsync("UserRoles", ur => GetInt(ur, "Id") == id);

            _logger.LogInformation("UserRole eliminado: {UserRoleId}", id);
            return Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando user role {UserRoleId}", id);
            return StatusCode(500, new { error = "Error eliminando user role" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // USER EFFECTIVE PERMISSIONS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtener permisos efectivos de un usuario (roles + custom)
    /// </summary>
    [HttpGet("users/{userName}/effective-permissions")]
    public async Task<ActionResult<UserPermissionSummaryDto>> GetUserEffectivePermissions(string userName)
    {
        try
        {
            _logger.LogInformation("GetUserEffectivePermissions llamado para usuario: {UserName}", userName);

            // 1. Obtener roles activos del usuario
            var userRolesData = await _excelService.ReadSheetAsync("UserRoles");
            var now = DateTime.UtcNow;
            var activeUserRoleIds = userRolesData
                .Where(ur => GetString(ur, "UserName") == userName
                    && (GetBool(ur, "IsActive") ?? true)
                    && GetDateTime(ur, "StartDate") <= now
                    && (GetDateTime(ur, "EndDate") == null || GetDateTime(ur, "EndDate") >= now))
                .Select(ur => GetInt(ur, "RoleId"))
                .ToList();

            _logger.LogInformation("Roles activos encontrados: {Count}", activeUserRoleIds.Count);

            // 2. Obtener información de los roles
            var rolesData = await _excelService.ReadSheetAsync("Roles");
            var activeRoles = rolesData
                .Where(r => activeUserRoleIds.Contains(GetInt(r, "Id")) && (GetBool(r, "IsActive") ?? true))
                .Select(r => new RoleDto(
                    GetInt(r, "Id"),
                    GetString(r, "Code"),
                    GetString(r, "Name"),
                    GetString(r, "Description"),
                    GetBool(r, "IsActive") ?? true,
                    GetBool(r, "IsSystem") ?? false,
                    0
                ))
                .ToList();

            // 3. Obtener permisos de los roles
            var rolePermissionsData = await _excelService.ReadSheetAsync("RolePermissions");
            var permissionIdsFromRoles = rolePermissionsData
                .Where(rp => activeUserRoleIds.Contains(GetInt(rp, "RoleId")) && (GetBool(rp, "IsGranted") ?? true))
                .Select(rp => GetInt(rp, "PermissionId"))
                .ToHashSet();

            _logger.LogInformation("Permisos desde roles: {Count}", permissionIdsFromRoles.Count);

            // 4. Obtener información de permisos
            var permissionsData = await _excelService.ReadSheetAsync("Permissions");
            var modulesData = await _excelService.ReadSheetAsync("Modules");
            var actionsData = await _excelService.ReadSheetAsync("Actions");

            var moduleDict = modulesData.ToDictionary(m => GetInt(m, "Id"), m => (Code: GetString(m, "Code"), Name: GetString(m, "Name")));
            var actionDict = actionsData.ToDictionary(a => GetInt(a, "Id"), a => (Code: GetString(a, "Code"), Name: GetString(a, "Name")));

            var permissionsFromRoles = permissionsData
                .Where(p => permissionIdsFromRoles.Contains(GetInt(p, "Id")) && (GetBool(p, "IsActive") ?? true))
                .Select(p => {
                    var modId = GetInt(p, "ModuleId");
                    var actId = GetInt(p, "ActionId");
                    var mod = moduleDict.GetValueOrDefault(modId, (Code: "unknown", Name: "Unknown"));
                    var act = actionDict.GetValueOrDefault(actId, (Code: "unknown", Name: "Unknown"));
                    return new PermissionDto(
                        GetInt(p, "Id"),
                        GetString(p, "Code"),
                        mod.Code,
                        mod.Name,
                        act.Code,
                        act.Name,
                        GetString(p, "Description"),
                        GetBool(p, "IsActive") ?? true
                    );
                })
                .ToList();

            // 5. Obtener permisos personalizados
            var userPermissionsData = await _excelService.ReadSheetAsync("UserPermissions");
            var customPermissions = userPermissionsData
                .Where(up => GetString(up, "UserName") == userName)
                .Select(up => {
                    var permId = GetInt(up, "PermissionId");
                    var permission = permissionsData.FirstOrDefault(p => GetInt(p, "Id") == permId);
                    var permissionCode = permission != null ? GetString(permission, "Code") : "Unknown";
                    var permissionDescription = permission != null ? GetString(permission, "Description") : "Unknown";
                    var startDate = GetDateTime(up, "StartDate");
                    var endDate = GetDateTime(up, "EndDate");
                    var isCurrentlyValid = (startDate == null || startDate <= now) && (endDate == null || endDate >= now);

                    return new UserPermissionDto(
                        GetInt(up, "Id"),
                        userName,
                        permId,
                        permissionCode,
                        permissionDescription,
                        GetBool(up, "IsGranted") ?? true,
                        startDate,
                        endDate,
                        GetDateTime(up, "CreatedAt"),
                        isCurrentlyValid
                    );
                })
                .ToList();

            _logger.LogInformation("Permisos personalizados: {Count}", customPermissions.Count);

            // 6. Calcular permisos efectivos: (Roles) + (Custom Granted) - (Custom Denied)
            var effectivePermissionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Agregar permisos de roles
            foreach (var perm in permissionsFromRoles)
            {
                effectivePermissionCodes.Add(perm.Code);
            }

            // Aplicar permisos personalizados válidos
            foreach (var customPerm in customPermissions.Where(cp => cp.IsCurrentlyValid))
            {
                if (customPerm.IsGranted)
                    effectivePermissionCodes.Add(customPerm.PermissionCode);
                else
                    effectivePermissionCodes.Remove(customPerm.PermissionCode);
            }

            var summary = new UserPermissionSummaryDto(
                userName,
                activeRoles,
                permissionsFromRoles,
                customPermissions,
                effectivePermissionCodes.ToList()
            );

            _logger.LogInformation("Permisos efectivos calculados: {Count}", effectivePermissionCodes.Count);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo permisos efectivos del usuario {UserName}", userName);
            return StatusCode(500, new { error = "Error obteniendo permisos efectivos" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // USER PERMISSIONS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtener permisos personalizados de usuarios
    /// </summary>
    [HttpGet("user-permissions")]
    public async Task<ActionResult<IEnumerable<UserPermissionDto>>> GetUserCustomPermissions([FromQuery] string? userName = null)
    {
        try
        {
            var userPermissionsData = await _excelService.ReadSheetAsync("UserPermissions");
            var permissionsData = await _excelService.ReadSheetAsync("Permissions");

            var permissionsDict = permissionsData.ToDictionary(
                p => GetInt(p, "Id"),
                p => (Code: GetString(p, "Code"), Description: GetString(p, "Description"))
            );

            var now = DateTime.UtcNow;
            var userPermissions = userPermissionsData
                .Where(up => string.IsNullOrEmpty(userName) || GetString(up, "UserName").Equals(userName, StringComparison.OrdinalIgnoreCase))
                .Select(up =>
                {
                    var permissionId = GetInt(up, "PermissionId");
                    var permission = permissionsDict.GetValueOrDefault(permissionId, (Code: "Unknown", Description: "Unknown"));
                    var startDate = GetDateTime(up, "StartDate");
                    var endDate = GetDateTime(up, "EndDate");
                    var isCurrentlyValid = (startDate == null || startDate <= now) && (endDate == null || endDate >= now);

                    return new UserPermissionDto(
                        GetInt(up, "Id"),
                        GetString(up, "UserName"),
                        permissionId,
                        permission.Code,
                        permission.Description,
                        GetBool(up, "IsGranted") ?? true,
                        startDate,
                        endDate,
                        GetDateTime(up, "CreatedAt"),
                        isCurrentlyValid
                    );
                }).ToList();

            return Ok(userPermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo user permissions");
            return StatusCode(500, new { error = "Error obteniendo user permissions" });
        }
    }

    /// <summary>
    /// Otorgar permiso personalizado a un usuario
    /// </summary>
    [HttpPost("user-permissions")]
    public async Task<ActionResult<UserPermissionDto>> GrantUserPermission([FromBody] GrantUserPermissionDto dto)
    {
        try
        {
            _logger.LogInformation("API GrantUserPermission called: {@Dto}", dto);
            
            var userPermissionsData = await _excelService.ReadSheetAsync("UserPermissions");
            _logger.LogInformation("Current UserPermissions count: {Count}", userPermissionsData.Count);
            
            // Obtener nuevo ID
            var newId = userPermissionsData.Any() ? userPermissionsData.Max(up => GetInt(up, "Id")) + 1 : 1;
            _logger.LogInformation("New UserPermission ID will be: {NewId}", newId);

            var now = DateTime.UtcNow;
            var newUserPermission = new Dictionary<string, object?>
            {
                ["Id"] = newId,
                ["UserName"] = dto.UserName,
                ["PermissionId"] = dto.PermissionId,
                ["IsGranted"] = dto.IsGranted,
                ["StartDate"] = dto.StartDate,
                ["EndDate"] = dto.EndDate,
                ["CreatedAt"] = now
            };

            await _excelService.AppendRowAsync("UserPermissions", newUserPermission);
            _logger.LogInformation("AppendRowAsync completed for UserPermissions");

            // Obtener información del permiso
            var permissionsData = await _excelService.ReadSheetAsync("Permissions");
            var permission = permissionsData.FirstOrDefault(p => GetInt(p, "Id") == dto.PermissionId);
            var permissionCode = permission != null ? GetString(permission, "Code") : "Unknown";
            var permissionDescription = permission != null ? GetString(permission, "Description") : "Unknown";

            var isCurrentlyValid = (dto.StartDate == null || dto.StartDate <= now) && (dto.EndDate == null || dto.EndDate >= now);

            var userPermissionDto = new UserPermissionDto(
                newId,
                dto.UserName,
                dto.PermissionId,
                permissionCode,
                permissionDescription,
                dto.IsGranted,
                dto.StartDate,
                dto.EndDate,
                now,
                isCurrentlyValid
            );

            _logger.LogInformation("UserPermission creado: {UserName} -> {PermissionCode}", dto.UserName, permissionCode);
            return CreatedAtAction(nameof(GetUserCustomPermissions), new { userName = dto.UserName }, userPermissionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando user permission {@Dto}", dto);
            return StatusCode(500, new { error = "Error creando user permission" });
        }
    }

    /// <summary>
    /// Revocar permiso personalizado de un usuario
    /// </summary>
    [HttpDelete("user-permissions/{id:int}")]
    public async Task<ActionResult<bool>> RevokeUserPermission(int id)
    {
        try
        {
            var userPermissionsData = await _excelService.ReadSheetAsync("UserPermissions");
            var userPermission = userPermissionsData.FirstOrDefault(up => GetInt(up, "Id") == id);

            if (userPermission == null)
                return NotFound(new { error = $"UserPermission {id} no encontrado" });

            // Eliminar usando DeleteRowsAsync
            await _excelService.DeleteRowsAsync("UserPermissions", up => GetInt(up, "Id") == id);

            _logger.LogInformation("UserPermission eliminado: {UserPermissionId}", id);
            return Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando user permission {UserPermissionId}", id);
            return StatusCode(500, new { error = "Error eliminando user permission" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════

    private static string GetString(Dictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;

    private static int GetInt(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return 0;
        if (value is int intValue)
            return intValue;
        if (value is double doubleValue)
            return (int)doubleValue;
        return int.TryParse(value.ToString(), out var parsed) ? parsed : 0;
    }

    private static bool? GetBool(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return null;
        if (value is bool boolValue)
            return boolValue;
        var strValue = value.ToString()?.ToUpperInvariant();
        return strValue switch
        {
            "TRUE" or "VERDADERO" or "1" => true,
            "FALSE" or "FALSO" or "0" => false,
            _ => null
        };
    }

    private static DateTime? GetDateTime(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return null;
        if (value is DateTime dateTimeValue)
            return dateTimeValue;
        if (DateTime.TryParse(value.ToString(), out var parsed))
            return parsed;
        return null;
    }
}

// ═══════════════════════════════════════════════════════════════════════
// DTOs (duplicados aquí para que compile, luego se mueven a archivo compartido)
// ═══════════════════════════════════════════════════════════════════════

public record RoleDto(int Id, string Code, string Name, string Description, bool IsActive, bool IsSystem, int PermissionCount);
public record CreateRoleDto(string Code, string Name, string Description, bool IsActive);
public record UpdateRoleDto(string Name, string Description, bool IsActive);
public record PermissionDto(int Id, string Code, string ModuleCode, string ModuleName, string ActionCode, string ActionName, string Description, bool IsActive);
public record ModuleDto(int Id, string Code, string Name, string Description, bool IsActive);
public record ActionDto(int Id, string Code, string Name, string Description, string Category, bool IsActive);
public record PermissionMatrixDto(IEnumerable<RoleDto> Roles, IEnumerable<PermissionDto> Permissions, Dictionary<int, List<int>> RolePermissionMap);
public record AssignPermissionDto(int PermissionId, bool IsGranted = true);
public record UserRoleDto(int Id, string UserName, int RoleId, string RoleCode, string RoleName, DateTime StartDate, DateTime? EndDate, bool IsActive, bool IsCurrentlyActive);
public record AssignUserRoleDto(string UserName, int RoleId, DateTime StartDate, DateTime? EndDate, bool IsActive);
public record UpdateUserRoleDto(DateTime? EndDate, bool IsActive);
public record UserPermissionDto(int Id, string UserName, int PermissionId, string PermissionCode, string PermissionDescription, bool IsGranted, DateTime? StartDate, DateTime? EndDate, DateTime? CreatedAt, bool IsCurrentlyValid);
public record GrantUserPermissionDto(string UserName, int PermissionId, bool IsGranted, DateTime? StartDate, DateTime? EndDate);
public record UserPermissionSummaryDto(string UserName, IEnumerable<RoleDto> ActiveRoles, IEnumerable<PermissionDto> PermissionsFromRoles, IEnumerable<UserPermissionDto> CustomPermissions, IEnumerable<string> EffectivePermissions);
