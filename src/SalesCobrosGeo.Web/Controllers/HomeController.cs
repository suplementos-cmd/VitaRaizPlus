using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models;
using SalesCobrosGeo.Web.Security;

namespace SalesCobrosGeo.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.HasPermission(AppPermissions.AdministrationView))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (User.HasPermission(AppPermissions.SalesView) && !User.HasPermission(AppPermissions.CollectionsView))
        {
            return RedirectToAction("Index", "Sales");
        }

        if (User.HasPermission(AppPermissions.CollectionsView) && !User.HasPermission(AppPermissions.SalesView))
        {
            return RedirectToAction("Index", "Cobros");
        }

        if (User.HasPermission(AppPermissions.DashboardView))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return RedirectToAction("Login", "Account");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
