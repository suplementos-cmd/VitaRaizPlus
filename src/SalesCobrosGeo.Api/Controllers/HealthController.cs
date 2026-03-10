using Microsoft.AspNetCore.Mvc;

namespace SalesCobrosGeo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            service = "SalesCobrosGeo.Api",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            status = "Healthy",
            timestampUtc = DateTime.UtcNow
        });
    }
}
