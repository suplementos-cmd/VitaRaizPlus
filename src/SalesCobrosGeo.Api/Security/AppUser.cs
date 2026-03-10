namespace SalesCobrosGeo.Api.Security;

public sealed record AppUser(
    string UserName,
    string Password,
    string FullName,
    SalesCobrosGeo.Shared.Security.UserRole Role,
    bool IsActive
);
