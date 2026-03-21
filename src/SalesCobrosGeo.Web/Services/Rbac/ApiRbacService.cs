using SalesCobrosGeo.Web.Security;

namespace SalesCobrosGeo.Web.Services.Rbac;

/// <summary>
/// Implementación del servicio RBAC que lee/escribe en Excel vía API
/// </summary>
public class ApiRbacService : IRbacService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiRbacService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiRbacService(
        IHttpClientFactory httpClientFactory,
        ILogger<ApiRbacService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    // ───────────────────────────────────────────────────────────────────
    // ROLES
    // ───────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/rbac/roles");
            response.EnsureSuccessStatusCode();

            var roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>();
            return roles ?? Enumerable.Empty<RoleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo roles");
            return Enumerable.Empty<RoleDto>();
        }
    }

    public async Task<RoleDto?> GetRoleByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/rbac/roles/{id}");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<RoleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo rol {RoleId}", id);
            return null;
        }
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/rbac/roles", dto);
            response.EnsureSuccessStatusCode();

            var role = await response.Content.ReadFromJsonAsync<RoleDto>();
            return role!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando rol {@Dto}", dto);
            throw;
        }
    }

    public async Task<RoleDto> UpdateRoleAsync(int id, UpdateRoleDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/rbac/roles/{id}", dto);
            response.EnsureSuccessStatusCode();

            var role = await response.Content.ReadFromJsonAsync<RoleDto>();
            return role!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando rol {RoleId}: {@Dto}", id, dto);
            throw;
        }
    }

    public async Task<bool> DeleteRoleAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/rbac/roles/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando rol {RoleId}", id);
            return false;
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // PERMISSIONS
    // ───────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/rbac/permissions");
            response.EnsureSuccessStatusCode();

            var permissions = await response.Content.ReadFromJsonAsync<List<PermissionDto>>();
            return permissions ?? Enumerable.Empty<PermissionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo permisos");
            return Enumerable.Empty<PermissionDto>();
        }
    }

    public async Task<IEnumerable<PermissionDto>> GetPermissionsByModuleAsync(int moduleId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/rbac/permissions?moduleId={moduleId}");
            response.EnsureSuccessStatusCode();

            var permissions = await response.Content.ReadFromJsonAsync<List<PermissionDto>>();
            return permissions ?? Enumerable.Empty<PermissionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo permisos del módulo {ModuleId}", moduleId);
            return Enumerable.Empty<PermissionDto>();
        }
    }

    public async Task<IEnumerable<ModuleDto>> GetAllModulesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/rbac/modules");
            response.EnsureSuccessStatusCode();

            var modules = await response.Content.ReadFromJsonAsync<List<ModuleDto>>();
            return modules ?? Enumerable.Empty<ModuleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo módulos");
            return Enumerable.Empty<ModuleDto>();
        }
    }

    public async Task<IEnumerable<ActionDto>> GetAllActionsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/rbac/actions");
            response.EnsureSuccessStatusCode();

            var actions = await response.Content.ReadFromJsonAsync<List<ActionDto>>();
            return actions ?? Enumerable.Empty<ActionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo acciones");
            return Enumerable.Empty<ActionDto>();
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // ROLE PERMISSIONS
    // ───────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<PermissionDto>> GetRolePermissionsAsync(int roleId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/rbac/roles/{roleId}/permissions");
            response.EnsureSuccessStatusCode();

            var permissions = await response.Content.ReadFromJsonAsync<List<PermissionDto>>();
            return permissions ?? Enumerable.Empty<PermissionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo permisos del rol {RoleId}", roleId);
            return Enumerable.Empty<PermissionDto>();
        }
    }

    public async Task<bool> AssignPermissionToRoleAsync(int roleId, int permissionId, bool isGranted = true)
    {
        try
        {
            var dto = new { RoleId = roleId, PermissionId = permissionId, IsGranted = isGranted };
            var response = await _httpClient.PostAsJsonAsync($"/api/rbac/roles/{roleId}/permissions", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asignando permiso {PermissionId} al rol {RoleId}", permissionId, roleId);
            return false;
        }
    }

    public async Task<bool> RemovePermissionFromRoleAsync(int roleId, int permissionId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/rbac/roles/{roleId}/permissions/{permissionId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removiendo permiso {PermissionId} del rol {RoleId}", permissionId, roleId);
            return false;
        }
    }

    public async Task<bool> SetRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds)
    {
        try
        {
            var dto = new { PermissionIds = permissionIds.ToList() };
            var response = await _httpClient.PutAsJsonAsync($"/api/rbac/roles/{roleId}/permissions", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estableciendo permisos del rol {RoleId}", roleId);
            return false;
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // USER ROLES
    // ───────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<UserRoleDto>> GetUserRolesAsync(string userName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/rbac/users/{userName}/roles");
            response.EnsureSuccessStatusCode();

            var userRoles = await response.Content.ReadFromJsonAsync<List<UserRoleDto>>();
            return userRoles ?? Enumerable.Empty<UserRoleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo roles del usuario {UserName}", userName);
            return Enumerable.Empty<UserRoleDto>();
        }
    }

    public async Task<IEnumerable<UserRoleDto>> GetAllUserRolesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/rbac/user-roles");
            response.EnsureSuccessStatusCode();

            var userRoles = await response.Content.ReadFromJsonAsync<List<UserRoleDto>>();
            return userRoles ?? Enumerable.Empty<UserRoleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo asignaciones usuario-rol");
            return Enumerable.Empty<UserRoleDto>();
        }
    }

    public async Task<UserRoleDto> AssignRoleToUserAsync(AssignUserRoleDto dto)
    {
        try
        {
            _logger.LogInformation("AssignRoleToUserAsync called: {@Dto}", dto);
            var response = await _httpClient.PostAsJsonAsync("/api/rbac/user-roles", dto);
            response.EnsureSuccessStatusCode();

            var userRole = await response.Content.ReadFromJsonAsync<UserRoleDto>();
            _logger.LogInformation("Successfully assigned role {RoleId} to user {UserName}, got UserRoleId={UserRoleId}", 
                dto.RoleId, dto.UserName, userRole?.Id);
            return userRole!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asignando rol al usuario {@Dto}", dto);
            throw;
        }
    }

    public async Task<bool> RemoveRoleFromUserAsync(int userRoleId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/rbac/user-roles/{userRoleId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removiendo asignación usuario-rol {UserRoleId}", userRoleId);
            return false;
        }
    }

    public async Task<bool> UpdateUserRoleAsync(int userRoleId, UpdateUserRoleDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/rbac/user-roles/{userRoleId}", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando asignación usuario-rol {UserRoleId}", userRoleId);
            return false;
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // USER PERMISSIONS
    // ───────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<UserPermissionDto>> GetUserCustomPermissionsAsync(string userName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/rbac/users/{userName}/permissions");
            response.EnsureSuccessStatusCode();

            var permissions = await response.Content.ReadFromJsonAsync<List<UserPermissionDto>>();
            return permissions ?? Enumerable.Empty<UserPermissionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo permisos custom del usuario {UserName}", userName);
            return Enumerable.Empty<UserPermissionDto>();
        }
    }

    public async Task<UserPermissionDto> GrantCustomPermissionAsync(GrantUserPermissionDto dto)
    {
        try
        {
            _logger.LogInformation("GrantCustomPermissionAsync called: {@Dto}", dto);
            var response = await _httpClient.PostAsJsonAsync("/api/rbac/user-permissions", dto);
            response.EnsureSuccessStatusCode();

            var permission = await response.Content.ReadFromJsonAsync<UserPermissionDto>();
            _logger.LogInformation("Successfully granted permission {PermissionId} to user {UserName}, IsGranted={IsGranted}, got UserPermissionId={UserPermissionId}", 
                dto.PermissionId, dto.UserName, dto.IsGranted, permission?.Id);
            return permission!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error otorgando permiso custom {@Dto}", dto);
            throw;
        }
    }

    public async Task<bool> RevokeCustomPermissionAsync(int userPermissionId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/rbac/user-permissions/{userPermissionId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revocando permiso custom {UserPermissionId}", userPermissionId);
            return false;
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // UTILITIES
    // ───────────────────────────────────────────────────────────────────

    public async Task<PermissionMatrixDto> GetRolePermissionMatrixAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/rbac/permission-matrix");
            response.EnsureSuccessStatusCode();

            var matrix = await response.Content.ReadFromJsonAsync<PermissionMatrixDto>();
            return matrix!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo matriz de permisos");
            throw;
        }
    }

    public async Task<UserPermissionSummaryDto> GetUserEffectivePermissionsAsync(string userName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/rbac/users/{userName}/effective-permissions");
            response.EnsureSuccessStatusCode();

            var summary = await response.Content.ReadFromJsonAsync<UserPermissionSummaryDto>();
            return summary!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo permisos efectivos del usuario {UserName}", userName);
            throw;
        }
    }

    /// <summary>
    /// Verifica si un usuario tiene un permiso específico
    /// Optimizado: consulta permisos efectivos y verifica en la lista
    /// </summary>
    public async Task<bool> HasPermissionAsync(string userName, string permissionCode)
    {
        try
        {
            var summary = await GetUserEffectivePermissionsAsync(userName);
            return summary.EffectivePermissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando permiso {PermissionCode} para usuario {UserName}", permissionCode, userName);
            return false;
        }
    }

    private string? GetBearerToken()
    {
        return _httpContextAccessor.HttpContext?.User.Claims
            .FirstOrDefault(c => c.Type == "ApiToken")?.Value;
    }
}
