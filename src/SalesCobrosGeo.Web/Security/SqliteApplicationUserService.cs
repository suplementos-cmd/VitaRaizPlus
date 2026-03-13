using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SalesCobrosGeo.Web.Data;

namespace SalesCobrosGeo.Web.Security;

public sealed class SqliteApplicationUserService : IApplicationUserService
{
    private readonly AppSecurityDbContext _dbContext;
    private readonly PasswordHasher<AppUserEntity> _passwordHasher = new();

    private static readonly IReadOnlyList<LoginCredentialHint> DefaultHints =
    [
        new("RaizAdmin", "RaizAdmin#2026!", "Full", "Administra todo el sistema, catalogos y usuarios."),
        new("sale01", "Sale01#2026!", "Ventas", "Captura ventas y consulta dashboard comercial."),
        new("gest01", "Gest01#2026!", "Cobros", "Gestiona cartera, cobros y dashboard de cobranza.")
    ];

    public SqliteApplicationUserService(AppSecurityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ClaimsPrincipal? ValidateCredentials(string username, string password)
    {
        var user = _dbContext.Users
            .Include(x => x.Permissions)
            .FirstOrDefault(x => x.Username == username);

        if (user is null || !user.IsActive)
        {
            RegisterAudit("LOGIN_DENIED", username, "Intento de acceso con cuenta inexistente o inactiva.");
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            RegisterAudit("LOGIN_DENIED", username, "Intento de acceso con contrasena invalida.");
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            user.UpdatedUtc = DateTime.UtcNow;
            _dbContext.SaveChanges();
        }

        return AppPrincipalFactory.BuildPrincipal(MapSummary(user));
    }

    public IReadOnlyList<ApplicationUserSummary> GetUsers()
    {
        return _dbContext.Users
            .Include(x => x.Permissions)
            .OrderBy(x => x.DisplayName)
            .AsEnumerable()
            .Select(MapSummary)
            .ToArray();
    }

    public IReadOnlyList<LoginCredentialHint> GetLoginHints() => DefaultHints;

    public bool SetActive(string username, bool isActive)
    {
        var user = _dbContext.Users.FirstOrDefault(x => x.Username == username);
        if (user is null)
        {
            return false;
        }

        user.IsActive = isActive;
        user.UpdatedUtc = DateTime.UtcNow;
        _dbContext.AuditLogs.Add(new AuditLogEntity
        {
            CreatedUtc = DateTime.UtcNow,
            EventType = isActive ? "USER_ENABLED" : "USER_DISABLED",
            Username = username,
            Description = isActive ? "Usuario activado por administrador." : "Usuario desactivado por administrador.",
            Metadata = "status-change"
        });
        _dbContext.SaveChanges();
        return true;
    }

    private void RegisterAudit(string eventType, string username, string description)
    {
        _dbContext.AuditLogs.Add(new AuditLogEntity
        {
            CreatedUtc = DateTime.UtcNow,
            EventType = eventType,
            Username = username,
            Description = description,
            Metadata = "auth"
        });
        _dbContext.SaveChanges();
    }

    private static ApplicationUserSummary MapSummary(AppUserEntity user)
    {
        return new ApplicationUserSummary(
            user.Username,
            user.DisplayName,
            user.Role,
            user.RoleLabel,
            user.Zone,
            user.Theme,
            user.IsActive,
            user.Permissions.Select(x => x.Permission).ToArray());
    }
}
