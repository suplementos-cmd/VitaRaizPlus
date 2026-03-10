namespace SalesCobrosGeo.Web.Models.Dashboard;

public sealed record KpiCard(string Title, string Value, string Trend);

public sealed record SaleRow(string SaleNumber, string Seller, string Zone, decimal Total, string Status, string UpdatedAt);
public sealed record CollectionRow(string SaleNumber, string Collector, decimal Collected, decimal Pending, string Status, string LastUpdate);

public sealed record DashboardPageViewModel(
    IReadOnlyList<KpiCard> Kpis,
    IReadOnlyList<SaleRow> Sales,
    IReadOnlyList<CollectionRow> Collections);
