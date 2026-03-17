using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Controllers;

/// <summary>
/// Clase base para todos los controladores de la API.
/// Centraliza helpers comunes: usuario actual, verificación de roles y manejo uniforme de errores.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    // ── Usuario ──────────────────────────────────────────────────────────────

    /// <summary>Nombre de usuario autenticado o "unknown" si no hay sesión.</summary>
    protected string CurrentUserName => User.Identity?.Name ?? "unknown";

    // ── Roles ────────────────────────────────────────────────────────────────

    /// <summary>Devuelve true si el usuario tiene al menos uno de los roles indicados.</summary>
    protected bool HasAnyRole(params UserRole[] roles)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        return roles.Any(r => string.Equals(role, r.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    // ── Manejo de errores uniforme ───────────────────────────────────────────

    /// <summary>
    /// Ejecuta <paramref name="action"/> y convierte las excepciones de negocio en
    /// respuestas HTTP con formato ProblemDetails (RFC 7807).
    /// </summary>
    protected IActionResult HandleBiz<T>(Func<T> action)
    {
        try
        {
            return Ok(action());
        }
        catch (UnauthorizedAccessException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Ejecuta <paramref name="action"/> (sin retorno) y convierte excepciones de negocio
    /// en respuestas HTTP con formato ProblemDetails.
    /// </summary>
    protected IActionResult HandleBizNoContent(Action action)
    {
        try
        {
            action();
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
