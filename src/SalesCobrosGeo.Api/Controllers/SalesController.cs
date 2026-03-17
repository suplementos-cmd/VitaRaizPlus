using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Business;
using SalesCobrosGeo.Api.Contracts.Sales;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[Route("api/sales")]
[Authorize]
public sealed class SalesController : ApiControllerBase
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
        var manageAll = HasAnyRole(UserRole.SupervisorVentas, UserRole.Administrador);
        var sales = _store.GetSalesForUser(CurrentUserName, manageAll).Select(MapToResponse);
        return Ok(sales);
    }

    [HttpGet("team")]
    [Authorize(Policy = RolePolicies.CanManageSales)]
    public IActionResult GetTeam()
    {
        var sales = _store.GetSalesForUser(CurrentUserName, manageAll: true).Select(MapToResponse);
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
            return Problem("Sale not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var isOwner = string.Equals(sale.SellerUserName, CurrentUserName, StringComparison.OrdinalIgnoreCase);
        if (!isOwner && !HasAnyRole(UserRole.SupervisorVentas, UserRole.Administrador))
        {
            return Forbid();
        }

        return Ok(MapToResponse(sale));
    }

    [HttpPost]
    [Authorize(Policy = RolePolicies.CanCreateSales)]
    public IActionResult Create([FromBody] CreateSaleRequest request)
        => HandleBiz(() => MapToResponse(_store.AddSale(request, CurrentUserName, canRegisterDirectly: true)));

    [HttpPut("{id:int}/draft")]
    [Authorize(Policy = RolePolicies.CanCreateSales)]
    public IActionResult UpdateDraft(int id, [FromBody] UpdateSaleDraftRequest request)
        => HandleBiz(() => MapToResponse(
            _store.UpdateSaleDraft(id, request, CurrentUserName, HasAnyRole(UserRole.SupervisorVentas, UserRole.Administrador))));

    [HttpPost("{id:int}/review")]
    [Authorize(Policy = RolePolicies.CanManageSales)]
    public IActionResult Review(int id, [FromBody] ReviewSaleRequest request)
        => HandleBiz(() => MapToResponse(_store.ReviewSale(id, request, CurrentUserName)));

    [HttpPost("{id:int}/assign-collector")]
    [Authorize(Policy = RolePolicies.CanManageSales)]
    public IActionResult AssignCollector(int id, [FromBody] AssignCollectorRequest request)
        => HandleBiz(() => MapToResponse(_store.AssignCollector(id, request, CurrentUserName)));

    private static SaleSummaryResponse MapToResponse(SaleRecord sale) => new(
        Id: sale.Id,
        SaleNumber: sale.SaleNumber,
        ClientId: sale.ClientId,
        SellerUserName: sale.SellerUserName,
        CollectorUserName: sale.CollectorUserName,
        Status: sale.Status,
        CollectionStatus: sale.CollectionStatus,
        TotalAmount: sale.TotalAmount,
        CollectedAmount: sale.CollectedAmount,
        RemainingAmount: sale.RemainingAmount,
        PaymentMethodCode: sale.PaymentMethodCode,
        CollectionDay: sale.CollectionDay,
        CreatedAtUtc: sale.CreatedAtUtc,
        UpdatedAtUtc: sale.UpdatedAtUtc,
        Notes: sale.Notes,
        Items: sale.Items.ToArray(),
        Evidence: sale.Evidence,
        History: sale.History.ToArray(),
        Collections: sale.Collections.ToArray());
}
