using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Sales;
using SalesCobrosGeo.Web.Services.Sales;
using System.Globalization;
using System.Text;

namespace SalesCobrosGeo.Web.Controllers;

public sealed class CobrosController : Controller
{
    private static readonly Dictionary<string, int> DayOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LUNES"] = 1,
        ["MARTES"] = 2,
        ["MIERCOLES"] = 3,
        ["JUEVES"] = 4,
        ["VIERNES"] = 5,
        ["SABADO"] = 6,
        ["DOMINGO"] = 7
    };

    private static readonly Dictionary<string, int> StatusOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["POR INICIAR"] = 1,
        ["AL CORRIENTE"] = 2,
        ["ATRASADO"] = 3,
        ["LIQUIDADO"] = 4
    };

    private readonly ISalesRepository _repository;

    public CobrosController(ISalesRepository repository)
    {
        _repository = repository;
    }

    public IActionResult Index(string? profile = null, string? day = null, string? status = null, string? zone = null)
    {
        var portfolio = _repository.GetCollectorPortfolio(profile)
            .Where(x => x.ImporteRestante > 0)
            .ToList();

        var selectedDay = NormalizeKey(day);
        var selectedStatus = NormalizeKey(status);
        var selectedZone = NormalizeKey(zone);

        var byDay = string.IsNullOrWhiteSpace(selectedDay)
            ? portfolio
            : portfolio.Where(x => NormalizeKey(x.DiaCobroPrevisto) == selectedDay).ToList();

        var byStatus = string.IsNullOrWhiteSpace(selectedStatus)
            ? byDay
            : byDay.Where(x => NormalizeKey(x.Estatus) == selectedStatus).ToList();

        var byZone = string.IsNullOrWhiteSpace(selectedZone)
            ? byStatus
            : byStatus.Where(x => NormalizeKey(x.Zona) == selectedZone).ToList();

        var model = new CollectorPortfolioViewModel
        {
            Profile = profile,
            SelectedDay = selectedDay,
            SelectedStatus = selectedStatus,
            SelectedZone = selectedZone,
            Profiles = _repository.GetCollectorProfiles(),
            Days = portfolio
                .GroupBy(x => NormalizeKey(x.DiaCobroPrevisto))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .OrderBy(g => DayOrder.TryGetValue(g.Key, out var order) ? order : 99)
                .Select(g => new CollectorDaySummary
                {
                    Day = g.First().DiaCobroPrevisto,
                    Count = g.Count()
                })
                .ToArray(),
            Statuses = byDay
                .GroupBy(x => NormalizeKey(x.Estatus))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .OrderBy(g => StatusOrder.TryGetValue(g.Key, out var order) ? order : 99)
                .Select(g => new CollectorStatusSummary
                {
                    Status = g.First().Estatus,
                    Count = g.Count()
                })
                .ToArray(),
            Zones = byStatus
                .GroupBy(x => NormalizeKey(x.Zona))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .OrderBy(g => g.First().Zona)
                .Select(g => new CollectorZoneSummary
                {
                    Zone = g.First().Zona,
                    Count = g.Count()
                })
                .ToArray(),
            Sales = byZone
                .OrderBy(x => x.NumVenta)
                .ToArray()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string id, string? profile = null, string? day = null, string? status = null, string? zone = null)
    {
        var item = _repository.GetPortfolioItem(id, profile);
        if (item is null)
        {
            return NotFound();
        }

        var model = new CollectionRegisterViewModel
        {
            PortfolioItem = item,
            Sale = _repository.GetById(id),
            Input = new CollectionFormInput
            {
                IdV = item.IdV,
                FechaCobro = DateTime.Today,
                Usuario = string.IsNullOrWhiteSpace(profile) ? item.Cobrador : profile,
                ImporteCobro = item.ImporteRestante > 0 ? item.ImporteRestante : 0,
                CoordenadasCobro = item.Coordenadas
            },
            Historial = _repository.GetCollections(idV: id),
            CollectorProfiles = _repository.GetCollectorProfiles(),
            ReturnProfile = profile,
            ReturnDay = day,
            ReturnStatus = status,
            ReturnZone = zone
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(CollectionFormInput input, string? profile = null, string? day = null, string? status = null, string? zone = null)
    {
        try
        {
            _repository.RegisterCollection(input);
            TempData["CobroMessage"] = "Cobro registrado correctamente.";
            return RedirectToAction(nameof(Index), new { profile, day, status, zone });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var model = new CollectionRegisterViewModel
            {
                PortfolioItem = _repository.GetPortfolioItem(input.IdV, profile ?? input.Usuario),
                Sale = _repository.GetById(input.IdV),
                Input = input,
                Historial = _repository.GetCollections(idV: input.IdV),
                CollectorProfiles = _repository.GetCollectorProfiles(),
                ReturnProfile = profile,
                ReturnDay = day,
                ReturnStatus = status,
                ReturnZone = zone
            };

            return View(model);
        }
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
