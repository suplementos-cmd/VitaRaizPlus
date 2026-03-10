using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Business;
using SalesCobrosGeo.Api.Contracts.Clients;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize(Policy = RolePolicies.CanManageClients)]
public sealed class ClientsController : ControllerBase
{
    private readonly IBusinessStore _store;

    public ClientsController(IBusinessStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult GetClients([FromQuery] bool includeInactive = false, [FromQuery] string? zoneCode = null)
    {
        return Ok(_store.GetClients(includeInactive, zoneCode));
    }

    [HttpGet("{id:int}")]
    public IActionResult GetClientById(int id)
    {
        var client = _store.GetClientById(id);
        if (client is null)
        {
            return NotFound(new { message = "Client not found." });
        }

        return Ok(client);
    }

    [HttpPost]
    public IActionResult CreateClient([FromBody] CreateClientRequest request)
    {
        try
        {
            return Ok(_store.AddClient(request, User.Identity?.Name ?? "unknown"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public IActionResult UpdateClient(int id, [FromBody] UpdateClientRequest request)
    {
        var currentRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var canManageAll = string.Equals(currentRole, UserRole.SupervisorVentas.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(currentRole, UserRole.Administrador.ToString(), StringComparison.OrdinalIgnoreCase);

        try
        {
            return Ok(_store.UpdateClient(id, request, User.Identity?.Name ?? "unknown", canManageAll));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
