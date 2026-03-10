using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Business;
using SalesCobrosGeo.Api.Contracts.Collections;
using SalesCobrosGeo.Api.Contracts.Sync;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize(Policy = RolePolicies.Authenticated)]
public sealed class SyncController : ControllerBase
{
    private readonly IBusinessStore _store;

    public SyncController(IBusinessStore store)
    {
        _store = store;
    }

    [HttpGet("pull")]
    public IActionResult Pull()
    {
        return Ok(_store.GetSyncPayload());
    }

    [HttpPost("push")]
    [Authorize(Policy = RolePolicies.CanCollect)]
    public IActionResult Push([FromBody] SyncPushRequest request)
    {
        var userName = User.Identity?.Name ?? "unknown";
        var appliedCollections = 0;

        foreach (var collection in request.Collections)
        {
            try
            {
                _store.RegisterCollection(
                    new RegisterCollectionRequest(
                        collection.SaleId,
                        collection.Amount,
                        collection.Coordinates,
                        collection.Notes,
                        collection.CollectedAtUtc),
                    userName);
                appliedCollections++;
            }
            catch (InvalidOperationException)
            {
            }
        }

        foreach (var update in request.SaleUpdates)
        {
            if (string.IsNullOrWhiteSpace(update.CollectorUserName))
            {
                continue;
            }

            try
            {
                _store.AssignCollector(
                    update.SaleId,
                    new SalesCobrosGeo.Api.Contracts.Sales.AssignCollectorRequest(update.CollectorUserName, update.Notes),
                    userName);
            }
            catch (InvalidOperationException)
            {
            }
        }

        return Ok(new
        {
            appliedCollections,
            updatedSales = request.SaleUpdates.Count,
            syncAtUtc = DateTime.UtcNow
        });
    }
}
