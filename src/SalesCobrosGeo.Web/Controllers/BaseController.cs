using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SalesCobrosGeo.Web.Services.Catalogs;

namespace SalesCobrosGeo.Web.Controllers;

/// <summary>
/// Controlador base que proporciona catálogos dinámicos a las vistas
/// </summary>
public abstract class BaseController : Controller
{
    private readonly ICatalogViewService? _catalogViewService;

    protected BaseController(ICatalogViewService catalogViewService)
    {
        _catalogViewService = catalogViewService;
    }

    protected BaseController()
    {
        // Constructor sin parámetros para controladores que no necesitan catálogos
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_catalogViewService != null)
        {
            try
            {
                // Cargar catálogos y ponerlos en ViewBag para todas las vistas
                var saleStatuses = await _catalogViewService.GetAllSaleStatusesAsync();
                var collectionStatuses = await _catalogViewService.GetAllCollectionStatusesAsync();

                ViewBag.SaleStatuses = saleStatuses;
                ViewBag.CollectionStatuses = collectionStatuses;
            }
            catch
            {
                // Log error pero no fallar la request
                // El controlador puede seguir funcionando sin catálogos
                ViewBag.SaleStatuses = new Dictionary<string, (string, string)>();
                ViewBag.CollectionStatuses = new Dictionary<string, (string, string, string?)>();
            }
        }

        await base.OnActionExecutionAsync(context, next);
    }
}
