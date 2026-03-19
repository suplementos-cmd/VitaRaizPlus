using SalesCobrosGeo.Api.Catalogs;

namespace SalesCobrosGeo.Web.Services.Catalogs;

/// <summary>
/// Implementación del servicio de catálogos para vistas, basado en ICatalogService de la API
/// </summary>
public sealed class CatalogViewService : ICatalogViewService
{
    private readonly ICatalogService _catalogService;
    private readonly ILogger<CatalogViewService> _logger;
    
    // Cache en memoria para evitar lecturas repetidas
    private IReadOnlyList<SaleStatus>? _saleStatusesCache;
    private IReadOnlyList<CollectionStatus>? _collectionStatusesCache;
    private readonly SemaphoreSlim _saleStatusLock = new(1, 1);
    private readonly SemaphoreSlim _collectionStatusLock = new(1, 1);

    public CatalogViewService(ICatalogService catalogService, ILogger<CatalogViewService> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    private async Task<IReadOnlyList<SaleStatus>> GetSaleStatusesCachedAsync()
    {
        if (_saleStatusesCache != null)
            return _saleStatusesCache;

        await _saleStatusLock.WaitAsync();
        try
        {
            if (_saleStatusesCache != null)
                return _saleStatusesCache;

            _saleStatusesCache = await _catalogService.GetSaleStatusesAsync();
            return _saleStatusesCache;
        }
        finally
        {
            _saleStatusLock.Release();
        }
    }

    private async Task<IReadOnlyList<CollectionStatus>> GetCollectionStatusesCachedAsync()
    {
        if (_collectionStatusesCache != null)
            return _collectionStatusesCache;

        await _collectionStatusLock.WaitAsync();
        try
        {
            if (_collectionStatusesCache != null)
                return _collectionStatusesCache;

            _collectionStatusesCache = await _catalogService.GetCollectionStatusesAsync();
            return _collectionStatusesCache;
        }
        finally
        {
            _collectionStatusLock.Release();
        }
    }

    public async Task<string> GetSaleStatusCssClassAsync(string? statusCode)
    {
        if (string.IsNullOrWhiteSpace(statusCode))
            return "status-unknown";

        try
        {
            var statuses = await GetSaleStatusesCachedAsync();
            var status = statuses.FirstOrDefault(s => 
                s.Code.Equals(statusCode, StringComparison.OrdinalIgnoreCase));
            
            return status?.ColorClass ?? "status-unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo clase CSS para estado de venta: {StatusCode}", statusCode);
            return "status-unknown";
        }
    }

    public async Task<string> GetSaleStatusDisplayTextAsync(string? statusCode)
    {
        if (string.IsNullOrWhiteSpace(statusCode))
            return statusCode ?? "Desconocido";

        try
        {
            var statuses = await GetSaleStatusesCachedAsync();
            var status = statuses.FirstOrDefault(s => 
                s.Code.Equals(statusCode, StringComparison.OrdinalIgnoreCase));
            
            return status?.Name ?? statusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo texto display para estado de venta: {StatusCode}", statusCode);
            return statusCode;
        }
    }

    public async Task<string> GetCollectionStatusCssClassAsync(string? statusCode)
    {
        if (string.IsNullOrWhiteSpace(statusCode))
            return "collection-unknown";

        try
        {
            var statuses = await GetCollectionStatusesCachedAsync();
            var status = statuses.FirstOrDefault(s => 
                s.Code.Equals(statusCode, StringComparison.OrdinalIgnoreCase));
            
            return status?.ColorClass ?? "collection-unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo clase CSS para estado de cobro: {StatusCode}", statusCode);
            return "collection-unknown";
        }
    }

    public async Task<string> GetCollectionStatusDisplayTextAsync(string? statusCode)
    {
        if (string.IsNullOrWhiteSpace(statusCode))
            return statusCode ?? "Desconocido";

        try
        {
            var statuses = await GetCollectionStatusesCachedAsync();
            var status = statuses.FirstOrDefault(s => 
                s.Code.Equals(statusCode, StringComparison.OrdinalIgnoreCase));
            
            return status?.Name ?? statusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo texto display para estado de cobro: {StatusCode}", statusCode);
            return statusCode;
        }
    }

    public async Task<string?> GetCollectionStatusIconAsync(string? statusCode)
    {
        if (string.IsNullOrWhiteSpace(statusCode))
            return null;

        try
        {
            var statuses = await GetCollectionStatusesCachedAsync();
            var status = statuses.FirstOrDefault(s => 
                s.Code.Equals(statusCode, StringComparison.OrdinalIgnoreCase));
            
            // Por ahora retornamos un SVG basado en el código, pero esto debería venir de la DB
            return status?.Code.ToUpperInvariant() switch
            {
                "ATRASADO" or "VENCIDO" => "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2'><circle cx='12' cy='12' r='9'/><path d='M12 7v5l3 3'/></svg>",
                "LIQUIDADO" or "COMPLETADO" => "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2'><path d='m5 13 4 4L19 7'/></svg>",
                "CANCELADO" => "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2'><line x1='18' y1='6' x2='6' y2='18'/><line x1='6' y1='6' x2='18' y2='18'/></svg>",
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo icono para estado de cobro: {StatusCode}", statusCode);
            return null;
        }
    }

    public async Task<IReadOnlyDictionary<string, (string CssClass, string DisplayText)>> GetAllSaleStatusesAsync()
    {
        try
        {
            var statuses = await GetSaleStatusesCachedAsync();
            return statuses.ToDictionary(
                s => s.Code,
                s => (s.ColorClass, s.Name),
                StringComparer.OrdinalIgnoreCase
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo todos los estados de venta");
            return new Dictionary<string, (string, string)>();
        }
    }

    public async Task<IReadOnlyDictionary<string, (string CssClass, string DisplayText, string? Icon)>> GetAllCollectionStatusesAsync()
    {
        try
        {
            var statuses = await GetCollectionStatusesCachedAsync();
            var result = new Dictionary<string, (string, string, string?)>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var status in statuses)
            {
                var icon = await GetCollectionStatusIconAsync(status.Code);
                result[status.Code] = (status.ColorClass, status.Name, icon);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo todos los estados de cobro");
            return new Dictionary<string, (string, string, string?)>();
        }
    }
}
