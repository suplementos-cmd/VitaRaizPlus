namespace SalesCobrosGeo.Web.Models.Sales;

public sealed record CatalogOption(string Code, string Name);
public sealed record ProductOption(string Code, string Name, decimal Price);

public sealed record SalesCatalogs(
    IReadOnlyList<CatalogOption> Zones,
    IReadOnlyList<CatalogOption> PaymentMethods,
    IReadOnlyList<CatalogOption> CollectionDays,
    IReadOnlyList<ProductOption> Products,
    IReadOnlyList<CatalogOption> Sellers,
    IReadOnlyList<CatalogOption> Collectors);

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

public sealed class SalesListViewModel
{
    public IReadOnlyList<SalesDayGroup> Groups { get; set; } = [];
    public int WeeklyCount { get; set; }
}

public sealed class SaleDetailViewModel
{
    public SaleRecord Sale { get; set; } = new();
}

public sealed class SaleFormViewModel
{
    public bool IsEdit { get; set; }
    public SaleFormInput Input { get; set; } = new();
    public SalesCatalogs Catalogs { get; set; } = new([], [], [], [], [], []);
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
    public IReadOnlyList<CatalogOption> Profiles { get; set; } = [];
    public IReadOnlyList<CollectorDaySummary> Days { get; set; } = [];
    public IReadOnlyList<CollectorStatusSummary> Statuses { get; set; } = [];
    public IReadOnlyList<CollectorZoneSummary> Zones { get; set; } = [];
    public IReadOnlyList<CollectorPortfolioItem> Sales { get; set; } = [];
}

public sealed class CollectionFormInput
{
    public string IdV { get; set; } = string.Empty;
    public decimal ImporteCobro { get; set; }
    public DateTime FechaCobro { get; set; } = DateTime.Today;
    public string? ObservacionCobro { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string? CoordenadasCobro { get; set; }
}

public sealed class CollectionRegisterViewModel
{
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
