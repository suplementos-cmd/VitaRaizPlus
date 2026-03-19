namespace SalesCobrosGeo.Api.Business;

/// <summary>
/// Modelos de negocio para Ventas - Compatible con estructura existente de Web
/// Fase 2: Migración de Web → API → Excel
/// </summary>

public sealed class SaleProductLineDto
{
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal? UnitPrice { get; set; }
}

public sealed class SaleFormInputDto
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
    public List<SaleProductLineDto> Productos { get; set; } = [new SaleProductLineDto()];
}

public sealed class SaleRecordDto
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
    public string Estado2 { get; set; } = string.Empty;
    public decimal ComisionVendedorPct { get; set; }
    public string Cobrar { get; set; } = string.Empty;
    public string? FotoAdd1 { get; set; }
    public string? FotoAdd2 { get; set; }
    public string? Coordenadas2 { get; set; }
    public int ProductosCantidad { get; set; }
    public decimal ImporteTotal { get; set; }
    public List<SaleProductLineDto> Productos { get; set; } = [];
}

public sealed class CollectionRecordDto
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

public sealed class CollectionFormInputDto
{
    public string? IdCc { get; set; }
    public string IdV { get; set; } = string.Empty;
    public decimal ImporteCobro { get; set; }
    public DateTime FechaCobro { get; set; } = DateTime.Today;
    public string? ObservacionCobro { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string? CoordenadasCobro { get; set; }
    public string? FotoCobro { get; set; }
}

public sealed class CollectorPortfolioItemDto
{
    public string IdV { get; set; } = string.Empty;
    public int NumVenta { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public decimal ImporteTotal { get; set; }
    public decimal ImporteAbonado { get; set; }
    public decimal ImporteRestante { get; set; }
    public string Zona { get; set; } = string.Empty;
    public string DiaCobroPrevisto { get; set; } = string.Empty;
    public DateTime? UltimaFechaCobro { get; set; }
    public string EstadoCobro { get; set; } = string.Empty;
}
