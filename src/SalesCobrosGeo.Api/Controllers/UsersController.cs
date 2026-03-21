using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Contracts.Users;
using SalesCobrosGeo.Api.Data;
using SalesCobrosGeo.Api.Security;

namespace SalesCobrosGeo.Api.Controllers;

/// <summary>
/// API para gestión de usuarios (CRUD)
/// Solo accesible para administradores
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly ExcelUserStore _userStore;
    private readonly ExcelDataService _excelService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ExcelUserStore userStore,
        ExcelDataService excelService,
        ILogger<UsersController> logger)
    {
        _userStore = userStore;
        _excelService = excelService;
        _logger = logger;
    }

    /// <summary>
    /// Listar todos los usuarios
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        try
        {
            var users = await _userStore.GetAllUsersAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var (activeRoles, customPermissionsCount) = await GetUserRbacSummaryAsync(user.UserName);

                userDtos.Add(new UserDto(
                    user.UserName,
                    user.FullName,
                    user.Role,
                    user.IsActive,
                    activeRoles,
                    customPermissionsCount
                ));
            }

            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo lista de usuarios");
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener detalle de un usuario específico
    /// </summary>
    [HttpGet("{userName}")]
    public async Task<ActionResult<UserDetailDto>> GetByUserName(string userName)
    {
        try
        {
            var user = _userStore.FindByUserName(userName);
            if (user == null)
            {
                return NotFound(new { error = $"Usuario '{userName}' no encontrado" });
            }

            var rbacSummary = await GetUserRbacDetailAsync(userName);

            var detailDto = new UserDetailDto(
                user.UserName,
                user.FullName,
                user.Role,
                user.IsActive,
                rbacSummary
            );

            return Ok(detailDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo usuario {UserName}", userName);
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear un nuevo usuario
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.UserName))
            {
                return BadRequest(new { error = "El nombre de usuario es requerido" });
            }

            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { error = "La contraseña es requerida" });
            }

            if (string.IsNullOrWhiteSpace(dto.DisplayName))
            {
                return BadRequest(new { error = "El nombre para mostrar es requerido" });
            }

            var user = await _userStore.AddUserAsync(
                dto.UserName,
                dto.Password,
                dto.DisplayName,
                dto.Role,
                dto.IsActive
            );

            _logger.LogInformation("Usuario creado: {UserName} ({DisplayName})", user.UserName, user.FullName);

            var userDto = new UserDto(
                user.UserName,
                user.FullName,
                user.Role,
                user.IsActive,
                0,
                0
            );

            return CreatedAtAction(nameof(GetByUserName), new { userName = user.UserName }, userDto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando usuario {@Dto}", dto);
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar un usuario existente
    /// </summary>
    [HttpPut("{userName}")]
    public async Task<ActionResult<UserDto>> Update(string userName, [FromBody] UpdateUserDto dto)
    {
        try
        {
            var existingUser = _userStore.FindByUserName(userName);
            if (existingUser == null)
            {
                return NotFound(new { error = $"Usuario '{userName}' no encontrado" });
            }

            var updatedUser = await _userStore.UpdateUserAsync(
                userName,
                dto.NewPassword,
                dto.DisplayName,
                dto.Role,
                dto.IsActive
            );

            _logger.LogInformation("Usuario actualizado: {UserName}", userName);

            var (activeRoles, customPermissionsCount) = await GetUserRbacSummaryAsync(userName);

            var userDto = new UserDto(
                updatedUser.UserName,
                updatedUser.FullName,
                updatedUser.Role,
                updatedUser.IsActive,
                activeRoles,
                customPermissionsCount
            );

            return Ok(userDto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando usuario {UserName}: {@Dto}", userName, dto);
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar un usuario
    /// </summary>
    [HttpDelete("{userName}")]
    public async Task<ActionResult> Delete(string userName)
    {
        try
        {
            // Proteger usuarios críticos
            if (userName.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                userName.Equals("RaizAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "No se puede eliminar el usuario administrador" });
            }

            var deleted = await _userStore.DeleteUserAsync(userName);
            if (!deleted)
            {
                return NotFound(new { error = $"Usuario '{userName}' no encontrado" });
            }

            _logger.LogWarning("Usuario eliminado: {UserName}", userName);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando usuario {UserName}", userName);
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // MÉTODOS HELPER PARA RBAC
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtener resumen RBAC de un usuario (roles activos y permisos custom)
    /// </summary>
    private async Task<(int ActiveRoles, int CustomPermissions)> GetUserRbacSummaryAsync(string userName)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Leer roles del usuario
            var userRolesData = await _excelService.ReadSheetAsync("UserRoles");
            var userRoles = userRolesData.Where(ur =>
                string.Equals(GetString(ur, "UserName"), userName, StringComparison.OrdinalIgnoreCase)
            );

            var activeRolesCount = userRoles.Count(ur =>
            {
                var isActive = GetBool(ur, "IsActive") ?? true;
                var startDate = GetDateTime(ur, "StartDate") ?? DateTime.MinValue;
                var endDate = GetDateTime(ur, "EndDate");
                return isActive && startDate <= now && (endDate == null || endDate >= now);
            });

            // Leer permisos custom del usuario
            var userPermissionsData = await _excelService.ReadSheetAsync("UserPermissions");
            var customPermissionsCount = userPermissionsData.Count(up =>
                string.Equals(GetString(up, "UserName"), userName, StringComparison.OrdinalIgnoreCase) &&
                (GetBool(up, "IsActive") ?? true)
            );

            return (activeRolesCount, customPermissionsCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error obteniendo resumen RBAC para {UserName}", userName);
            return (0, 0);
        }
    }

    /// <summary>
    /// Obtener detalle completo RBAC de un usuario
    /// </summary>
    private async Task<RbacUserSummary> GetUserRbacDetailAsync(string userName)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Leer roles del usuario
            var userRolesData = await _excelService.ReadSheetAsync("UserRoles");
            var rolesData = await _excelService.ReadSheetAsync("Roles");

            var rolesDict = rolesData.ToDictionary(
                r => GetInt(r, "Id"),
                r => GetString(r, "Code")
            );

            var userRolesList = userRolesData
                .Where(ur => string.Equals(GetString(ur, "UserName"), userName, StringComparison.OrdinalIgnoreCase))
                .Select(ur =>
                {
                    var isActive = GetBool(ur, "IsActive") ?? true;
                    var startDate = GetDateTime(ur, "StartDate") ?? DateTime.MinValue;
                    var endDate = GetDateTime(ur, "EndDate");
                    var roleId = GetInt(ur, "RoleId");
                    var roleCode = rolesDict.GetValueOrDefault(roleId, "Unknown");

                    return new
                    {
                        RoleCode = roleCode,
                        IsActive = isActive,
                        StartDate = startDate,
                        EndDate = endDate,
                        IsCurrentlyActive = isActive && startDate <= now && (endDate == null || endDate >= now),
                        IsExpired = endDate.HasValue && endDate.Value < now,
                        IsFuture = startDate > now
                    };
                })
                .ToList();

            // Leer permisos custom del usuario
            var userPermissionsData = await _excelService.ReadSheetAsync("UserPermissions");
            var permissionsData = await _excelService.ReadSheetAsync("Permissions");

            var permissionsDict = permissionsData.ToDictionary(
                p => GetInt(p, "Id"),
                p => GetString(p, "Code")
            );

            var customPermissions = userPermissionsData
                .Where(up =>
                    string.Equals(GetString(up, "UserName"), userName, StringComparison.OrdinalIgnoreCase) &&
                    (GetBool(up, "IsActive") ?? true)
                )
                .Select(up =>
                {
                    var permissionId = GetInt(up, "PermissionId");
                    var permissionCode = permissionsDict.GetValueOrDefault(permissionId, "Unknown");
                    var isGranted = GetBool(up, "IsGranted") ?? true;

                    return new { PermissionCode = permissionCode, IsGranted = isGranted };
                })
                .ToList();

            var activeRoleCodes = userRolesList
                .Where(ur => ur.IsCurrentlyActive)
                .Select(ur => ur.RoleCode)
                .Distinct()
                .ToArray();

            var grantedPermissionCodes = customPermissions
                .Where(p => p.IsGranted)
                .Select(p => p.PermissionCode)
                .ToArray();

            return new RbacUserSummary(
                TotalRoles: userRolesList.Count,
                ActiveRoles: userRolesList.Count(ur => ur.IsCurrentlyActive),
                ExpiredRoles: userRolesList.Count(ur => ur.IsExpired),
                FutureRoles: userRolesList.Count(ur => ur.IsFuture),
                GrantedPermissions: customPermissions.Count(p => p.IsGranted),
                DeniedPermissions: customPermissions.Count(p => !p.IsGranted),
                RoleCodes: activeRoleCodes,
                EffectivePermissionCodes: grantedPermissionCodes
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error obteniendo detalle RBAC para {UserName}", userName);
            return new RbacUserSummary(0, 0, 0, 0, 0, 0, Array.Empty<string>(), Array.Empty<string>());
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // MÉTODOS HELPER PARA LECTURA DE EXCEL
    // ═══════════════════════════════════════════════════════════════════

    private static string GetString(Dictionary<string, object?> row, string columnName)
        => row.TryGetValue(columnName, out var value) ? value?.ToString() ?? string.Empty : string.Empty;

    private static int GetInt(Dictionary<string, object?> row, string columnName)
        => row.TryGetValue(columnName, out var value) && value != null ? Convert.ToInt32(value) : 0;

    private static bool? GetBool(Dictionary<string, object?> row, string columnName)
        => row.TryGetValue(columnName, out var value) && value != null ? Convert.ToBoolean(value) : null;

    private static DateTime? GetDateTime(Dictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value) || value == null)
            return null;

        if (value is DateTime dt)
            return dt;

        if (DateTime.TryParse(value.ToString(), out var parsed))
            return parsed;

        return null;
    }
}
