using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Security;
using SalesCobrosGeo.Web.Security;
using System.Security.Claims;

namespace SalesCobrosGeo.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly IApplicationUserService _userService;
    private readonly IUserSessionTracker _sessionTracker;

    public AccountController(IApplicationUserService userService, IUserSessionTracker sessionTracker)
    {
        _userService = userService;
        _sessionTracker = sessionTracker;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var landing = GetLandingRoute(User);
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

        var principal = _userService.ValidateCredentials(input.Username, input.Password);
        if (principal is null)
        {
            ModelState.AddModelError(string.Empty, "Usuario, contrasena invalida o cuenta inactiva.");
            return View(input);
        }

        // Crear userSummary desde los claims del principal retornado por la API
        var fullName = principal.FindFirst(ClaimTypes.GivenName)?.Value ?? input.Username;
        var webRole = principal.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown"; // FULL, SALES, etc.
        var displayRole = principal.FindFirst(AppClaimTypes.DisplayRole)?.Value ?? webRole; // Administrador, Vendedor, etc.
        var permissions = principal.FindAll(AppClaimTypes.Permission).Select(c => c.Value).ToArray();
        
        // DEBUG: Log de permisos
        Console.WriteLine($"[LOGIN DEBUG] Usuario: {input.Username}");
        Console.WriteLine($"[LOGIN DEBUG] WebRole: {webRole}");
        Console.WriteLine($"[LOGIN DEBUG] DisplayRole: {displayRole}");
        Console.WriteLine($"[LOGIN DEBUG] Permisos: {string.Join(", ", permissions)}");
        Console.WriteLine($"[LOGIN DEBUG] IsInRole(FULL): {principal.IsInRole(AppRoles.Full)}");
        Console.WriteLine($"[LOGIN DEBUG] HasPermission(DashboardView): {principal.HasPermission(AppPermissions.DashboardView)}");
        Console.WriteLine($"[LOGIN DEBUG] HasPermission(AdministrationView): {principal.HasPermission(AppPermissions.AdministrationView)}");
        
        var userSummary = new ApplicationUserSummary(
            Username: input.Username,
            DisplayName: fullName,
            Role: webRole,
            RoleLabel: displayRole,
            Zone: "Default",
            Theme: "default",
            IsActive: true,
            TwoFactorEnabled: false,
            Permissions: permissions
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

        var landingRoute = GetLandingRoute(sessionPrincipal);
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

    private static (string Controller, string Action)? GetLandingRoute(System.Security.Claims.ClaimsPrincipal principal)
    {
        if (principal.HasPermission(AppPermissions.AdministrationView))
        {
            return ("Dashboard", "Index");
        }

        if (principal.HasPermission(AppPermissions.SalesView) && !principal.HasPermission(AppPermissions.CollectionsView))
        {
            return ("Sales", "Index");
        }

        if (principal.HasPermission(AppPermissions.CollectionsView) && !principal.HasPermission(AppPermissions.SalesView))
        {
            return ("Cobros", "Index");
        }

        if (principal.HasPermission(AppPermissions.DashboardView))
        {
            return ("Dashboard", "Index");
        }

        return null;
    }
}
