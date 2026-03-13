namespace SalesCobrosGeo.Web.Models.Administration;

public sealed class UserAdminInput
{
    public string? OriginalUsername { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Theme { get; set; } = "root";
    public string Role { get; set; } = "FULL";
    public string RoleLabel { get; set; } = "Acceso total";
    public bool IsActive { get; set; } = true;
    public bool TwoFactorEnabled { get; set; }
    public string? Password { get; set; }
    public List<string> Permissions { get; set; } = [];
}

public sealed record UserEditViewModel(
    string Title,
    UserAdminInput Input,
    IReadOnlyList<string> AvailablePermissions,
    IReadOnlyList<string> AvailableRoles,
    IReadOnlyList<string> AvailableThemes);

public sealed record AuditDetailViewModel(
    long Id,
    string Timestamp,
    string EventType,
    string Username,
    string Description,
    string Path,
    string Coordinates,
    string Metadata);
