using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SalesCobrosGeo.Web.Security;

namespace SalesCobrosGeo.Web.Data;

public sealed class SecurityDatabaseInitializer
{
    private readonly AppSecurityDbContext _dbContext;

    public SecurityDatabaseInitializer(AppSecurityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Initialize()
    {
        _dbContext.Database.EnsureCreated();

        if (_dbContext.Users.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var hasher = new PasswordHasher<AppUserEntity>();

        var users = new[]
        {
            BuildUser("RaizAdmin", "Raiz Admin", "RaizAdmin#2026!", "root", AppRoles.Full, "Acceso total", "Global", now,
                [AppPermissions.DashboardView, AppPermissions.SalesView, AppPermissions.CollectionsView, AppPermissions.MaintenanceView, AppPermissions.AdministrationView]),
            BuildUser("sale01", "Ventas 01", "Sale01#2026!", "sales", AppRoles.Sales, "Modulo ventas", "Heroes Chalco", now,
                [AppPermissions.DashboardView, AppPermissions.SalesView]),
            BuildUser("gest01", "Cobros 01", "Gest01#2026!", "collections", AppRoles.Collections, "Modulo cobros", "Jardines", now,
                [AppPermissions.DashboardView, AppPermissions.CollectionsView])
        };

        foreach (var user in users)
        {
            user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);
        }

        _dbContext.Users.AddRange(users);
        _dbContext.AuditLogs.Add(new AuditLogEntity
        {
            CreatedUtc = now,
            EventType = "SECURITY_INIT",
            Username = "system",
            Description = "Base de seguridad SQLite inicializada con usuarios semilla.",
            Metadata = "bootstrap"
        });
        _dbContext.SaveChanges();
    }

    private static AppUserEntity BuildUser(
        string username,
        string displayName,
        string password,
        string theme,
        string role,
        string roleLabel,
        string zone,
        DateTime now,
        IReadOnlyList<string> permissions)
    {
        return new AppUserEntity
        {
            Username = username,
            DisplayName = displayName,
            PasswordHash = password,
            Theme = theme,
            Role = role,
            RoleLabel = roleLabel,
            Zone = zone,
            IsActive = true,
            TwoFactorEnabled = false,
            CreatedUtc = now,
            UpdatedUtc = now,
            Permissions = permissions.Select(permission => new AppUserPermissionEntity
            {
                Username = username,
                Permission = permission
            }).ToList()
        };
    }
}
