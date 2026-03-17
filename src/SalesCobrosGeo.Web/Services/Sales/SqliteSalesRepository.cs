using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SalesCobrosGeo.Web.Data;
using SalesCobrosGeo.Web.Models.Maintenance;
using SalesCobrosGeo.Web.Models.Sales;

namespace SalesCobrosGeo.Web.Services.Sales;

public sealed class SqliteSalesRepository : ISalesRepository
{
    private readonly AppSecurityDbContext _dbContext;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public SqliteSalesRepository(AppSecurityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public SalesCatalogs GetCatalogs()
    {
        var items = _dbContext.CatalogItems
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        return new SalesCatalogs(
            items.Where(x => x.Category == "zone").Select(x => new CatalogOption(x.Code, x.Name)).ToArray(),
            items.Where(x => x.Category == "payment").Select(x => new CatalogOption(x.Code, x.Name)).ToArray(),
            items.Where(x => x.Category == "collection_day").Select(x => new CatalogOption(x.Code, x.Name)).ToArray(),
            items.Where(x => x.Category == "product").Select(x => new ProductOption(x.Code, x.Name, x.Price ?? 0m)).ToArray(),
            items.Where(x => x.Category == "seller").Select(x => new CatalogOption(x.Code, x.Name)).ToArray(),
            items.Where(x => x.Category == "collector").Select(x => new CatalogOption(x.Code, x.Name)).ToArray(),
            items.Where(x => x.Category == "sale_status").Select(x => new CatalogOption(x.Code, x.Name)).ToArray());
    }

    public IReadOnlyList<MaintenanceCatalogRecord> GetMaintenanceCatalog(string section)
    {
        var category = ResolveCategory(section);
        if (string.IsNullOrWhiteSpace(category))
        {
            return [];
        }

        return _dbContext.CatalogItems
            .AsNoTracking()
            .Where(x => x.Category == category)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new MaintenanceCatalogRecord(x.Id, section, x.Code, x.Name, x.Price, x.IsActive))
            .ToArray();
    }

    public MaintenanceCatalogRecord SaveMaintenanceCatalogItem(MaintenanceCatalogSaveInput input)
    {
        var category = ResolveCategory(input.Section);
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException("Seccion de catalogo invalida.");
        }

        CatalogItemEntity entity;
        if (input.Id is > 0)
        {
            entity = _dbContext.CatalogItems.FirstOrDefault(x => x.Id == input.Id.Value)
                ?? throw new InvalidOperationException("Catalogo no encontrado.");
        }
        else
        {
            entity = new CatalogItemEntity
            {
                Category = category,
                SortOrder = _dbContext.CatalogItems.Where(x => x.Category == category).Select(x => (int?)x.SortOrder).Max() ?? 0
            };
            _dbContext.CatalogItems.Add(entity);
        }

        entity.Category = category;
        entity.Code = input.Code.Trim();
        entity.Name = input.Name.Trim();
        entity.Price = category == "product" ? input.Price : null;
        entity.IsActive = input.IsActive;
        entity.SortOrder = entity.SortOrder == 0 ? 1 : entity.SortOrder;
        _dbContext.SaveChanges();

        return new MaintenanceCatalogRecord(entity.Id, input.Section, entity.Code, entity.Name, entity.Price, entity.IsActive);
    }

    public bool DeleteMaintenanceCatalogItem(string section, long id)
    {
        var category = ResolveCategory(section);
        var entity = _dbContext.CatalogItems.FirstOrDefault(x => x.Id == id && x.Category == category);
        if (entity is null)
        {
            return false;
        }

        _dbContext.CatalogItems.Remove(entity);
        _dbContext.SaveChanges();
        return true;
    }

    public IReadOnlyList<SaleRecord> GetAll()
    {
        return _dbContext.Sales
            .AsNoTracking()
            .OrderByDescending(x => x.FechaActu)
            .Select(MapSale)
            .ToArray();
    }

    public SaleRecord? GetById(string idV)
    {
        var entity = _dbContext.Sales.AsNoTracking().FirstOrDefault(x => x.IdV == idV);
        return entity is null ? null : MapSale(entity);
    }

    public SaleRecord Create(SaleFormInput input)
    {
        var nextNum = (_dbContext.Sales.Select(x => (int?)x.NumVenta).Max() ?? 0) + 1;
        var entity = MapSaleEntity(input, Guid.NewGuid().ToString("N")[..8], nextNum);
        _dbContext.Sales.Add(entity);
        _dbContext.SaveChanges();
        return MapSale(entity);
    }

    public SaleRecord Update(string idV, SaleFormInput input)
    {
        var entity = _dbContext.Sales.FirstOrDefault(x => x.IdV == idV)
            ?? throw new InvalidOperationException("No se encontro la venta.");

        ApplySale(entity, input, entity.IdV, entity.NumVenta);
        _dbContext.SaveChanges();
        return MapSale(entity);
    }

    public IReadOnlyList<CollectorPortfolioItem> GetCollectorPortfolio(string? profile)
    {
        var normalized = NormalizeProfile(profile);
        var query = _dbContext.Sales
            .AsNoTracking()
            .Include(x => x.Collections)
            .AsEnumerable()
            .Select(sale =>
            {
                var orderedCollections = sale.Collections.OrderBy(c => c.FechaCobro).ToList();
                var abonado = orderedCollections.LastOrDefault()?.ImporteAbonado ?? 0m;
                var restante = Math.Max(0m, sale.ImporteTotal - abonado);
                var estatus = restante <= 0 ? "LIQUIDADO" : ResolvePortfolioStatus(sale, orderedCollections, restante);

                return new CollectorPortfolioItem
                {
                    IdV = sale.IdV,
                    NumVenta = sale.NumVenta,
                    NombreCliente = sale.NombreCliente,
                    Zona = sale.Zona,
                    DiaCobroPrevisto = sale.DiaCobro,
                    Cobrador = string.IsNullOrWhiteSpace(sale.Cobrador) ? sale.Usuario : sale.Cobrador,
                    ImporteTotal = sale.ImporteTotal,
                    ImporteAbonado = abonado,
                    ImporteRestante = restante,
                    Estatus = estatus,
                    EstadoVenta = sale.Estado,
                    FotoCliente = sale.FotoCliente,
                    FotoFachada = sale.FotoFachada,
                    Coordenadas = sale.Coordenadas,
                    Celular = sale.Celular
                };
            });

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            query = query.Where(x => x.Cobrador.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderBy(x => x.Estatus).ThenBy(x => x.NumVenta).ToArray();
    }

    public CollectorPortfolioItem? GetPortfolioItem(string idV, string? profile)
    {
        return GetCollectorPortfolio(profile).FirstOrDefault(x => x.IdV.Equals(idV, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CollectionRecord> GetCollections(string? profile = null, string? idV = null)
    {
        var normalized = NormalizeProfile(profile);
        var query = _dbContext.Collections.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            query = query.Where(x => x.Usuario == normalized);
        }

        if (!string.IsNullOrWhiteSpace(idV))
        {
            query = query.Where(x => x.IdV == idV);
        }

        return query
            .OrderByDescending(x => x.FechaCaptura)
            .Select(MapCollection)
            .ToArray();
    }

    public CollectionRecord RegisterCollection(CollectionFormInput input)
    {
        if (string.IsNullOrWhiteSpace(input.IdV))
        {
            throw new InvalidOperationException("Venta invalida.");
        }

        if (input.ImporteCobro <= 0)
        {
            throw new InvalidOperationException("El importe de cobro debe ser mayor a 0.");
        }

        if (string.IsNullOrWhiteSpace(input.Usuario))
        {
            throw new InvalidOperationException("Usuario cobrador es obligatorio.");
        }

        var sale = _dbContext.Sales.Include(x => x.Collections).FirstOrDefault(x => x.IdV == input.IdV)
            ?? throw new InvalidOperationException("No se encontro la venta.");

        var totalAbonado = sale.Collections.Select(x => x.ImporteCobro).DefaultIfEmpty(0m).Sum();
        var restanteActual = Math.Max(0m, sale.ImporteTotal - totalAbonado);

        if (input.ImporteCobro > restanteActual)
        {
            throw new InvalidOperationException($"El cobro no puede superar el restante ({restanteActual:0.00}).");
        }

        var nuevoAbonado = totalAbonado + input.ImporteCobro;
        var nuevoRestante = Math.Max(0m, sale.ImporteTotal - nuevoAbonado);
        var estatus = nuevoRestante <= 0 ? "LIQUIDADO" : "AL CORRIENTE";

        var entity = new CollectionEntity
        {
            IdCc = Guid.NewGuid().ToString("N")[..8],
            IdV = sale.IdV,
            NumVenta = sale.NumVenta,
            NombreCliente = sale.NombreCliente,
            ImporteCobro = input.ImporteCobro,
            FechaCobro = input.FechaCobro,
            ObservacionCobro = input.ObservacionCobro?.Trim(),
            FechaCaptura = DateTime.Now,
            ImporteTotal = sale.ImporteTotal,
            ImporteRestante = nuevoRestante,
            EstadoCc = "SI PAGO",
            Usuario = input.Usuario.Trim(),
            ImporteAbonado = nuevoAbonado,
            Estatus = estatus,
            Zona = sale.Zona,
            DiaCobroPrevisto = sale.DiaCobro,
            DiaCobrado = GetDayName(input.FechaCobro),
            CoordenadasCobro = input.CoordenadasCobro?.Trim()
        };

        _dbContext.Collections.Add(entity);

        sale.Estado = estatus == "LIQUIDADO" ? "LIQUIDADO" : "EN COBRO";
        sale.Estado2 = estatus == "LIQUIDADO" ? "CLOSED" : "OPEN";
        sale.FechaActu = DateTime.Now;
        sale.FechaPrimerCobro ??= input.FechaCobro;

        _dbContext.SaveChanges();
        return MapCollection(entity);
    }

    public IReadOnlyList<CatalogOption> GetCollectorProfiles()
    {
        return _dbContext.CatalogItems
            .AsNoTracking()
            .Where(x => x.Category == "collector" && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new CatalogOption(x.Code, x.Name))
            .ToArray();
    }

    private static string ResolveCategory(string section)
    {
        return section switch
        {
            "zonas" => "zone",
            "dias-cobro" => "collection_day",
            "formas-pago" => "payment",
            "productos" => "product",
            "vendedores" => "seller",
            "cobradores" => "collector",
            "estatus-venta" => "sale_status",
            "estatus-cobro-grupos" => "collection_status_group",
            _ => string.Empty
        };
    }

    private SaleEntity MapSaleEntity(SaleFormInput input, string idV, int numVenta)
    {
        var entity = new SaleEntity();
        ApplySale(entity, input, idV, numVenta);
        return entity;
    }

    private void ApplySale(SaleEntity entity, SaleFormInput input, string idV, int numVenta)
    {
        var validLines = input.Productos
            .Where(p => !string.IsNullOrWhiteSpace(p.ProductCode) && p.Quantity > 0)
            .ToList();

        if (validLines.Count == 0)
        {
            throw new InvalidOperationException("Debe agregar al menos un producto.");
        }

        var catalogs = GetCatalogs();
        foreach (var line in validLines)
        {
            var match = catalogs.Products.FirstOrDefault(p => p.Code == line.ProductCode)
                ?? throw new InvalidOperationException($"Producto invalido: {line.ProductCode}");
            line.UnitPrice ??= match.Price;
        }

        var total = validLines.Sum(x => x.Quantity * (x.UnitPrice ?? 0m));

        entity.IdV = idV;
        entity.NumVenta = numVenta;
        entity.FechaVenta = input.FechaVenta;
        entity.NombreCliente = input.NombreCliente.Trim();
        entity.Celular = input.Celular.Trim();
        entity.Telefono = input.Telefono?.Trim();
        entity.Zona = input.Zona;
        entity.FormaPago = input.FormaPago;
        entity.DiaCobro = input.DiaCobro;
        entity.FotoCliente = input.FotoCliente?.Trim();
        entity.FotoFachada = input.FotoFachada?.Trim();
        entity.FotoContrato = input.FotoContrato?.Trim();
        entity.ObservacionVenta = input.ObservacionVenta?.Trim();
        entity.Vendedor = input.Vendedor;
        entity.Usuario = input.Usuario.Trim();
        entity.Cobrador = input.Cobrador;
        entity.Coordenadas = input.Coordenadas.Trim();
        entity.UrlUbicacion = input.UrlUbicacion?.Trim();
        entity.FechaPrimerCobro = input.FechaPrimerCobro;
        entity.Estado = input.Estado;
        entity.FechaActu = DateTime.Now;
        entity.Cliente = input.NombreCliente.Trim();
        entity.Producto = string.Join(", ", validLines.Select(x => x.ProductCode));
        entity.Estado2 = input.Estado2;
        entity.ComisionVendedorPct = input.ComisionVendedorPct;
        entity.Cobrar = input.Cobrar;
        entity.FotoAdd1 = input.FotoAdd1?.Trim();
        entity.FotoAdd2 = input.FotoAdd2?.Trim();
        entity.Coordenadas2 = input.Coordenadas2?.Trim();
        entity.ProductosCantidad = validLines.Sum(x => x.Quantity);
        entity.ImporteTotal = total;
        entity.ProductLinesJson = JsonSerializer.Serialize(validLines, _jsonOptions);
    }

    private SaleRecord MapSale(SaleEntity entity)
    {
        return new SaleRecord
        {
            IdV = entity.IdV,
            NumVenta = entity.NumVenta,
            FechaVenta = entity.FechaVenta,
            NombreCliente = entity.NombreCliente,
            Celular = entity.Celular,
            Telefono = entity.Telefono,
            Zona = entity.Zona,
            FormaPago = entity.FormaPago,
            DiaCobro = entity.DiaCobro,
            FotoCliente = entity.FotoCliente,
            FotoFachada = entity.FotoFachada,
            FotoContrato = entity.FotoContrato,
            ObservacionVenta = entity.ObservacionVenta,
            Vendedor = entity.Vendedor,
            Usuario = entity.Usuario,
            Cobrador = entity.Cobrador,
            Coordenadas = entity.Coordenadas,
            UrlUbicacion = entity.UrlUbicacion,
            FechaPrimerCobro = entity.FechaPrimerCobro,
            Estado = entity.Estado,
            FechaActu = entity.FechaActu,
            Cliente = entity.Cliente,
            Producto = entity.Producto,
            Estado2 = entity.Estado2,
            ComisionVendedorPct = entity.ComisionVendedorPct,
            Cobrar = entity.Cobrar,
            FotoAdd1 = entity.FotoAdd1,
            FotoAdd2 = entity.FotoAdd2,
            Coordenadas2 = entity.Coordenadas2,
            ProductosCantidad = entity.ProductosCantidad,
            ImporteTotal = entity.ImporteTotal,
            Productos = JsonSerializer.Deserialize<List<SaleProductLineInput>>(entity.ProductLinesJson, _jsonOptions) ?? []
        };
    }

    private static CollectionRecord MapCollection(CollectionEntity entity)
    {
        return new CollectionRecord
        {
            IdCc = entity.IdCc,
            IdV = entity.IdV,
            NumVenta = entity.NumVenta,
            NombreCliente = entity.NombreCliente,
            ImporteCobro = entity.ImporteCobro,
            FechaCobro = entity.FechaCobro,
            ObservacionCobro = entity.ObservacionCobro,
            FechaCaptura = entity.FechaCaptura,
            ImporteTotal = entity.ImporteTotal,
            ImporteRestante = entity.ImporteRestante,
            EstadoCc = entity.EstadoCc,
            Usuario = entity.Usuario,
            ImporteAbonado = entity.ImporteAbonado,
            Estatus = entity.Estatus,
            Zona = entity.Zona,
            DiaCobroPrevisto = entity.DiaCobroPrevisto,
            DiaCobrado = entity.DiaCobrado,
            CoordenadasCobro = entity.CoordenadasCobro
        };
    }

    private static string NormalizeProfile(string? profile)
    {
        return string.IsNullOrWhiteSpace(profile) ? string.Empty : profile.Trim();
    }

    private static string GetDayName(DateTime date)
    {
        return date.ToString("dddd", new CultureInfo("es-ES")).ToUpperInvariant();
    }

    private static string ResolvePortfolioStatus(SaleEntity sale, IReadOnlyList<CollectionEntity> collections, decimal restante)
    {
        if (restante <= 0)
        {
            return "LIQUIDADO";
        }

        if (collections.Count == 0)
        {
            return "POR INICIAR";
        }

        var lastCollection = collections.OrderByDescending(x => x.FechaCobro).First();
        var currentWeekStart = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
        return lastCollection.FechaCobro.Date >= currentWeekStart ? "AL CORRIENTE" : "ATRASADO";
    }
}
