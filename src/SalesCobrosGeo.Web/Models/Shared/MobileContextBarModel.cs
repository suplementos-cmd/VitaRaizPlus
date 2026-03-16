namespace SalesCobrosGeo.Web.Models.Shared;

public sealed record MobileContextAction(
    string Label,
    string Icon,
    string? Url = null,
    string? DataAction = null,
    string? AriaLabel = null,
    string? CssClass = null);

public sealed record MobileContextBarModel(
    string Title,
    string? Subtitle = null,
    string? BackUrl = null,
    IReadOnlyList<MobileContextAction>? Actions = null);
