namespace SalesCobrosGeo.Web.Security;

public sealed class AppUserAccount
{
    public required string Username { get; init; }

    public required string DisplayName { get; init; }

    public required string PasswordHash { get; set; }

    public required string Theme { get; init; }

    public required string Role { get; init; }

    public required string RoleLabel { get; init; }

    public required string Zone { get; init; }

    public bool IsActive { get; set; } = true;

    public required IReadOnlyList<string> Permissions { get; init; }
}
