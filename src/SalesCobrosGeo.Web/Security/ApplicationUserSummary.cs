namespace SalesCobrosGeo.Web.Security;

public sealed record ApplicationUserSummary(
    string Username,
    string DisplayName,
    string Role,
    string RoleLabel,
    string Zone,
    string Theme,
    bool IsActive,
    IReadOnlyList<string> Permissions);
