using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Maintenance;
using SalesCobrosGeo.Web.Security;
using SalesCobrosGeo.Web.Services.Sales;

namespace SalesCobrosGeo.Web.Controllers;

[Authorize(Policy = AppPolicies.MaintenanceAccess)]
public sealed class MaintenanceController : Controller
{
    private static readonly string[] EditableSections = ["zonas", "dias-cobro", "formas-pago", "productos", "vendedores", "cobradores"];

    private readonly ISalesRepository _repository;
    private readonly IApplicationUserService _userService;

    public MaintenanceController(ISalesRepository repository, IApplicationUserService userService)
    {
        _repository = repository;
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Index(string section = "catalogos", long? editId = null)
    {
        var model = BuildViewModel(section, editId, TempData["MaintenanceMessage"] as string);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save(MaintenanceCatalogSaveInput input)
    {
        try
        {
            _repository.SaveMaintenanceCatalogItem(input);
            TempData["MaintenanceMessage"] = string.IsNullOrWhiteSpace(input.Id?.ToString())
                ? "Registro creado correctamente."
                : "Registro actualizado correctamente.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["MaintenanceMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { section = input.Section });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string section, long id)
    {
        TempData["MaintenanceMessage"] = _repository.DeleteMaintenanceCatalogItem(section, id)
            ? "Registro eliminado correctamente."
            : "No se encontro el registro para eliminar.";

        return RedirectToAction(nameof(Index), new { section });
    }

    private MaintenancePageViewModel BuildViewModel(string section, long? editId, string? message)
    {
        var selected = NormalizeSection(section);
        var sections = BuildSections();
        var current = sections.First(x => x.Key == selected);
        var stats = BuildStats(sections);
        var currentCatalogItems = EditableSections.Contains(selected, StringComparer.OrdinalIgnoreCase)
            ? _repository.GetMaintenanceCatalog(selected)
            : [];

        var editorRecord = currentCatalogItems.FirstOrDefault(x => x.Id == editId) ?? currentCatalogItems.FirstOrDefault();
        var editor = new MaintenanceEditorInput
        {
            Id = editorRecord?.Id,
            Section = selected,
            Code = editorRecord?.Code ?? string.Empty,
            Name = editorRecord?.Name ?? string.Empty,
            Price = editorRecord?.Price,
            IsActive = editorRecord?.IsActive ?? true
        };

        return new MaintenancePageViewModel(selected, stats, sections, editor, message);
    }

    private IReadOnlyList<MaintenanceStat> BuildStats(IReadOnlyList<MaintenanceSection> sections)
    {
        var editableCount = sections.Where(x => EditableSections.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).Sum(x => x.Items.Count);
        var activeUsers = _userService.GetUsers().Count(x => x.IsActive);
        var productCount = _repository.GetMaintenanceCatalog("productos").Count;
        var zoneCount = _repository.GetMaintenanceCatalog("zonas").Count;

        return
        [
            new MaintenanceStat("Catalogos editables", editableCount.ToString(), "brand"),
            new MaintenanceStat("Personal activo", activeUsers.ToString(), "success"),
            new MaintenanceStat("Productos", productCount.ToString(), "warning"),
            new MaintenanceStat("Zonas", zoneCount.ToString(), "danger")
        ];
    }

    private IReadOnlyList<MaintenanceSection> BuildSections()
    {
        var zones = BuildCatalogSection("zonas", "Zonas", "Cobertura geografica utilizada en ventas y cobros.", "Catalogo editable para asignar zonas comerciales y de ruta.", "Zona", "brand");
        var days = BuildCatalogSection("dias-cobro", "Dias de cobro", "Dias configurados para planear la ruta semanal.", "Puedes activar dias de ruta y ordenarlos en la operacion.", "Cobro", "warning");
        var paymentMethods = BuildCatalogSection("formas-pago", "Formas de pago", "Esquemas comerciales disponibles para registrar ventas.", "Define los planes de pago visibles en ventas.", "Pago", "success");
        var products = BuildCatalogSection("productos", "Productos", "Catalogo comercial con precio base editable.", "Base comercial para ventas y comisiones.", "Producto", "brand");
        var sellers = BuildCatalogSection("vendedores", "Vendedores", "Equipo comercial disponible para asignacion de ventas.", "Catalogo base del equipo de ventas.", "Vendedor", "brand");
        var collectors = BuildCatalogSection("cobradores", "Cobradores", "Equipo de cobranza asignable a cartera y ruta.", "Catalogo base del equipo de cobros.", "Cobrador", "success");

        var users = _userService.GetUsers();
        var employees = new MaintenanceSection(
            "empleados",
            "Empleados",
            "Los empleados y accesos se administran desde Usuarios y permisos.",
            $"{users.Count} perfiles operativos en la plataforma.",
            users.Select((user, index) => new MaintenanceItem(
                index + 1,
                user.Username,
                user.DisplayName,
                $"{user.RoleLabel} • Zona {user.Zone}",
                user.IsActive ? "Activo" : "Inactivo",
                user.IsActive ? "success" : "danger")).ToArray());

        var summary = new MaintenanceSection(
            "catalogos",
            "Catalogos",
            "Resumen central de configuraciones base y personal operativo.",
            "Desde aqui puedes brincar a cualquier modulo de configuracion editable.",
            [
                new MaintenanceItem(1, "ZON", "Zonas", $"{zones.Items.Count} registros editables", "Base", "brand"),
                new MaintenanceItem(2, "DIA", "Dias de cobro", $"{days.Items.Count} opciones activas", "Ruta", "warning"),
                new MaintenanceItem(3, "PAG", "Formas de pago", $"{paymentMethods.Items.Count} esquemas configurados", "Comercial", "success"),
                new MaintenanceItem(4, "PRO", "Productos", $"{products.Items.Count} productos base", "Catalogo", "brand"),
                new MaintenanceItem(5, "VEN", "Vendedores", $"{sellers.Items.Count} perfiles comerciales", "Equipo", "brand"),
                new MaintenanceItem(6, "COB", "Cobradores", $"{collectors.Items.Count} perfiles de cobranza", "Equipo", "success"),
                new MaintenanceItem(7, "EMP", "Empleados", $"{employees.Items.Count} accesos operativos", "Usuarios", "danger")
            ]);

        return [summary, sellers, collectors, employees, zones, days, paymentMethods, products];
    }

    private MaintenanceSection BuildCatalogSection(string key, string title, string subtitle, string summary, string badgeLabel, string tone)
    {
        var items = _repository.GetMaintenanceCatalog(key)
            .Select(item => new MaintenanceItem(
                item.Id,
                item.Code,
                item.Name,
                key == "productos" && item.Price.HasValue
                    ? $"Precio base Bs {item.Price:0,0.##}"
                    : item.IsActive ? "Registro activo para operacion." : "Registro inactivo.",
                item.IsActive ? badgeLabel : "Inactivo",
                item.IsActive ? tone : "danger"))
            .ToArray();

        return new MaintenanceSection(key, title, subtitle, summary, items);
    }

    private static string NormalizeSection(string section)
    {
        return section switch
        {
            "vendedores" => "vendedores",
            "cobradores" => "cobradores",
            "empleados" => "empleados",
            "zonas" => "zonas",
            "dias-cobro" => "dias-cobro",
            "formas-pago" => "formas-pago",
            "productos" => "productos",
            _ => "catalogos"
        };
    }
}
