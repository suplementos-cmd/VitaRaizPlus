using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Administration;
using SalesCobrosGeo.Web.Security;

namespace SalesCobrosGeo.Web.Controllers;

[Authorize(Policy = AppPolicies.AdministrationAccess)]
public sealed class AdministrationController : Controller
{
    private readonly IApplicationUserService _userService;
    private readonly IUserSessionTracker _sessionTracker;

    public AdministrationController(IApplicationUserService userService, IUserSessionTracker sessionTracker)
    {
        _userService = userService;
        _sessionTracker = sessionTracker;
    }

    [HttpGet]
    public IActionResult Users()
    {
        var roles = new[]
        {
            new RoleProfileCard(
                AppRoles.Full,
                "Acceso total",
                "Control completo del sistema, seguridad, mantenimiento, ventas, cobros y dashboards.",
                new RoleTheme("Root", "#24334f", "#6da7ff", "#eef4ff"),
                ["Inicio", "Dashboard", "Ventas", "Cobros", "Mantenimiento", "Usuarios"],
                [
                    new RolePermissionRow("Seguridad", "Full", "Usuarios, perfiles, cookies, sesiones y auditoria base"),
                    new RolePermissionRow("Ventas", "Full", "Todos los campos de venta y catalogos"),
                    new RolePermissionRow("Cobros", "Full", "Toda la cartera, estados, abonos y detalle")
                ]),
            new RoleProfileCard(
                AppRoles.Sales,
                "Modulo ventas",
                "Usuario comercial con acceso al registro, consulta y edicion operativa de ventas.",
                new RoleTheme("Ventas", "#2c74d8", "#7bb3ff", "#edf5ff"),
                ["Inicio", "Dashboard", "Ventas"],
                [
                    new RolePermissionRow("Ventas", "Ver/crear/editar", "Cliente, producto, fotos, zona, coordenadas, forma de pago"),
                    new RolePermissionRow("Cobros", "Sin acceso", "No registra cobros ni entra a cartera"),
                    new RolePermissionRow("Dashboard", "Comercial", "Solo indicadores de ventas")
                ]),
            new RoleProfileCard(
                AppRoles.Collections,
                "Modulo cobros",
                "Usuario de ruta con acceso a cartera, detalle de venta y registro de cobros.",
                new RoleTheme("Cobros", "#a44b24", "#f2a86c", "#fff4ea"),
                ["Inicio", "Dashboard", "Cobros"],
                [
                    new RolePermissionRow("Cobros", "Ver/registrar", "Importe, observacion, coordenadas, historial y estatus"),
                    new RolePermissionRow("Ventas", "Consulta limitada", "Cliente, zona, dia de cobro, importe, fotos base"),
                    new RolePermissionRow("Dashboard", "Cobranza", "Indicadores de cobro, ruta y atrasos")
                ])
        };

        var userSummaries = _userService.GetUsers();

        var users = userSummaries
            .Select(user => new AdminUserCard(
                user.DisplayName,
                user.Username,
                user.Role,
                user.Zone,
                user.IsActive ? "Activo" : "Inactivo",
                user.RoleLabel))
            .ToArray();

        var sessions = _sessionTracker.GetSnapshots(userSummaries)
            .Select(session => new AdminSessionCard(
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
                session.LastLocationSource))
            .ToArray();

        var auditTrail = _sessionTracker.GetAuditTrail()
            .Select(entry => new AdminAuditCard(
                entry.Timestamp,
                entry.EventType,
                entry.Username,
                entry.Description,
                entry.Path,
                entry.Coordinates))
            .ToArray();

        return View(new AdministrationPageViewModel(roles, users, sessions, auditTrail));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetUserStatus(string username, bool isActive)
    {
        if (_userService.SetActive(username, isActive))
        {
            _sessionTracker.SetUserActive(username, isActive);
            if (!isActive)
            {
                TempData["AdminSecurityMessage"] = $"{username} fue desactivado y cualquier sesion abierta quedo invalidada.";
            }
            else
            {
                TempData["AdminSecurityMessage"] = $"{username} vuelve a estar activo para ingresar.";
            }
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
}
