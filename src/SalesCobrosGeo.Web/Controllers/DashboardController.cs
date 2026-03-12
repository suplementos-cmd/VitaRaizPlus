using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Dashboard;
using SalesCobrosGeo.Web.Services.Sales;
using System.Globalization;

namespace SalesCobrosGeo.Web.Controllers;

public sealed class DashboardController : Controller
{
    private readonly ISalesRepository _repository;

    public DashboardController(ISalesRepository repository)
    {
        _repository = repository;
    }

    public IActionResult Index(string scope = "week", int offset = 0, string collectionsBy = "zone")
    {
        return View(BuildModel(scope, offset, collectionsBy));
    }

    public IActionResult Sales(string scope = "week", int offset = 0, string? seller = null, string? day = null)
    {
        return View(BuildModel(scope, offset, "zone", sellerFilter: seller, salesDay: day));
    }

    public IActionResult Collections(string scope = "week", int offset = 0, string collectionsBy = "zone", string? value = null, string? day = null)
    {
        return View(BuildModel(scope, offset, collectionsBy, collectionValue: value, collectionDay: day));
    }

    private DashboardPageViewModel BuildModel(string? scope, int offset, string? collectionsBy, string? sellerFilter = null, string? collectionValue = null, string? salesDay = null, string? collectionDay = null)
    {
        var period = BuildPeriod(scope, offset, DateTime.Today);
        var grouping = NormalizeGrouping(collectionsBy);

        var allSales = _repository.GetAll();
        var allCollections = _repository.GetCollections();
        var portfolio = _repository.GetCollectorPortfolio(profile: null);

        var salesInPeriod = allSales
            .Where(x => x.FechaVenta.Date >= period.Start && x.FechaVenta.Date <= period.End)
            .ToList();

        var collectionsInPeriod = allCollections
            .Where(x => x.FechaCobro.Date >= period.Start && x.FechaCobro.Date <= period.End)
            .ToList();

        var comparison = BuildComparisonPeriod(period);
        var previousSalesAmount = allSales
            .Where(x => x.FechaVenta.Date >= comparison.Start && x.FechaVenta.Date <= comparison.End)
            .Sum(x => x.ImporteTotal);
        var previousCollectionsAmount = allCollections
            .Where(x => x.FechaCobro.Date >= comparison.Start && x.FechaCobro.Date <= comparison.End)
            .Sum(x => x.ImporteCobro);

        var kpis = new[]
        {
            new KpiCard("Ventas del periodo", $"Bs {salesInPeriod.Sum(x => x.ImporteTotal):0,0.##}", BuildTrend(salesInPeriod.Sum(x => x.ImporteTotal), previousSalesAmount), "brand"),
            new KpiCard("Cobrado del periodo", $"Bs {collectionsInPeriod.Sum(x => x.ImporteCobro):0,0.##}", BuildTrend(collectionsInPeriod.Sum(x => x.ImporteCobro), previousCollectionsAmount), "success"),
            new KpiCard("Cuentas activas", portfolio.Count(x => x.ImporteRestante > 0).ToString(), $"Bs {portfolio.Sum(x => x.ImporteRestante):0,0.##} pendiente", "warning"),
            new KpiCard("Atrasadas", portfolio.Count(x => x.Estatus.Equals("ATRASADO", StringComparison.OrdinalIgnoreCase)).ToString(), "seguimiento prioritario", "danger")
        };

        var weeklySales = BuildDailySales(salesInPeriod, period).ToArray();
        var weeklyCollections = BuildDailyCollections(collectionsInPeriod, period).ToArray();

        var sellerSummaries = salesInPeriod
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Vendedor) ? "SIN VENDEDOR" : x.Vendedor)
            .OrderByDescending(g => g.Sum(x => x.ImporteTotal))
            .ThenBy(g => g.Key)
            .Select(g => new SellerPerformanceSummary(
                g.Key,
                g.Count(),
                g.Count(IsClosedSale),
                g.Sum(x => x.ImporteTotal)))
            .ToArray();

        var collectionSummaries = collectionsInPeriod
            .GroupBy(x => GetCollectionGroupKey(x, grouping))
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .OrderByDescending(g => g.Sum(x => x.ImporteCobro))
            .ThenBy(g => g.Key)
            .Select(g => new CollectionGroupingSummary(
                g.Key,
                GetCollectionGroupLabel(g.First(), grouping),
                g.Count(),
                g.Sum(x => x.ImporteCobro)))
            .ToArray();

        var filteredSales = salesInPeriod.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(sellerFilter))
        {
            filteredSales = filteredSales.Where(x => string.Equals(x.Vendedor, sellerFilter, StringComparison.OrdinalIgnoreCase));
        }
        if (DateTime.TryParse(salesDay, out var salesDayDate))
        {
            filteredSales = filteredSales.Where(x => x.FechaVenta.Date == salesDayDate.Date);
        }

        var filteredCollections = collectionsInPeriod.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(collectionValue))
        {
            filteredCollections = filteredCollections.Where(x => string.Equals(GetCollectionGroupKey(x, grouping), collectionValue, StringComparison.OrdinalIgnoreCase));
        }
        if (DateTime.TryParse(collectionDay, out var collectionDayDate))
        {
            filteredCollections = filteredCollections.Where(x => x.FechaCobro.Date == collectionDayDate.Date);
        }

        var saleRows = filteredSales
            .OrderByDescending(x => x.FechaActu)
            .Select(x => new SaleRow(
                $"VTA-{x.NumVenta:0000}",
                x.Vendedor,
                x.Zona,
                x.ImporteTotal,
                x.Estado,
                x.FechaActu.ToString("dd/MM/yyyy HH:mm"),
                x.NombreCliente))
            .ToArray();

        var collectionRows = filteredCollections
            .OrderByDescending(x => x.FechaCaptura)
            .Select(x => new CollectionRow(
                $"VTA-{x.NumVenta:0000}",
                x.Usuario,
                x.ImporteCobro,
                x.ImporteRestante,
                x.Estatus,
                x.FechaCaptura.ToString("dd/MM/yyyy HH:mm"),
                x.NombreCliente,
                x.Zona,
                x.FechaCobro.ToString("ddd dd", new CultureInfo("es-ES"))))
            .ToArray();

        return new DashboardPageViewModel(
            new DashboardPeriodInfo(period.Scope, period.Offset, period.Label, period.Subtitle),
            grouping,
            kpis,
            weeklySales,
            weeklyCollections,
            sellerSummaries,
            collectionSummaries,
            saleRows,
            collectionRows);
    }

    private static IEnumerable<DailySummary> BuildDailySales(IReadOnlyList<Models.Sales.SaleRecord> sales, (DateTime Start, DateTime End, string Scope, int Offset, string Label, string Subtitle) period)
    {
        var days = period.Scope == "month"
            ? Enumerable.Range(0, (period.End - period.Start).Days + 1).Select(i => period.Start.AddDays(i))
            : Enumerable.Range(0, 7).Select(i => period.Start.AddDays(i));

        return days.Select(day => new DailySummary(
            day.ToString("yyyy-MM-dd"),
            day.ToString("ddd dd", new CultureInfo("es-ES")),
            sales.Count(x => x.FechaVenta.Date == day.Date),
            sales.Where(x => x.FechaVenta.Date == day.Date).Sum(x => x.ImporteTotal)));
    }

    private static IEnumerable<DailySummary> BuildDailyCollections(IReadOnlyList<Models.Sales.CollectionRecord> collections, (DateTime Start, DateTime End, string Scope, int Offset, string Label, string Subtitle) period)
    {
        var days = period.Scope == "month"
            ? Enumerable.Range(0, (period.End - period.Start).Days + 1).Select(i => period.Start.AddDays(i))
            : Enumerable.Range(0, 7).Select(i => period.Start.AddDays(i));

        return days.Select(day => new DailySummary(
            day.ToString("yyyy-MM-dd"),
            day.ToString("ddd dd", new CultureInfo("es-ES")),
            collections.Count(x => x.FechaCobro.Date == day.Date),
            collections.Where(x => x.FechaCobro.Date == day.Date).Sum(x => x.ImporteCobro)));
    }

    private static bool IsClosedSale(Models.Sales.SaleRecord sale)
    {
        return sale.Estado2.Equals("CLOSED", StringComparison.OrdinalIgnoreCase)
            || sale.Estado.Contains("LIQUIDADO", StringComparison.OrdinalIgnoreCase)
            || sale.Estado.Contains("CANCEL", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeGrouping(string? grouping)
    {
        return grouping?.Trim().ToLowerInvariant() switch
        {
            "collector" => "collector",
            "day" => "day",
            _ => "zone"
        };
    }

    private static string GetCollectionGroupKey(Models.Sales.CollectionRecord row, string grouping)
    {
        return grouping switch
        {
            "collector" => string.IsNullOrWhiteSpace(row.Usuario) ? "SIN COBRADOR" : row.Usuario,
            "day" => row.FechaCobro.ToString("yyyy-MM-dd"),
            _ => string.IsNullOrWhiteSpace(row.Zona) ? "SIN ZONA" : row.Zona
        };
    }

    private static string GetCollectionGroupLabel(Models.Sales.CollectionRecord row, string grouping)
    {
        return grouping switch
        {
            "collector" => string.IsNullOrWhiteSpace(row.Usuario) ? "SIN COBRADOR" : row.Usuario,
            "day" => row.FechaCobro.ToString("ddd dd/MM", new CultureInfo("es-ES")),
            _ => string.IsNullOrWhiteSpace(row.Zona) ? "SIN ZONA" : row.Zona
        };
    }

    private static (DateTime Start, DateTime End, string Scope, int Offset, string Label, string Subtitle) BuildPeriod(string? scope, int offset, DateTime today)
    {
        var normalized = string.Equals(scope, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "week";

        if (normalized == "month")
        {
            var first = new DateTime(today.Year, today.Month, 1).AddMonths(offset);
            var last = first.AddMonths(1).AddDays(-1);
            return (first, last, normalized, offset, first.ToString("MMMM yyyy", new CultureInfo("es-ES")), "Periodo mensual");
        }

        var diff = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.Date.AddDays(-diff).AddDays(offset * 7);
        var sunday = monday.AddDays(6);
        return (monday, sunday, normalized, offset, $"{monday:dd/MM} - {sunday:dd/MM}", "Semana seleccionada");
    }

    private static (DateTime Start, DateTime End) BuildComparisonPeriod((DateTime Start, DateTime End, string Scope, int Offset, string Label, string Subtitle) period)
    {
        if (period.Scope == "month")
        {
            var start = period.Start.AddMonths(-1);
            return (start, start.AddMonths(1).AddDays(-1));
        }

        return (period.Start.AddDays(-7), period.End.AddDays(-7));
    }

    private static string BuildTrend(decimal current, decimal previous)
    {
        if (previous <= 0 && current > 0)
        {
            return "nuevo movimiento";
        }

        if (previous <= 0)
        {
            return "sin variacion";
        }

        var pct = ((current - previous) / previous) * 100m;
        var sign = pct >= 0 ? "+" : string.Empty;
        return $"{sign}{pct:0.#}% vs periodo previo";
    }
}
