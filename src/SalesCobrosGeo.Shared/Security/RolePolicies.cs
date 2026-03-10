namespace SalesCobrosGeo.Shared.Security;

public static class RolePolicies
{
    public const string Authenticated = "Authenticated";
    public const string CanManageSales = "CanManageSales";
    public const string CanCreateSales = "CanCreateSales";
    public const string CanManageCatalogs = "CanManageCatalogs";
    public const string CanManageClients = "CanManageClients";
    public const string CanCollect = "CanCollect";
    public const string CanSuperviseCollections = "CanSuperviseCollections";
    public const string AdminOnly = "AdminOnly";
}
