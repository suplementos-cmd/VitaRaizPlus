namespace SalesCobrosGeo.Api.Audit;

public interface IAuditTrailStore
{
    void Add(AuditEntry entry);
    IReadOnlyList<AuditEntry> GetRecent(int take);
}
