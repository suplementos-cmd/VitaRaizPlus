using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Security;
using SalesCobrosGeo.Web.Security;
using SalesCobrosGeo.Web.Services.Rbac;
using System.Security.Claims;

namespace SalesCobrosGeo.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly IApplicationUserService _userService;
    private readonly IUserSessionTracker _sessionTracker;
    private readonly IRbacService _rbacService;

    public AccountController(
        IApplicationUserService userService, 
        IUserSessionTracker sessionTracker,
        IRbacService rbacService)
    {
        _userService = userService;
        _sessionTracker = sessionTracker;
        _rbacService = rbacService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var landing = await GetLandingRouteAsync(User);
            if (landing is not null)
            {
                return RedirectToAction(landing.Value.Action, landing.Value.Controller);
            }

            _sessionTracker.SignOut(User);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["AppToastMessage"] = "Tu sesion no tiene un modulo valido asignado. Vuelve a ingresar.";
            TempData["AppToastTone"] = "warning";
        }

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl,
            Hints = _userService.GetLoginHints()
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel input)
    {
        input.Hints = _userService.GetLoginHints();

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        ClaimsPrincipal? principal = null;
        try
        {
            principal = _userService.ValidateCredentials(input.Username, input.Password);
        }
        catch (HttpRequestException ex)
        {
            ModelState.AddModelError(string.Empty, 
                $"Error de conexión con el servidor API. Verifica que la API esté corriendo en http://localhost:5207. Detalles: {ex.Message}");
            return View(input);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, 
                $"Error inesperado al validar credenciales: {ex.Message}");
            return View(input);
        }

        if (principal is null)
        {
            ModelState.AddModelError(string.Empty, "Usuario, contraseña inválida o cuenta inactiva.");
            return View(input);
        }

        // Crear userSummary desde los claims del principal retornado por la API
        var fullName = principal.FindFirst(ClaimTypes.GivenName)?.Value ?? input.Username;
        var displayRole = principal.FindFirst(AppClaimTypes.DisplayRole)?.Value ?? "Usuario";
        var userTheme = principal.FindFirst(AppClaimTypes.Theme)?.Value ?? "root";
        var userZone = principal.FindFirst("Zone")?.Value ?? "Default";
        
        var userSummary = new ApplicationUserSummary(
            Username: input.Username,
            DisplayName: fullName,
            Role: "SALES", // Compatibilidad legacy
            RoleLabel: displayRole,
            Zone: userZone,
            Theme: userTheme,
            IsActive: true,
            TwoFactorEnabled: false,
            Permissions: Array.Empty<string>() // Permisos vienen de RBAC
        );

        var sessionPrincipal = _sessionTracker.AttachSession(principal, userSummary, HttpContext);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            sessionPrincipal,
            new AuthenticationProperties
            {
                IsPersistent = input.RememberMe,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(input.RememberMe ? 12 : 8)
            });

        var landingRoute = await GetLandingRouteAsync(sessionPrincipal);
        if (landingRoute is null)
        {
            _sessionTracker.SignOut(sessionPrincipal);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ModelState.AddModelError(string.Empty, "Tu usuario no tiene un modulo asignado para entrar al sistema.");
            return View(input);
        }

        if (!string.IsNullOrWhiteSpace(input.ReturnUrl) && Url.IsLocalUrl(input.ReturnUrl))
        {
            return Redirect(input.ReturnUrl);
        }

        return RedirectToAction(landingRoute.Value.Action, landingRoute.Value.Controller);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        _sessionTracker.SignOut(User);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpPost]
    public IActionResult Heartbeat([FromBody] HeartbeatInput input)
    {
        _sessionTracker.UpdateCoordinates(User.Identity?.Name ?? string.Empty, input.Coordinates, "Heartbeat web");
        return Json(new { ok = true });
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// Página de diagnóstico de permisos (solo para debugging)
    /// NOTA: Usa métodos obsoletos para mostrar estado legacy
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> DiagnosticPermissions()
    {
        var userName = User.Identity?.Name ?? "N/A";
        
        // Obtener permisos RBAC efectivos
        var rbacPermissions = new List<string>();
        try
        {
            if (!string.IsNullOrEmpty(userName) && userName != "N/A")
            {
                var summary = await _rbacService.GetUserEffectivePermissionsAsync(userName);
                rbacPermissions = summary.EffectivePermissions.ToList();
            }
        }
        catch (Exception ex)
        {
            rbacPermissions.Add($"ERROR: {ex.Message}");
        }

        var diagnosticInfo = new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            Username = userName,
            DisplayName = User.GetDisplayName(),
            DisplayRole = User.GetDisplayRole(),
            Theme = User.GetTheme(),
            
            // Permisos RBAC (Sistema actual)
            RbacPermissions = rbacPermissions,
            RbacPermissionCount = rbacPermissions.Count,
            
            // Claims actuales
            AllClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        };

        return Json(diagnosticInfo);
    }

    /// <summary>
    /// Determina la ruta de aterrizaje según permisos RBAC del usuario
    /// </summary>
    private async Task<(string Controller, string Action)?> GetLandingRouteAsync(ClaimsPrincipal principal)
    {
        var userName = principal.Identity?.Name;
        if (string.IsNullOrEmpty(userName))
        {
            return null;
        }

        try
        {
            // Verificar permisos RBAC en orden de prioridad
            if (await _rbacService.HasPermissionAsync(userName, AppPermissions.AdministrationView))
            {
                return ("Dashboard", "Index");
            }

            var hasSales = await _rbacService.HasPermissionAsync(userName, AppPermissions.SalesView);
            var hasCollections = await _rbacService.HasPermissionAsync(userName, AppPermissions.CollectionsView);

            if (hasSales && !hasCollections)
            {
                return ("Sales", "Index");
            }

            if (hasCollections && !hasSales)
            {
                return ("Cobros", "Index");
            }

            if (await _rbacService.HasPermissionAsync(userName, AppPermissions.DashboardView))
            {
                return ("Dashboard", "Index");
            }

            return null;
        }
        catch (Exception ex)
        {
            // En caso de error al consultar RBAC, log y retornar null
            Console.WriteLine($"Error al determinar landing route para {userName}: {ex.Message}");
            return null;
        }
    }
}
