using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Business;
using SalesCobrosGeo.Api.Contracts.Catalogs;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[Route("api/catalogs")]
[Authorize]
public sealed class CatalogsController : ApiControllerBase
{
    private readonly IBusinessStore _store;

    public CatalogsController(IBusinessStore store)
    {
        _store = store;
    }

    [HttpGet("snapshot")]
    public IActionResult GetSnapshot()
        => Ok(_store.GetCatalogSnapshot());

    [HttpGet("zones")]
    public IActionResult GetZones([FromQuery] bool includeInactive = false)
        => Ok(_store.GetZones(includeInactive));

    [HttpPost("zones")]
    [Authorize(Policy = RolePolicies.CanManageCatalogs)]
    public IActionResult CreateZone([FromBody] CreateZoneRequest request)
        => HandleBiz(() => _store.AddZone(request));

    [HttpPut("zones/{id:int}")]
    [Authorize(Policy = RolePolicies.CanManageCatalogs)]
    public IActionResult UpdateZone(int id, [FromBody] UpdateZoneRequest request)
        => HandleBiz(() => _store.UpdateZone(id, request));

    [HttpGet("products")]
    public IActionResult GetProducts([FromQuery] bool includeInactive = false)
        => Ok(_store.GetProducts(includeInactive));

    [HttpPost("products")]
    [Authorize(Policy = RolePolicies.CanManageCatalogs)]
    public IActionResult CreateProduct([FromBody] CreateProductRequest request)
        => HandleBiz(() => _store.AddProduct(request));

    [HttpPut("products/{id:int}")]
    [Authorize(Policy = RolePolicies.CanManageCatalogs)]
    public IActionResult UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        => HandleBiz(() => _store.UpdateProduct(id, request));

    [HttpGet("payment-methods")]
    public IActionResult GetPaymentMethods([FromQuery] bool includeInactive = false)
        => Ok(_store.GetPaymentMethods(includeInactive));

    [HttpPost("payment-methods")]
    [Authorize(Policy = RolePolicies.CanManageCatalogs)]
    public IActionResult CreatePaymentMethod([FromBody] CreatePaymentMethodRequest request)
        => HandleBiz(() => _store.AddPaymentMethod(request));

    [HttpPut("payment-methods/{id:int}")]
    [Authorize(Policy = RolePolicies.CanManageCatalogs)]
    public IActionResult UpdatePaymentMethod(int id, [FromBody] UpdatePaymentMethodRequest request)
        => HandleBiz(() => _store.UpdatePaymentMethod(id, request));
}
