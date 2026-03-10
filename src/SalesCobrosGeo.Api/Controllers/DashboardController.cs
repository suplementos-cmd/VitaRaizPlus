using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Business;
using SalesCobrosGeo.Api.Contracts.Dashboard;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = RolePolicies.Authenticated)]
public sealed class DashboardController : ControllerBase
{
    private readonly IBusinessStore _store;

    public DashboardController(IBusinessStore store)
    {
        _store = store;
    }

    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var response = new DashboardResponse(_store.GetDashboardSummary(), DateTime.UtcNow);
        return Ok(response);
    }
}
