using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesCobrosGeo.Web.Data;
using SalesCobrosGeo.Web.Models.Administration;
using SalesCobrosGeo.Web.Security;

namespace SalesCobrosGeo.Web.Controllers;

[Authorize(Policy = AppPolicies.AdministrationAccess)]
public sealed class AdministrationController : Controller
{
    private const int AuditPageSize = 12;

    private static readonly string[] AvailablePermissions =
    [
        AppPermissions.DashboardView,
        AppPermissions.SalesView,
        AppPermissions.CollectionsView,
        AppPermissions.MaintenanceView,
        AppPermissions.AdministrationView
    ];

    private static readonly string[] AvailableThemes = ["root", "sales", "forest", "sunset", "collections", "graphite"];

    private readonly IApplicationUserService _userService;
    private readonly IUserSessionTracker _sessionTracker;
    private readonly AppSecurityDbContext _dbContext;

    public AdministrationController(IApplicationUserService userService, IUserSessionTracker sessionTracker, AppSecurityDbContext dbContext)
    {
        _userService = userService;
        _sessionTracker = sessionTracker;
        _dbContext = dbContext;
    }

    [HttpGet]
    public IActionResult Users(string? editUsername = null, bool create = false)
    {
        var userSummaries = _userService.GetUsers();
        var message = TempData["AdminSecurityMessage"] as string;
        var sessionSnapshots = _sessionTracker.GetSnapshots(userSummaries).ToArray();
        var editorUser = string.IsNullOrWhiteSpace(editUsername) ? null : _userService.GetUser(editUsername);
        var editor = BuildEditor(editorUser);

        var model = new AdministrationPageViewModel
        {
            Roles = BuildRoles(),
            Users = userSummaries.Select(user => new AdminUserCard(
                user.DisplayName,
                user.Username,
                user.Role,
                user.Zone,
                user.IsActive ? "Activo" : "Inactivo",
                user.RoleLabel,
                user.Theme,
                user.TwoFactorEnabled,
                user.Permissions.Count)).ToArray(),
            Sessions = sessionSnapshots.Select(session => new AdminSessionCard(
                session.Username,
                session.DisplayName,
                session.RoleLabel,
                session.Zone,
                session.IsActive,
                session.IsConnected,
                session.LastSeenLabel,
                session.LastPath,
                session.LastIp,
                session.LastUserAgent,
                session.LastCoordinates,
                session.LastLocationSource)).ToArray(),
            Editor = editor,
            ShowEditor = create || !string.IsNullOrWhiteSpace(editUsername),
            SummaryCards =
            [
                new AdminSummaryCard("Usuarios", userSummaries.Count.ToString(), "neutral", "Configurados en el sistema"),
                new AdminSummaryCard("Activos", userSummaries.Count(x => x.IsActive).ToString(), "success", "Con acceso habilitado"),
                new AdminSummaryCard("Sesiones", sessionSnapshots.Count(x => x.IsConnected).ToString(), "info", "Conectadas en este momento"),
                new AdminSummaryCard("2FA", userSummaries.Count(x => x.TwoFactorEnabled).ToString(), "warning", "Doble factor habilitado")
            ],
            AuditTotal = _dbContext.AuditLogs.Count(),
            Message = message
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Audit(int page = 1)
    {
        var auditTotal = _dbContext.AuditLogs.Count();
        var totalPages = Math.Max(1, (int)Math.Ceiling(auditTotal / (double)AuditPageSize));
        page = Math.Clamp(page, 1, totalPages);

        var model = new AuditListPageViewModel
        {
            AuditPage = page,
            TotalAuditPages = totalPages,
            TotalEvents = auditTotal,
            AuditTrail = _dbContext.AuditLogs
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedUtc)
                .Skip((page - 1) * AuditPageSize)
                .Take(AuditPageSize)
                .Select(x => new AdminAuditCard(
                    x.Id,
                    x.CreatedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    x.EventType,
                    x.Username,
                    x.Description,
                    x.Path,
                    x.Coordinates))
                .ToArray()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveUser(UserAdminInput input)
    {
        try
        {
            ApplyRoleDefaults(input);
            var saved = _userService.SaveUser(input);
            TempData["AdminSecurityMessage"] = $"Usuario {saved.Username} guardado correctamente.";
            return RedirectToAction(nameof(Users), new { editUsername = saved.Username, create = false });
        }
        catch (InvalidOperationException ex)
        {
            TempData["AdminSecurityMessage"] = ex.Message;
            return RedirectToAction(nameof(Users), new { editUsername = input.OriginalUsername ?? input.Username, create = false });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetPassword(string username, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            TempData["AdminSecurityMessage"] = "La nueva contrasena debe tener al menos 8 caracteres.";
            return RedirectToAction(nameof(Users), new { editUsername = username, create = false });
        }

        TempData["AdminSecurityMessage"] = _userService.ResetPassword(username, newPassword)
            ? $"Contrasena reiniciada para {username}."
            : "No se pudo reiniciar la contrasena.";

        return RedirectToAction(nameof(Users), new { editUsername = username, create = false });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetUserStatus(string username, bool isActive)
    {
        if (username.Equals("RaizAdmin", StringComparison.OrdinalIgnoreCase))
        {
            TempData["AdminSecurityMessage"] = "RaizAdmin es el administrador principal y siempre debe permanecer activo.";
            return RedirectToAction(nameof(Users));
        }

        if (_userService.SetActive(username, isActive))
        {
            _sessionTracker.SetUserActive(username, isActive);
            TempData["AdminSecurityMessage"] = isActive
                ? $"{username} vuelve a estar activo para ingresar."
                : $"{username} fue desactivado y cualquier sesion abierta quedo invalidada.";
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForceLogout(string username)
    {
        _sessionTracker.ForceLogout(username);
        TempData["AdminSecurityMessage"] = $"Se forzo el cierre de sesion de {username}.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public IActionResult AuditDetail(long id)
    {
        var entry = _dbContext.AuditLogs.AsNoTracking().FirstOrDefault(x => x.Id == id);
        if (entry is null)
        {
            return NotFound();
        }

        return View(new AuditDetailViewModel(
            entry.Id,
            entry.CreatedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"),
            entry.EventType,
            entry.Username,
            entry.Description,
            entry.Path,
            entry.Coordinates,
            string.IsNullOrWhiteSpace(entry.Metadata) ? "-" : entry.Metadata));
    }

    private static void ApplyRoleDefaults(UserAdminInput input)
    {
        input.Role = string.IsNullOrWhiteSpace(input.Role) ? AppRoles.Sales : input.Role.ToUpperInvariant();
        input.RoleLabel = input.Role switch
        {
            AppRoles.Full => "Acceso total",
            AppRoles.Collections => "Modulo cobros",
            _ => "Modulo ventas"
        };

        if (string.IsNullOrWhiteSpace(input.Theme))
        {
            input.Theme = input.Role switch
            {
                AppRoles.Full => "root",
                AppRoles.Collections => "collections",
                _ => "sales"
            };
        }

        if (input.Permissions.Count > 0)
        {
            return;
        }

        input.Permissions = input.Role switch
        {
            AppRoles.Full => [.. AvailablePermissions],
            AppRoles.Collections => [AppPermissions.DashboardView, AppPermissions.CollectionsView],
            _ => [AppPermissions.DashboardView, AppPermissions.SalesView]
        };
    }

    private static UserEditViewModel BuildEditor(ApplicationUserSummary? user)
    {
        return new UserEditViewModel(
            user is null ? "Alta de usuario" : $"Editar {user.DisplayName}",
            user is null
                ? new UserAdminInput
                {
                    Theme = "sales",
                    Role = AppRoles.Sales,
                    RoleLabel = "Modulo ventas",
                    IsActive = true,
                    Permissions = [AppPermissions.DashboardView, AppPermissions.SalesView]
                }
                : new UserAdminInput
                {
                    OriginalUsername = user.Username,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Zone = user.Zone,
                    Theme = user.Theme,
                    Role = user.Role,
                    RoleLabel = user.RoleLabel,
                    IsActive = user.IsActive,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    Permissions = [.. user.Permissions]
                },
            AvailablePermissions,
            [AppRoles.Full, AppRoles.Sales, AppRoles.Collections],
            AvailableThemes);
    }

    private static IReadOnlyList<RoleProfileCard> BuildRoles()
    {
        return
        [
            new RoleProfileCard(
                AppRoles.Full,
                "Acceso total",
                "Control completo del sistema, seguridad, mantenimiento, ventas, cobros y dashboards.",
                new RoleTheme("Azul ejecutivo", "#203a72", "#5f8cff", "#eef3ff"),
                ["Inicio", "Dashboard", "Ventas", "Cobros", "Mantenimiento", "Usuarios"],
                [
                    new RolePermissionRow("Seguridad", "Full", "Usuarios, sesiones, bitacora, 2FA y configuracion sensible"),
                    new RolePermissionRow("Ventas", "Full", "Todos los campos de venta y sus catalogos"),
                    new RolePermissionRow("Cobros", "Full", "Cartera, ruta, abonos y geolocalizacion")
                ]),
            new RoleProfileCard(
                AppRoles.Sales,
                "Modulo ventas",
                "Usuario comercial con acceso al registro, consulta y edicion operativa de ventas.",
                new RoleTheme("Azul claro", "#2f7dff", "#8bc2ff", "#eff7ff"),
                ["Inicio", "Dashboard", "Ventas"],
                [
                    new RolePermissionRow("Ventas", "Ver/crear/editar", "Cliente, producto, fotos, zona, coordenadas y forma de pago"),
                    new RolePermissionRow("Cobros", "Sin acceso", "No registra cobros ni entra a cartera"),
                    new RolePermissionRow("Dashboard", "Comercial", "Solo indicadores de ventas")
                ]),
            new RoleProfileCard(
                AppRoles.Collections,
                "Modulo cobros",
                "Usuario de ruta con acceso a cartera, detalle de venta y registro de cobros.",
                new RoleTheme("Morado ejecutivo", "#6d4fd5", "#b39afc", "#f3efff"),
                ["Inicio", "Dashboard", "Cobros"],
                [
                    new RolePermissionRow("Cobros", "Ver/registrar", "Importe, observacion, coordenadas, historial y estatus"),
                    new RolePermissionRow("Ventas", "Consulta limitada", "Cliente, zona, dia de cobro, importe y fotos base"),
                    new RolePermissionRow("Dashboard", "Cobranza", "Indicadores de cobro, ruta y atrasos")
                ])
        ];
    }
}
