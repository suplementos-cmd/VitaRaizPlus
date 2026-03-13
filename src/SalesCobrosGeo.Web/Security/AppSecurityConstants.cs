namespace SalesCobrosGeo.Web.Security;

public static class AppRoles
{
    public const string Full = "FULL";
    public const string Sales = "SALES";
    public const string Collections = "COLLECTIONS";
}

public static class AppPermissions
{
    public const string DashboardView = "dashboard:view";
    public const string SalesView = "sales:view";
    public const string CollectionsView = "collections:view";
    public const string MaintenanceView = "maintenance:view";
    public const string AdministrationView = "administration:view";
}

public static class AppPolicies
{
    public const string DashboardAccess = "DashboardAccess";
    public const string SalesAccess = "SalesAccess";
    public const string CollectionsAccess = "CollectionsAccess";
    public const string MaintenanceAccess = "MaintenanceAccess";
    public const string AdministrationAccess = "AdministrationAccess";
}

public static class AppClaimTypes
{
    public const string Permission = "vrp:permission";
    public const string Theme = "vrp:theme";
    public const string DisplayRole = "vrp:display-role";
    public const string SessionId = "vrp:session-id";
}
