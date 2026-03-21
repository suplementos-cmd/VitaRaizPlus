namespace SalesCobrosGeo.Web.Services.Users;

/// <summary>
/// Servicio para gestionar usuarios (CRUD, roles RBAC, permisos)
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Obtener todos los usuarios con información agregada de RBAC
    /// </summary>
    Task<IEnumerable<UserDto>> GetAllUsersAsync();

    /// <summary>
    /// Obtener detalle completo de un usuario específico
    /// </summary>
    Task<UserDetailDto?> GetUserDetailAsync(string userName);

    /// <summary>
    /// Crear un nuevo usuario
    /// </summary>
    Task<UserDto> CreateUserAsync(CreateUserDto dto);

    /// <summary>
    /// Actualizar un usuario existente
    /// </summary>
    Task<UserDto> UpdateUserAsync(string userName, UpdateUserDto dto);

    /// <summary>
    /// Eliminar un usuario
    /// </summary>
    Task<bool> DeleteUserAsync(string userName);
}
