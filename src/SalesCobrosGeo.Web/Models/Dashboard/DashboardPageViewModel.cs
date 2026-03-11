namespace SalesCobrosGeo.Web.Models.Dashboard;

public sealed record KpiCard(string Title, string Value, string Trend, string Tone);

public sealed record DashboardMiniStat(string Label, string Value);

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
    string ClientName);

public sealed record ZoneSummary(string Zone, int SalesCount, decimal PendingTotal);

public sealed record DailySummary(string Label, int SalesCount, decimal Amount);

public sealed record DashboardPageViewModel(
    IReadOnlyList<KpiCard> Kpis,
    IReadOnlyList<SaleRow> Sales,
    IReadOnlyList<CollectionRow> Collections,
    IReadOnlyList<ZoneSummary> Zones,
    IReadOnlyList<DailySummary> WeeklySales,
    IReadOnlyList<DailySummary> WeeklyCollections,
    DashboardMiniStat Portfolio,
    DashboardMiniStat Recovery);
