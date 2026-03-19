namespace SalesCobrosGeo.Web.Services.Catalogs;

/// <summary>
/// Servicio para obtener catálogos formateados para vistas
/// </summary>
public interface ICatalogViewService
{
    /// <summary>
    /// Obtiene la clase CSS para un estado de venta dado su código
    /// </summary>
    Task<string> GetSaleStatusCssClassAsync(string? statusCode);
    
    /// <summary>
    /// Obtiene el texto de display para un estado de venta dado su código
    /// </summary>
    Task<string> GetSaleStatusDisplayTextAsync(string? statusCode);
    
    /// <summary>
    /// Obtiene la clase CSS para un estado de cobro dado su código
    /// </summary>
    Task<string> GetCollectionStatusCssClassAsync(string? statusCode);
    
    /// <summary>
    /// Obtiene el texto de display para un estado de cobro dado su código
    /// </summary>
    Task<string> GetCollectionStatusDisplayTextAsync(string? statusCode);
    
    /// <summary>
    /// Obtiene el icono SVG para un estado de cobro dado su código
    /// </summary>
    Task<string?> GetCollectionStatusIconAsync(string? statusCode);
    
    /// <summary>
    /// Obtiene todos los estados de venta con su información de display
    /// </summary>
    Task<IReadOnlyDictionary<string, (string CssClass, string DisplayText)>> GetAllSaleStatusesAsync();
    
    /// <summary>
    /// Obtiene todos los estados de cobro con su información de display
    /// </summary>
    Task<IReadOnlyDictionary<string, (string CssClass, string DisplayText, string? Icon)>> GetAllCollectionStatusesAsync();
}
