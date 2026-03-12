namespace SalesCobrosGeo.Web.Models.Maintenance;

public sealed record MaintenanceStat(string Title, string Value, string Tone);

public sealed record MaintenanceItem(
    string Code,
    string Name,
    string Detail,
    string Badge,
    string Tone);

public sealed record MaintenanceSection(
    string Key,
    string Title,
    string Subtitle,
    string Summary,
    IReadOnlyList<MaintenanceItem> Items);

public sealed record MaintenancePageViewModel(
    string SelectedSection,
    IReadOnlyList<MaintenanceStat> Stats,
    IReadOnlyList<MaintenanceSection> Sections);
