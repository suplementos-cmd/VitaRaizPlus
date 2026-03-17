using System.ComponentModel.DataAnnotations;
using SalesCobrosGeo.Api.Business;

namespace SalesCobrosGeo.Api.Contracts.Sales;

public sealed record CreateSaleItemRequest(
    [Range(1, int.MaxValue)] int ProductId,
    [Range(1, 9999)] int Quantity,
    [Range(0.01, 999_999.99)] decimal? UnitPrice);

public sealed record SaleEvidenceRequest(
    [Required, MaxLength(128)] string PrimaryCoordinates,
    [MaxLength(128)] string? SecondaryCoordinates,
    [MaxLength(512), Url] string? LocationUrl,
    IReadOnlyList<string> PhotoUrls);

public sealed record CreateSaleRequest(
    [Range(1, int.MaxValue)] int ClientId,
    [Required, MaxLength(32)] string PaymentMethodCode,
    [Required, MaxLength(32)] string CollectionDay,
    [MaxLength(64)] string? CollectorUserName,
    [Range(0, 100)] decimal SellerCommissionPercent,
    bool Collectable,
    [MaxLength(512)] string? Notes,
    bool IsDraft,
    [Required, MinLength(1)] IReadOnlyList<CreateSaleItemRequest> Items,
    [Required] SaleEvidenceRequest Evidence);

public sealed record UpdateSaleDraftRequest(
    [Range(1, int.MaxValue)] int ClientId,
    [Required, MaxLength(32)] string PaymentMethodCode,
    [Required, MaxLength(32)] string CollectionDay,
    [MaxLength(64)] string? CollectorUserName,
    [Range(0, 100)] decimal SellerCommissionPercent,
    bool Collectable,
    [MaxLength(512)] string? Notes,
    bool SubmitForReview,
    [MaxLength(512)] string? ChangeReason,
    [Required, MinLength(1)] IReadOnlyList<CreateSaleItemRequest> Items,
    [Required] SaleEvidenceRequest Evidence);

public sealed record ReviewSaleRequest(
    [Required, MaxLength(32)] string Action,
    [MaxLength(512)] string? Reason);

public sealed record AssignCollectorRequest(
    [Required, MaxLength(64)] string CollectorUserName,
    [MaxLength(512)] string? Reason);

public sealed record SaleSummaryResponse(
    int Id,
    string SaleNumber,
    int ClientId,
    string SellerUserName,
    string? CollectorUserName,
    SaleWorkflowStatus Status,
    CollectionWorkflowStatus CollectionStatus,
    decimal TotalAmount,
    decimal CollectedAmount,
    decimal RemainingAmount,
    string PaymentMethodCode,
    string CollectionDay,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? Notes,
    IReadOnlyList<SaleItem> Items,
    SaleEvidence Evidence,
    IReadOnlyList<SaleHistoryEntry> History,
    IReadOnlyList<CollectionEntry> Collections);
