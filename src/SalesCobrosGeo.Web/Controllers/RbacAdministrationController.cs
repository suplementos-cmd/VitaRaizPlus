using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Services.Rbac;

namespace SalesCobrosGeo.Web.Controllers;

/// <summary>
/// Controller para administración del sistema RBAC
/// (Roles, Permisos, Asignaciones)
/// </summary>
[Authorize(Policy = "AdministrationAccess")]
public class RbacAdministrationController : Controller
{
    private readonly IRbacService _rbacService;
    private readonly ILogger<RbacAdministrationController> _logger;

    public RbacAdministrationController(
        IRbacService rbacService,
        ILogger<RbacAdministrationController> logger)
    {
        _rbacService = rbacService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════
    // ROLES
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Vista principal de gestión de roles
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Roles()
    {
        try
        {
            var roles = await _rbacService.GetAllRolesAsync();
            return View(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo roles");
            TempData["Error"] = "Error al cargar roles";
            return View(Enumerable.Empty<RoleDto>());
        }
    }

    /// <summary>
    /// Crear nuevo rol (Ajax)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        try
        {
            var role = await _rbacService.CreateRoleAsync(dto);
            return Json(new { success = true, role });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando rol {@Dto}", dto);
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Actualizar rol existente (Ajax)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleDto dto)
    {
        try
        {
            var role = await _rbacService.UpdateRoleAsync(id, dto);
            return Json(new { success = true, role });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando rol {RoleId}: {@Dto}", id, dto);
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Eliminar rol (Ajax)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteRole(int id)
    {
        try
        {
            var success = await _rbacService.DeleteRoleAsync(id);
            return Json(new { success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando rol {RoleId}", id);
            return Json(new { success = false, error = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // PERMISSIONS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Vista catálogo de permisos (read-only)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Permissions()
    {
        try
        {
            var permissions = await _rbacService.GetAllPermissionsAsync();
            var modules = await _rbacService.GetAllModulesAsync();
            var actions = await _rbacService.GetAllActionsAsync();

            ViewBag.Modules = modules;
            ViewBag.Actions = actions;
            
            return View(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo permisos");
            TempData["Error"] = "Error al cargar permisos";
            return View(Enumerable.Empty<PermissionDto>());
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // ROLE PERMISSIONS (Matriz Rol ↔ Permisos)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Vista matriz de permisos por rol
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> RolePermissions(int? roleId)
    {
        try
        {
            var matrix = await _rbacService.GetRolePermissionMatrixAsync();
            
            if (roleId.HasValue)
            {
                ViewBag.SelectedRoleId = roleId.Value;
            }

            return View(matrix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo matriz de permisos");
            TempData["Error"] = "Error al cargar matriz de permisos";
            return View();
        }
    }

    /// <summary>
    /// Asignar/remover permiso de rol (Ajax)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleRolePermission(int roleId, int permissionId, bool isGranted)
    {
        try
        {
            bool success;
            if (isGranted)
            {
                success = await _rbacService.AssignPermissionToRoleAsync(roleId, permissionId, true);
            }
            else
            {
                success = await _rbacService.RemovePermissionFromRoleAsync(roleId, permissionId);
            }

            return Json(new { success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggle permiso {PermissionId} en rol {RoleId}", permissionId, roleId);
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Establecer todos los permisos de un rol (Ajax)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SetRolePermissions(int roleId, [FromBody] int[] permissionIds)
    {
        try
        {
            var success = await _rbacService.SetRolePermissionsAsync(roleId, permissionIds);
            return Json(new { success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estableciendo permisos del rol {RoleId}", roleId);
            return Json(new { success = false, error = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // USER ROLES (Asignación Usuarios ↔ Roles)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Vista asignación de roles a usuarios
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> UserRoles(string? userName)
    {
        try
        {
            var allUserRoles = await _rbacService.GetAllUserRolesAsync();
            var roles = await _rbacService.GetAllRolesAsync();

            ViewBag.Roles = roles;

            if (!string.IsNullOrEmpty(userName))
            {
                ViewBag.SelectedUserName = userName;
                var userRoles = allUserRoles.Where(ur => ur.UserName == userName);
                return View(userRoles);
            }

            return View(allUserRoles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo asignaciones usuario-rol");
            TempData["Error"] = "Error al cargar asignaciones";
            return View(Enumerable.Empty<UserRoleDto>());
        }
    }

    /// <summary>
    /// Asignar rol a usuario (Ajax)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AssignUserRole([FromBody] AssignUserRoleDto dto)
    {
        try
        {
            var userRole = await _rbacService.AssignRoleToUserAsync(dto);
            return Json(new { success = true, userRole });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asignando rol a usuario {@Dto}", dto);
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Remover rol de usuario (Ajax)
    /// </summary>
    [HttpPost("RemoveUserRole/{userRoleId}")]
    public async Task<IActionResult> RemoveUserRole(int userRoleId)
    {
        try
        {
            var success = await _rbacService.RemoveRoleFromUserAsync(userRoleId);
            return Json(new { success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removiendo asignación {UserRoleId}", userRoleId);
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Actualizar asignación usuario-rol (Ajax)
    /// </summary>
    [HttpPost("UpdateUserRole/{userRoleId}")]
    public async Task<IActionResult> UpdateUserRole(int userRoleId, [FromBody] UpdateUserRoleDto dto)
    {
        try
        {
            var success = await _rbacService.UpdateUserRoleAsync(userRoleId, dto);
            return Json(new { success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando asignación {UserRoleId}", userRoleId);
            return Json(new { success = false, error = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // USER PERMISSIONS (Permisos Custom)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Vista permisos custom por usuario
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> UserPermissions(string? userName)
    {
        try
        {
            IEnumerable<UserPermissionDto> userPermissions;
            
            if (!string.IsNullOrEmpty(userName))
            {
                userPermissions = await _rbacService.GetUserCustomPermissionsAsync(userName);
                ViewBag.SelectedUserName = userName;
            }
            else
            {
                userPermissions = Enumerable.Empty<UserPermissionDto>();
            }

            var permissions = await _rbacService.GetAllPermissionsAsync();
            ViewBag.Permissions = permissions;

            return View(userPermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo permisos custom");
            TempData["Error"] = "Error al cargar permisos custom";
            return View(Enumerable.Empty<UserPermissionDto>());
        }
    }

    /// <summary>
    /// Otorgar permiso custom a usuario (Ajax)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GrantUserPermission([FromBody] GrantUserPermissionDto dto)
    {
        try
        {
            var permission = await _rbacService.GrantCustomPermissionAsync(dto);
            return Json(new { success = true, permission });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error otorgando permiso custom {@Dto}", dto);
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Revocar permiso custom (Ajax)
    /// </summary>
    [HttpPost("RevokeUserPermission/{userPermissionId}")]
    public async Task<IActionResult> RevokeUserPermission(int userPermissionId)
    {
        try
        {
            var success = await _rbacService.RevokeCustomPermissionAsync(userPermissionId);
            return Json(new { success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revocando permiso custom {UserPermissionId}", userPermissionId);
            return Json(new { success = false, error = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // UTILITIES
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Vista permisos efectivos de un usuario (diagnostic/read-only)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> UserEffectivePermissions(string userName)
    {
        try
        {
            if (string.IsNullOrEmpty(userName))
            {
                TempData["Error"] = "Debe especificar un nombre de usuario";
                return RedirectToAction(nameof(UserRoles));
            }

            var summary = await _rbacService.GetUserEffectivePermissionsAsync(userName);
            return View(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo permisos efectivos de {UserName}", userName);
            TempData["Error"] = "Error al calcular permisos efectivos";
            return RedirectToAction(nameof(UserRoles));
        }
    }
}
