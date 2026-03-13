namespace SalesCobrosGeo.Web.Data;

public sealed class AppUserEntity
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public List<AppUserPermissionEntity> Permissions { get; set; } = [];
    public List<AppSessionEntity> Sessions { get; set; } = [];
}

public sealed class AppUserPermissionEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;

    public AppUserEntity? User { get; set; }
}

public sealed class AppSessionEntity
{
    public string SessionId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsActiveUser { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public DateTime? LastHeartbeatUtc { get; set; }
    public DateTime? ClosedUtc { get; set; }
    public string LastPath { get; set; } = "-";
    public string LastIp { get; set; } = "-";
    public string LastUserAgent { get; set; } = "-";
    public string LastCoordinates { get; set; } = "-";
    public string LastLocationSource { get; set; } = "Sin traza";

    public AppUserEntity? User { get; set; }
}

public sealed class AuditLogEntity
{
    public long Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = "-";
    public string Ip { get; set; } = "-";
    public string Coordinates { get; set; } = "-";
    public string Metadata { get; set; } = string.Empty;
}

public sealed class CatalogItemEntity
{
    public long Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class SaleEntity
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
    public string ProductLinesJson { get; set; } = "[]";

    public List<CollectionEntity> Collections { get; set; } = [];
}

public sealed class CollectionEntity
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

    public SaleEntity? Sale { get; set; }
}
