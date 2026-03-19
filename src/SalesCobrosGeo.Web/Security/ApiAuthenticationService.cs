using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using SalesCobrosGeo.Web.Models.Administration;

namespace SalesCobrosGeo.Web.Security;

/// <summary>
/// Servicio de autenticación que consume la API (Excel como fuente de datos)
/// Reemplaza SqliteApplicationUserService para unificar datos en Excel
/// </summary>
public sealed class ApiAuthenticationService : IApplicationUserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiAuthenticationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiAuthenticationService(
        HttpClient httpClient,
        ILogger<ApiAuthenticationService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
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
                _logger.LogWarning("Autenticación fallida para usuario: {UserName}", username);
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

            // Crear ClaimsPrincipal compatible con el sistema existente
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, loginResponse.UserName),
                new Claim(ClaimTypes.GivenName, loginResponse.FullName),
                new Claim(ClaimTypes.Role, loginResponse.Role),
                new Claim("ApiToken", loginResponse.AccessToken)
            };

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
            // TODO: Implementar endpoint en API para listar usuarios
            // Por ahora retornar lista vacía
            _logger.LogWarning("GetUsers() no implementado aún en API");
            return Array.Empty<ApplicationUserSummary>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios via API");
            return Array.Empty<ApplicationUserSummary>();
        }
    }

    public IReadOnlyList<LoginCredentialHint> GetLoginHints()
    {
        try
        {
            // TODO: Implementar endpoint en API para hints de login
            // Por ahora retornar lista vacía
            _logger.LogWarning("GetLoginHints() no implementado aún en API");
            return Array.Empty<LoginCredentialHint>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener hints de login via API");
            return Array.Empty<LoginCredentialHint>();
        }
    }

    public bool SetActive(string username, bool isActive)
    {
        try
        {
            // TODO: Implementar endpoint en API para activar/desactivar usuarios
            _logger.LogWarning("SetActive() no implementado aún en API");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al establecer estado activo via API");
            return false;
        }
    }

    public ApplicationUserSummary? GetUser(string username)
    {
        try
        {
            // TODO: Implementar endpoint en API para obtener usuario específico
            _logger.LogWarning("GetUser() no implementado aún en API");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario via API");
            return null;
        }
    }

    public ApplicationUserSummary SaveUser(UserAdminInput input)
    {
        try
        {
            // TODO: Implementar endpoint en API para guardar usuario
            _logger.LogWarning("SaveUser() no implementado aún en API");
            throw new NotImplementedException("SaveUser no implementado aún");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar usuario via API");
            throw;
        }
    }

    public bool ResetPassword(string username, string newPassword)
    {
        try
        {
            // TODO: Implementar endpoint en API para resetear contraseña
            _logger.LogWarning("ResetPassword() no implementado aún en API");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al resetear contraseña via API");
            return false;
        }
    }

    // DTO interno para deserializar respuesta de API
    private sealed record LoginResponseDto(
        string AccessToken,
        string UserName,
        string FullName,
        string Role,
        DateTime ExpiresAtUtc
    );
}
