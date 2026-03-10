using SalesCobrosGeo.Api.Business;

namespace SalesCobrosGeo.Api.Contracts.Catalogs;

public sealed record CatalogSnapshot(
    int Version,
    IReadOnlyList<Zone> Zones,
    IReadOnlyList<Product> Products,
    IReadOnlyList<PaymentMethod> PaymentMethods);

public sealed record CreateZoneRequest(string Code, string Name, bool IsActive = true);
public sealed record UpdateZoneRequest(string Code, string Name, bool IsActive = true);

public sealed record CreateProductRequest(string Code, string Name, decimal Price, bool IsActive = true);
public sealed record UpdateProductRequest(string Code, string Name, decimal Price, bool IsActive = true);

public sealed record CreatePaymentMethodRequest(string Code, string Name, bool IsActive = true);
public sealed record UpdatePaymentMethodRequest(string Code, string Name, bool IsActive = true);
