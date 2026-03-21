using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesCobrosGeo.Web.Data;
using SalesCobrosGeo.Web.Models.Administration;
using SalesCobrosGeo.Web.Security;
using SalesCobrosGeo.Web.Services.Rbac;
using SalesCobrosGeo.Web.Services.Users;
using System.Globalization;

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
    private readonly IRbacService _rbacService;
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<AdministrationController> _logger;

    public AdministrationController(
        IApplicationUserService userService, 
        IUserSessionTracker sessionTracker, 
        AppSecurityDbContext dbContext,
        IRbacService rbacService,
        IUserManagementService userManagementService,
        ILogger<AdministrationController> logger)
    {
        _userService = userService;
        _sessionTracker = sessionTracker;
        _dbContext = dbContext;
        _rbacService = rbacService;
        _userManagementService = userManagementService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Users(string? editUsername = null, bool create = false)
    {
        var userSummaries = _userService.GetUsers();
        var message = TempData["AdminSecurityMessage"] as string;
        var sessionSnapshots = _sessionTracker.GetSnapshots(userSummaries).ToArray();
        var editorUser = string.IsNullOrWhiteSpace(editUsername) ? null : _userService.GetUser(editUsername);
        
        // Cargar catálogos RBAC para el editor
        var rbacRoles = Array.Empty<RoleDto>();
        var rbacPermissions = Array.Empty<PermissionDto>();
        var userRoles = Array.Empty<UserRoleDto>();
        var userPermissions = Array.Empty<UserPermissionDto>();
        
        try
        {
            rbacRoles = (await _rbacService.GetAllRolesAsync()).ToArray();
            rbacPermissions = (await _rbacService.GetAllPermissionsAsync()).ToArray();
            
            if (!string.IsNullOrEmpty(editUsername))
            {
                userRoles = (await _rbacService.GetUserRolesAsync(editUsername)).ToArray();
                userPermissions = (await _rbacService.GetUserCustomPermissionsAsync(editUsername)).ToArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando catálogos RBAC");
        }
        
        var editor = await BuildEditorWithRbacAsync(editorUser, rbacRoles, rbacPermissions, userRoles, userPermissions);

        // Obtener información RBAC de todos los usuarios
        var userRbacInfo = new Dictionary<string, (int ActiveRoles, int TotalPermissions)>();
        try
        {
            var allUsers = await _userManagementService.GetAllUsersAsync();
            foreach (var user in allUsers)
            {
                userRbacInfo[user.UserName] = (user.ActiveRoleCount, user.CustomPermissionCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo información RBAC de usuarios");
        }

        var model = new AdministrationPageViewModel
        {
            Roles = BuildRoles(),
            Users = userSummaries.Select(user => 
            {
                var rbacInfo = userRbacInfo.GetValueOrDefault(user.Username);
                return new AdminUserCard(
                    user.DisplayName,
                    user.Username,
                    user.Role,
                    user.Zone,
                    user.IsActive ? "Activo" : "Inactivo",
                    user.RoleLabel,
                    user.Theme,
                    user.TwoFactorEnabled,
                    user.Permissions.Count,
                    rbacInfo.ActiveRoles,
                    rbacInfo.TotalPermissions
                );
            }).ToArray(),
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
            EditorWithRbac = editor,
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

    /// <summary>
    /// OBSOLETO: Usar SaveUserWithRbac en su lugar
    /// Mantenido temporalmente para evitar romper formularios legacy
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Obsolete("Usar SaveUserWithRbac que incluye gestión RBAC completa")]
    public IActionResult SaveUser(UserAdminInput input)
    {
        try
        {
            var saved = _userService.SaveUser(input);
            TempData["AdminSecurityMessage"] = $"⚠️ Usuario {saved.Username} guardado (método legacy). Usa el editor RBAC para gestión completa de permisos.";
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

    // ═══════════════════════════════════════════════════════════════════
    // GUARDADO UNIFICADO CON RBAC
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Guardar usuario con roles RBAC y permisos custom en un solo guardado
    /// Sistema RBAC puro - sin lógica legacy
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveUserWithRbac(UserWithRbacInput input)
    {
        try
        {
            _logger.LogInformation("SaveUserWithRbac called: Username={Username}, IsActive={IsActive}, RbacRoles={RolesCount}, CustomPermissions={PermissionsCount}", 
                input.Username, input.IsActive, input.RbacRoles.Count, input.CustomPermissions.Count);

            // 1. Guardar datos básicos del usuario (sin procesamiento legacy)
            var saved = _userService.SaveUser(input.ToBasicInput());

            // 2. Procesar roles RBAC
            if (input.RbacRoles.Any())
            {
                _logger.LogInformation("Processing {Count} RBAC roles for user {Username}", input.RbacRoles.Count, saved.Username);
                await ProcessUserRolesAsync(saved.Username, input.RbacRoles);
            }

            // 3. Procesar permisos custom
            if (input.CustomPermissions.Any())
            {
                _logger.LogInformation("Processing {Count} custom permissions for user {Username}", input.CustomPermissions.Count, saved.Username);
                await ProcessUserPermissionsAsync(saved.Username, input.CustomPermissions);
            }

            TempData["AdminSecurityMessage"] = $"✓ Usuario {saved.Username} guardado con {input.RbacRoles.Count} roles RBAC y {input.CustomPermissions.Count} permisos personalizados.";
            return RedirectToAction(nameof(Users), new { editUsername = saved.Username, create = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando usuario con RBAC {@Input}", input);
            TempData["AdminSecurityMessage"] = $"Error: {ex.Message}";
            return RedirectToAction(nameof(Users), new { editUsername = input.OriginalUsername ?? input.Username, create = false });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // PROCESAMIENTO DE ROLES Y PERMISOS
    // ═══════════════════════════════════════════════════════════════════

    private async Task ProcessUserRolesAsync(string username, List<UserRoleAssignmentInput> roles)
    {
        _logger.LogInformation("ProcessUserRolesAsync: Starting for user {Username} with {Count} roles", username, roles.Count);
        
        // Obtener roles actuales del usuario
        var currentRoles = (await _rbacService.GetUserRolesAsync(username)).ToList();
        _logger.LogInformation("Current user has {Count} roles assigned", currentRoles.Count);

        foreach (var roleInput in roles)
        {
            try
            {
                _logger.LogInformation("Attempting to assign RoleId={RoleId} to {Username}, StartDate={StartDate}, EndDate={EndDate}", 
                    roleInput.RoleId, username, roleInput.StartDate, roleInput.EndDate);
                
                var dto = new AssignUserRoleDto(
                    UserName: username,
                    RoleId: roleInput.RoleId,
                    StartDate: ParseOptionalDate(roleInput.StartDate) ?? DateTime.UtcNow,
                    EndDate: ParseOptionalDate(roleInput.EndDate),
                    IsActive: true
                );

                await _rbacService.AssignRoleToUserAsync(dto);
                _logger.LogInformation("Successfully assigned RoleId={RoleId} to {Username}", roleInput.RoleId, username);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error asignando rol {RoleId} a usuario {UserName}", roleInput.RoleId, username);
            }
        }
        
        _logger.LogInformation("ProcessUserRolesAsync: Completed for user {Username}", username);
    }

    private async Task ProcessUserPermissionsAsync(string username, List<UserPermissionGrantInput> permissions)
    {
        _logger.LogInformation("ProcessUserPermissionsAsync: Starting for user {Username} with {Count} permissions", username, permissions.Count);
        
        foreach (var permInput in permissions)
        {
            try
            {
                _logger.LogInformation("Attempting to grant PermissionId={PermissionId} to {Username}, IsGranted={IsGranted}, StartDate={StartDate}, EndDate={EndDate}", 
                    permInput.PermissionId, username, permInput.IsGranted, permInput.StartDate, permInput.EndDate);
                
                var dto = new GrantUserPermissionDto(
                    UserName: username,
                    PermissionId: permInput.PermissionId,
                    IsGranted: permInput.IsGranted,
                    StartDate: ParseOptionalDate(permInput.StartDate),
                    EndDate: ParseOptionalDate(permInput.EndDate)
                );

                await _rbacService.GrantCustomPermissionAsync(dto);
                _logger.LogInformation("Successfully granted PermissionId={PermissionId} to {Username}", permInput.PermissionId, username);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error otorgando permiso {PermissionId} a usuario {UserName}", permInput.PermissionId, username);
            }
        }
        
        _logger.LogInformation("ProcessUserPermissionsAsync: Completed for user {Username}", username);
    }

    private static DateTime? ParseOptionalDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        return null;
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

    /// <summary>
    /// Construir editor con catálogos RBAC incluidos
    /// </summary>
    private static async Task<UserWithRbacEditViewModel> BuildEditorWithRbacAsync(
        ApplicationUserSummary? user,
        IReadOnlyList<RoleDto> rbacRoles,
        IReadOnlyList<PermissionDto> rbacPermissions,
        IReadOnlyList<UserRoleDto> currentUserRoles,
        IReadOnlyList<UserPermissionDto> currentUserPermissions)
    {
        var basicInput = user is null
            ? new UserWithRbacInput
            {
                Theme = "sales",
                Role = AppRoles.Sales,
                RoleLabel = "Modulo ventas",
                IsActive = true,
                Permissions = [AppPermissions.DashboardView, AppPermissions.SalesView]
            }
            : new UserWithRbacInput
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
            };

        return new UserWithRbacEditViewModel(
            user is null ? "Alta de usuario" : $"Editar {user.DisplayName}",
            basicInput,
            AvailablePermissions,
            [AppRoles.Full, AppRoles.Sales, AppRoles.Collections],
            AvailableThemes,
            rbacRoles,
            rbacPermissions,
            currentUserRoles,
            currentUserPermissions
        );
    }

    // ═══════════════════════════════════════════════════════════════════
    // LEGACY BUILDERS (Mantenidos para compatibilidad, serán eliminados)
    // ═══════════════════════════════════════════════════════════════════

    [Obsolete("Sistema legacy - usar BuildEditorWithRbacAsync")]
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
                ["Dashboard", "Ventas", "Cobros", "Mantenimiento", "Usuarios"],
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
                ["Ventas", "Dashboard"],
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
                ["Cobros", "Dashboard"],
                [
                    new RolePermissionRow("Cobros", "Ver/registrar", "Importe, observacion, coordenadas, historial y estatus"),
                    new RolePermissionRow("Ventas", "Consulta limitada", "Cliente, zona, dia de cobro, importe y fotos base"),
                    new RolePermissionRow("Dashboard", "Cobranza", "Indicadores de cobro, ruta y atrasos")
                ])
        ];
    }
}
