using System.Collections.Concurrent;

namespace SalesCobrosGeo.Api.Audit;

public sealed class InMemoryAuditTrailStore : IAuditTrailStore
{
    private readonly ConcurrentQueue<AuditEntry> _entries = new();
    private const int MaxEntries = 5000;

    public void Add(AuditEntry entry)
    {
        _entries.Enqueue(entry);

        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<AuditEntry> GetRecent(int take)
    {
        if (take <= 0)
        {
            take = 50;
        }

        if (take > 500)
        {
            take = 500;
        }

        return _entries
            .Reverse()
            .Take(take)
            .ToArray();
    }
}
