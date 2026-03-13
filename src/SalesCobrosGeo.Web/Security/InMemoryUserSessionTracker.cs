using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SalesCobrosGeo.Web.Security;

public sealed class InMemoryUserSessionTracker : IUserSessionTracker
{
    private sealed class SessionState
    {
        public required string SessionId { get; set; }
        public required string Username { get; init; }
        public required string DisplayName { get; init; }
        public required string RoleLabel { get; init; }
        public required string Zone { get; init; }
        public bool IsConnected { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public string LastPath { get; set; } = "-";
        public string LastIp { get; set; } = "-";
        public string LastUserAgent { get; set; } = "-";
        public string LastCoordinates { get; set; } = "-";
        public string LastLocationSource { get; set; } = "Sin traza";
    }

    private readonly object _sync = new();
    private readonly Dictionary<string, SessionState> _sessions = new(StringComparer.OrdinalIgnoreCase);

    public ClaimsPrincipal AttachSession(ClaimsPrincipal principal, ApplicationUserSummary user, HttpContext httpContext)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var claims = principal.Claims.ToList();
        claims.Add(new Claim(AppClaimTypes.SessionId, sessionId));
        var sessionPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, principal.Identity?.AuthenticationType));

        lock (_sync)
        {
            _sessions[user.Username] = new SessionState
            {
                SessionId = sessionId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                RoleLabel = user.RoleLabel,
                Zone = user.Zone,
                IsConnected = true,
                IsActive = user.IsActive,
                LastSeenUtc = DateTime.UtcNow,
                LastPath = httpContext.Request.Path.Value ?? "/",
                LastIp = ResolveIp(httpContext),
                LastUserAgent = ResolveAgent(httpContext)
            };
        }

        return sessionPrincipal;
    }

    public bool IsSessionValid(ClaimsPrincipal principal)
    {
        var username = principal.Identity?.Name;
        var sessionId = principal.FindFirstValue(AppClaimTypes.SessionId);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        lock (_sync)
        {
            if (!_sessions.TryGetValue(username, out var state))
            {
                return false;
            }

            return state.IsActive && state.IsConnected && state.SessionId == sessionId;
        }
    }

    public void TouchRequest(ClaimsPrincipal principal, HttpContext httpContext)
    {
        var username = principal.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        lock (_sync)
        {
            if (_sessions.TryGetValue(username, out var state))
            {
                state.LastSeenUtc = DateTime.UtcNow;
                state.LastPath = httpContext.Request.Path.Value ?? state.LastPath;
                state.LastIp = ResolveIp(httpContext);
                state.LastUserAgent = ResolveAgent(httpContext);
            }
        }
    }

    public void SignOut(ClaimsPrincipal principal)
    {
        var username = principal.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        lock (_sync)
        {
            if (_sessions.TryGetValue(username, out var state))
            {
                state.IsConnected = false;
                state.LastSeenUtc = DateTime.UtcNow;
            }
        }
    }

    public void ForceLogout(string username)
    {
        lock (_sync)
        {
            if (_sessions.TryGetValue(username, out var state))
            {
                state.IsConnected = false;
                state.LastSeenUtc = DateTime.UtcNow;
                state.SessionId = Guid.NewGuid().ToString("N");
            }
        }
    }

    public void SetUserActive(string username, bool isActive)
    {
        lock (_sync)
        {
            if (_sessions.TryGetValue(username, out var state))
            {
                state.IsActive = isActive;
                if (!isActive)
                {
                    state.IsConnected = false;
                    state.LastSeenUtc = DateTime.UtcNow;
                    state.SessionId = Guid.NewGuid().ToString("N");
                }
            }
        }
    }

    public void UpdateCoordinates(string username, string? coordinates, string source)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(coordinates))
        {
            return;
        }

        lock (_sync)
        {
            if (_sessions.TryGetValue(username, out var state))
            {
                state.LastCoordinates = coordinates;
                state.LastLocationSource = source;
                state.LastSeenUtc = DateTime.UtcNow;
            }
        }
    }

    public IReadOnlyList<UserSessionSnapshot> GetSnapshots(IReadOnlyList<ApplicationUserSummary> users)
    {
        lock (_sync)
        {
            return users
                .Select(user =>
                {
                    _sessions.TryGetValue(user.Username, out var state);
                    return new UserSessionSnapshot(
                        user.Username,
                        user.DisplayName,
                        user.RoleLabel,
                        user.Zone,
                        user.IsActive,
                        state?.IsConnected ?? false,
                        state is null ? "Sin ingreso" : state.LastSeenUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                        state?.LastPath ?? "-",
                        state?.LastIp ?? "-",
                        state?.LastUserAgent ?? "-",
                        state?.LastCoordinates ?? "-",
                        state?.LastLocationSource ?? "Sin traza");
                })
                .ToArray();
        }
    }

    public IReadOnlyList<AuditTrailEntry> GetAuditTrail(int take = 30)
    {
        return [];
    }

    private static string ResolveIp(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "-";
    }

    private static string ResolveAgent(HttpContext httpContext)
    {
        var agent = httpContext.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(agent) ? "Desconocido" : agent;
    }
}
