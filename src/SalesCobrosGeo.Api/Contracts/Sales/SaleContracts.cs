using SalesCobrosGeo.Api.Business;

namespace SalesCobrosGeo.Api.Contracts.Sales;

public sealed record CreateSaleItemRequest(int ProductId, int Quantity, decimal? UnitPrice);

public sealed record SaleEvidenceRequest(
    string PrimaryCoordinates,
    string? SecondaryCoordinates,
    string? LocationUrl,
    IReadOnlyList<string> PhotoUrls);

public sealed record CreateSaleRequest(
    int ClientId,
    string PaymentMethodCode,
    string CollectionDay,
    string? CollectorUserName,
    decimal SellerCommissionPercent,
    bool Collectable,
    string? Notes,
    bool IsDraft,
    IReadOnlyList<CreateSaleItemRequest> Items,
    SaleEvidenceRequest Evidence);

public sealed record UpdateSaleDraftRequest(
    int ClientId,
    string PaymentMethodCode,
    string CollectionDay,
    string? CollectorUserName,
    decimal SellerCommissionPercent,
    bool Collectable,
    string? Notes,
    bool SubmitForReview,
    string? ChangeReason,
    IReadOnlyList<CreateSaleItemRequest> Items,
    SaleEvidenceRequest Evidence);

public sealed record ReviewSaleRequest(string Action, string? Reason);
public sealed record AssignCollectorRequest(string CollectorUserName, string? Reason);

public sealed record SaleSummaryResponse(
    int Id,
    string SaleNumber,
    int ClientId,
    string SellerUserName,
    string? CollectorUserName,
    SaleWorkflowStatus Status,
    decimal TotalAmount,
    string PaymentMethodCode,
    string CollectionDay,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? Notes,
    IReadOnlyList<SaleItem> Items,
    SaleEvidence Evidence,
    IReadOnlyList<SaleHistoryEntry> History);
