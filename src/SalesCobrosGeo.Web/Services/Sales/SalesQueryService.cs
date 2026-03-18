using SalesCobrosGeo.Web.Models.Sales;

namespace SalesCobrosGeo.Web.Services.Sales;

/// <summary>
/// Application-layer service that builds view models for Sales views.
/// Keeps the controller thin by encapsulating all query + mapping logic.
/// </summary>
public interface ISalesQueryService
{
    /// <summary>
    /// Builds the Sales list view model with server-side paging and filtering.
    /// </summary>
    SalesListViewModel BuildListView(SalesQuery query, int page, int pageSize = 25);

    /// <summary>
    /// Builds the form view model for Create or Edit.
    /// When <paramref name="saleId"/> is null a fresh default input is returned.
    /// </summary>
    SaleFormViewModel BuildFormView(string? saleId);

    /// <summary>
    /// Executes a server-side search and returns lightweight DTOs suitable for
    /// JSON or partial-HTML rendering (replaces client-side DOM filtering).
    /// </summary>
    SalesSearchResult Search(string? searchText, string? zone, string? seller, int maxResults = 50);
}

// ---------------------------------------------------------------------------
// Implementation
// ---------------------------------------------------------------------------

public sealed class SalesQueryService : ISalesQueryService
{
    private readonly ISalesRepository _repository;

    public SalesQueryService(ISalesRepository repository)
    {
        _repository = repository;
    }

    public SalesListViewModel BuildListView(SalesQuery query, int page, int pageSize = 25)
    {
        var paged = _repository.GetPaged(query, page, pageSize);
        var today = DateTime.Today;
        var currentWeekStart = StartOfWeek(today);
        var currentWeekEnd = currentWeekStart.AddDays(6);

        var weeks = paged.Items
            .GroupBy(x => StartOfWeek(x.FechaVenta.Date))
            .OrderByDescending(g => g.Key)
            .Select(g => new SalesWeekGroup
            {
                WeekStart = g.Key,
                WeekEnd = g.Key.AddDays(6),
                IsCurrentWeek = g.Key == currentWeekStart,
                Days = g.GroupBy(x => x.FechaVenta.Date)
                    .OrderByDescending(x => x.Key)
                    .Select(x => new SalesDayGroup
                    {
                        Day = x.Key,
                        Sales = x.OrderBy(item => item.NombreCliente).ToList()
                    })
                    .ToList()
            })
            .ToList();

        var weeklyCount = paged.Items
            .Count(x => x.FechaVenta.Date >= currentWeekStart && x.FechaVenta.Date <= currentWeekEnd);

        return new SalesListViewModel
        {
            Weeks = weeks,
            WeeklyCount = weeklyCount,
            Query = query,
            CurrentPage = paged.Page,
            TotalPages = paged.TotalPages,
            TotalCount = paged.TotalCount
        };
    }

    public SaleFormViewModel BuildFormView(string? saleId)
    {
        if (string.IsNullOrWhiteSpace(saleId))
        {
            return new SaleFormViewModel
            {
                IsEdit = false,
                PageTitle = "Registrar Venta",
                Catalogs = _repository.GetCatalogs(),
                Input = BuildDefaultInput()
            };
        }

        var sale = _repository.GetById(saleId)
            ?? throw new KeyNotFoundException($"Venta '{saleId}' no encontrada.");

        return new SaleFormViewModel
        {
            IsEdit = true,
            PageTitle = $"Editar Venta {sale.NumVenta}",
            Catalogs = _repository.GetCatalogs(),
            Input = MapToInput(sale)
        };
    }

    public SalesSearchResult Search(string? searchText, string? zone, string? seller, int maxResults = 50)
    {
        var query = new SalesQuery(
            SearchText: searchText,
            Zone: zone,
            Seller: seller);

        var paged = _repository.GetPaged(query, page: 1, pageSize: maxResults + 1);
        var isTruncated = paged.TotalCount > maxResults;

        var items = paged.Items
            .Take(maxResults)
            .Select(s => new SalesSearchItem(
                s.IdV,
                s.NombreCliente,
                s.Zona,
                s.DiaCobro,
                s.FormaPago,
                s.Vendedor,
                s.Productos.FirstOrDefault()?.ProductCode ?? s.Producto,
                s.ImporteTotal,
                s.Coordenadas,
                s.Celular,
                s.FotoFachada,
                s.Estado))
            .ToArray();

        return new SalesSearchResult
        {
            Items = items,
            TotalCount = paged.TotalCount,
            IsTruncated = isTruncated
        };
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private static DateTime StartOfWeek(DateTime date)
    {
        var day = (int)date.DayOfWeek;
        if (day == 0) day = 7;
        return date.Date.AddDays(1 - day);
    }

    private static SaleFormInput BuildDefaultInput() => new()
    {
        FechaVenta = DateTime.Today,
        FechaActu = DateTime.Now,
        Estado = "PENDIENTE",
        Estado2 = "OPEN",
        Cobrar = "OK",
        Productos = [new SaleProductLineInput { Quantity = 1 }]
    };

    private static SaleFormInput MapToInput(SaleRecord sale) => new()
    {
        IdV = sale.IdV,
        NumVenta = sale.NumVenta,
        FechaVenta = sale.FechaVenta,
        NombreCliente = sale.NombreCliente,
        Celular = sale.Celular,
        Telefono = sale.Telefono,
        Zona = sale.Zona,
        FormaPago = sale.FormaPago,
        DiaCobro = sale.DiaCobro,
        FotoCliente = sale.FotoCliente,
        FotoFachada = sale.FotoFachada,
        FotoContrato = sale.FotoContrato,
        ObservacionVenta = sale.ObservacionVenta,
        Vendedor = sale.Vendedor,
        Usuario = sale.Usuario,
        Cobrador = sale.Cobrador,
        Coordenadas = sale.Coordenadas,
        UrlUbicacion = sale.UrlUbicacion,
        FechaPrimerCobro = sale.FechaPrimerCobro,
        Estado = sale.Estado,
        FechaActu = sale.FechaActu,
        Estado2 = sale.Estado2,
        ComisionVendedorPct = sale.ComisionVendedorPct,
        Cobrar = sale.Cobrar,
        FotoAdd1 = sale.FotoAdd1,
        FotoAdd2 = sale.FotoAdd2,
        Coordenadas2 = sale.Coordenadas2,
        Productos = sale.Productos.Count > 0
            ? sale.Productos
            : [new SaleProductLineInput { Quantity = 1 }]
    };
}
