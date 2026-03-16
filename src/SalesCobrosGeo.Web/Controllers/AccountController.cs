using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Security;
using SalesCobrosGeo.Web.Security;

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
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLanding(User);
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

        var userSummary = _userService.GetUsers()
            .First(x => x.Username.Equals(input.Username, StringComparison.OrdinalIgnoreCase));

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

        if (!string.IsNullOrWhiteSpace(input.ReturnUrl) && Url.IsLocalUrl(input.ReturnUrl))
        {
            return Redirect(input.ReturnUrl);
        }

        return RedirectToLanding(sessionPrincipal);
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

    private IActionResult RedirectToLanding(System.Security.Claims.ClaimsPrincipal principal)
    {
        if (principal.HasPermission(AppPermissions.AdministrationView) || principal.HasPermission(AppPermissions.DashboardView))
        {
            if (principal.HasPermission(AppPermissions.AdministrationView))
            {
                return RedirectToAction("Index", "Dashboard");
            }

            if (principal.HasPermission(AppPermissions.SalesView) && !principal.HasPermission(AppPermissions.CollectionsView))
            {
                return RedirectToAction("Index", "Sales");
            }

            if (principal.HasPermission(AppPermissions.CollectionsView) && !principal.HasPermission(AppPermissions.SalesView))
            {
                return RedirectToAction("Index", "Cobros");
            }

            return RedirectToAction("Index", "Dashboard");
        }

        return RedirectToAction(nameof(Login));
    }
}
