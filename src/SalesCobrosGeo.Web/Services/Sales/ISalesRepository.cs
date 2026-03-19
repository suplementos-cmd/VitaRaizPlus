using SalesCobrosGeo.Web.Models.Maintenance;
using SalesCobrosGeo.Web.Models.Sales;

namespace SalesCobrosGeo.Web.Services.Sales;

/// <summary>
/// Repositorio para operaciones de ventas y cobros
/// </summary>
public interface ISalesRepository
{
    SalesCatalogs GetCatalogs();
    IReadOnlyList<MaintenanceCatalogRecord> GetMaintenanceCatalog(string section);
    MaintenanceCatalogRecord SaveMaintenanceCatalogItem(MaintenanceCatalogSaveInput input);
    bool DeleteMaintenanceCatalogItem(string section, long id);
    IReadOnlyList<SaleRecord> GetAll();
    SaleRecord? GetById(string idV);
    SaleRecord Create(SaleFormInput input);
    SaleRecord Update(string idV, SaleFormInput input);

    IReadOnlyList<CollectorPortfolioItem> GetCollectorPortfolio(string? profile);
    CollectorPortfolioItem? GetPortfolioItem(string idV, string? profile);
    IReadOnlyList<CollectionRecord> GetCollections(string? profile = null, string? idV = null);
    CollectionRecord? GetCollectionById(string idCc);
    CollectionRecord RegisterCollection(CollectionFormInput input);
    CollectionRecord UpdateCollection(string idCc, CollectionFormInput input);
    IReadOnlyList<CatalogOption> GetCollectorProfiles();
}
