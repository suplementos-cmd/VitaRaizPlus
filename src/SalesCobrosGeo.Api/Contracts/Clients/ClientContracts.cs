namespace SalesCobrosGeo.Api.Contracts.Clients;

public sealed record CreateClientRequest(
    string FullName,
    string Mobile,
    string? Phone,
    string ZoneCode,
    string CollectionDay,
    string Address,
    bool IsActive = true);

public sealed record UpdateClientRequest(
    string FullName,
    string Mobile,
    string? Phone,
    string ZoneCode,
    string CollectionDay,
    string Address,
    bool IsActive = true);
