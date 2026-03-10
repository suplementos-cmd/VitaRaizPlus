using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Business;
using SalesCobrosGeo.Api.Contracts.Collections;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[ApiController]
[Route("api/collections")]
[Authorize(Policy = RolePolicies.CanCollect)]
public sealed class CollectionsController : ControllerBase
{
    private readonly IBusinessStore _store;

    public CollectionsController(IBusinessStore store)
    {
        _store = store;
    }

    [HttpGet("assigned")]
    public IActionResult GetAssignedPortfolio()
    {
        var userName = User.Identity?.Name ?? "unknown";
        var manageAll = HasAnyRole(UserRole.SupervisorCobranza, UserRole.Administrador);
        return Ok(_store.GetCollectionPortfolio(userName, manageAll));
    }

    [HttpPost("register")]
    public IActionResult RegisterPayment([FromBody] RegisterCollectionRequest request)
    {
        try
        {
            var sale = _store.RegisterCollection(request, User.Identity?.Name ?? "unknown");
            return Ok(new
            {
                saleId = sale.Id,
                sale.SaleNumber,
                sale.CollectedAmount,
                remaining = sale.RemainingAmount,
                sale.CollectionStatus,
                sale.Status
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("reassign")]
    [Authorize(Policy = RolePolicies.CanSuperviseCollections)]
    public IActionResult ReassignPortfolio([FromBody] ReassignPortfolioRequest request)
    {
        try
        {
            var affected = _store.ReassignPortfolio(request, User.Identity?.Name ?? "unknown");
            return Ok(new { affected });
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
}
