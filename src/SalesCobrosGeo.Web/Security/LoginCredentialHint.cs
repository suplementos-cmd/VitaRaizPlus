namespace SalesCobrosGeo.Web.Security;

public sealed record LoginCredentialHint(
    string Username,
    string Password,
    string RoleLabel,
    string Summary);
