namespace SalesCobrosGeo.Web.Models.Dashboard;

public sealed record KpiCard(string Title, string Value, string Trend, string Tone);

public sealed record DashboardPeriodInfo(string Scope, int Offset, string Label, string Subtitle);

public sealed record SellerPerformanceSummary(string Seller, int TotalSales, int ClosedSales, decimal TotalAmount);

public sealed record CollectionGroupingSummary(string Key, string Label, int Count, decimal Amount);

public sealed record DailySummary(string Key, string Label, int Count, decimal Amount);

public sealed record SaleRow(
    string SaleNumber,
    string Seller,
    string Zone,
    decimal Total,
    string Status,
    string UpdatedAt,
    string ClientName);

public sealed record CollectionRow(
    string SaleNumber,
    string Collector,
    decimal Collected,
    decimal Pending,
    string Status,
    string LastUpdate,
    string ClientName,
    string Zone,
    string DayLabel);

public sealed record DashboardPageViewModel(
    DashboardPeriodInfo Period,
    string CollectionGrouping,
    IReadOnlyList<KpiCard> Kpis,
    IReadOnlyList<DailySummary> WeeklySales,
    IReadOnlyList<DailySummary> WeeklyCollections,
    IReadOnlyList<CollectionGroupingSummary> SellerSummaries,
    IReadOnlyList<CollectionGroupingSummary> SalesByZone,
    IReadOnlyList<CollectionGroupingSummary> CollectionsByZone,
    IReadOnlyList<CollectionGroupingSummary> CollectionsByCollector,
    IReadOnlyList<SaleRow> Sales,
    IReadOnlyList<CollectionRow> Collections);
