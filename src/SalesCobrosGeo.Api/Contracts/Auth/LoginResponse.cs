namespace SalesCobrosGeo.Api.Contracts.Auth;

public sealed record LoginResponse(
    string AccessToken,
    string UserName,
    string FullName,
    string Role,
    DateTime ExpiresAtUtc
);
