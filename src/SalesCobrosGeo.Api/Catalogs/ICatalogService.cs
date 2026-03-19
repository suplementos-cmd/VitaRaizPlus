namespace SalesCobrosGeo.Api.Catalogs;

/// <summary>
/// Interfaz para acceso a catálogos dinámicos desde Excel
/// </summary>
public interface ICatalogService
{
    // Menús
    Task<IReadOnlyList<MenuItem>> GetMenuItemsAsync(bool includeInactive = false);
    Task<IReadOnlyList<MenuItem>> GetMenuItemsForPlatformAsync(string platform);
    
    // Días
    Task<IReadOnlyList<WeekDay>> GetWeekDaysAsync(bool includeInactive = false);
    Task<WeekDay?> GetWeekDayByCodeAsync(string code);
    Task<WeekDay?> GetTodayWeekDayAsync();
    
    // Estados
    Task<IReadOnlyList<SaleStatus>> GetSaleStatusesAsync(bool includeInactive = false);
    Task<SaleStatus?> GetSaleStatusByCodeAsync(string code);
    Task<IReadOnlyList<CollectionStatus>> GetCollectionStatusesAsync(bool includeInactive = false);
    Task<CollectionStatus?> GetCollectionStatusByCodeAsync(string code);
    
    // Tipos de catálogo
    Task<IReadOnlyList<CatalogType>> GetCatalogTypesAsync(bool includeInactive = false);
    Task<CatalogType?> GetCatalogTypeByCodeAsync(string code);
    
    // Configuración UI
    Task<UISetting?> GetUISettingAsync(string category, string key);
    Task<IReadOnlyList<UISetting>> GetUISettingsByCategoryAsync(string category);
}
