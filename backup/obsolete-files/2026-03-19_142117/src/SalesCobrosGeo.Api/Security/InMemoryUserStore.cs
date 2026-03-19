using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Security;

public sealed class InMemoryUserStore : IUserStore
{
    private static readonly IReadOnlyDictionary<string, AppUser> Users = new Dictionary<string, AppUser>(StringComparer.OrdinalIgnoreCase)
    {
        ["vendedor.demo"] = new("vendedor.demo", "demo123", "Vendedor Demo", UserRole.Vendedor, true),
        ["supventas.demo"] = new("supventas.demo", "demo123", "Supervisor Ventas Demo", UserRole.SupervisorVentas, true),
        ["cobrador.demo"] = new("cobrador.demo", "demo123", "Cobrador Demo", UserRole.Cobrador, true),
        ["supcobranza.demo"] = new("supcobranza.demo", "demo123", "Supervisor Cobranza Demo", UserRole.SupervisorCobranza, true),
        ["admin.demo"] = new("admin.demo", "demo123", "Administrador Demo", UserRole.Administrador, true)
    };

    public AppUser? ValidateCredentials(string userName, string password)
    {
        if (!Users.TryGetValue(userName, out var user))
        {
            return null;
        }

        if (!user.IsActive || user.Password != password)
        {
            return null;
        }

        return user;
    }

    public AppUser? FindByUserName(string userName)
    {
        return Users.TryGetValue(userName, out var user) ? user : null;
    }
}
