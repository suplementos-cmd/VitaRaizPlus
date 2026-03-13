namespace SalesCobrosGeo.Web.Security;

public sealed record UserSessionSnapshot(
    string Username,
    string DisplayName,
    string RoleLabel,
    string Zone,
    bool IsActive,
    bool IsConnected,
    string LastSeenLabel,
    string LastPath,
    string LastIp,
    string LastUserAgent,
    string LastCoordinates,
    string LastLocationSource);
