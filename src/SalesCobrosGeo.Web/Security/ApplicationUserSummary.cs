namespace SalesCobrosGeo.Web.Security;

public sealed record ApplicationUserSummary(
    string Username,
    string DisplayName,
    string Role,
    string RoleLabel,
    string Zone,
    string Theme,
    bool IsActive,
    bool TwoFactorEnabled,
    IReadOnlyList<string> Permissions);
