namespace SalesCobrosGeo.Api.Audit;

public sealed record AuditEntry(
    DateTime TimestampUtc,
    string UserName,
    string Method,
    string Path,
    int StatusCode,
    string TraceId
);
