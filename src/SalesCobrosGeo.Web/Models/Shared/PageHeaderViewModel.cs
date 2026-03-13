namespace SalesCobrosGeo.Web.Models.Shared;

public sealed record PageHeaderViewModel(
    string Eyebrow,
    string Title,
    string Subtitle,
    string? Summary = null);
