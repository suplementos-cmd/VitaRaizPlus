namespace SalesCobrosGeo.Web.Models.Maintenance;

public sealed record MaintenanceStat(string Title, string Value, string Tone);

public sealed record MaintenanceItem(
    long Id,
    string Code,
    string Name,
    string Detail,
    string Badge,
    string Tone,
    bool IsActive);

public sealed record MaintenanceSection(
    string Key,
    string Title,
    string Subtitle,
    string Summary,
    IReadOnlyList<MaintenanceItem> Items);

public sealed record MaintenancePageViewModel(
    string SelectedSection,
    IReadOnlyList<MaintenanceStat> Stats,
    IReadOnlyList<MaintenanceSection> Sections,
    MaintenanceEditorInput Editor,
    bool ShowEditor,
    long? ViewItemId = null,
    string? Message = null);

public sealed class MaintenanceEditorInput
{
    public long? Id { get; set; }
    public string Section { get; set; } = "catalogos";
    public Dictionary<string, object?> Fields { get; set; } = new();
    
    // Backward compatibility properties (deprecated)
    public string Code { get => GetField<string>("Code") ?? string.Empty; set => Fields["Code"] = value; }
    public string Name { get => GetField<string>("Name") ?? string.Empty; set => Fields["Name"] = value; }
    public decimal? Price { get => GetField<decimal?>("Price"); set => Fields["Price"] = value; }
    public bool IsActive { get => GetField<bool>("IsActive"); set => Fields["IsActive"] = value; }

    private T? GetField<T>(string key) => Fields.TryGetValue(key, out var value) && value is T typed ? typed : default;
}

public sealed record MaintenanceCatalogRecord(
    long Id,
    string Section,
    string Code,
    string Name,
    decimal? Price,
    bool IsActive,
    Dictionary<string, object?> AllFields); // Todas las columnas dinámicas

public sealed class MaintenanceCatalogSaveInput
{
    public long? Id { get; set; }
    public string Section { get; set; } = string.Empty;
    public Dictionary<string, object?> Fields { get; set; } = new();
    
    // Backward compatibility
    public string Code { get => GetField<string>("Code") ?? string.Empty; set => Fields["Code"] = value; }
    public string Name { get => GetField<string>("Name") ?? string.Empty; set => Fields["Name"] = value; }
    public decimal? Price { get => GetField<decimal?>("Price"); set => Fields["Price"] = value; }
    public bool IsActive { get => GetField<bool>("IsActive"); set => Fields["IsActive"] = value; }

    private T? GetField<T>(string key) => Fields.TryGetValue(key, out var value) && value is T typed ? typed : default;
}
