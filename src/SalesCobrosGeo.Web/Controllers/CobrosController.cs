using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Sales;
using SalesCobrosGeo.Web.Services.Sales;

namespace SalesCobrosGeo.Web.Controllers;

public sealed class CobrosController : Controller
{
    private readonly ISalesRepository _repository;

    public CobrosController(ISalesRepository repository)
    {
        _repository = repository;
    }

    public IActionResult Index(string? profile = null)
    {
        ViewBag.Profile = profile;
        ViewBag.Profiles = _repository.GetCollectorProfiles();
        ViewBag.History = _repository.GetCollections(profile);
        var portfolio = _repository.GetCollectorPortfolio(profile);
        return View(portfolio);
    }

    [HttpGet]
    public IActionResult Register(string id, string? profile = null)
    {
        var item = _repository.GetPortfolioItem(id, profile);
        if (item is null)
        {
            return NotFound();
        }

        var model = new CollectionRegisterViewModel
        {
            PortfolioItem = item,
            Input = new CollectionFormInput
            {
                IdV = item.IdV,
                FechaCobro = DateTime.Today,
                Usuario = string.IsNullOrWhiteSpace(profile) ? item.Cobrador : profile,
                ImporteCobro = item.ImporteRestante > 0 ? item.ImporteRestante : 0
            },
            Historial = _repository.GetCollections(idV: id),
            CollectorProfiles = _repository.GetCollectorProfiles()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(CollectionFormInput input)
    {
        try
        {
            _repository.RegisterCollection(input);
            TempData["CobroMessage"] = "Cobro registrado correctamente.";
            return RedirectToAction(nameof(Index), new { profile = input.Usuario });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var model = new CollectionRegisterViewModel
            {
                PortfolioItem = _repository.GetPortfolioItem(input.IdV, input.Usuario),
                Input = input,
                Historial = _repository.GetCollections(idV: input.IdV),
                CollectorProfiles = _repository.GetCollectorProfiles()
            };

            return View(model);
        }
    }
}
