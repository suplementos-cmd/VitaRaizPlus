using SalesCobrosGeo.Api.Business;

namespace SalesCobrosGeo.Api.Contracts.Dashboard;

public sealed record DashboardResponse(DashboardSummary Summary, DateTime GeneratedAtUtc);
