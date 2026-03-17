using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Business;
using SalesCobrosGeo.Api.Contracts.Clients;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

[Route("api/clients")]
[Authorize(Policy = RolePolicies.CanManageClients)]
public sealed class ClientsController : ApiControllerBase
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
        return client is null
            ? Problem("Client not found.", statusCode: StatusCodes.Status404NotFound)
            : Ok(client);
    }

    [HttpPost]
    public IActionResult CreateClient([FromBody] CreateClientRequest request)
        => HandleBiz(() => _store.AddClient(request, CurrentUserName));

    [HttpPut("{id:int}")]
    public IActionResult UpdateClient(int id, [FromBody] UpdateClientRequest request)
        => HandleBiz(() => _store.UpdateClient(
            id, request, CurrentUserName,
            canManageAll: HasAnyRole(UserRole.SupervisorVentas, UserRole.Administrador)));
}
