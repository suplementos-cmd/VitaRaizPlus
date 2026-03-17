using SalesCobrosGeo.Web.Models.Sales;

namespace SalesCobrosGeo.Web.Models.Collections;

public sealed class CollectorQuickFilter
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CollectorOperationalCard
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Accent { get; set; } = string.Empty;
    public string StatusKey { get; set; } = string.Empty;
    public string TargetAction { get; set; } = string.Empty;
    public object? RouteValues { get; set; }
}

public sealed class CollectorClientListItem
{
    public int OrderIndex { get; set; }
    public string IdV { get; set; } = string.Empty;
    public int NumVenta { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string NextAction { get; set; } = string.Empty;
    public string ReferenceText { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Coordinates { get; set; }
    public string? Thumbnail { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public string LastNote { get; set; } = string.Empty;
    public string SaleState { get; set; } = string.Empty;
    public bool HasPromise { get; set; }
    public bool HasPromiseOverdue { get; set; }
    public bool IsNotLocated { get; set; }
    public bool IsRefused { get; set; }
    public bool WasVisited { get; set; }
}

public sealed class CollectorDayTab
{
    public string Code { get; set; } = string.Empty;
    public string ShortLabel { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CollectorMobileZoneGroup
{
    public string Zone { get; set; } = string.Empty;
    public int Accounts { get; set; }
    public IReadOnlyList<CollectorClientListItem> Clients { get; set; } = [];
}

public sealed class CollectorMobileStatusGroup
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsOpen { get; set; }
    public IReadOnlyList<CollectorMobileZoneGroup> Zones { get; set; } = [];
}

public sealed class CollectorQueueGroupViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public int Accounts { get; set; }
    public int UrgentCount { get; set; }
    public int PromiseCount { get; set; }
    public IReadOnlyList<CollectorClientListItem> Clients { get; set; } = [];
}

public sealed class CollectorHomeViewModel
{
    public string Profile { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RouteLabel { get; set; } = string.Empty;
    public IReadOnlyList<CollectionHistorySummaryCard> HistorySummaryCards { get; set; } = [];
    public IReadOnlyList<CollectorOperationalCard> Cards { get; set; } = [];
    public IReadOnlyList<CollectorClientListItem> TodayClients { get; set; } = [];
}

public sealed class CollectorQueueViewModel
{
    public string Profile { get; set; } = string.Empty;
    public string GroupBy { get; set; } = string.Empty;
    public string Filter { get; set; } = string.Empty;
    public string SelectedDay { get; set; } = string.Empty;
    public string SelectedStatus { get; set; } = string.Empty;
    public string SelectedZone { get; set; } = string.Empty;
    public string SearchPlaceholder { get; set; } = string.Empty;
    public IReadOnlyList<CollectionHistorySummaryCard> HistorySummaryCards { get; set; } = [];
    public IReadOnlyList<CollectorQuickFilter> QuickFilters { get; set; } = [];
    public IReadOnlyList<CollectorDayTab> DayTabs { get; set; } = [];
    public IReadOnlyList<CollectorMobileStatusGroup> MobileStatusGroups { get; set; } = [];
    public IReadOnlyList<CollectorMobileZoneGroup> MobileZoneGroups { get; set; } = [];
    public IReadOnlyList<CollectorClientListItem> MobileZoneClients { get; set; } = [];
    public IReadOnlyList<CollectorQueueGroupViewModel> Groups { get; set; } = [];
}

public sealed class CollectorRouteViewModel
{
    public string Profile { get; set; } = string.Empty;
    public string ZoneLabel { get; set; } = string.Empty;
    public string RouteUrl { get; set; } = string.Empty;
    public int GeoPoints { get; set; }
    public int TotalStops { get; set; }
    public IReadOnlyList<CollectorClientListItem> Clients { get; set; } = [];
}

public sealed class SupervisorMetricCard
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
}

public sealed class SupervisorCollectorMonitorItem
{
    public string Collector { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Accounts { get; set; }
    public int VisitsDone { get; set; }
    public int PendingVisits { get; set; }
    public int Promises { get; set; }
    public int Overdue { get; set; }
    public decimal RecoveredAmount { get; set; }
    public string LastActivity { get; set; } = string.Empty;
    public string LastCoordinates { get; set; } = string.Empty;
}

public sealed class SupervisorCollectionsViewModel
{
    public string GroupBy { get; set; } = string.Empty;
    public string? SelectedCollector { get; set; }
    public IReadOnlyList<SupervisorMetricCard> Metrics { get; set; } = [];
    public IReadOnlyList<SupervisorCollectorMonitorItem> Collectors { get; set; } = [];
    public IReadOnlyList<CollectorQueueGroupViewModel> Groups { get; set; } = [];
}
