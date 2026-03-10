namespace SalesCobrosGeo.Api.Business;

public enum SaleWorkflowStatus
{
    Draft = 1,
    Registered = 2,
    Observed = 3,
    Corrected = 4,
    Approved = 5,
    Rejected = 6,
    Closed = 7
}

public enum CollectionWorkflowStatus
{
    Pending = 1,
    Partial = 2,
    Paid = 3,
    Overdue = 4,
    Uncollectible = 5
}

public sealed record Zone(int Id, string Code, string Name, bool IsActive);
public sealed record Product(int Id, string Code, string Name, decimal Price, bool IsActive);
public sealed record PaymentMethod(int Id, string Code, string Name, bool IsActive);

public sealed record Client(
    int Id,
    string FullName,
    string Mobile,
    string? Phone,
    string ZoneCode,
    string CollectionDay,
    string Address,
    string CreatedBy,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    bool IsActive);

public sealed record SaleItem(int ProductId, string ProductCode, string ProductName, int Quantity, decimal UnitPrice, decimal Subtotal);

public sealed record SaleEvidence(
    string PrimaryCoordinates,
    string? SecondaryCoordinates,
    string? LocationUrl,
    IReadOnlyList<string> PhotoUrls);

public sealed record SaleHistoryEntry(
    DateTime TimestampUtc,
    string UserName,
    SaleWorkflowStatus From,
    SaleWorkflowStatus To,
    string? Reason,
    string Action);

public sealed record CollectionEntry(
    int Id,
    int SaleId,
    decimal Amount,
    string Coordinates,
    string? Notes,
    string CollectedBy,
    DateTime CollectedAtUtc,
    DateTime CapturedAtUtc);

public sealed class SaleRecord
{
    public int Id { get; init; }
    public string SaleNumber { get; init; } = string.Empty;
    public int ClientId { get; set; }
    public string SellerUserName { get; set; } = string.Empty;
    public string? CollectorUserName { get; set; }
    public string PaymentMethodCode { get; set; } = string.Empty;
    public string CollectionDay { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public SaleWorkflowStatus Status { get; set; }
    public CollectionWorkflowStatus CollectionStatus { get; set; } = CollectionWorkflowStatus.Pending;
    public bool Collectable { get; set; }
    public decimal SellerCommissionPercent { get; set; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? FirstCollectionAtUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public SaleEvidence Evidence { get; set; } = new(string.Empty, null, null, Array.Empty<string>());
    public List<SaleItem> Items { get; } = [];
    public List<SaleHistoryEntry> History { get; } = [];
    public List<CollectionEntry> Collections { get; } = [];

    public decimal RemainingAmount => Math.Max(0m, TotalAmount - CollectedAmount);
}

public sealed record CollectionSummary(
    int SaleId,
    string SaleNumber,
    int ClientId,
    string SellerUserName,
    string? CollectorUserName,
    CollectionWorkflowStatus CollectionStatus,
    decimal TotalAmount,
    decimal CollectedAmount,
    decimal RemainingAmount,
    string CollectionDay,
    DateTime UpdatedAtUtc);

public sealed record DashboardSummary(
    int TotalSales,
    decimal TotalSalesAmount,
    decimal TotalCollectedAmount,
    decimal TotalPendingAmount,
    int PendingCollections,
    int PaidCollections,
    int OverdueCollections,
    int ActiveSellers,
    int ActiveCollectors);

public sealed record SyncPayload(
    int CatalogVersion,
    DateTime GeneratedAtUtc,
    IReadOnlyList<Zone> Zones,
    IReadOnlyList<Product> Products,
    IReadOnlyList<PaymentMethod> PaymentMethods,
    IReadOnlyList<Client> Clients,
    IReadOnlyList<SaleRecord> Sales);
