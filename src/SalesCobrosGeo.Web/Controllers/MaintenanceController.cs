using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Maintenance;
using SalesCobrosGeo.Web.Models.Sales;
using SalesCobrosGeo.Web.Services.Sales;

namespace SalesCobrosGeo.Web.Controllers;

public sealed class MaintenanceController : Controller
{
    private readonly ISalesRepository _repository;

    public MaintenanceController(ISalesRepository repository)
    {
        _repository = repository;
    }

    public IActionResult Index(string section = "catalogos")
    {
        var catalogs = _repository.GetCatalogs();
        var sales = _repository.GetAll();
        var collectorProfiles = _repository.GetCollectorProfiles();

        var vendedores = catalogs.Sellers
            .Select(x => new MaintenanceItem(
                x.Code,
                x.Name,
                $"{sales.Count(s => string.Equals(s.Vendedor, x.Code, StringComparison.OrdinalIgnoreCase))} ventas registradas",
                "Vendedor",
                "brand"))
            .ToArray();

        var cobradores = catalogs.Collectors
            .Concat(collectorProfiles)
            .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Select(x => new MaintenanceItem(
                x.Code,
                x.Name,
                $"{sales.Count(s => string.Equals(s.Cobrador, x.Code, StringComparison.OrdinalIgnoreCase))} cuentas asignadas",
                "Cobrador",
                "success"))
            .OrderBy(x => x.Name)
            .ToArray();

        var empleados = vendedores
            .Select(x => new MaintenanceItem(x.Code, x.Name, "Equipo comercial", "Ventas", "brand"))
            .Concat(cobradores.Select(x => new MaintenanceItem(x.Code, x.Name, "Equipo de cobranza", "Cobros", "success")))
            .Concat(
            [
                new MaintenanceItem("SUPERVISOR", "Supervisor general", "Supervisa ventas y cobros", "Supervisor", "warning"),
                new MaintenanceItem("ADMIN", "Administrador", "Control total del sistema", "Admin", "danger")
            ])
            .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(x => x.Name)
            .ToArray();

        var zonas = catalogs.Zones
            .Select(x => new MaintenanceItem(
                x.Code,
                x.Name,
                $"{sales.Count(s => string.Equals(s.Zona, x.Code, StringComparison.OrdinalIgnoreCase))} ventas detectadas",
                "Zona",
                "brand"))
            .ToArray();

        var dias = catalogs.CollectionDays
            .Select(x => new MaintenanceItem(
                x.Code,
                x.Name,
                $"{sales.Count(s => string.Equals(s.DiaCobro, x.Code, StringComparison.OrdinalIgnoreCase))} ventas programadas",
                "Cobro",
                "warning"))
            .ToArray();

        var formasPago = catalogs.PaymentMethods
            .Select(x => new MaintenanceItem(
                x.Code,
                x.Name,
                $"{sales.Count(s => string.Equals(s.FormaPago, x.Code, StringComparison.OrdinalIgnoreCase))} ventas configuradas",
                "Pago",
                "success"))
            .ToArray();

        var productos = catalogs.Products
            .Select(x => new MaintenanceItem(
                x.Code,
                x.Name,
                $"Precio base Bs {x.Price:0,0.##}",
                $"{sales.Count(s => s.Productos.Any(p => string.Equals(p.ProductCode, x.Code, StringComparison.OrdinalIgnoreCase)) || string.Equals(s.Producto, x.Code, StringComparison.OrdinalIgnoreCase))} ventas",
                "brand"))
            .ToArray();

        var sections = new[]
        {
            new MaintenanceSection(
                "catalogos",
                "Catalogos",
                "Resumen visual de configuraciones base del sistema.",
                "Vista centralizada para revisar estructura comercial y operativa.",
                [
                    new MaintenanceItem("ZON", "Zonas", $"{zonas.Length} registros configurados", "Base", "brand"),
                    new MaintenanceItem("DIA", "Dias de cobro", $"{dias.Length} opciones activas", "Base", "warning"),
                    new MaintenanceItem("PAG", "Formas de pago", $"{formasPago.Length} esquemas disponibles", "Base", "success"),
                    new MaintenanceItem("PRO", "Productos", $"{productos.Length} productos en demo", "Base", "brand"),
                    new MaintenanceItem("VEN", "Vendedores", $"{vendedores.Length} perfiles comerciales", "Equipo", "brand"),
                    new MaintenanceItem("COB", "Cobradores", $"{cobradores.Length} perfiles de cobranza", "Equipo", "success")
                ]),
            new MaintenanceSection("vendedores", "Vendedores", "Perfiles de venta y productividad base.", $"{vendedores.Length} perfiles registrados", vendedores),
            new MaintenanceSection("cobradores", "Cobradores", "Perfiles disponibles para asignacion de cartera.", $"{cobradores.Length} perfiles registrados", cobradores),
            new MaintenanceSection("empleados", "Empleados", "Vista consolidada de usuarios operativos.", $"{empleados.Length} perfiles en demo", empleados),
            new MaintenanceSection("zonas", "Zonas", "Cobertura geocomercial utilizada en ventas y cobros.", $"{zonas.Length} zonas activas", zonas),
            new MaintenanceSection("dias-cobro", "Dias de cobro", "Dias configurados para programacion de ruta.", $"{dias.Length} dias configurados", dias),
            new MaintenanceSection("formas-pago", "Formas de pago", "Esquemas comerciales disponibles para registrar ventas.", $"{formasPago.Length} formas configuradas", formasPago),
            new MaintenanceSection("productos", "Productos", "Catalogo comercial con precio base de referencia.", $"{productos.Length} productos configurados", productos)
        };

        var selected = sections.FirstOrDefault(x => string.Equals(x.Key, section, StringComparison.OrdinalIgnoreCase))?.Key
            ?? "catalogos";

        var model = new MaintenancePageViewModel(
            selected,
            [
                new MaintenanceStat("Catalogos base", sections.Length.ToString(), "brand"),
                new MaintenanceStat("Personal operativo", empleados.Length.ToString(), "success"),
                new MaintenanceStat("Productos", productos.Length.ToString(), "warning"),
                new MaintenanceStat("Zonas activas", zonas.Length.ToString(), "danger")
            ],
            sections);

        return View(model);
    }
}
