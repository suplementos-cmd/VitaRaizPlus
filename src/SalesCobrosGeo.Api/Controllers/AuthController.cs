using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Contracts.Auth;
using SalesCobrosGeo.Api.Security;

namespace SalesCobrosGeo.Api.Controllers;

/// <summary>
/// Controller para autenticación y gestión de usuarios
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserStore _userStore;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserStore userStore,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _userStore = userStore;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Autentica un usuario y retorna un token de acceso
    /// </summary>
    /// <param name="request">Credenciales de usuario</param>
    /// <returns>Token de acceso y datos del usuario</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Intento de login para usuario: {UserName}", request.UserName);

        // Validar credenciales contra Excel
        var user = _userStore.ValidateCredentials(request.UserName, request.Password);

        if (user == null)
        {
            _logger.LogWarning("Login fallido para usuario: {UserName}", request.UserName);
            return Unauthorized(new { message = "Usuario o contraseña incorrectos" });
        }

        // Generar token
        var token = _tokenService.IssueToken(user);
        
        _logger.LogInformation("Login exitoso para usuario: {UserName}, Role: {Role}", 
            user.UserName, user.Role);

        var response = new LoginResponse(
            AccessToken: token.Token,
            UserName: user.UserName,
            FullName: user.FullName,
            Role: user.Role.ToString(),
            ExpiresAtUtc: token.ExpiresAtUtc
        );

        return Ok(response);
    }

    /// <summary>
    /// Obtiene información del usuario autenticado actual
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userName = User.Identity?.Name;
        
        if (string.IsNullOrEmpty(userName))
        {
            return Unauthorized();
        }

        var user = _userStore.FindByUserName(userName);
        
        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        return Ok(new
        {
            user.UserName,
            user.FullName,
            Role = user.Role.ToString()
        });
    }

    /// <summary>
    /// Cierra la sesión del usuario actual
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // Extraer token del header Authorization
        var authHeader = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            _tokenService.Revoke(token);
            
            _logger.LogInformation("Usuario {UserName} cerró sesión", User.Identity?.Name);
        }

        return Ok(new { message = "Sesión cerrada exitosamente" });
    }
}
