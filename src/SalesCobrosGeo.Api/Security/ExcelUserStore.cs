using SalesCobrosGeo.Api.Data;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Security;

/// <summary>
/// Implementación de IUserStore que usa Excel como almacenamiento.
/// Reemplaza InMemoryUserStore eliminando datos hardcodeados.
/// </summary>
public sealed class ExcelUserStore : IUserStore
{
    private readonly ExcelDataService _excelService;
    private const string SheetName = "Users";

    public ExcelUserStore(ExcelDataService excelService)
    {
        _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
    }

    public AppUser? ValidateCredentials(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var users = _excelService.ReadSheetAsync(SheetName).GetAwaiter().GetResult();
        
        var userRow = users.FirstOrDefault(row => 
            string.Equals(row["UserName"]?.ToString(), userName, StringComparison.OrdinalIgnoreCase));

        if (userRow == null)
        {
            return null;
        }

        var storedPassword = userRow["Password"]?.ToString() ?? string.Empty;
        var isActive = Convert.ToBoolean(userRow["IsActive"]);

        if (!isActive || storedPassword != password)
        {
            return null;
        }

        return MapToAppUser(userRow);
    }

    public AppUser? FindByUserName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        var users = _excelService.ReadSheetAsync(SheetName).GetAwaiter().GetResult();
        
        var userRow = users.FirstOrDefault(row => 
            string.Equals(row["UserName"]?.ToString(), userName, StringComparison.OrdinalIgnoreCase));

        return userRow != null ? MapToAppUser(userRow) : null;
    }

    public async Task<AppUser> AddUserAsync(string userName, string password, string displayName, UserRole role, bool isActive = true)
    {
        var users = await _excelService.ReadSheetAsync(SheetName);
        
        if (users.Any(row => string.Equals(row["UserName"]?.ToString(), userName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"User '{userName}' already exists.");
        }

        var newUser = new Dictionary<string, object?>
        {
            ["UserName"] = userName,
            ["Password"] = password,
            ["DisplayName"] = displayName,
            ["Role"] = role.ToString(),
            ["IsActive"] = isActive
        };

        await _excelService.AppendRowAsync(SheetName, newUser);

        return new AppUser(userName, password, displayName, role, isActive);
    }

    public async Task<AppUser> UpdateUserAsync(string userName, string? newPassword, string? newDisplayName, UserRole? newRole, bool? newIsActive)
    {
        await _excelService.UpdateRowsAsync(
            SheetName,
            row => string.Equals(row["UserName"]?.ToString(), userName, StringComparison.OrdinalIgnoreCase),
            row =>
            {
                if (newPassword != null) row["Password"] = newPassword;
                if (newDisplayName != null) row["DisplayName"] = newDisplayName;
                if (newRole.HasValue) row["Role"] = newRole.Value.ToString();
                if (newIsActive.HasValue) row["IsActive"] = newIsActive.Value;
            });

        var user = FindByUserName(userName);
        if (user == null)
        {
            throw new InvalidOperationException($"User '{userName}' not found after update.");
        }

        return user;
    }

    public async Task<bool> DeleteUserAsync(string userName)
    {
        var users = await _excelService.ReadSheetAsync(SheetName);
        var userExists = users.Any(row => string.Equals(row["UserName"]?.ToString(), userName, StringComparison.OrdinalIgnoreCase));

        if (!userExists)
        {
            return false;
        }

        await _excelService.DeleteRowsAsync(
            SheetName,
            row => string.Equals(row["UserName"]?.ToString(), userName, StringComparison.OrdinalIgnoreCase));

        return true;
    }

    public async Task<IReadOnlyList<AppUser>> GetAllUsersAsync()
    {
        var users = await _excelService.ReadSheetAsync(SheetName);
        return users.Select(MapToAppUser).ToList();
    }

    private static AppUser MapToAppUser(Dictionary<string, object?> row)
    {
        var userName = row["UserName"]?.ToString() ?? string.Empty;
        var password = row["Password"]?.ToString() ?? string.Empty;
        var displayName = row["DisplayName"]?.ToString() ?? string.Empty;
        var roleStr = row["Role"]?.ToString() ?? nameof(UserRole.Vendedor);
        var isActive = Convert.ToBoolean(row["IsActive"]);

        Enum.TryParse<UserRole>(roleStr, out var role);

        return new AppUser(userName, password, displayName, role, isActive);
    }
}
