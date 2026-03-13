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

    public IActionResult Index()
    {
        var today = DateTime.Today;
        var currentWeekStart = StartOfWeek(today);
        var currentWeekEnd = currentWeekStart.AddDays(6);
        var sales = _repository.GetAll().ToList();

        var weeks = sales
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
            WeeklyCount = sales.Count(x => x.FechaVenta.Date >= currentWeekStart && x.FechaVenta.Date <= currentWeekEnd)
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
}
