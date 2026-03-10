using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Security;

public sealed record TokenSession(
    string Token,
    string UserName,
    string FullName,
    UserRole Role,
    DateTime ExpiresAtUtc
);
