using SalesCobrosGeo.Shared;

namespace SalesCobrosGeo.Web.Models.Sales;

// ---------------------------------------------------------------------------
// Query / paging types
// ---------------------------------------------------------------------------

/// <summary>
/// Immutable filter bag for server-side sale queries.
/// All fields are optional; null means "no filter on this dimension".
/// </summary>
public sealed record SalesQuery(
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Zone = null,
    string? Seller = null,
    string? SearchText = null,
    string? Estado = null)
{
    /// <summary>True when at least one filter dimension is non-null / non-empty.</summary>
    public bool HasFilters =>
        DateFrom.HasValue || DateTo.HasValue ||
        !string.IsNullOrWhiteSpace(Zone) ||
        !string.IsNullOrWhiteSpace(Seller) ||
        !string.IsNullOrWhiteSpace(SearchText) ||
        !string.IsNullOrWhiteSpace(Estado);

    /// <summary>Convenience: a query that matches everything.</summary>
    public static SalesQuery Empty => new();
}

/// <summary>
/// Generic paged result envelope returned by server-side paginated queries.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

// ---------------------------------------------------------------------------
// Catalog / option types
// ---------------------------------------------------------------------------

public sealed record CatalogOption(string Code, string Name);
public sealed record ProductOption(string Code, string Name, decimal Price);

public sealed record SalesCatalogs(
    IReadOnlyList<CatalogOption> Zones,
    IReadOnlyList<CatalogOption> PaymentMethods,
    IReadOnlyList<CatalogOption> CollectionDays,
    IReadOnlyList<ProductOption> Products,
    IReadOnlyList<CatalogOption> Sellers,
    IReadOnlyList<CatalogOption> Collectors,
    IReadOnlyList<CatalogOption> SaleStatuses);

public sealed class SaleProductLineInput
{
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal? UnitPrice { get; set; }
}

public sealed class SaleFormInput
{
    public string? IdV { get; set; }
    public int? NumVenta { get; set; }
    public DateTime FechaVenta { get; set; } = DateTime.Today;
    public string NombreCliente { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Zona { get; set; } = string.Empty;
    public string FormaPago { get; set; } = string.Empty;
    public string DiaCobro { get; set; } = string.Empty;
    public string? FotoCliente { get; set; }
    public string? FotoFachada { get; set; }
    public string? FotoContrato { get; set; }
    public string? ObservacionVenta { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Cobrador { get; set; } = string.Empty;
    public string Coordenadas { get; set; } = string.Empty;
    public string? UrlUbicacion { get; set; }
    public DateTime? FechaPrimerCobro { get; set; }
    public string Estado { get; set; } = "PENDIENTE";
    public DateTime FechaActu { get; set; } = DateTime.Now;
    public string Estado2 { get; set; } = "OPEN";
    public decimal ComisionVendedorPct { get; set; }
    public string Cobrar { get; set; } = "OK";
    public string? FotoAdd1 { get; set; }
    public string? FotoAdd2 { get; set; }
    public string? Coordenadas2 { get; set; }
    public List<SaleProductLineInput> Productos { get; set; } = [new SaleProductLineInput()];
}

public sealed class SaleRecord
{
    public string IdV { get; set; } = string.Empty;
    public int NumVenta { get; set; }
    public DateTime FechaVenta { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Zona { get; set; } = string.Empty;
    public string FormaPago { get; set; } = string.Empty;
    public string DiaCobro { get; set; } = string.Empty;
    public string? FotoCliente { get; set; }
    public string? FotoFachada { get; set; }
    public string? FotoContrato { get; set; }
    public string? ObservacionVenta { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Cobrador { get; set; } = string.Empty;
    public string Coordenadas { get; set; } = string.Empty;
    public string? UrlUbicacion { get; set; }
    public DateTime? FechaPrimerCobro { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaActu { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public string Estado2 { get; set; } = string.Empty;
    public decimal ComisionVendedorPct { get; set; }
    public string Cobrar { get; set; } = string.Empty;
    public string? FotoAdd1 { get; set; }
    public string? FotoAdd2 { get; set; }
    public string? Coordenadas2 { get; set; }
    public int ProductosCantidad { get; set; }
    public decimal ImporteTotal { get; set; }
    public List<SaleProductLineInput> Productos { get; set; } = [];
}

public sealed class SalesDayGroup
{
    public DateTime Day { get; set; }
    public List<SaleRecord> Sales { get; set; } = [];
}

public sealed class SalesWeekGroup
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public bool IsCurrentWeek { get; set; }
    public List<SalesDayGroup> Days { get; set; } = [];
}

public sealed class SalesListViewModel
{
    public IReadOnlyList<SalesWeekGroup> Weeks { get; set; } = [];
    public int WeeklyCount { get; set; }

    // Paging & filter context (populated by SalesQueryService)
    public SalesQuery Query { get; set; } = SalesQuery.Empty;
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

public sealed class SaleDetailViewModel
{
    public SaleRecord Sale { get; set; } = new();
}

public sealed class SaleFormViewModel
{
    public bool IsEdit { get; set; }
    public SaleFormInput Input { get; set; } = new();
    public SalesCatalogs Catalogs { get; set; } = new([], [], [], [], [], [], []);
    public string PageTitle { get; set; } = string.Empty;
}

public sealed class CollectionRecord
{
    public string IdCc { get; set; } = string.Empty;
    public string IdV { get; set; } = string.Empty;
    public int NumVenta { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public decimal ImporteCobro { get; set; }
    public DateTime FechaCobro { get; set; }
    public string? ObservacionCobro { get; set; }
    public DateTime FechaCaptura { get; set; }
    public decimal ImporteTotal { get; set; }
    public decimal ImporteRestante { get; set; }
    public string EstadoCc { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public decimal ImporteAbonado { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string Zona { get; set; } = string.Empty;
    public string DiaCobroPrevisto { get; set; } = string.Empty;
    public string DiaCobrado { get; set; } = string.Empty;
    public string? CoordenadasCobro { get; set; }
}

public sealed class CollectorPortfolioItem
{
    public string IdV { get; set; } = string.Empty;
    public int NumVenta { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;
    public string Zona { get; set; } = string.Empty;
    public string DiaCobroPrevisto { get; set; } = string.Empty;
    public string Cobrador { get; set; } = string.Empty;
    public decimal ImporteTotal { get; set; }
    public decimal ImporteAbonado { get; set; }
    public decimal ImporteRestante { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string EstadoVenta { get; set; } = string.Empty;
    public string? FotoCliente { get; set; }
    public string? FotoFachada { get; set; }
    public string? Coordenadas { get; set; }
}

public sealed class CollectorDaySummary
{
    public string Day { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class CollectorDayTreeSummary
{
    public string Day { get; set; } = string.Empty;
    public int Count { get; set; }
    public IReadOnlyList<CollectorStatusSummary> Statuses { get; set; } = [];
}

public sealed class CollectorStatusSummary
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class CollectorZoneSummary
{
    public string Zone { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class CollectorPortfolioViewModel
{
    public string? Profile { get; set; }
    public string? SelectedDay { get; set; }
    public string? SelectedStatus { get; set; }
    public string? SelectedZone { get; set; }
    public bool ShowAll { get; set; }
    public IReadOnlyList<CatalogOption> Profiles { get; set; } = [];
    public IReadOnlyList<CollectorDaySummary> Days { get; set; } = [];
    public IReadOnlyList<CollectorDayTreeSummary> DayTree { get; set; } = [];
    public IReadOnlyList<CollectorStatusSummary> Statuses { get; set; } = [];
    public IReadOnlyList<CollectorZoneSummary> Zones { get; set; } = [];
    public IReadOnlyList<CollectorPortfolioItem> Sales { get; set; } = [];
}

public sealed class CollectionFormInput
{
    public string? IdCc { get; set; }
    public string IdV { get; set; } = string.Empty;
    public decimal ImporteCobro { get; set; }
    public DateTime FechaCobro { get; set; } = DateTime.Today;
    public string? ObservacionCobro { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string? CoordenadasCobro { get; set; }
    public string ActionStatus { get; set; } = "AL CORRIENTE";
}

public sealed class CollectionRegisterViewModel
{
    public bool IsEdit { get; set; }
    public CollectorPortfolioItem? PortfolioItem { get; set; }
    public SaleRecord? Sale { get; set; }
    public CollectionFormInput Input { get; set; } = new();
    public IReadOnlyList<CollectionRecord> Historial { get; set; } = [];
    public IReadOnlyList<CatalogOption> CollectorProfiles { get; set; } = [];
    public string? ReturnProfile { get; set; }
    public string? ReturnDay { get; set; }
    public string? ReturnStatus { get; set; }
    public string? ReturnZone { get; set; }
}

public sealed class CollectionHistorySummaryCard
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CollectionHistoryViewModel
{
    public string Profile { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public IReadOnlyList<CollectionHistorySummaryCard> SummaryCards { get; set; } = [];
    public IReadOnlyList<CollectionRecord> Records { get; set; } = [];
}

// ---------------------------------------------------------------------------
// Server-side search DTO  (returned by GET /Sales/Search?q=...)
// ---------------------------------------------------------------------------

/// <summary>
/// Lightweight DTO returned by the JSON search endpoint.
/// Designed to be rendered client-side or replaced via HTMX partial.
/// </summary>
public sealed record SalesSearchItem(
    string IdV,
    string NombreCliente,
    string Zona,
    string DiaCobro,
    string FormaPago,
    string Vendedor,
    string FirstProduct,
    decimal ImporteTotal,
    string? Coordenadas,
    string? Celular,
    string? FotoFachada,
    string Estado);

public sealed class SalesSearchResult
{
    public IReadOnlyList<SalesSearchItem> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public bool IsTruncated { get; init; }
}
