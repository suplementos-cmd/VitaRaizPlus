using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Sales;
using SalesCobrosGeo.Web.Security;
using SalesCobrosGeo.Web.Services.Sales;

namespace SalesCobrosGeo.Web.Controllers;

[Authorize(Policy = AppPolicies.SalesAccess)]
public sealed class SalesController : Controller
{
    private readonly ISalesRepository _repository;
    private readonly IUserSessionTracker _sessionTracker;

    public SalesController(ISalesRepository repository, IUserSessionTracker sessionTracker)
    {
        _repository = repository;
        _sessionTracker = sessionTracker;
    }

    public IActionResult Index(DateTime? from = null, DateTime? to = null, string? day = null, string? zone = null, string? seller = null)
    {
        // Parse and validate filters from Dashboard
        var filterContext = BuildSalesFilterContext(from, to, day, zone, seller);
        
        var today = DateTime.Today;
        var currentWeekStart = StartOfWeek(today);
        var currentWeekEnd = currentWeekStart.AddDays(6);
        
        // Apply filters to sales data
        var allSales = _repository.GetAll().ToList();
        var filteredSales = ApplySalesFilters(allSales, filterContext);

        var weeks = filteredSales
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

        var model = new SalesListViewModel
        {
            Weeks = weeks,
            WeeklyCount = filteredSales.Count(x => x.FechaVenta.Date >= currentWeekStart && x.FechaVenta.Date <= currentWeekEnd),
            FilterContext = filterContext
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Details(string id)
    {
        var sale = _repository.GetById(id);
        if (sale is null)
        {
            return NotFound();
        }

        return View(new SaleDetailViewModel { Sale = sale });
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new SaleFormViewModel
        {
            IsEdit = false,
            PageTitle = "Registrar Venta",
            Catalogs = _repository.GetCatalogs(),
            Input = BuildDefaultInput()
        };

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(SaleFormInput input)
    {
        if (!TryValidateInput(input))
        {
            return View("Form", new SaleFormViewModel
            {
                IsEdit = false,
                PageTitle = "Registrar Venta",
                Catalogs = _repository.GetCatalogs(),
                Input = NormalizeInput(input)
            });
        }

        try
        {
            var saved = _repository.Create(NormalizeInput(input));
            _sessionTracker.UpdateCoordinates(User.Identity?.Name ?? string.Empty, saved.Coordenadas, "Venta registrada");
            TempData["SalesMessage"] = "Venta registrada correctamente.";
            return RedirectToAction(nameof(Details), new { id = saved.IdV });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", new SaleFormViewModel
            {
                IsEdit = false,
                PageTitle = "Registrar Venta",
                Catalogs = _repository.GetCatalogs(),
                Input = NormalizeInput(input)
            });
        }
    }

    [HttpGet]
    public IActionResult Edit(string id)
    {
        var sale = _repository.GetById(id);
        if (sale is null)
        {
            return NotFound();
        }

        var model = new SaleFormViewModel
        {
            IsEdit = true,
            PageTitle = $"Editar Venta {sale.NumVenta}",
            Catalogs = _repository.GetCatalogs(),
            Input = MapToInput(sale)
        };

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(string id, SaleFormInput input)
    {
        if (!TryValidateInput(input))
        {
            return View("Form", new SaleFormViewModel
            {
                IsEdit = true,
                PageTitle = "Editar Venta",
                Catalogs = _repository.GetCatalogs(),
                Input = NormalizeInput(input)
            });
        }

        try
        {
            var saved = _repository.Update(id, NormalizeInput(input));
            _sessionTracker.UpdateCoordinates(User.Identity?.Name ?? string.Empty, saved.Coordenadas, "Venta actualizada");
            TempData["SalesMessage"] = "Venta actualizada correctamente.";
            return RedirectToAction(nameof(Details), new { id = saved.IdV });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", new SaleFormViewModel
            {
                IsEdit = true,
                PageTitle = "Editar Venta",
                Catalogs = _repository.GetCatalogs(),
                Input = NormalizeInput(input)
            });
        }
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var day = (int)date.DayOfWeek;
        if (day == 0)
        {
            day = 7;
        }

        return date.Date.AddDays(1 - day);
    }

    private static SaleFormInput BuildDefaultInput()
    {
        return new SaleFormInput
        {
            FechaVenta = DateTime.Today,
            FechaActu = DateTime.Now,
            Estado = "PENDIENTE",
            Estado2 = "OPEN",
            Cobrar = "OK",
            Productos = [new SaleProductLineInput { Quantity = 1 }]
        };
    }

    private static SaleFormInput MapToInput(SaleRecord sale)
    {
        return new SaleFormInput
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
            Productos = sale.Productos.Count > 0 ? sale.Productos : [new SaleProductLineInput { Quantity = 1 }]
        };
    }

    private static SaleFormInput NormalizeInput(SaleFormInput input)
    {
        input.Productos = input.Productos
            .Where(p => !string.IsNullOrWhiteSpace(p.ProductCode) && p.Quantity > 0)
            .ToList();

        if (input.Productos.Count == 0)
        {
            input.Productos = [new SaleProductLineInput { Quantity = 1 }];
        }

        input.FechaActu = DateTime.Now;
        return input;
    }

    private bool TryValidateInput(SaleFormInput input)
    {
        if (string.IsNullOrWhiteSpace(input.NombreCliente))
        {
            ModelState.AddModelError(nameof(input.NombreCliente), "Nombre cliente es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(input.Celular))
        {
            ModelState.AddModelError(nameof(input.Celular), "Celular es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(input.Zona))
        {
            ModelState.AddModelError(nameof(input.Zona), "Zona es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(input.FormaPago))
        {
            ModelState.AddModelError(nameof(input.FormaPago), "Forma de pago es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(input.Coordenadas))
        {
            ModelState.AddModelError(nameof(input.Coordenadas), "Coordenadas es obligatorio.");
        }

        if (input.Productos is null || input.Productos.Count == 0)
        {
            ModelState.AddModelError(nameof(input.Productos), "Debe agregar productos.");
        }

        return ModelState.IsValid;
    }

    #region Sales Filtering - Dashboard Integration

    /// <summary>
    /// Builds a filter context from Dashboard parameters
    /// </summary>
    private static SalesFilterContext BuildSalesFilterContext(DateTime? from, DateTime? to, string? day, string? zone, string? seller)
    {
        var context = new SalesFilterContext
        {
            HasFilters = from.HasValue || to.HasValue || !string.IsNullOrWhiteSpace(day) || !string.IsNullOrWhiteSpace(zone) || !string.IsNullOrWhiteSpace(seller)
        };

        // Date range filter
        if (from.HasValue)
        {
            context.DateFrom = from.Value.Date;
        }

        if (to.HasValue)
        {
            context.DateTo = to.Value.Date;
        }

        // Single day filter (overrides date range if provided)
        if (!string.IsNullOrWhiteSpace(day) && DateTime.TryParse(day, out var dayDate))
        {
            context.DateFrom = dayDate.Date;
            context.DateTo = dayDate.Date;
            context.FilteredDay = dayDate.Date;
        }

        // Zone filter
        if (!string.IsNullOrWhiteSpace(zone))
        {
            context.Zone = zone.Trim();
        }

        // Seller filter
        if (!string.IsNullOrWhiteSpace(seller))
        {
            context.Seller = seller.Trim();
        }

        return context;
    }

    /// <summary>
    /// Applies filters to sales collection
    /// </summary>
    private static List<SaleRecord> ApplySalesFilters(List<SaleRecord> sales, SalesFilterContext context)
    {
        if (!context.HasFilters)
        {
            return sales;
        }

        var filtered = sales.AsEnumerable();

        // Apply date range filter
        if (context.DateFrom.HasValue)
        {
            filtered = filtered.Where(x => x.FechaVenta.Date >= context.DateFrom.Value);
        }

        if (context.DateTo.HasValue)
        {
            filtered = filtered.Where(x => x.FechaVenta.Date <= context.DateTo.Value);
        }

        // Apply zone filter
        if (!string.IsNullOrWhiteSpace(context.Zone))
        {
            filtered = filtered.Where(x => string.Equals(x.Zona, context.Zone, StringComparison.OrdinalIgnoreCase));
        }

        // Apply seller filter
        if (!string.IsNullOrWhiteSpace(context.Seller))
        {
            filtered = filtered.Where(x => string.Equals(x.Vendedor, context.Seller, StringComparison.OrdinalIgnoreCase));
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Filter context for sales data
    /// </summary>
    private sealed class SalesFilterContext
    {
        public bool HasFilters { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public DateTime? FilteredDay { get; set; }
        public string? Zone { get; set; }
        public string? Seller { get; set; }
    }

    #endregion
