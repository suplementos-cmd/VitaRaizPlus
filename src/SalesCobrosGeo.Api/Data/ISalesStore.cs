using SalesCobrosGeo.Api.Business;

namespace SalesCobrosGeo.Api.Data;

/// <summary>
/// Interfaz para gestión de ventas y cobros
/// Fase 2: Excel como fuente de datos para transacciones
/// </summary>
public interface ISalesStore
{
    // Sales
    Task<IReadOnlyList<SaleRecordDto>> GetAllSalesAsync();
    Task<SaleRecordDto?> GetSaleByIdAsync(string idV);
    Task<SaleRecordDto> CreateSaleAsync(SaleFormInputDto input);
    Task<SaleRecordDto> UpdateSaleAsync(string idV, SaleFormInputDto input);
    
    // Collections
    Task<IReadOnlyList<CollectionRecordDto>> GetAllCollectionsAsync();
    Task<IReadOnlyList<CollectionRecordDto>> GetCollectionsBySaleAsync(string idV);
    Task<CollectionRecordDto?> GetCollectionByIdAsync(string idCc);
    Task<CollectionRecordDto> RegisterCollectionAsync(CollectionFormInputDto input);
    Task<CollectionRecordDto> UpdateCollectionAsync(string idCc, CollectionFormInputDto input);
    
    // Collector Portfolio
    Task<IReadOnlyList<CollectorPortfolioItemDto>> GetCollectorPortfolioAsync(string? collectorUsername = null);
    Task<CollectorPortfolioItemDto?> GetPortfolioItemAsync(string idV, string? collectorUsername = null);
}
