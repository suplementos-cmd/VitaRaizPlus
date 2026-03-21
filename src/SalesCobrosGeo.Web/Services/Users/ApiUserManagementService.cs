using System.Net;
using System.Net.Http.Json;

namespace SalesCobrosGeo.Web.Services.Users;

/// <summary>
/// Implementación que consume el API de usuarios
/// </summary>
public sealed class ApiUserManagementService : IUserManagementService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiUserManagementService> _logger;

    public ApiUserManagementService(IHttpClientFactory httpClientFactory, ILogger<ApiUserManagementService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
        _logger = logger;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/users");
            response.EnsureSuccessStatusCode();

            var users = await response.Content.ReadFromJsonAsync<UserDto[]>();
            return users ?? Array.Empty<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo lista de usuarios del API");
            throw;
        }
    }

    public async Task<UserDetailDto?> GetUserDetailAsync(string userName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/users/{Uri.EscapeDataString(userName)}");
            
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<UserDetailDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo detalle del usuario {UserName}", userName);
            throw;
        }
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/users", dto);
            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            if (user == null)
            {
                throw new InvalidOperationException("El API retornó una respuesta vacía");
            }

            return user;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            _logger.LogWarning(ex, "Validación fallida al crear usuario {@Dto}", dto);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando usuario {@Dto}", dto);
            throw;
        }
    }

    public async Task<UserDto> UpdateUserAsync(string userName, UpdateUserDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/users/{Uri.EscapeDataString(userName)}", dto);
            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            if (user == null)
            {
                throw new InvalidOperationException("El API retornó una respuesta vacía");
            }

            return user;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Usuario no encontrado: {UserName}", userName);
            throw new InvalidOperationException($"Usuario '{userName}' no encontrado");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            _logger.LogWarning(ex, "Validación fallida al actualizar usuario {UserName}", userName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando usuario {UserName}: {@Dto}", userName, dto);
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(string userName)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/users/{Uri.EscapeDataString(userName)}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            _logger.LogWarning(ex, "No se puede eliminar usuario {UserName} (protegido)", userName);
            throw new InvalidOperationException("No se puede eliminar este usuario");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando usuario {UserName}", userName);
            throw;
        }
    }
}
