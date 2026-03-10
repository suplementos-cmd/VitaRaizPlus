using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = RolePolicies.CanCollect)]
public sealed class CollectionsController : ControllerBase
{
    [HttpGet("assigned")]
    public IActionResult GetAssignedPortfolio()
    {
        return Ok(new
        {
            message = "Assigned collections portfolio.",
            collector = User.Identity?.Name
        });
    }

    [HttpPost("register")]
    public IActionResult RegisterPayment([FromQuery] int saleId, [FromQuery] decimal amount)
    {
        if (saleId <= 0 || amount <= 0)
        {
            return BadRequest(new { message = "saleId and amount must be valid." });
        }

        return Ok(new
        {
            saleId,
            amount,
            recordedBy = User.Identity?.Name,
            timestampUtc = DateTime.UtcNow
        });
    }

    [HttpPost("reassign")]
    [Authorize(Policy = RolePolicies.CanSuperviseCollections)]
    public IActionResult ReassignPortfolio([FromQuery] string fromCollector, [FromQuery] string toCollector)
    {
        if (string.IsNullOrWhiteSpace(fromCollector) || string.IsNullOrWhiteSpace(toCollector))
        {
            return BadRequest(new { message = "Both collectors are required." });
        }

        return Ok(new
        {
            fromCollector,
            toCollector,
            reassignedBy = User.Identity?.Name,
            timestampUtc = DateTime.UtcNow
        });
    }
}
