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
    int PermissionCount,
    int RbacActiveRoles = 0,
    int RbacTotalPermissions = 0);

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

public sealed record AdminSummaryCard(
    string Label,
    string Value,
    string Tone,
    string Caption);

public sealed class AdministrationPageViewModel
{
    public IReadOnlyList<RoleProfileCard> Roles { get; init; } = [];
    public IReadOnlyList<AdminUserCard> Users { get; init; } = [];
    public IReadOnlyList<AdminSessionCard> Sessions { get; init; } = [];
    public UserEditViewModel Editor { get; init; } = new(string.Empty, new UserAdminInput(), [], [], []);
    public UserWithRbacEditViewModel EditorWithRbac { get; init; } = new(string.Empty, new UserWithRbacInput(), [], [], [], [], [], [], []);
    public bool ShowEditor { get; init; }
    public IReadOnlyList<AdminSummaryCard> SummaryCards { get; init; } = [];
    public int AuditTotal { get; init; }
    public string? Message { get; init; }
}

public sealed class AuditListPageViewModel
{
    public IReadOnlyList<AdminAuditCard> AuditTrail { get; init; } = [];
    public int AuditPage { get; init; }
    public int TotalAuditPages { get; init; }
    public int TotalEvents { get; init; }
}
