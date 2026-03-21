using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using SalesCobrosGeo.Api.Data;
using SalesCobrosGeo.Web.Models.Administration;
using SalesCobrosGeo.Web.Services.Rbac;

namespace SalesCobrosGeo.Web.Security;

/// <summary>
/// Servicio de autenticación que consume la API (Excel como fuente de datos)
/// Reemplaza SqliteApplicationUserService para unificar datos en Excel
/// INTEGRACIÓN RBAC: Carga permisos efectivos del usuario desde RBAC al login
/// </summary>
public sealed class ApiAuthenticationService : IApplicationUserService
{
    private readonly HttpClient _httpClient;
    private readonly ExcelDataService _excelService;
    private readonly IRbacService _rbacService;
    private readonly ILogger<ApiAuthenticationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiAuthenticationService(
        HttpClient httpClient,
        ExcelDataService excelService,
        IRbacService rbacService,
        ILogger<ApiAuthenticationService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _excelService = excelService;
        _rbacService = rbacService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal? ValidateCredentials(string username, string password)
    {
        try
        {
            _logger.LogInformation("Validando credenciales via API para usuario: {UserName}", username);

            // Llamar a API para autenticar
            var response = _httpClient.PostAsJsonAsync("api/auth/login", new
            {
                userName = username,
                password = password
            }).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                _logger.LogWarning("Autenticación fallida para usuario: {UserName}, Status: {StatusCode}", username, statusCode);
                
                // Log del contenido de error para debugging
                var errorContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                _logger.LogWarning("Error de API: {ErrorContent}", errorContent);
                
                return null;
            }

            var loginResponse = response.Content.ReadFromJsonAsync<LoginResponseDto>()
                .GetAwaiter().GetResult();

            if (loginResponse == null)
            {
                _logger.LogError("Respuesta de API nula para usuario: {UserName}", username);
                return null;
            }

            _logger.LogInformation("Autenticación exitosa via API para usuario: {UserName}, Role: {Role}",
                loginResponse.UserName, loginResponse.Role);

            // Guardar token en sesión para requests futuros
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                httpContext.Session.SetString("ApiToken", loginResponse.AccessToken);
                httpContext.Session.SetString("ApiTokenExpiry", loginResponse.ExpiresAtUtc.ToString("O"));
            }

            // ═══════════════════════════════════════════════════════════════════
            // OBTENER DATOS COMPLETOS DEL USUARIO DESDE EXCEL (Tema, Zona, etc)
            // ═══════════════════════════════════════════════════════════════════
            var userSummary = GetUser(loginResponse.UserName);
            var userTheme = userSummary?.Theme ?? "root";
            var userZone = userSummary?.Zone ?? "Default";

            // ═══════════════════════════════════════════════════════════════════
            // SISTEMA RBAC PURO: Solo cargar claims básicos, permisos vienen de RBAC
            // ═══════════════════════════════════════════════════════════════════
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, loginResponse.UserName),
                new Claim(ClaimTypes.GivenName, loginResponse.FullName),
                new Claim(AppClaimTypes.DisplayRole, loginResponse.Role),
                new Claim(AppClaimTypes.Theme, userTheme),
                new Claim("ApiToken", loginResponse.AccessToken),
                new Claim("Zone", userZone)
            };

            _logger.LogInformation(
                "Claims básicos creados para {UserName} (Tema: {Theme}, Zona: {Zone}). Permisos se validarán via RBAC en tiempo real.",
                loginResponse.UserName, userTheme, userZone);

            var identity = new ClaimsIdentity(claims, "ApiAuth");
            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar credenciales via API para usuario: {UserName}", username);
            return null;
        }
    }

    public IReadOnlyList<ApplicationUserSummary> GetUsers()
    {
        try
        {
            var users = _excelService.ReadSheetAsync("Users").GetAwaiter().GetResult();
            
            return users.Select(row => new ApplicationUserSummary(
                Username: GetString(row, "UserName") ?? "",
                DisplayName: GetString(row, "DisplayName") ?? "",
                Role: GetString(row, "Role") ?? "Unknown",
                RoleLabel: GetString(row, "RoleLabel") ?? "Unknown",
                Zone: GetString(row, "Zone") ?? "Default",
                Theme: GetString(row, "Theme") ?? "root",
                IsActive: GetBool(row, "IsActive") ?? true,
                TwoFactorEnabled: GetBool(row, "TwoFactorEnabled") ?? false,
                Permissions: Array.Empty<string>()
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios desde Excel");
            return Array.Empty<ApplicationUserSummary>();
        }
    }

    public IReadOnlyList<LoginCredentialHint> GetLoginHints()
    {
        // Login hints (sugerencias visuales) no implementadas
        // Funcionalidad opcional que no afecta autenticación
        return Array.Empty<LoginCredentialHint>();
    }

    public bool SetActive(string username, bool isActive)
    {
        try
        {
            _excelService.UpdateRowsAsync("Users",
                row => GetString(row, "UserName")?.Equals(username, StringComparison.OrdinalIgnoreCase) == true,
                row => row["IsActive"] = isActive
            ).GetAwaiter().GetResult();

            _logger.LogInformation("Usuario {Username} estado cambiado a {IsActive}", username, isActive);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al establecer estado activo para usuario {Username}", username);
            return false;
        }
    }

    public ApplicationUserSummary? GetUser(string username)
    {
        try
        {
            var users = _excelService.ReadSheetAsync("Users").GetAwaiter().GetResult();
            var userRow = users.FirstOrDefault(row => 
                GetString(row, "UserName")?.Equals(username, StringComparison.OrdinalIgnoreCase) == true);
            
            if (userRow == null)
                return null;

            return new ApplicationUserSummary(
                Username: GetString(userRow, "UserName") ?? "",
                DisplayName: GetString(userRow, "DisplayName") ?? "",
                Role: GetString(userRow, "Role") ?? "Unknown",
                RoleLabel: GetString(userRow, "RoleLabel") ?? "Unknown",
                Zone: GetString(userRow, "Zone") ?? "Default",
                Theme: GetString(userRow, "Theme") ?? "root",
                IsActive: GetBool(userRow, "IsActive") ?? true,
                TwoFactorEnabled: GetBool(userRow, "TwoFactorEnabled") ?? false,
                Permissions: Array.Empty<string>()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario {Username} desde Excel", username);
            return null;
        }
    }

    public ApplicationUserSummary SaveUser(UserAdminInput input)
    {
        try
        {
            var isUpdate = !string.IsNullOrEmpty(input.OriginalUsername);

            if (isUpdate)
            {
                // Actualizar usuario existente
                _excelService.UpdateRowsAsync("Users",
                    row => GetString(row, "UserName")?.Equals(input.OriginalUsername, StringComparison.OrdinalIgnoreCase) == true,
                    row =>
                    {
                        row["UserName"] = input.Username;
                        row["DisplayName"] = input.DisplayName;
                        row["Role"] = input.Role;
                        row["RoleLabel"] = input.RoleLabel;
                        row["Zone"] = input.Zone;
                        row["Theme"] = input.Theme;
                        row["IsActive"] = input.IsActive;
                        row["TwoFactorEnabled"] = input.TwoFactorEnabled;
                        
                        if (!string.IsNullOrEmpty(input.Password))
                        {
                            row["Password"] = input.Password;
                        }
                    }).GetAwaiter().GetResult();

                _logger.LogInformation("Usuario actualizado: {Username}", input.Username);
            }
            else
            {
                // Crear nuevo usuario
                var newUserRow = new Dictionary<string, object?>
                {
                    ["UserName"] = input.Username,
                    ["DisplayName"] = input.DisplayName,
                    ["Password"] = string.IsNullOrEmpty(input.Password) 
                        ? "cambiar123" 
                        : input.Password,
                    ["Role"] = input.Role,
                    ["RoleLabel"] = input.RoleLabel,
                    ["Zone"] = input.Zone ?? "Default",
                    ["Theme"] = input.Theme ?? "root",
                    ["IsActive"] = input.IsActive,
                    ["TwoFactorEnabled"] = input.TwoFactorEnabled,
                    ["TwoFactorSecret"] = string.Empty,
                    ["CreatedUtc"] = DateTime.UtcNow,
                    ["UpdatedUtc"] = DateTime.UtcNow
                };

                _excelService.AppendRowAsync("Users", newUserRow).GetAwaiter().GetResult();
                _logger.LogInformation("Usuario creado: {Username}", input.Username);
            }

            return new ApplicationUserSummary(
                Username: input.Username,
                DisplayName: input.DisplayName,
                Role: input.Role,
                RoleLabel: input.RoleLabel,
                Zone: input.Zone,
                Theme: input.Theme,
                IsActive: input.IsActive,
                TwoFactorEnabled: input.TwoFactorEnabled,
                Permissions: input.Permissions?.ToArray() ?? Array.Empty<string>()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar usuario {Username}", input.Username);
            throw;
        }
    }

    public bool ResetPassword(string username, string newPassword)
    {
        try
        {
            _excelService.UpdateRowsAsync("Users",
                row => GetString(row, "UserName")?.Equals(username, StringComparison.OrdinalIgnoreCase) == true,
                row =>
                {
                    row["Password"] = newPassword;
                    row["UpdatedUtc"] = DateTime.UtcNow;
                }
            ).GetAwaiter().GetResult();

            _logger.LogInformation("Contraseña reseteada para usuario {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al resetear contraseña para usuario {Username}", username);
            return false;
        }
    }

    #region Helper Methods

    private static string? GetString(Dictionary<string, object?> row, string key)
    {
        return row.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static bool? GetBool(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is bool boolValue)
            return boolValue;

        return bool.TryParse(value.ToString(), out var result) ? result : null;
    }

    #endregion

    // DTO interno para deserializar respuesta de API
    private sealed record LoginResponseDto(
        string AccessToken,
        string UserName,
        string FullName,
        string Role,
        DateTime ExpiresAtUtc
    );
}
