using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class SalesController : ControllerBase
{
    [HttpGet("mine")]
    public IActionResult GetMine()
    {
        return Ok(new
        {
            message = "Sales visible for the authenticated user.",
            user = User.Identity?.Name,
            role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value
        });
    }

    [HttpGet("team")]
    [Authorize(Policy = RolePolicies.CanManageSales)]
    public IActionResult GetTeam()
    {
        return Ok(new
        {
            message = "Team sales view for supervisors and admin."
        });
    }

    [HttpPut("{saleId:int}/status")]
    [Authorize(Policy = RolePolicies.CanManageSales)]
    public IActionResult UpdateSaleStatus(int saleId, [FromQuery] string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return BadRequest(new { message = "status is required" });
        }

        return Ok(new
        {
            saleId,
            status,
            updatedBy = User.Identity?.Name,
            timestampUtc = DateTime.UtcNow
        });
    }
}
