using SalesCobrosGeo.Api.Contracts.Catalogs;
using SalesCobrosGeo.Api.Contracts.Clients;
using SalesCobrosGeo.Api.Contracts.Collections;
using SalesCobrosGeo.Api.Contracts.Sales;

namespace SalesCobrosGeo.Api.Business;

public interface IBusinessStore
{
    CatalogSnapshot GetCatalogSnapshot();
    IReadOnlyList<Zone> GetZones(bool includeInactive);
    IReadOnlyList<Product> GetProducts(bool includeInactive);
    IReadOnlyList<PaymentMethod> GetPaymentMethods(bool includeInactive);
    Zone AddZone(CreateZoneRequest request);
    Zone UpdateZone(int id, UpdateZoneRequest request);
    Product AddProduct(CreateProductRequest request);
    Product UpdateProduct(int id, UpdateProductRequest request);
    PaymentMethod AddPaymentMethod(CreatePaymentMethodRequest request);
    PaymentMethod UpdatePaymentMethod(int id, UpdatePaymentMethodRequest request);

    IReadOnlyList<Client> GetClients(bool includeInactive, string? zoneCode);
    Client? GetClientById(int id);
    Client AddClient(CreateClientRequest request, string createdBy);
    Client UpdateClient(int id, UpdateClientRequest request, string updatedBy, bool canManageAll);

    IReadOnlyList<SaleRecord> GetSalesForUser(string userName, bool manageAll);
    IReadOnlyList<SaleRecord> GetSalesForReview();
    SaleRecord? GetSaleById(int id);
    SaleRecord AddSale(CreateSaleRequest request, string userName, bool canRegisterDirectly);
    SaleRecord UpdateSaleDraft(int id, UpdateSaleDraftRequest request, string userName, bool canManageAll);
    SaleRecord ReviewSale(int id, ReviewSaleRequest request, string reviewer);
    SaleRecord AssignCollector(int id, AssignCollectorRequest request, string reviewer);

    IReadOnlyList<CollectionSummary> GetCollectionPortfolio(string userName, bool manageAll);
    SaleRecord RegisterCollection(RegisterCollectionRequest request, string collectorUserName);
    int ReassignPortfolio(ReassignPortfolioRequest request, string supervisorUserName);

    DashboardSummary GetDashboardSummary();
    SyncPayload GetSyncPayload();
}
