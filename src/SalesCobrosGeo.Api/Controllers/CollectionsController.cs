using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Business;
using SalesCobrosGeo.Api.Contracts.Collections;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[Route("api/collections")]
[Authorize(Policy = RolePolicies.CanCollect)]
public sealed class CollectionsController : ApiControllerBase
{
    private readonly IBusinessStore _store;

    public CollectionsController(IBusinessStore store)
    {
        _store = store;
    }

    [HttpGet("assigned")]
    public IActionResult GetAssignedPortfolio()
    {
        var manageAll = HasAnyRole(UserRole.SupervisorCobranza, UserRole.Administrador);
        return Ok(_store.GetCollectionPortfolio(CurrentUserName, manageAll));
    }

    [HttpPost("register")]
    public IActionResult RegisterPayment([FromBody] RegisterCollectionRequest request)
        => HandleBiz(() =>
        {
            var sale = _store.RegisterCollection(request, CurrentUserName);
            return new
            {
                saleId = sale.Id,
                sale.SaleNumber,
                sale.CollectedAmount,
                remaining = sale.RemainingAmount,
                sale.CollectionStatus,
                sale.Status
            };
        });

    [HttpPost("reassign")]
    [Authorize(Policy = RolePolicies.CanSuperviseCollections)]
    public IActionResult ReassignPortfolio([FromBody] ReassignPortfolioRequest request)
        => HandleBiz(() => new { affected = _store.ReassignPortfolio(request, CurrentUserName) });
}
