using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SalesCobrosGeo.Api.Contracts.Auth;
using SalesCobrosGeo.Api.Security;

namespace SalesCobrosGeo.Api.Controllers;

[Route("api/[controller]")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IUserStore _userStore;
    private readonly ITokenService _tokenService;

    public AuthController(IUserStore userStore, ITokenService tokenService)
    {
        _userStore = userStore;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "UserName and Password are required." });
        }

        var user = _userStore.ValidateCredentials(request.UserName, request.Password);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var session = _tokenService.IssueToken(user);
        return Ok(new LoginResponse(
            AccessToken: session.Token,
            UserName: session.UserName,
            FullName: session.FullName,
            Role: session.Role.ToString(),
            ExpiresAtUtc: session.ExpiresAtUtc));
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var token = ReadBearerToken();
        if (!string.IsNullOrWhiteSpace(token))
        {
            _tokenService.Revoke(token);
        }

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var fullName = User.FindFirstValue(ClaimTypes.GivenName) ?? CurrentUserName;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "unknown";

        return Ok(new { userName = CurrentUserName, fullName, role });
    }

    private string? ReadBearerToken()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        return null;
    }
}
