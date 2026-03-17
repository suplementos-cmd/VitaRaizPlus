using System.ComponentModel.DataAnnotations;
using SalesCobrosGeo.Api.Business;

namespace SalesCobrosGeo.Api.Contracts.Catalogs;

public sealed record CatalogSnapshot(
    int Version,
    IReadOnlyList<Zone> Zones,
    IReadOnlyList<Product> Products,
    IReadOnlyList<PaymentMethod> PaymentMethods);

public sealed record CreateZoneRequest(
    [Required, MaxLength(32)] string Code,
    [Required, MaxLength(128)] string Name,
    bool IsActive = true);

public sealed record UpdateZoneRequest(
    [Required, MaxLength(32)] string Code,
    [Required, MaxLength(128)] string Name,
    bool IsActive = true);

public sealed record CreateProductRequest(
    [Required, MaxLength(32)] string Code,
    [Required, MaxLength(256)] string Name,
    [Range(0.01, 999_999.99)] decimal Price,
    bool IsActive = true);

public sealed record UpdateProductRequest(
    [Required, MaxLength(32)] string Code,
    [Required, MaxLength(256)] string Name,
    [Range(0.01, 999_999.99)] decimal Price,
    bool IsActive = true);

public sealed record CreatePaymentMethodRequest(
    [Required, MaxLength(32)] string Code,
    [Required, MaxLength(128)] string Name,
    bool IsActive = true);

public sealed record UpdatePaymentMethodRequest(
    [Required, MaxLength(32)] string Code,
    [Required, MaxLength(128)] string Name,
    bool IsActive = true);
