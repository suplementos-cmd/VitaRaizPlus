using System.ComponentModel.DataAnnotations;

namespace SalesCobrosGeo.Api.Contracts.Clients;

public sealed record CreateClientRequest(
    [Required, MaxLength(160)] string FullName,
    [Required, MaxLength(20), Phone] string Mobile,
    [MaxLength(20), Phone] string? Phone,
    [Required, MaxLength(32)] string ZoneCode,
    [Required, MaxLength(32)] string CollectionDay,
    [Required, MaxLength(256)] string Address,
    bool IsActive = true);

public sealed record UpdateClientRequest(
    [Required, MaxLength(160)] string FullName,
    [Required, MaxLength(20), Phone] string Mobile,
    [MaxLength(20), Phone] string? Phone,
    [Required, MaxLength(32)] string ZoneCode,
    [Required, MaxLength(32)] string CollectionDay,
    [Required, MaxLength(256)] string Address,
    bool IsActive = true);
