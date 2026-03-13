namespace SalesCobrosGeo.Web.Security;

public sealed record AuditTrailEntry(
    string Timestamp,
    string EventType,
    string Username,
    string Description,
    string Path,
    string Coordinates);
