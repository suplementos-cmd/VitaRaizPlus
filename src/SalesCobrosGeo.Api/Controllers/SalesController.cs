using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Business;
using SalesCobrosGeo.Api.Contracts.Sales;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize]
public sealed class SalesController : ControllerBase
{
    private readonly IBusinessStore _store;

    public SalesController(IBusinessStore store)
    {
        _store = store;
    }

    [HttpGet("mine")]
    [Authorize(Policy = RolePolicies.CanCreateSales)]
    public IActionResult GetMine()
    {
        var user = User.Identity?.Name ?? "unknown";
        var manageAll = HasAnyRole(UserRole.SupervisorVentas, UserRole.Administrador);
        var sales = _store.GetSalesForUser(user, manageAll)
            .Select(MapToResponse);

        return Ok(sales);
    }

    [HttpGet("team")]
    [Authorize(Policy = RolePolicies.CanManageSales)]
    public IActionResult GetTeam()
    {
        var sales = _store.GetSalesForUser(User.Identity?.Name ?? "unknown", manageAll: true)
            .Select(MapToResponse);

        return Ok(sales);
    }

    [HttpGet("review")]
    [Authorize(Policy = RolePolicies.CanManageSales)]
    public IActionResult GetReviewQueue()
    {
        return Ok(_store.GetSalesForReview().Select(MapToResponse));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = RolePolicies.Authenticated)]
    public IActionResult GetById(int id)
    {
        var sale = _store.GetSaleById(id);
        if (sale is null)
        {
            return NotFound(new { message = "Sale not found." });
        }

        var isOwner = string.Equals(sale.SellerUserName, User.Identity?.Name, StringComparison.OrdinalIgnoreCase);
        if (!isOwner && !HasAnyRole(UserRole.SupervisorVentas, UserRole.Administrador))
        {
            return Forbid();
        }

        return Ok(MapToResponse(sale));
    }

    [HttpPost]
    [Authorize(Policy = RolePolicies.CanCreateSales)]
    public IActionResult Create([FromBody] CreateSaleRequest request)
    {
        try
        {
            var sale = _store.AddSale(request, User.Identity?.Name ?? "unknown", canRegisterDirectly: true);
            return Ok(MapToResponse(sale));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/draft")]
    [Authorize(Policy = RolePolicies.CanCreateSales)]
    public IActionResult UpdateDraft(int id, [FromBody] UpdateSaleDraftRequest request)
    {
        try
        {
            var sale = _store.UpdateSaleDraft(id, request, User.Identity?.Name ?? "unknown", HasAnyRole(UserRole.SupervisorVentas, UserRole.Administrador));
            return Ok(MapToResponse(sale));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/review")]
    [Authorize(Policy = RolePolicies.CanManageSales)]
    public IActionResult Review(int id, [FromBody] ReviewSaleRequest request)
    {
        try
        {
            var sale = _store.ReviewSale(id, request, User.Identity?.Name ?? "unknown");
            return Ok(MapToResponse(sale));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/assign-collector")]
    [Authorize(Policy = RolePolicies.CanManageSales)]
    public IActionResult AssignCollector(int id, [FromBody] AssignCollectorRequest request)
    {
        try
        {
            var sale = _store.AssignCollector(id, request, User.Identity?.Name ?? "unknown");
            return Ok(MapToResponse(sale));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool HasAnyRole(params UserRole[] roles)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        return roles.Any(r => string.Equals(role, r.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    private static SaleSummaryResponse MapToResponse(SaleRecord sale)
    {
        return new SaleSummaryResponse(
            Id: sale.Id,
            SaleNumber: sale.SaleNumber,
            ClientId: sale.ClientId,
            SellerUserName: sale.SellerUserName,
            CollectorUserName: sale.CollectorUserName,
            Status: sale.Status,
            TotalAmount: sale.TotalAmount,
            PaymentMethodCode: sale.PaymentMethodCode,
            CollectionDay: sale.CollectionDay,
            CreatedAtUtc: sale.CreatedAtUtc,
            UpdatedAtUtc: sale.UpdatedAtUtc,
            Notes: sale.Notes,
            Items: sale.Items.ToArray(),
            Evidence: sale.Evidence,
            History: sale.History.ToArray());
    }
}
