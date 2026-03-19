using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SalesCobrosGeo.Web.Data;
using SalesCobrosGeo.Web.Models.Administration;

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
            .AsNoTracking()
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

        if (user.Username.Equals("RaizAdmin", StringComparison.OrdinalIgnoreCase) && !isActive)
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

    public ApplicationUserSummary? GetUser(string username)
    {
        var user = _dbContext.Users.AsNoTracking().Include(x => x.Permissions).FirstOrDefault(x => x.Username == username);
        return user is null ? null : MapSummary(user);
    }

    public ApplicationUserSummary SaveUser(UserAdminInput input)
    {
        var username = input.Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("Usuario obligatorio.");
        }

        var originalUsername = input.OriginalUsername?.Trim();
        AppUserEntity entity;
        if (string.IsNullOrWhiteSpace(originalUsername))
        {
            if (_dbContext.Users.Any(x => x.Username == username))
            {
                throw new InvalidOperationException("El usuario ya existe.");
            }

            entity = new AppUserEntity
            {
                Username = username,
                CreatedUtc = DateTime.UtcNow
            };
            _dbContext.Users.Add(entity);
        }
        else
        {
            entity = _dbContext.Users.Include(x => x.Permissions).FirstOrDefault(x => x.Username == originalUsername)
                ?? throw new InvalidOperationException("Usuario no encontrado.");

            if (!originalUsername.Equals(username, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Por seguridad, el usuario de acceso no se renombra. Crea uno nuevo y desactiva el anterior si lo necesitas.");
            }
        }

        entity.DisplayName = input.DisplayName.Trim();
        entity.Zone = input.Zone.Trim();
        entity.Theme = input.Theme.Trim();
        entity.Role = input.Role.Trim();
        entity.RoleLabel = input.RoleLabel.Trim();
        entity.IsActive = input.IsActive;
        entity.TwoFactorEnabled = input.TwoFactorEnabled;
        entity.UpdatedUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            entity.PasswordHash = _passwordHasher.HashPassword(entity, input.Password);
        }
        else if (string.IsNullOrWhiteSpace(entity.PasswordHash))
        {
            throw new InvalidOperationException("Contrasena obligatoria para usuarios nuevos.");
        }

        var selected = input.Permissions
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        entity.Permissions.RemoveAll(x => !selected.Contains(x.Permission));
        foreach (var permission in selected.Where(permission => entity.Permissions.All(x => !x.Permission.Equals(permission, StringComparison.OrdinalIgnoreCase))))
        {
            entity.Permissions.Add(new AppUserPermissionEntity
            {
                Username = entity.Username,
                Permission = permission
            });
        }

        _dbContext.AuditLogs.Add(new AuditLogEntity
        {
            CreatedUtc = DateTime.UtcNow,
            EventType = string.IsNullOrWhiteSpace(originalUsername) ? "USER_CREATED" : "USER_UPDATED",
            Username = username,
            Description = string.IsNullOrWhiteSpace(originalUsername) ? "Alta de usuario." : "Edicion de usuario.",
            Metadata = string.Join(",", selected)
        });

        _dbContext.SaveChanges();
        return MapSummary(_dbContext.Users.AsNoTracking().Include(x => x.Permissions).First(x => x.Username == username));
    }

    public bool ResetPassword(string username, string newPassword)
    {
        var user = _dbContext.Users.FirstOrDefault(x => x.Username == username);
        if (user is null)
        {
            return false;
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.UpdatedUtc = DateTime.UtcNow;
        _dbContext.AuditLogs.Add(new AuditLogEntity
        {
            CreatedUtc = DateTime.UtcNow,
            EventType = "PASSWORD_RESET",
            Username = username,
            Description = "Contrasena reiniciada por administrador.",
            Metadata = "reset"
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
            user.TwoFactorEnabled,
            user.Permissions.Select(x => x.Permission).ToArray());
    }
}
