namespace SalesCobrosGeo.Web.Models.Administration;

public sealed record RoleTheme(string Name, string PrimaryColor, string AccentColor, string SurfaceColor);

public sealed record RolePermissionRow(string Area, string Access, string Fields);

public sealed record RoleProfileCard(
    string Code,
    string Name,
    string Summary,
    RoleTheme Theme,
    IReadOnlyList<string> Views,
    IReadOnlyList<RolePermissionRow> Permissions);

public sealed record AdminUserCard(
    string Name,
    string Username,
    string RoleCode,
    string Zone,
    string Status,
    string AccessLabel,
    string Theme,
    bool TwoFactorEnabled,
    int PermissionCount);

public sealed record AdminSessionCard(
    string Username,
    string DisplayName,
    string RoleLabel,
    string Zone,
    bool IsActive,
    bool IsConnected,
    string LastSeen,
    string LastPath,
    string LastIp,
    string LastUserAgent,
    string LastCoordinates,
    string LastLocationSource);

public sealed record AdminAuditCard(
    long Id,
    string Timestamp,
    string EventType,
    string Username,
    string Description,
    string Path,
    string Coordinates);

public sealed record AdministrationPageViewModel(
    IReadOnlyList<RoleProfileCard> Roles,
    IReadOnlyList<AdminUserCard> Users,
    IReadOnlyList<AdminSessionCard> Sessions,
    IReadOnlyList<AdminAuditCard> AuditTrail,
    UserEditViewModel Editor,
    int AuditPage,
    int TotalAuditPages,
    string? Message = null);
