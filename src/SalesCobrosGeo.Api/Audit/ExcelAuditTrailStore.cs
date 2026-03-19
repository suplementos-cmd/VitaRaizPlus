using SalesCobrosGeo.Api.Data;

namespace SalesCobrosGeo.Api.Audit;

/// <summary>
/// Implementación de IAuditTrailStore que usa Excel como almacenamiento.
/// Reemplaza InMemoryAuditTrailStore para persistencia duradera.
/// </summary>
public sealed class ExcelAuditTrailStore : IAuditTrailStore
{
    private readonly ExcelDataService _excelService;
    private const string SheetName = "AuditTrail";
    private static long _nextId = 1;
    private static readonly object _idLock = new();

    public ExcelAuditTrailStore(ExcelDataService excelService)
    {
        _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
        InitializeIdCounter();
    }

    private void InitializeIdCounter()
    {
        try
        {
            var data = _excelService.ReadSheetAsync(SheetName).GetAwaiter().GetResult();
            if (data.Count > 0)
            {
                var maxId = data.Max(row => Convert.ToInt64(row["Id"] ?? 0));
                lock (_idLock)
                {
                    _nextId = maxId + 1;
                }
            }
        }
        catch
        {
            // Si falla, comenzamos desde 1
        }
    }

    public void Add(AuditEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        long id;
        lock (_idLock)
        {
            id = _nextId++;
        }

        var rowData = new Dictionary<string, object?>
        {
            ["Id"] = id,
            ["TimestampUtc"] = entry.TimestampUtc.ToString("O"),
            ["EventType"] = entry.Method,
            ["UserName"] = entry.UserName,
            ["Description"] = $"{entry.Method} {entry.Path} - Status: {entry.StatusCode}",
            ["Path"] = entry.Path ?? "-",
            ["IpAddress"] = "-",
            ["Coordinates"] = "-",
            ["Metadata"] = entry.TraceId ?? string.Empty
        };

        try
        {
            _excelService.AppendRowAsync(SheetName, rowData).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // Log error pero no fallar la operación principal
            Console.Error.WriteLine($"Failed to write audit entry: {ex.Message}");
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

        try
        {
            var data = _excelService.ReadSheetAsync(SheetName).GetAwaiter().GetResult();
            
            return data
                .Select(MapToAuditEntry)
                .OrderByDescending(entry => entry.TimestampUtc)
                .Take(take)
                .ToArray();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to read audit entries: {ex.Message}");
            return Array.Empty<AuditEntry>();
        }
    }

    private static AuditEntry MapToAuditEntry(Dictionary<string, object?> row)
    {
        return new AuditEntry(
            TimestampUtc: DateTime.Parse(row["TimestampUtc"]?.ToString() ?? DateTime.UtcNow.ToString("O")),
            UserName: row["UserName"]?.ToString() ?? string.Empty,
            Method: row["EventType"]?.ToString() ?? string.Empty,
            Path: row["Path"]?.ToString() ?? "-",
            StatusCode: 200,
            TraceId: row["Metadata"]?.ToString() ?? string.Empty);
    }
}
