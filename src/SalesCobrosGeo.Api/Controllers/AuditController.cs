using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Audit;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[Route("api/[controller]")]
[Authorize(Policy = RolePolicies.AdminOnly)]
public sealed class AuditController : ApiControllerBase
{
    private readonly IAuditTrailStore _auditTrailStore;

    public AuditController(IAuditTrailStore auditTrailStore)
    {
        _auditTrailStore = auditTrailStore;
    }

    [HttpGet("recent")]
    public IActionResult GetRecent([FromQuery] int take = 50)
    {
        return Ok(_auditTrailStore.GetRecent(take));
    }
}
