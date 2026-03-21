using SalesCobrosGeo.Api.Data;
using SalesCobrosGeo.Api.Models.Security;

namespace SalesCobrosGeo.Api.Services;

/// <summary>
/// Implementación del servicio de permisos usando Excel como storage
/// </summary>
public class ExcelPermissionService : IPermissionService
{
    private readonly ExcelDataService _excelService;
    private readonly ILogger<ExcelPermissionService> _logger;

    // Cache para mejorar rendimiento
    private Dictionary<string, Permission>? _permissionsCache;
    private Dictionary<int, Role>? _rolesCache;
    private DateTime? _lastCacheUpdate;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public ExcelPermissionService(
        ExcelDataService excelService,
        ILogger<ExcelPermissionService> logger)
    {
        _excelService = excelService;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userName)
    {
        try
        {
            var grantedPermissions = new HashSet<string>();
            var deniedPermissions = new HashSet<string>();

            // 1. Obtener roles activos del usuario
            var userRoleIds = await GetUserActiveRoleIdsAsync(userName);

            if (!userRoleIds.Any())
            {
                _logger.LogWarning("Usuario {UserName} no tiene roles asignados", userName);
                return Array.Empty<string>();
            }

            // 2. Obtener permisos de los roles
            foreach (var roleId in userRoleIds)
            {
                var rolePermissions = await GetRolePermissionsAsync(roleId);
                foreach (var rolePerm in rolePermissions.Where(rp => rp.IsGranted))
                {
                    var permCode = await GetPermissionCodeAsync(rolePerm.PermissionId);
                    if (permCode != null)
                    {
                        grantedPermissions.Add(permCode);
                    }
                }
            }

            // 3. Aplicar permisos custom del usuario (overrides)
            var customPermissions = await GetUserCustomPermissionsAsync(userName);
            foreach (var customPerm in customPermissions.Where(cp => cp.IsCurrentlyValid()))
            {
                var permCode = await GetPermissionCodeAsync(customPerm.PermissionId);
                if (permCode == null) continue;

                if (customPerm.IsGranted)
                {
                    grantedPermissions.Add(permCode);
                    deniedPermissions.Remove(permCode); // Override de deny anterior
                }
                else
                {
                    deniedPermissions.Add(permCode);
                    grantedPermissions.Remove(permCode); // Override de grant anterior
                }
            }

            var finalPermissions = grantedPermissions.Except(deniedPermissions).ToList();
            
            _logger.LogDebug(
                "Usuario {UserName}: {RoleCount} roles, {PermCount} permisos", 
                userName, userRoleIds.Count(), finalPermissions.Count);

            return finalPermissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo permisos para usuario {UserName}", userName);
            return Array.Empty<string>();
        }
    }

    public async Task<bool> HasPermissionAsync(string userName, string permissionCode)
    {
        var permissions = await GetUserPermissionsAsync(userName);
        return permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<PermissionEvaluationResult> EvaluatePermissionAsync(
        string userName, 
        string permissionCode)
    {
        var result = new PermissionEvaluationResult
        {
            UserName = userName,
            PermissionCode = permissionCode,
            IsGranted = false
        };

        try
        {
            // Evaluar roles que otorgan el permiso
            var userRoleIds = await GetUserActiveRoleIdsAsync(userName);
            foreach (var roleId in userRoleIds)
            {
                var role = await GetRoleByIdAsync(roleId);
                if (role == null) continue;

                var hasPermission = await RoleHasPermissionAsync(roleId, permissionCode);
                if (hasPermission)
                {
                    result.GrantedBy.Add($"Rol: {role.Name}");
                }
            }

            // Evaluar permisos custom
            var customPermissions = await GetUserCustomPermissionsAsync(userName);
            var customPerm = customPermissions.FirstOrDefault(cp =>
            {
                var code = GetPermissionCodeAsync(cp.PermissionId).Result;
                return code?.Equals(permissionCode, StringComparison.OrdinalIgnoreCase) == true
                    && cp.IsCurrentlyValid();
            });

            if (customPerm != null)
            {
                if (customPerm.IsGranted)
                {
                    result.GrantedBy.Add("Permiso Custom (Usuario)");
                }
                else
                {
                    result.DeniedBy.Add("Permiso Custom (Usuario) - DENEGADO");
                }
            }

            // Evaluación final
            result.IsGranted = result.GrantedBy.Any() && !result.DeniedBy.Any();
            result.Reason = result.IsGranted
                ? $"Otorgado por: {string.Join(", ", result.GrantedBy)}"
                : result.DeniedBy.Any()
                    ? $"Denegado por: {string.Join(", ", result.DeniedBy)}"
                    : "No posee el permiso";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluando permiso {PermissionCode} para {UserName}", 
                permissionCode, userName);
            
            result.Reason = $"Error: {ex.Message}";
            return result;
        }
    }

    public async Task<IEnumerable<Role>> GetUserActiveRolesAsync(string userName)
    {
        try
        {
            var roleIds = await GetUserActiveRoleIdsAsync(userName);
            var roles = new List<Role>();

            foreach (var roleId in roleIds)
            {
                var role = await GetRoleByIdAsync(roleId);
                if (role != null)
                {
                    roles.Add(role);
                }
            }

            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo roles activos para {UserName}", userName);
            return Array.Empty<Role>();
        }
    }

    public async Task<bool> HasAnyPermissionAsync(string userName, params string[] permissionCodes)
    {
        var userPermissions = await GetUserPermissionsAsync(userName);
        return permissionCodes.Any(p => 
            userPermissions.Contains(p, StringComparer.OrdinalIgnoreCase));
    }

    public async Task<bool> HasAllPermissionsAsync(string userName, params string[] permissionCodes)
    {
        var userPermissions = await GetUserPermissionsAsync(userName);
        return permissionCodes.All(p => 
            userPermissions.Contains(p, StringComparer.OrdinalIgnoreCase));
    }

    #region Métodos Privados de Lectura Excel

    private async Task<List<int>> GetUserActiveRoleIdsAsync(string userName)
    {
        try
        {
            var userRoles = await _excelService.ReadSheetAsync("UserRoles");
            var now = DateTime.UtcNow;

            return userRoles
                .Where(row =>
                {
                    var rowUserName = GetString(row, "UserName");
                    var isActive = GetBool(row, "IsActive");
                    var startDate = GetDate(row, "StartDate");
                    var endDate = GetDate(row, "EndDate");

                    return rowUserName == userName &&
                           isActive == true &&
                           startDate <= now &&
                           (endDate == null || endDate.Value >= now);
                })
                .Select(row => GetInt(row, "RoleId"))
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo UserRoles para {UserName}", userName);
            return new List<int>();
        }
    }

    private async Task<List<RolePermission>> GetRolePermissionsAsync(int roleId)
    {
        try
        {
            var rolePermissions = await _excelService.ReadSheetAsync("RolePermissions");

            return rolePermissions
                .Where(row => GetInt(row, "RoleId") == roleId)
                .Select(row => new RolePermission
                {
                    Id = GetInt(row, "Id"),
                    RoleId = GetInt(row, "RoleId"),
                    PermissionId = GetInt(row, "PermissionId"),
                    IsGranted = GetBool(row, "IsGranted") ?? true
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo RolePermissions para RoleId {RoleId}", roleId);
            return new List<RolePermission>();
        }
    }

    private async Task<List<UserPermission>> GetUserCustomPermissionsAsync(string userName)
    {
        try
        {
            var userPermissions = await _excelService.ReadSheetAsync("UserPermissions");

            return userPermissions
                .Where(row => GetString(row, "UserName") == userName)
                .Select(row => new UserPermission
                {
                    Id = GetInt(row, "Id"),
                    UserName = GetString(row, "UserName"),
                    PermissionId = GetInt(row, "PermissionId"),
                    IsGranted = GetBool(row, "IsGranted") ?? true,
                    StartDate = GetDate(row, "StartDate"),
                    EndDate = GetDate(row, "EndDate")
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo UserPermissions para {UserName}", userName);
            return new List<UserPermission>();
        }
    }

    private async Task<string?> GetPermissionCodeAsync(int permissionId)
    {
        try
        {
            // Usar cache si está disponible y vigente
            if (_permissionsCache != null && 
                _lastCacheUpdate != null && 
                DateTime.UtcNow - _lastCacheUpdate.Value < _cacheExpiration)
            {
                return _permissionsCache.Values
                    .FirstOrDefault(p => p.Id == permissionId)?.Code;
            }

            // Cargar cache
            var permissions = await _excelService.ReadSheetAsync("Permissions");
            _permissionsCache = permissions
                .Select(row => new Permission
                {
                    Id = GetInt(row, "Id"),
                    Code = GetString(row, "Code"),
                    ModuleId = GetInt(row, "ModuleId"),
                    ActionId = GetInt(row, "ActionId"),
                    Description = GetString(row, "Description"),
                    IsActive = GetBool(row, "IsActive") ?? true
                })
                .ToDictionary(p => p.Code, p => p);

            _lastCacheUpdate = DateTime.UtcNow;

            return _permissionsCache.Values
                .FirstOrDefault(p => p.Id == permissionId)?.Code;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo código de permiso para Id {PermissionId}", permissionId);
            return null;
        }
    }

    private async Task<Role?> GetRoleByIdAsync(int roleId)
    {
        try
        {
            // Usar cache si está disponible y vigente
            if (_rolesCache != null && 
                _lastCacheUpdate != null && 
                DateTime.UtcNow - _lastCacheUpdate.Value < _cacheExpiration)
            {
                return _rolesCache.GetValueOrDefault(roleId);
            }

            // Cargar cache
            var roles = await _excelService.ReadSheetAsync("Roles");
            _rolesCache = roles
                .Select(row => new Role
                {
                    Id = GetInt(row, "Id"),
                    Code = GetString(row, "Code"),
                    Name = GetString(row, "Name"),
                    Description = GetString(row, "Description"),
                    IsActive = GetBool(row, "IsActive") ?? true,
                    IsSystem = GetBool(row, "IsSystem") ?? false
                })
                .ToDictionary(r => r.Id, r => r);

            _lastCacheUpdate = DateTime.UtcNow;

            return _rolesCache.GetValueOrDefault(roleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo rol para Id {RoleId}", roleId);
            return null;
        }
    }

    private async Task<bool> RoleHasPermissionAsync(int roleId, string permissionCode)
    {
        try
        {
            var rolePermissions = await GetRolePermissionsAsync(roleId);
            
            foreach (var rolePerm in rolePermissions.Where(rp => rp.IsGranted))
            {
                var code = await GetPermissionCodeAsync(rolePerm.PermissionId);
                if (code?.Equals(permissionCode, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando permiso {PermissionCode} para RoleId {RoleId}", 
                permissionCode, roleId);
            return false;
        }
    }

    #endregion

    #region Helpers de Conversión

    private static string GetString(Dictionary<string, object?> row, string key)
    {
        return row.TryGetValue(key, out var value) && value != null
            ? value.ToString() ?? string.Empty
            : string.Empty;
    }

    private static int GetInt(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return 0;

        if (value is int intValue)
            return intValue;

        if (value is double doubleValue)
            return (int)doubleValue;

        if (int.TryParse(value.ToString(), out var parsed))
            return parsed;

        return 0;
    }

    private static bool? GetBool(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is bool boolValue)
            return boolValue;

        var strValue = value.ToString()?.ToUpperInvariant();
        if (strValue == "TRUE" || strValue == "VERDADERO" || strValue == "1")
            return true;
        if (strValue == "FALSE" || strValue == "FALSO" || strValue == "0")
            return false;

        return null;
    }

    private static DateTime? GetDate(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is DateTime dateValue)
            return dateValue;

        if (DateTime.TryParse(value.ToString(), out var parsed))
            return parsed;

        return null;
    }

    #endregion
}
