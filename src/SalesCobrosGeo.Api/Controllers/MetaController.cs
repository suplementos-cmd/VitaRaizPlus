using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MetaController : ControllerBase
{
    [HttpGet("roles")]
    public IActionResult GetRoles()
    {
        var roles = Enum.GetValues<UserRole>()
            .Select(role => new { id = (int)role, name = role.ToString() });

        return Ok(roles);
    }
}
