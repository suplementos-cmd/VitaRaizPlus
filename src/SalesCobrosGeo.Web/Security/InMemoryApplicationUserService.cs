using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using SalesCobrosGeo.Web.Models.Administration;

namespace SalesCobrosGeo.Web.Security;

public sealed class InMemoryApplicationUserService : IApplicationUserService
{
    private readonly PasswordHasher<AppUserAccount> _passwordHasher = new();
    private readonly List<AppUserAccount> _users;
    private readonly IReadOnlyList<LoginCredentialHint> _loginHints;

    public InMemoryApplicationUserService()
    {
        _users =
        [
            CreateUser(
                username: "RaizAdmin",
                displayName: "Raiz Admin",
                password: "RaizAdmin#2026!",
                theme: "root",
                role: AppRoles.Full,
                roleLabel: "Acceso total",
                zone: "Global",
                permissions:
                [
                    AppPermissions.DashboardView,
                    AppPermissions.SalesView,
                    AppPermissions.CollectionsView,
                    AppPermissions.MaintenanceView,
                    AppPermissions.AdministrationView
                ]),
            CreateUser(
                username: "sale01",
                displayName: "Ventas 01",
                password: "Sale01#2026!",
                theme: "sales",
                role: AppRoles.Sales,
                roleLabel: "Modulo ventas",
                zone: "Heroes Chalco",
                permissions:
                [
                    AppPermissions.DashboardView,
                    AppPermissions.SalesView
                ]),
            CreateUser(
                username: "gest01",
                displayName: "Cobros 01",
                password: "Gest01#2026!",
                theme: "collections",
                role: AppRoles.Collections,
                roleLabel: "Modulo cobros",
                zone: "Jardines",
                permissions:
                [
                    AppPermissions.DashboardView,
                    AppPermissions.CollectionsView
                ])
        ];

        _loginHints =
        [
            new LoginCredentialHint("RaizAdmin", "RaizAdmin#2026!", "Full", "Administra todo el sistema, catalogos y usuarios."),
            new LoginCredentialHint("sale01", "Sale01#2026!", "Ventas", "Captura ventas y consulta dashboard comercial."),
            new LoginCredentialHint("gest01", "Gest01#2026!", "Cobros", "Gestiona cartera, cobros y dashboard de cobranza.")
        ];
    }

    public ClaimsPrincipal? ValidateCredentials(string username, string password)
    {
        var user = _users.FirstOrDefault(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
        }

        return BuildPrincipal(user);
    }

    public IReadOnlyList<ApplicationUserSummary> GetUsers()
    {
        return _users
            .Select(MapSummary)
            .ToArray();
    }

    public IReadOnlyList<LoginCredentialHint> GetLoginHints() => _loginHints;

    public bool SetActive(string username, bool isActive)
    {
        var user = _users.FirstOrDefault(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user is null)
        {
            return false;
        }

        user.IsActive = isActive;
        return true;
    }

    public ApplicationUserSummary? GetUser(string username)
    {
        var user = _users.FirstOrDefault(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return user is null ? null : MapSummary(user);
    }

    public ApplicationUserSummary SaveUser(UserAdminInput input)
    {
        var username = input.Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("Usuario obligatorio.");
        }

        var existing = _users.FirstOrDefault(x => x.Username.Equals(input.OriginalUsername ?? username, StringComparison.OrdinalIgnoreCase));
        var permissions = input.Permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        if (existing is null)
        {
            if (string.IsNullOrWhiteSpace(input.Password))
            {
                throw new InvalidOperationException("Contrasena obligatoria para usuarios nuevos.");
            }

            var account = CreateUser(
                username,
                input.DisplayName,
                input.Password,
                input.Theme,
                input.Role,
                input.RoleLabel,
                input.Zone,
                permissions);
            account.IsActive = input.IsActive;
            _users.Add(account);
            return MapSummary(account);
        }

        if (!existing.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && _users.Any(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("El usuario ya existe.");
        }

        var updated = new AppUserAccount
        {
            Username = username,
            DisplayName = input.DisplayName,
            PasswordHash = existing.PasswordHash,
            Theme = input.Theme,
            Role = input.Role,
            RoleLabel = input.RoleLabel,
            Zone = input.Zone,
            IsActive = input.IsActive,
            Permissions = permissions
        };

        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            updated.PasswordHash = _passwordHasher.HashPassword(updated, input.Password);
        }

        var index = _users.IndexOf(existing);
        _users[index] = updated;
        return MapSummary(updated);
    }

    public bool ResetPassword(string username, string newPassword)
    {
        var user = _users.FirstOrDefault(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user is null)
        {
            return false;
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        return true;
    }

    private AppUserAccount CreateUser(
        string username,
        string displayName,
        string password,
        string theme,
        string role,
        string roleLabel,
        string zone,
        IReadOnlyList<string> permissions)
    {
        var user = new AppUserAccount
        {
            Username = username,
            DisplayName = displayName,
            PasswordHash = string.Empty,
            Theme = theme,
            Role = role,
            RoleLabel = roleLabel,
            Zone = zone,
            IsActive = true,
            Permissions = permissions
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        return user;
    }

    private static ApplicationUserSummary MapSummary(AppUserAccount user)
    {
        return new ApplicationUserSummary(
            user.Username,
            user.DisplayName,
            user.Role,
            user.RoleLabel,
            user.Zone,
            user.Theme,
            user.IsActive,
            false,
            user.Permissions);
    }

    private static ClaimsPrincipal BuildPrincipal(AppUserAccount user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Username),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.DisplayName),
            new(ClaimTypes.Role, user.Role),
            new(AppClaimTypes.Theme, user.Theme),
            new(AppClaimTypes.DisplayRole, user.RoleLabel)
        };

        claims.AddRange(user.Permissions.Select(permission => new Claim(AppClaimTypes.Permission, permission)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
