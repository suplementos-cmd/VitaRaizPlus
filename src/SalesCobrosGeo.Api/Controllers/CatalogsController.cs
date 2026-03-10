using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Business;
using SalesCobrosGeo.Api.Contracts.Catalogs;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[ApiController]
[Route("api/catalogs")]
[Authorize]
public sealed class CatalogsController : ControllerBase
{
    private readonly IBusinessStore _store;

    public CatalogsController(IBusinessStore store)
    {
        _store = store;
    }

    [HttpGet("snapshot")]
    public IActionResult GetSnapshot()
    {
        return Ok(_store.GetCatalogSnapshot());
    }

    [HttpGet("zones")]
    public IActionResult GetZones([FromQuery] bool includeInactive = false)
    {
        return Ok(_store.GetZones(includeInactive));
    }

    [HttpPost("zones")]
    [Authorize(Policy = RolePolicies.CanManageCatalogs)]
    public IActionResult CreateZone([FromBody] CreateZoneRequest request)
    {
        try
        {
            return Ok(_store.AddZone(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("zones/{id:int}")]
    [Authorize(Policy = RolePolicies.CanManageCatalogs)]
    public IActionResult UpdateZone(int id, [FromBody] UpdateZoneRequest request)
    {
        try
        {
            return Ok(_store.UpdateZone(id, request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("products")]
    public IActionResult GetProducts([FromQuery] bool includeInactive = false)
    {
        return Ok(_store.GetProducts(includeInactive));
    }

    [HttpPost("products")]
    [Authorize(Policy = RolePolicies.CanManageCatalogs)]
    public IActionResult CreateProduct([FromBody] CreateProductRequest request)
    {
        try
        {
            return Ok(_store.AddProduct(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("products/{id:int}")]
    [Authorize(Policy = RolePolicies.CanManageCatalogs)]
    public IActionResult UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        try
        {
            return Ok(_store.UpdateProduct(id, request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("payment-methods")]
    public IActionResult GetPaymentMethods([FromQuery] bool includeInactive = false)
    {
        return Ok(_store.GetPaymentMethods(includeInactive));
    }

    [HttpPost("payment-methods")]
    [Authorize(Policy = RolePolicies.CanManageCatalogs)]
    public IActionResult CreatePaymentMethod([FromBody] CreatePaymentMethodRequest request)
    {
        try
        {
            return Ok(_store.AddPaymentMethod(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("payment-methods/{id:int}")]
    [Authorize(Policy = RolePolicies.CanManageCatalogs)]
    public IActionResult UpdatePaymentMethod(int id, [FromBody] UpdatePaymentMethodRequest request)
    {
        try
        {
            return Ok(_store.UpdatePaymentMethod(id, request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
