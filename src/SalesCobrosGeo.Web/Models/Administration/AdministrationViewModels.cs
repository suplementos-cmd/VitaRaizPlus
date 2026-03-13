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
    string Email,
    string RoleCode,
    string Zone,
    string Status,
    string LastAccess);

public sealed record AdministrationPageViewModel(
    IReadOnlyList<RoleProfileCard> Roles,
    IReadOnlyList<AdminUserCard> Users);
