using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Maintenance;
using SalesCobrosGeo.Web.Security;
using SalesCobrosGeo.Web.Services.Sales;

namespace SalesCobrosGeo.Web.Controllers;

[Authorize(Policy = AppPolicies.MaintenanceAccess)]
public sealed class MaintenanceController : Controller
{
    private static readonly string[] EditableSections = ["zonas", "dias-cobro", "formas-pago", "productos", "estatus-venta", "estatus-cobro-grupos", "menu-items", "tipos-catalogo", "config-ui"];

    private readonly ISalesRepository _repository;
    private readonly IApplicationUserService _userService;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(ISalesRepository repository, IApplicationUserService userService, ILogger<MaintenanceController> logger)
    {
        _repository = repository;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index(string section = "catalogos", long? viewId = null, long? editId = null, bool create = false)
    {
        var model = BuildViewModel(section, viewId, editId, create, TempData["MaintenanceMessage"] as string);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save(MaintenanceCatalogSaveInput input, IFormCollection form)
    {
        try
        {
            // Extraer todos los campos del formulario dinámicamente
            input.Fields = new Dictionary<string, object?>();
            foreach (var key in form.Keys.Where(k => k.StartsWith("Field_")))
            {
                var fieldName = key.Substring(6); // Remove "Field_" prefix
                var value = form[key].ToString();
                
                // Intentar convertir a tipos apropiados
                if (bool.TryParse(value, out var boolValue))
                {
                    input.Fields[fieldName] = boolValue;
                }
                else if (int.TryParse(value, out var intValue))
                {
                    input.Fields[fieldName] = intValue;
                }
                else if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var decValue))
                {
                    input.Fields[fieldName] = decValue;
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    input.Fields[fieldName] = null;
                }
                else
                {
                    input.Fields[fieldName] = value;
                }
            }
            
            // Log para depuración
            _logger.LogInformation("Guardando {Section} (ID:{Id}): {FieldCount} campos - {Fields}", 
                input.Section, 
                input.Id,
                input.Fields.Count, 
                string.Join(", ", input.Fields.Select(f => $"{f.Key}={f.Value}")));
            
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
            Fields = record.AllFields.Where(f => f.Key != "Id").ToDictionary(f => f.Key, f => f.Value)
        });

        TempData["MaintenanceMessage"] = isActive
            ? "Registro habilitado correctamente."
            : "Registro inhabilitado correctamente.";

        return RedirectToAction(nameof(Index), new { section });
    }

    private MaintenancePageViewModel BuildViewModel(string section, long? viewId, long? editId, bool create, string? message)
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
            Fields = editorRecord?.AllFields.Where(f => f.Key != "Id").ToDictionary(f => f.Key, f => f.Value) 
                ?? (create ? GetDefaultFieldsForSection(selected, currentCatalogItems) : new Dictionary<string, object?>())
        };

        var showEditor = EditableSections.Contains(selected, StringComparer.OrdinalIgnoreCase) && (create || editId is > 0);
        
        // Log para depuración
        if (showEditor)
        {
            _logger.LogInformation("Editor para {Section} (ID:{EditorId}, Create:{Create}): {FieldCount} campos - {Fields}", 
                selected, 
                editId,
                create,
                editor.Fields.Count, 
                string.Join(", ", editor.Fields.Keys));
        }
        // When editing, suppress viewId so the detail panel doesn't also try to render
        var resolvedViewId = showEditor ? null : viewId;

        return new MaintenancePageViewModel(
            selected,
            stats,
            sections,
            editor,
            showEditor,
            resolvedViewId,
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
        var sellers = BuildUserSection("vendedores", "Vendedores", "Equipo comercial disponible para asignacion de ventas.", "(Los vendedores se gestionan desde Administracion > Usuarios)");
        var collectors = BuildUserSection("cobradores", "Cobradores", "Equipo de cobranza asignable a cartera y ruta.", "(Los cobradores se gestionan desde Administracion > Usuarios)");
        var saleStatuses = BuildCatalogSection("estatus-venta", "Estatus venta", "Estados disponibles para capturar y editar ventas.", "Controla los estados que se veran en el formulario de ventas.", "Venta", "warning");
        var collectionGroups = BuildCatalogSection("estatus-cobro-grupos", "Estatus cobro", "Grupos operativos visibles en cobros movil y arbol de cartera.", "Puedes activar o renombrar los grupos operativos del arbol de cobros.", "Cobro", "brand");
        var menuItems = BuildCatalogSection("menu-items", "Menu items", "Configuracion de items del menu de navegacion.", "Define los items visibles en el menu principal.", "Menu", "brand");
        var catalogTypes = BuildCatalogSection("tipos-catalogo", "Tipos catalogos", "Tipos de catalogos disponibles en el sistema.", "Clasificacion y metadatos de catalogos.", "Tipo", "success");
        var uiSettings = BuildCatalogSection("config-ui", "Config UI", "Configuraciones de interfaz y comportamiento visual.", "Ajustes de la experiencia de usuario.", "Config", "warning");

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
                new MaintenanceItem(9, "MENU", "Menu items", $"{menuItems.Items.Count} items", "Sistema", "brand", true),
                new MaintenanceItem(10, "TIPO", "Tipos catalogo", $"{catalogTypes.Items.Count} tipos", "Sistema", "success", true),
                new MaintenanceItem(11, "UI", "Config UI", $"{uiSettings.Items.Count} configs", "Sistema", "warning", true),
                new MaintenanceItem(12, "EMP", "Empleados", $"{employees.Items.Count} accesos", "Usuarios", "danger", true)
            ]);

        return [summary, sellers, collectors, saleStatuses, collectionGroups, employees, zones, days, paymentMethods, products, menuItems, catalogTypes, uiSettings];
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

    private MaintenanceSection BuildUserSection(string key, string title, string subtitle, string summary)
    {
        var users = _repository.GetMaintenanceCatalog(key);
        var items = users
            .Select(user => new MaintenanceItem(
                user.Id,
                user.Code,
                user.Name,
                user.IsActive ? "Usuario activo en el sistema" : "Usuario inactivo",
                user.IsActive ? "Activo" : "Inactivo",
                user.IsActive ? "success" : "danger",
                user.IsActive))
            .ToArray();

        return new MaintenanceSection(key, title, subtitle, summary, items);
    }

    private static Dictionary<string, object?> GetDefaultFieldsForSection(string section, IReadOnlyList<MaintenanceCatalogRecord> existingItems)
    {
        // Si hay items existentes, usar sus columnas como template
        if (existingItems.Count > 0)
        {
            var template = new Dictionary<string, object?>();
            foreach (var field in existingItems[0].AllFields.Where(f => f.Key != "Id"))
            {
                template[field.Key] = field.Value switch
                {
                    bool => true,
                    int or long => 0,
                    decimal or double or float => 0m,
                    _ => string.Empty
                };
            }
            return template;
        }

        // Templates específicos por sección cuando no hay datos
        return section switch
        {
            "vendedores" or "cobradores" => new Dictionary<string, object?>
            {
                ["UserName"] = string.Empty,
                ["DisplayName"] = string.Empty,
                ["Password"] = string.Empty,
                ["Role"] = section == "vendedores" ? "Vendedor" : "Cobrador",
                ["RoleLabel"] = section == "vendedores" ? "Vendedor" : "Cobrador",
                ["Zone"] = "Default",
                ["Theme"] = "root",
                ["IsActive"] = true,
                ["TwoFactorEnabled"] = false,
                ["TwoFactorSecret"] = string.Empty
            },
            "productos" => new Dictionary<string, object?>
            {
                ["Code"] = string.Empty,
                ["Name"] = string.Empty,
                ["Price"] = 0m,
                ["IsActive"] = true
            },
            "dias-cobro" => new Dictionary<string, object?>
            {
                ["Code"] = string.Empty,
                ["Name"] = string.Empty,
                ["ShortCode"] = string.Empty,
                ["SortOrder"] = 0,
                ["IsActive"] = true
            },
            "estatus-venta" => new Dictionary<string, object?>
            {
                ["Code"] = string.Empty,
                ["Name"] = string.Empty,
                ["ColorClass"] = "bg-secondary",
                ["IconSvg"] = (string?)null,
                ["SortOrder"] = 0,
                ["IsActive"] = true,
                ["IsFinal"] = false
            },
            "estatus-cobro-grupos" => new Dictionary<string, object?>
            {
                ["Code"] = string.Empty,
                ["Name"] = string.Empty,
                ["ColorClass"] = "secondary",
                ["Priority"] = 0,
                ["IsActive"] = true
            },
            "menu-items" => new Dictionary<string, object?>
            {
                ["Code"] = string.Empty,
                ["Label"] = string.Empty,
                ["IconSvg"] = string.Empty,
                ["Controller"] = string.Empty,
                ["Action"] = string.Empty,
                ["RequiredPolicy"] = (string?)null,
                ["SortOrder"] = 0,
                ["IsActive"] = true,
                ["ParentId"] = (int?)null,
                ["Platform"] = "Web"
            },
            "tipos-catalogo" => new Dictionary<string, object?>
            {
                ["Code"] = string.Empty,
                ["Name"] = string.Empty,
                ["Description"] = string.Empty,
                ["IconClass"] = string.Empty,
                ["Category"] = string.Empty,
                ["SortOrder"] = 0,
                ["IsActive"] = true
            },
            "config-ui" => new Dictionary<string, object?>
            {
                ["Key"] = string.Empty,
                ["Value"] = string.Empty,
                ["Description"] = string.Empty,
                ["Category"] = "General",
                ["IsActive"] = true
            },
            // Template genérico para zonas, formas-pago
            _ => new Dictionary<string, object?>
            {
                ["Code"] = string.Empty,
                ["Name"] = string.Empty,
                ["IsActive"] = true
            }
        };
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
            "menu-items" => "menu-items",
            "tipos-catalogo" => "tipos-catalogo",
            "config-ui" => "config-ui",
            _ => "catalogos"
        };
    }
}


