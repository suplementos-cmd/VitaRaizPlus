namespace SalesCobrosGeo.Shared.Security;

public static class UserRoleExtensions
{
    public static string ToClaimValue(this UserRole role)
    {
        return role.ToString();
    }

    public static bool TryParseClaim(string? value, out UserRole role)
    {
        return Enum.TryParse(value, ignoreCase: true, out role);
    }
}
