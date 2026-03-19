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
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record MaintenanceCatalogRecord(
    long Id,
    string Section,
    string Code,
    string Name,
    decimal? Price,
    bool IsActive);

public sealed class MaintenanceCatalogSaveInput
{
    public long? Id { get; set; }
    public string Section { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public bool IsActive { get; set; } = true;
}
