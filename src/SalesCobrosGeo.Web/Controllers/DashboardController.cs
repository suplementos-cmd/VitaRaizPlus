using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Dashboard;
using SalesCobrosGeo.Web.Services.Sales;

namespace SalesCobrosGeo.Web.Controllers;

public sealed class DashboardController : Controller
{
    private readonly ISalesRepository _repository;

    public DashboardController(ISalesRepository repository)
    {
        _repository = repository;
    }

    public IActionResult Index()
    {
        var model = BuildModel();
        return View(model);
    }

    public IActionResult Sales()
    {
        return View(BuildModel());
    }

    public IActionResult Collections()
    {
        return View(BuildModel());
    }

    private DashboardPageViewModel BuildModel()
    {
        var sales = _repository.GetAll();
        var collections = _repository.GetCollections();
        var portfolio = _repository.GetCollectorPortfolio(profile: null);
        var today = DateTime.Today;
        var start = today.AddDays(-6);

        var totalSales = sales.Sum(x => x.ImporteTotal);
        var totalCollected = collections.Sum(x => x.ImporteCobro);
        var totalPending = portfolio.Sum(x => x.ImporteRestante);
        var lateAccounts = portfolio.Count(x => x.Estatus.Equals("ATRASADO", StringComparison.OrdinalIgnoreCase));

        var salesThisWeek = sales.Where(x => x.FechaVenta.Date >= start).ToList();
        var collectionsThisWeek = collections.Where(x => x.FechaCobro.Date >= start).ToList();
        var previousSalesStart = start.AddDays(-7);
        var previousCollectionsStart = start.AddDays(-7);
        var previousSales = sales.Where(x => x.FechaVenta.Date >= previousSalesStart && x.FechaVenta.Date < start).Sum(x => x.ImporteTotal);
        var previousCollections = collections.Where(x => x.FechaCobro.Date >= previousCollectionsStart && x.FechaCobro.Date < start).Sum(x => x.ImporteCobro);

        var kpis = new[]
        {
            new KpiCard("Ventas Totales", $"Bs {totalSales:0,0.##}", BuildTrend(salesThisWeek.Sum(x => x.ImporteTotal), previousSales), "brand"),
            new KpiCard("Cobrado", $"Bs {totalCollected:0,0.##}", BuildTrend(collectionsThisWeek.Sum(x => x.ImporteCobro), previousCollections), "success"),
            new KpiCard("Pendiente", $"Bs {totalPending:0,0.##}", $"{portfolio.Count} cuentas activas", "warning"),
            new KpiCard("Atrasadas", lateAccounts.ToString(), lateAccounts == 0 ? "sin mora critica" : "requieren seguimiento", "danger")
        };

        var saleRows = sales
            .OrderByDescending(x => x.FechaActu)
            .Take(8)
            .Select(x => new SaleRow(
                $"VTA-{x.NumVenta:0000}",
                x.Vendedor,
                x.Zona,
                x.ImporteTotal,
                x.Estado,
                x.FechaActu.ToString("dd/MM/yyyy HH:mm"),
                x.NombreCliente))
            .ToArray();

        var collectionRows = collections
            .OrderByDescending(x => x.FechaCaptura)
            .Take(8)
            .Select(x => new CollectionRow(
                $"VTA-{x.NumVenta:0000}",
                x.Usuario,
                x.ImporteCobro,
                x.ImporteRestante,
                x.Estatus,
                x.FechaCaptura.ToString("dd/MM/yyyy HH:mm"),
                x.NombreCliente))
            .ToArray();

        var zones = portfolio
            .Where(x => x.ImporteRestante > 0)
            .GroupBy(x => x.Zona)
            .OrderByDescending(g => g.Sum(x => x.ImporteRestante))
            .Take(6)
            .Select(g => new ZoneSummary(g.Key, g.Count(), g.Sum(x => x.ImporteRestante)))
            .ToArray();

        var weeklySales = Enumerable.Range(0, 7)
            .Select(offset => start.AddDays(offset))
            .Select(day => new DailySummary(
                day.ToString("ddd dd"),
                sales.Count(x => x.FechaVenta.Date == day),
                sales.Where(x => x.FechaVenta.Date == day).Sum(x => x.ImporteTotal)))
            .ToArray();

        var weeklyCollections = Enumerable.Range(0, 7)
            .Select(offset => start.AddDays(offset))
            .Select(day => new DailySummary(
                day.ToString("ddd dd"),
                collections.Count(x => x.FechaCobro.Date == day),
                collections.Where(x => x.FechaCobro.Date == day).Sum(x => x.ImporteCobro)))
            .ToArray();

        var recoveryPercent = totalSales <= 0 ? 0m : (totalCollected / totalSales) * 100m;

        return new DashboardPageViewModel(
            kpis,
            saleRows,
            collectionRows,
            zones,
            weeklySales,
            weeklyCollections,
            new DashboardMiniStat("Cartera activa", $"Bs {totalPending:0,0.##}"),
            new DashboardMiniStat("Recuperacion", $"{recoveryPercent:0.#}%"));
    }

    private static string BuildTrend(decimal current, decimal previous)
    {
        if (previous <= 0 && current > 0)
        {
            return "nuevo movimiento esta semana";
        }

        if (previous <= 0)
        {
            return "sin variacion";
        }

        var pct = ((current - previous) / previous) * 100m;
        var sign = pct >= 0 ? "+" : string.Empty;
        return $"{sign}{pct:0.#}% vs semana previa";
    }
}
