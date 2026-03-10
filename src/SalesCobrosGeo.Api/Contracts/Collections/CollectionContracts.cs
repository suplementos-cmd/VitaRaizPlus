namespace SalesCobrosGeo.Api.Contracts.Collections;

public sealed record RegisterCollectionRequest(
    int SaleId,
    decimal Amount,
    string Coordinates,
    string? Notes,
    DateTime? CollectedAtUtc);

public sealed record ReassignPortfolioRequest(
    string FromCollector,
    string ToCollector,
    IReadOnlyList<int> SaleIds,
    string? Reason);
