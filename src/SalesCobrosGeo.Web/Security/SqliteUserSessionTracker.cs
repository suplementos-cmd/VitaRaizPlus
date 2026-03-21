using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SalesCobrosGeo.Web.Data;

namespace SalesCobrosGeo.Web.Security;

public sealed class SqliteUserSessionTracker : IUserSessionTracker
{
    private readonly AppSecurityDbContext _dbContext;
    private static readonly ConcurrentDictionary<string, DateTime> _lastUpdateTimes = new();
    private static readonly TimeSpan _updateThrottle = TimeSpan.FromSeconds(30);

    public SqliteUserSessionTracker(AppSecurityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ClaimsPrincipal AttachSession(ClaimsPrincipal principal, ApplicationUserSummary user, HttpContext httpContext)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var claims = principal.Claims.ToList();
        claims.Add(new Claim(AppClaimTypes.SessionId, sessionId));
        var sessionPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, principal.Identity?.AuthenticationType));

        _dbContext.Sessions.Add(new AppSessionEntity
        {
            SessionId = sessionId,
            Username = user.Username,
            DisplayName = user.DisplayName,
            RoleLabel = user.RoleLabel,
            Zone = user.Zone,
            IsConnected = true,
            IsRevoked = false,
            IsActiveUser = user.IsActive,
            CreatedUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow,
            LastPath = httpContext.Request.Path.Value ?? "/",
            LastIp = ResolveIp(httpContext),
            LastUserAgent = ResolveAgent(httpContext)
        });

        AddAudit("LOGIN_SUCCESS", user.Username, "Inicio de sesion correcto.", httpContext);
        _dbContext.SaveChanges();
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

        // FASE 1: Los usuarios están en Excel vía API, no en SQLite
        // Solo validamos la sesión, no la tabla Users
        var session = _dbContext.Sessions.AsNoTracking()
            .FirstOrDefault(x => x.SessionId == sessionId && x.Username == username);

        return session is not null && session.IsConnected && !session.IsRevoked && session.IsActiveUser;
    }

    public void TouchRequest(ClaimsPrincipal principal, HttpContext httpContext)
    {
        var session = ResolveCurrentSession(principal);
        if (session is null)
        {
            return;
        }

        // Throttling: Solo actualizar si han pasado más de 30 segundos desde la última actualización
        var now = DateTime.UtcNow;
        if (_lastUpdateTimes.TryGetValue(session.SessionId, out var lastUpdate))
        {
            if (now - lastUpdate < _updateThrottle)
            {
                return; // Skip update - demasiado pronto
            }
        }

        session.LastSeenUtc = now;
        session.LastPath = httpContext.Request.Path.Value ?? session.LastPath;
        session.LastIp = ResolveIp(httpContext);
        session.LastUserAgent = ResolveAgent(httpContext);

        // Retry logic para manejar "database is locked"
        var maxRetries = 3;
        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                _dbContext.SaveChanges();
                _lastUpdateTimes[session.SessionId] = now;
                break; // Éxito - salir del loop
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 5)
            {
                // Error 5 = database is locked
                if (retry == maxRetries - 1)
                {
                    // Último intento falló - log y continuar sin romper la app
                    Console.WriteLine($"[WARNING] Failed to update session {session.SessionId} after {maxRetries} retries: database locked");
                    return;
                }
                
                // Esperar un poco antes de reintentar (exponential backoff)
                Thread.Sleep(50 * (retry + 1));
            }
            catch (Exception ex)
            {
                // Otros errores - log y continuar
                Console.WriteLine($"[ERROR] Failed to update session {session.SessionId}: {ex.Message}");
                return;
            }
        }
    }

    public void SignOut(ClaimsPrincipal principal)
    {
        var session = ResolveCurrentSession(principal);
        if (session is null)
        {
            return;
        }

        session.IsConnected = false;
        session.ClosedUtc = DateTime.UtcNow;
        session.LastSeenUtc = DateTime.UtcNow;
        _dbContext.AuditLogs.Add(new AuditLogEntity
        {
            CreatedUtc = DateTime.UtcNow,
            EventType = "LOGOUT",
            Username = session.Username,
            Description = "Cierre de sesion voluntario.",
            Path = session.LastPath,
            Ip = session.LastIp,
            Coordinates = session.LastCoordinates
        });
        _dbContext.SaveChanges();
    }

    public void ForceLogout(string username)
    {
        var sessions = _dbContext.Sessions.Where(x => x.Username == username && x.IsConnected && !x.IsRevoked).ToList();
        foreach (var session in sessions)
        {
            session.IsConnected = false;
            session.IsRevoked = true;
            session.ClosedUtc = DateTime.UtcNow;
            session.LastSeenUtc = DateTime.UtcNow;
        }

        _dbContext.AuditLogs.Add(new AuditLogEntity
        {
            CreatedUtc = DateTime.UtcNow,
            EventType = "FORCE_LOGOUT",
            Username = username,
            Description = "Sesion cerrada por administrador.",
            Metadata = $"count={sessions.Count}"
        });
        _dbContext.SaveChanges();
    }

    public void SetUserActive(string username, bool isActive)
    {
        var sessions = _dbContext.Sessions.Where(x => x.Username == username).ToList();
        foreach (var session in sessions)
        {
            session.IsActiveUser = isActive;
            if (!isActive)
            {
                session.IsConnected = false;
                session.IsRevoked = true;
                session.ClosedUtc = DateTime.UtcNow;
                session.LastSeenUtc = DateTime.UtcNow;
            }
        }

        _dbContext.SaveChanges();
    }

    public void UpdateCoordinates(string username, string? coordinates, string source)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(coordinates))
        {
            return;
        }

        var session = _dbContext.Sessions
            .Where(x => x.Username == username)
            .OrderByDescending(x => x.LastSeenUtc)
            .FirstOrDefault();

        if (session is null)
        {
            return;
        }

        session.LastCoordinates = coordinates;
        session.LastLocationSource = source;
        session.LastSeenUtc = DateTime.UtcNow;
        if (source.Contains("Heartbeat", StringComparison.OrdinalIgnoreCase))
        {
            session.LastHeartbeatUtc = DateTime.UtcNow;
        }

        if (!source.Contains("Heartbeat", StringComparison.OrdinalIgnoreCase))
        {
            _dbContext.AuditLogs.Add(new AuditLogEntity
            {
                CreatedUtc = DateTime.UtcNow,
                EventType = "LOCATION_UPDATE",
                Username = username,
                Description = $"Actualizacion de ubicacion: {source}.",
                Coordinates = coordinates,
                Metadata = source
            });
        }

        _dbContext.SaveChanges();
    }

    public IReadOnlyList<UserSessionSnapshot> GetSnapshots(IReadOnlyList<ApplicationUserSummary> users)
    {
        var latestSessions = _dbContext.Sessions
            .AsNoTracking()
            .OrderByDescending(x => x.LastSeenUtc)
            .ToList()
            .GroupBy(x => x.Username)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        return users.Select(user =>
        {
            latestSessions.TryGetValue(user.Username, out var session);
            return new UserSessionSnapshot(
                user.Username,
                user.DisplayName,
                user.RoleLabel,
                user.Zone,
                user.IsActive,
                session?.IsConnected ?? false,
                session is null ? "Sin ingreso" : session.LastSeenUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                session?.LastPath ?? "-",
                session?.LastIp ?? "-",
                session?.LastUserAgent ?? "-",
                session?.LastCoordinates ?? "-",
                session?.LastLocationSource ?? "Sin traza");
        }).ToArray();
    }

    public IReadOnlyList<AuditTrailEntry> GetAuditTrail(int take = 30)
    {
        return _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedUtc)
            .Take(take)
            .Select(x => new AuditTrailEntry(
                x.CreatedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                x.EventType,
                x.Username,
                x.Description,
                x.Path,
                x.Coordinates))
            .ToArray();
    }

    private AppSessionEntity? ResolveCurrentSession(ClaimsPrincipal principal)
    {
        var sessionId = principal.FindFirstValue(AppClaimTypes.SessionId);
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        return _dbContext.Sessions.FirstOrDefault(x => x.SessionId == sessionId);
    }

    private void AddAudit(string eventType, string username, string description, HttpContext httpContext)
    {
        _dbContext.AuditLogs.Add(new AuditLogEntity
        {
            CreatedUtc = DateTime.UtcNow,
            EventType = eventType,
            Username = username,
            Description = description,
            Path = httpContext.Request.Path.Value ?? "-",
            Ip = ResolveIp(httpContext)
        });
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
