using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Dashboard;

namespace SalesCobrosGeo.Web.Controllers;

public sealed class DashboardController : Controller
{
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

    private static DashboardPageViewModel BuildModel()
    {
        var kpis = new[]
        {
            new KpiCard("Ventas Totales", "Bs 18,430", "+12% semana"),
            new KpiCard("Cobrado", "Bs 9,850", "+8% semana"),
            new KpiCard("Pendiente", "Bs 8,580", "-3% semana"),
            new KpiCard("Mora", "11 cuentas", "-1 semana")
        };

        var sales = new[]
        {
            new SaleRow("VTA-20260310-00001", "vendedor.demo", "CENTRO", 640m, "Aprobada", "2026-03-10 10:15"),
            new SaleRow("VTA-20260310-00002", "vendedor.demo", "NORTE", 220m, "Observada", "2026-03-10 10:33"),
            new SaleRow("VTA-20260310-00003", "supventas.demo", "SUR", 980m, "Aprobada", "2026-03-10 11:01")
        };

        var collections = new[]
        {
            new CollectionRow("VTA-20260310-00001", "cobrador.demo", 200m, 440m, "Parcial", "2026-03-10 11:25"),
            new CollectionRow("VTA-20260309-00008", "cobrador.demo", 450m, 0m, "Pagado", "2026-03-10 09:05"),
            new CollectionRow("VTA-20260307-00012", "supcobranza.demo", 0m, 510m, "Vencido", "2026-03-10 08:45")
        };

        return new DashboardPageViewModel(kpis, sales, collections);
    }
}
