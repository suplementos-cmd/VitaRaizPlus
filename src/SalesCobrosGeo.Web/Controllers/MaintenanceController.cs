using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Maintenance;
using SalesCobrosGeo.Web.Security;
using SalesCobrosGeo.Web.Services.Sales;

namespace SalesCobrosGeo.Web.Controllers;

[Authorize(Policy = AppPolicies.MaintenanceAccess)]
public sealed class MaintenanceController : Controller
{
    private static readonly string[] EditableSections = ["zonas", "dias-cobro", "formas-pago", "productos", "vendedores", "cobradores", "estatus-venta", "estatus-cobro-grupos"];

    private readonly ISalesRepository _repository;
    private readonly IApplicationUserService _userService;

    public MaintenanceController(ISalesRepository repository, IApplicationUserService userService)
    {
        _repository = repository;
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Index(string section = "catalogos", long? editId = null, bool create = false)
    {
        var model = BuildViewModel(section, editId, create, TempData["MaintenanceMessage"] as string);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save(MaintenanceCatalogSaveInput input)
    {
        try
        {
            _repository.SaveMaintenanceCatalogItem(input);
            TempData["MaintenanceMessage"] = input.Id is > 0
                ? "Registro actualizado correctamente."
                : "Registro creado correctamente.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["MaintenanceMessage"] = ex.Message;
            return RedirectToAction(nameof(Index), new { section = input.Section, editId = input.Id, create = input.Id is null or 0 });
        }

        return RedirectToAction(nameof(Index), new { section = input.Section });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleStatus(string section, long id, bool isActive)
    {
        var record = _repository.GetMaintenanceCatalog(section).FirstOrDefault(x => x.Id == id);
        if (record is null)
        {
            TempData["MaintenanceMessage"] = "No se encontro el registro.";
            return RedirectToAction(nameof(Index), new { section });
        }

        _repository.SaveMaintenanceCatalogItem(new MaintenanceCatalogSaveInput
        {
            Id = record.Id,
            Section = record.Section,
            Code = record.Code,
            Name = record.Name,
            Price = record.Price,
            IsActive = isActive
        });

        TempData["MaintenanceMessage"] = isActive
            ? "Registro habilitado correctamente."
            : "Registro inhabilitado correctamente.";

        return RedirectToAction(nameof(Index), new { section });
    }

    private MaintenancePageViewModel BuildViewModel(string section, long? editId, bool create, string? message)
    {
        var selected = NormalizeSection(section);
        var sections = BuildSections();
        var stats = BuildStats(sections);
        var currentCatalogItems = EditableSections.Contains(selected, StringComparer.OrdinalIgnoreCase)
            ? _repository.GetMaintenanceCatalog(selected)
            : [];

        var editorRecord = editId is > 0 ? currentCatalogItems.FirstOrDefault(x => x.Id == editId) : null;
        var editor = new MaintenanceEditorInput
        {
            Id = editorRecord?.Id,
            Section = selected,
            Code = editorRecord?.Code ?? string.Empty,
            Name = editorRecord?.Name ?? string.Empty,
            Price = editorRecord?.Price,
            IsActive = editorRecord?.IsActive ?? true
        };

        return new MaintenancePageViewModel(
            selected,
            stats,
            sections,
            editor,
            EditableSections.Contains(selected, StringComparer.OrdinalIgnoreCase) && (create || editId is > 0),
            message);
    }

    private IReadOnlyList<MaintenanceStat> BuildStats(IReadOnlyList<MaintenanceSection> sections)
    {
        var editableCount = sections.Where(x => EditableSections.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).Sum(x => x.Items.Count);
        var activeUsers = _userService.GetUsers().Count(x => x.IsActive);
        var productCount = _repository.GetMaintenanceCatalog("productos").Count(x => x.IsActive);
        var zoneCount = _repository.GetMaintenanceCatalog("zonas").Count(x => x.IsActive);
        var statusCount = _repository.GetMaintenanceCatalog("estatus-cobro-grupos").Count(x => x.IsActive);

        return
        [
            new MaintenanceStat("Registros", editableCount.ToString(), "brand"),
            new MaintenanceStat("Personal activo", activeUsers.ToString(), "success"),
            new MaintenanceStat("Productos activos", productCount.ToString(), "warning"),
            new MaintenanceStat("Zonas activas", zoneCount.ToString(), "danger"),
            new MaintenanceStat("Estatus cobro", statusCount.ToString(), "brand")
        ];
    }

    private IReadOnlyList<MaintenanceSection> BuildSections()
    {
        var zones = BuildCatalogSection("zonas", "Zonas", "Cobertura geografica utilizada en ventas y cobros.", "Catalogo editable para asignar zonas comerciales y de ruta.", "Zona", "brand");
        var days = BuildCatalogSection("dias-cobro", "Dias cobro", "Dias configurados para planear la ruta semanal.", "Puedes activar dias de ruta y ordenarlos en la operacion.", "Cobro", "warning");
        var paymentMethods = BuildCatalogSection("formas-pago", "Formas pago", "Esquemas comerciales disponibles para registrar ventas.", "Define los planes de pago visibles en ventas.", "Pago", "success");
        var products = BuildCatalogSection("productos", "Productos", "Catalogo comercial con precio base editable.", "Base comercial para ventas y comisiones.", "Producto", "brand");
        var sellers = BuildCatalogSection("vendedores", "Vendedores", "Equipo comercial disponible para asignacion de ventas.", "Catalogo base del equipo de ventas.", "Vendedor", "brand");
        var collectors = BuildCatalogSection("cobradores", "Cobradores", "Equipo de cobranza asignable a cartera y ruta.", "Catalogo base del equipo de cobros.", "Cobrador", "success");
        var saleStatuses = BuildCatalogSection("estatus-venta", "Estatus venta", "Estados disponibles para capturar y editar ventas.", "Controla los estados que se veran en el formulario de ventas.", "Venta", "warning");
        var collectionGroups = BuildCatalogSection("estatus-cobro-grupos", "Estatus cobro", "Grupos operativos visibles en cobros movil y arbol de cartera.", "Puedes activar o renombrar los grupos operativos del arbol de cobros.", "Cobro", "brand");

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
                user.IsActive ? "success" : "danger",
                user.IsActive)).ToArray());

        var summary = new MaintenanceSection(
            "catalogos",
            "Catalogos",
            "Resumen rapido de configuraciones base y personal operativo.",
            "Selecciona una pestana para editar o habilitar registros.",
            [
                new MaintenanceItem(1, "ZON", "Zonas", $"{zones.Items.Count} registros", "Base", "brand", true),
                new MaintenanceItem(2, "DIA", "Dias de cobro", $"{days.Items.Count} opciones", "Ruta", "warning", true),
                new MaintenanceItem(3, "PAG", "Formas de pago", $"{paymentMethods.Items.Count} esquemas", "Comercial", "success", true),
                new MaintenanceItem(4, "PRO", "Productos", $"{products.Items.Count} productos", "Catalogo", "brand", true),
                new MaintenanceItem(5, "VEN", "Vendedores", $"{sellers.Items.Count} perfiles", "Equipo", "brand", true),
                new MaintenanceItem(6, "COB", "Cobradores", $"{collectors.Items.Count} perfiles", "Equipo", "success", true),
                new MaintenanceItem(7, "ESTV", "Estatus venta", $"{saleStatuses.Items.Count} estados", "Ventas", "warning", true),
                new MaintenanceItem(8, "ESTC", "Estatus cobro", $"{collectionGroups.Items.Count} grupos", "Cobros", "brand", true),
                new MaintenanceItem(9, "EMP", "Empleados", $"{employees.Items.Count} accesos", "Usuarios", "danger", true)
            ]);

        return [summary, sellers, collectors, saleStatuses, collectionGroups, employees, zones, days, paymentMethods, products];
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
                    : item.IsActive ? "Disponible para operacion" : "Registro inhabilitado",
                item.IsActive ? badgeLabel : "Inactivo",
                item.IsActive ? tone : "danger",
                item.IsActive))
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
            "estatus-venta" => "estatus-venta",
            "estatus-cobro-grupos" => "estatus-cobro-grupos",
            _ => "catalogos"
        };
    }
}


