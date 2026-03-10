namespace SalesCobrosGeo.Api.Contracts.Sync;

public sealed record SyncPushRequest(
    IReadOnlyList<SyncCollectionEvent> Collections,
    IReadOnlyList<SyncSaleUpdateEvent> SaleUpdates);

public sealed record SyncCollectionEvent(
    int SaleId,
    decimal Amount,
    string Coordinates,
    string? Notes,
    DateTime? CollectedAtUtc);

public sealed record SyncSaleUpdateEvent(
    int SaleId,
    string? CollectorUserName,
    string? Notes);
