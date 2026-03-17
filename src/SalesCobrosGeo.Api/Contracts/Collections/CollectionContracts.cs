using System.ComponentModel.DataAnnotations;

namespace SalesCobrosGeo.Api.Contracts.Collections;

public sealed record RegisterCollectionRequest(
    [Range(1, int.MaxValue)] int SaleId,
    [Range(0.01, 999_999.99)] decimal Amount,
    [Required, MaxLength(128)] string Coordinates,
    [MaxLength(512)] string? Notes,
    DateTime? CollectedAtUtc);

public sealed record ReassignPortfolioRequest(
    [Required, MaxLength(64)] string FromCollector,
    [Required, MaxLength(64)] string ToCollector,
    [Required, MinLength(1)] IReadOnlyList<int> SaleIds,
    [MaxLength(512)] string? Reason);
