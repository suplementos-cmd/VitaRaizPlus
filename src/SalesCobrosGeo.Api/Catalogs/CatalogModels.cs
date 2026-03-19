namespace SalesCobrosGeo.Api.Catalogs;

/// <summary>
/// Item de menú configurable desde Excel
/// </summary>
public sealed record MenuItem(
    int Id,
    string Code,
    string Label,
    string? IconSvg,
    string? Controller,
    string? Action,
    string? RequiredPolicy,
    int SortOrder,
    bool IsActive,
    int? ParentId,
    string Platform);

/// <summary>
/// Día de la semana configurable
/// </summary>
public sealed record WeekDay(
    int Id,
    string Code,
    string Name,
    string ShortCode,
    int SortOrder,
    bool IsActive);

/// <summary>
/// Estado de venta configurable
/// </summary>
public sealed record SaleStatus(
    int Id,
    string Code,
    string Name,
    string ColorClass,
    string? IconSvg,
    int SortOrder,
    bool IsActive,
    bool IsFinal);

/// <summary>
/// Estado de cobro configurable
/// </summary>
public sealed record CollectionStatus(
    int Id,
    string Code,
    string Name,
    string ColorClass,
    int Priority,
    bool IsActive);

/// <summary>
/// Tipo de catálogo para sección de mantenimiento
/// </summary>
public sealed record CatalogType(
    int Id,
    string Code,
    string Name,
    string Description,
    string IconClass,
    string Category,
    int SortOrder,
    bool IsActive);

/// <summary>
/// Configuración de UI almacenada en Excel
/// </summary>
public sealed record UISetting(
    int Id,
    string Category,
    string Key,
    string Value,
    string Description,
    bool IsActive);
