using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Shared;
using SalesCobrosGeo.Web.Models.Sales;
using SalesCobrosGeo.Web.Security;
using SalesCobrosGeo.Web.Services.Sales;

namespace SalesCobrosGeo.Web.Controllers;

[Authorize(Policy = AppPolicies.SalesAccess)]
public sealed class SalesController : Controller
{
    private readonly ISalesRepository _repository;
    private readonly ISalesQueryService _queryService;
    private readonly IUserSessionTracker _sessionTracker;
    private readonly IValidator<SaleFormInput> _validator;

    public SalesController(
        ISalesRepository repository,
        ISalesQueryService queryService,
        IUserSessionTracker sessionTracker,
        IValidator<SaleFormInput> validator)
    {
        _repository = repository;
        _queryService = queryService;
        _sessionTracker = sessionTracker;
        _validator = validator;
    }

    // -------------------------------------------------------------------------
    // LIST — server-side paged + filtered
    // -------------------------------------------------------------------------

    public IActionResult Index(
        DateTime? from = null,
        DateTime? to = null,
        string? day = null,
        string? zone = null,
        string? seller = null,
        string? estado = null,
        string? q = null,
        int page = 1,
        int pageSize = 25)
    {
        // Single-day shortcut (from Dashboard click)
        if (!string.IsNullOrWhiteSpace(day) && DateTime.TryParse(day, out var dayDate))
        {
            from ??= dayDate.Date;
            to ??= dayDate.Date;
        }

        var query = new SalesQuery(
            DateFrom: from?.Date,
            DateTo: to?.Date,
            Zone: zone,
            Seller: seller,
            SearchText: q,
            Estado: estado);

        var model = _queryService.BuildListView(query, page, pageSize);
        return View(model);
    }

    // -------------------------------------------------------------------------
    // SERVER-SIDE JSON SEARCH  (replaces client-side DOM filtering)
    // GET /Sales/Search?q=juan&zone=norte&seller=jake&maxResults=50
    // -------------------------------------------------------------------------

    [HttpGet]
    [Produces("application/json")]
    public IActionResult Search(string? q, string? zone, string? seller, int maxResults = 50)
    {
        var result = _queryService.Search(q, zone, seller, Math.Clamp(maxResults, 1, 200));
        return Json(result);
    }

    // -------------------------------------------------------------------------
    // DETAILS
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // CREATE
    // -------------------------------------------------------------------------

    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", _queryService.BuildFormView(null));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(SaleFormInput input)
    {
        NormalizeInput(input);
        var validation = _validator.Validate(input);

        if (!validation.IsValid)
        {
            validation.AddToModelState(ModelState);
            var createForm = _queryService.BuildFormView(null);
            createForm.Input = input;
            return View("Form", createForm);
        }

        try
        {
            var saved = _repository.Create(input);
            _sessionTracker.UpdateCoordinates(
                User.Identity?.Name ?? string.Empty,
                saved.Coordenadas,
                "Venta registrada");
            TempData["SalesMessage"] = "Venta registrada correctamente.";
            return RedirectToAction(nameof(Details), new { id = saved.IdV });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var createForm = _queryService.BuildFormView(null);
            createForm.Input = input;
            return View("Form", createForm);
        }
    }

    // -------------------------------------------------------------------------
    // EDIT
    // -------------------------------------------------------------------------

    [HttpGet]
    public IActionResult Edit(string id)
    {
        try
        {
            return View("Form", _queryService.BuildFormView(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(string id, SaleFormInput input)
    {
        NormalizeInput(input);
        var validation = _validator.Validate(input);

        if (!validation.IsValid)
        {
            validation.AddToModelState(ModelState);
            return View("Form", new SaleFormViewModel
            {
                IsEdit = true,
                PageTitle = "Editar Venta",
                Catalogs = _repository.GetCatalogs(),
                Input = input
            });
        }

        try
        {
            var saved = _repository.Update(id, input);
            _sessionTracker.UpdateCoordinates(
                User.Identity?.Name ?? string.Empty,
                saved.Coordenadas,
                "Venta actualizada");
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
                Input = input
            });
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Normalises product list and timestamps in-place before validation or save.
    /// </summary>
    private static void NormalizeInput(SaleFormInput input)
    {
        input.Productos = input.Productos
            .Where(p => !string.IsNullOrWhiteSpace(p.ProductCode) && p.Quantity > 0)
            .ToList();

        if (input.Productos.Count == 0)
        {
            input.Productos = [new SaleProductLineInput { Quantity = 1 }];
        }

        input.FechaActu = DateTime.Now;

        // Normalise to canonical GeoPoint format (fixes comma/space issues from mobile)
        if (GeoPoint.TryParse(input.Coordenadas, out var primary))
        {
            input.Coordenadas = primary.ToString();
        }

        if (GeoPoint.TryParse(input.Coordenadas2, out var secondary))
        {
            input.Coordenadas2 = secondary.ToString();
        }
    }
}

