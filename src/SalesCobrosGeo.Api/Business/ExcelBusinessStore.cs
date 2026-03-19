using SalesCobrosGeo.Api.Contracts.Catalogs;
using SalesCobrosGeo.Api.Contracts.Clients;
using SalesCobrosGeo.Api.Contracts.Collections;
using SalesCobrosGeo.Api.Contracts.Sales;
using SalesCobrosGeo.Api.Data;
using System.Text.Json;

namespace SalesCobrosGeo.Api.Business;

/// <summary>
/// Implementación de IBusinessStore que usa Excel como almacenamiento.
/// Reemplaza InMemoryBusinessStore eliminando datos hardcodeados.
/// </summary>
public sealed class ExcelBusinessStore : IBusinessStore
{
    private readonly ExcelDataService _excelService;
    private readonly Lock _sync = new();

    public ExcelBusinessStore(ExcelDataService excelService)
    {
        _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
    }

    #region Catalogs

    public CatalogSnapshot GetCatalogSnapshot()
    {
        lock (_sync)
        {
            var zones = GetZones(false);
            var products = GetProducts(false);
            var paymentMethods = GetPaymentMethods(false);
            var version = GetCatalogVersion();

            return new CatalogSnapshot(version, zones.ToArray(), products.ToArray(), paymentMethods.ToArray());
        }
    }

    public IReadOnlyList<Zone> GetZones(bool includeInactive)
    {
        lock (_sync)
        {
            var data = _excelService.ReadSheetAsync("Zones").GetAwaiter().GetResult();
            return data
                .Select(MapToZone)
                .Where(z => includeInactive || z.IsActive)
                .ToArray();
        }
    }

    public IReadOnlyList<Product> GetProducts(bool includeInactive)
    {
        lock (_sync)
        {
            var data = _excelService.ReadSheetAsync("Products").GetAwaiter().GetResult();
            return data
                .Select(MapToProduct)
                .Where(p => includeInactive || p.IsActive)
                .ToArray();
        }
    }

    public IReadOnlyList<PaymentMethod> GetPaymentMethods(bool includeInactive)
    {
        lock (_sync)
        {
            var data = _excelService.ReadSheetAsync("PaymentMethods").GetAwaiter().GetResult();
            return data
                .Select(MapToPaymentMethod)
                .Where(pm => includeInactive || pm.IsActive)
                .ToArray();
        }
    }

    public Zone AddZone(CreateZoneRequest request)
    {
        lock (_sync)
        {
            var zones = GetZones(true);
            EnsureUniqueCode(zones.Select(z => z.Code), request.Code, "zone code");
            
            var nextId = zones.Count == 0 ? 1 : zones.Max(z => z.Id) + 1;
            var zone = new Zone(nextId, NormalizeCode(request.Code), request.Name.Trim(), request.IsActive);

            var rowData = new Dictionary<string, object?>
            {
                ["Id"] = zone.Id,
                ["Code"] = zone.Code,
                ["Name"] = zone.Name,
                ["IsActive"] = zone.IsActive
            };

            _excelService.AppendRowAsync("Zones", rowData).GetAwaiter().GetResult();
            TouchCatalogVersion();
            return zone;
        }
    }

    public Zone UpdateZone(int id, UpdateZoneRequest request)
    {
        lock (_sync)
        {
            var zones = GetZones(true);
            var existing = zones.FirstOrDefault(z => z.Id == id);
            if (existing == null)
            {
                throw new InvalidOperationException("Zone not found.");
            }

            var normalizedCode = NormalizeCode(request.Code);
            EnsureUniqueCode(zones.Where(z => z.Id != id).Select(z => z.Code), normalizedCode, "zone code");

            var updated = new Zone(id, normalizedCode, request.Name.Trim(), request.IsActive);

            _excelService.UpdateRowsAsync("Zones",
                row => Convert.ToInt32(row["Id"]) == id,
                row =>
                {
                    row["Code"] = updated.Code;
                    row["Name"] = updated.Name;
                    row["IsActive"] = updated.IsActive;
                }).GetAwaiter().GetResult();

            TouchCatalogVersion();
            return updated;
        }
    }

    public Product AddProduct(CreateProductRequest request)
    {
        lock (_sync)
        {
            var products = GetProducts(true);
            EnsureUniqueCode(products.Select(p => p.Code), request.Code, "product code");

            var nextId = products.Count == 0 ? 1 : products.Max(p => p.Id) + 1;
            var product = new Product(nextId, NormalizeCode(request.Code), request.Name.Trim(), request.Price, request.IsActive);

            var rowData = new Dictionary<string, object?>
            {
                ["Id"] = product.Id,
                ["Code"] = product.Code,
                ["Name"] = product.Name,
                ["Price"] = product.Price,
                ["IsActive"] = product.IsActive
            };

            _excelService.AppendRowAsync("Products", rowData).GetAwaiter().GetResult();
            TouchCatalogVersion();
            return product;
        }
    }

    public Product UpdateProduct(int id, UpdateProductRequest request)
    {
        lock (_sync)
        {
            var products = GetProducts(true);
            var existing = products.FirstOrDefault(p => p.Id == id);
            if (existing == null)
            {
                throw new InvalidOperationException("Product not found.");
            }

            var normalizedCode = NormalizeCode(request.Code);
            EnsureUniqueCode(products.Where(p => p.Id != id).Select(p => p.Code), normalizedCode, "product code");

            var updated = new Product(id, normalizedCode, request.Name.Trim(), request.Price, request.IsActive);

            _excelService.UpdateRowsAsync("Products",
                row => Convert.ToInt32(row["Id"]) == id,
                row =>
                {
                    row["Code"] = updated.Code;
                    row["Name"] = updated.Name;
                    row["Price"] = updated.Price;
                    row["IsActive"] = updated.IsActive;
                }).GetAwaiter().GetResult();

            TouchCatalogVersion();
            return updated;
        }
    }

    public PaymentMethod AddPaymentMethod(CreatePaymentMethodRequest request)
    {
        lock (_sync)
        {
            var paymentMethods = GetPaymentMethods(true);
            EnsureUniqueCode(paymentMethods.Select(pm => pm.Code), request.Code, "payment method code");

            var nextId = paymentMethods.Count == 0 ? 1 : paymentMethods.Max(pm => pm.Id) + 1;
            var method = new PaymentMethod(nextId, NormalizeCode(request.Code), request.Name.Trim(), request.IsActive);

            var rowData = new Dictionary<string, object?>
            {
                ["Id"] = method.Id,
                ["Code"] = method.Code,
                ["Name"] = method.Name,
                ["IsActive"] = method.IsActive
            };

            _excelService.AppendRowAsync("PaymentMethods", rowData).GetAwaiter().GetResult();
            TouchCatalogVersion();
            return method;
        }
    }

    public PaymentMethod UpdatePaymentMethod(int id, UpdatePaymentMethodRequest request)
    {
        lock (_sync)
        {
            var paymentMethods = GetPaymentMethods(true);
            var existing = paymentMethods.FirstOrDefault(pm => pm.Id == id);
            if (existing == null)
            {
                throw new InvalidOperationException("Payment method not found.");
            }

            var normalizedCode = NormalizeCode(request.Code);
            EnsureUniqueCode(paymentMethods.Where(pm => pm.Id != id).Select(pm => pm.Code), normalizedCode, "payment method code");

            var updated = new PaymentMethod(id, normalizedCode, request.Name.Trim(), request.IsActive);

            _excelService.UpdateRowsAsync("PaymentMethods",
                row => Convert.ToInt32(row["Id"]) == id,
                row =>
                {
                    row["Code"] = updated.Code;
                    row["Name"] = updated.Name;
                    row["IsActive"] = updated.IsActive;
                }).GetAwaiter().GetResult();

            TouchCatalogVersion();
            return updated;
        }
    }

    #endregion

    #region Clients

    public IReadOnlyList<Client> GetClients(bool includeInactive, string? zoneCode)
    {
        lock (_sync)
        {
            var data = _excelService.ReadSheetAsync("Clients").GetAwaiter().GetResult();
            var normalizedZone = string.IsNullOrWhiteSpace(zoneCode) ? null : NormalizeCode(zoneCode);
            
            return data
                .Select(MapToClient)
                .Where(c => includeInactive || c.IsActive)
                .Where(c => normalizedZone is null || c.ZoneCode == normalizedZone)
                .OrderBy(c => c.FullName)
                .ToArray();
        }
    }

    public Client? GetClientById(int id)
    {
        lock (_sync)
        {
            var data = _excelService.ReadSheetAsync("Clients").GetAwaiter().GetResult();
            var clientRow = data.FirstOrDefault(row => Convert.ToInt32(row["Id"]) == id);
            return clientRow != null ? MapToClient(clientRow) : null;
        }
    }

    public Client AddClient(CreateClientRequest request, string createdBy)
    {
        lock (_sync)
        {
            var zoneCode = NormalizeCode(request.ZoneCode);
            EnsureZoneExists(zoneCode);

            var clients = GetClients(true, null);
            var nextId = clients.Count == 0 ? 1 : clients.Max(c => c.Id) + 1;
            var now = DateTime.UtcNow;

            var client = new Client(
                Id: nextId,
                FullName: request.FullName.Trim(),
                Mobile: request.Mobile.Trim(),
                Phone: request.Phone?.Trim(),
                ZoneCode: zoneCode,
                CollectionDay: request.CollectionDay.Trim(),
                Address: request.Address.Trim(),
                CreatedBy: createdBy,
                CreatedAtUtc: now,
                UpdatedAtUtc: now,
                IsActive: request.IsActive);

            var rowData = new Dictionary<string, object?>
            {
                ["Id"] = client.Id,
                ["FullName"] = client.FullName,
                ["Mobile"] = client.Mobile,
                ["Phone"] = client.Phone,
                ["ZoneCode"] = client.ZoneCode,
                ["CollectionDay"] = client.CollectionDay,
                ["Address"] = client.Address,
                ["CreatedBy"] = client.CreatedBy,
                ["CreatedAtUtc"] = client.CreatedAtUtc.ToString("O"),
                ["UpdatedAtUtc"] = client.UpdatedAtUtc.ToString("O"),
                ["IsActive"] = client.IsActive
            };

            _excelService.AppendRowAsync("Clients", rowData).GetAwaiter().GetResult();
            return client;
        }
    }

    public Client UpdateClient(int id, UpdateClientRequest request, string updatedBy, bool canManageAll)
    {
        lock (_sync)
        {
            var current = GetClientById(id);
            if (current == null)
            {
                throw new InvalidOperationException("Client not found.");
            }

            if (!canManageAll && !string.Equals(current.CreatedBy, updatedBy, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("You cannot update clients created by other users.");
            }

            var zoneCode = NormalizeCode(request.ZoneCode);
            EnsureZoneExists(zoneCode);

            var updated = current with
            {
                FullName = request.FullName.Trim(),
                Mobile = request.Mobile.Trim(),
                Phone = request.Phone?.Trim(),
                ZoneCode = zoneCode,
                CollectionDay = request.CollectionDay.Trim(),
                Address = request.Address.Trim(),
                UpdatedAtUtc = DateTime.UtcNow,
                IsActive = request.IsActive
            };

            _excelService.UpdateRowsAsync("Clients",
                row => Convert.ToInt32(row["Id"]) == id,
                row =>
                {
                    row["FullName"] = updated.FullName;
                    row["Mobile"] = updated.Mobile;
                    row["Phone"] = updated.Phone;
                    row["ZoneCode"] = updated.ZoneCode;
                    row["CollectionDay"] = updated.CollectionDay;
                    row["Address"] = updated.Address;
                    row["UpdatedAtUtc"] = updated.UpdatedAtUtc.ToString("O");
                    row["IsActive"] = updated.IsActive;
                }).GetAwaiter().GetResult();

            return updated;
        }
    }

    #endregion

    #region Sales

    public IReadOnlyList<SaleRecord> GetSalesForUser(string userName, bool manageAll)
    {
        lock (_sync)
        {
            var sales = LoadAllSales();
            RefreshCollectionStatuses(sales);
            
            return sales
                .Where(s => manageAll || string.Equals(s.SellerUserName, userName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.CreatedAtUtc)
                .ToArray();
        }
    }

    public IReadOnlyList<SaleRecord> GetSalesForReview()
    {
        lock (_sync)
        {
            var sales = LoadAllSales();
            return sales
                .Where(s => s.Status is SaleWorkflowStatus.Registered or SaleWorkflowStatus.Corrected)
                .OrderByDescending(s => s.UpdatedAtUtc)
                .ToArray();
        }
    }

    public SaleRecord? GetSaleById(int id)
    {
        lock (_sync)
        {
            var sales = LoadAllSales();
            return sales.FirstOrDefault(s => s.Id == id);
        }
    }

    public SaleRecord AddSale(CreateSaleRequest request, string userName, bool canRegisterDirectly)
    {
        lock (_sync)
        {
            var client = GetClientById(request.ClientId);
            if (client == null || !client.IsActive)
            {
                throw new InvalidOperationException("Client not found or inactive.");
            }

            var normalizedPayment = NormalizeCode(request.PaymentMethodCode);
            EnsurePaymentMethodExists(normalizedPayment);

            var items = BuildSaleItems(request.Items);
            var evidence = BuildEvidence(request.Evidence, request.IsDraft);
            var now = DateTime.UtcNow;

            var sales = LoadAllSales();
            var nextId = sales.Count == 0 ? 1 : sales.Max(s => s.Id) + 1;

            var sale = new SaleRecord
            {
                Id = nextId,
                SaleNumber = $"VTA-{now:yyyyMMdd}-{nextId:D5}",
                ClientId = request.ClientId,
                SellerUserName = userName,
                CollectorUserName = request.CollectorUserName?.Trim(),
                PaymentMethodCode = normalizedPayment,
                CollectionDay = request.CollectionDay.Trim(),
                Notes = request.Notes?.Trim(),
                Status = request.IsDraft || !canRegisterDirectly ? SaleWorkflowStatus.Draft : SaleWorkflowStatus.Registered,
                CollectionStatus = CollectionWorkflowStatus.Pending,
                Collectable = request.Collectable,
                SellerCommissionPercent = request.SellerCommissionPercent,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                TotalAmount = items.Sum(i => i.Subtotal),
                Evidence = evidence
            };

            sale.SetItems(items);
            sale.AddHistoryEntry(new SaleHistoryEntry(now, userName, SaleWorkflowStatus.Draft, sale.Status, null, "create"));

            SaveSale(sale);
            return sale;
        }
    }

    public SaleRecord UpdateSaleDraft(int id, UpdateSaleDraftRequest request, string userName, bool canManageAll)
    {
        lock (_sync)
        {
            var sale = GetSaleById(id);
            if (sale == null)
            {
                throw new InvalidOperationException("Sale not found.");
            }

            var canEditOwn = string.Equals(sale.SellerUserName, userName, StringComparison.OrdinalIgnoreCase);
            if (!canManageAll && !canEditOwn)
            {
                throw new UnauthorizedAccessException("You cannot edit this sale.");
            }

            if (sale.Status is SaleWorkflowStatus.Approved or SaleWorkflowStatus.Rejected or SaleWorkflowStatus.Closed)
            {
                throw new InvalidOperationException("This sale can no longer be edited in draft mode.");
            }

            EnsurePaymentMethodExists(NormalizeCode(request.PaymentMethodCode));
            var client = GetClientById(request.ClientId);
            if (client == null || !client.IsActive)
            {
                throw new InvalidOperationException("Client not found or inactive.");
            }

            var previousStatus = sale.Status;
            var nextStatus = request.SubmitForReview ? SaleWorkflowStatus.Corrected : SaleWorkflowStatus.Draft;

            sale.ClientId = request.ClientId;
            sale.PaymentMethodCode = NormalizeCode(request.PaymentMethodCode);
            sale.CollectionDay = request.CollectionDay.Trim();
            sale.CollectorUserName = request.CollectorUserName?.Trim();
            sale.Notes = request.Notes?.Trim();
            sale.Collectable = request.Collectable;
            sale.SellerCommissionPercent = request.SellerCommissionPercent;
            sale.Evidence = BuildEvidence(request.Evidence, !request.SubmitForReview);
            sale.SetItems(BuildSaleItems(request.Items));
            sale.TotalAmount = sale.Items.Sum(i => i.Subtotal);
            sale.Status = nextStatus;
            sale.UpdatedAtUtc = DateTime.UtcNow;
            sale.AddHistoryEntry(new SaleHistoryEntry(sale.UpdatedAtUtc, userName, previousStatus, nextStatus, request.ChangeReason?.Trim(), "update-draft"));

            SaveSale(sale);
            return sale;
        }
    }

    public SaleRecord ReviewSale(int id, ReviewSaleRequest request, string reviewer)
    {
        lock (_sync)
        {
            var sale = GetSaleById(id);
            if (sale == null)
            {
                throw new InvalidOperationException("Sale not found.");
            }

            if (sale.Status is not (SaleWorkflowStatus.Registered or SaleWorkflowStatus.Corrected))
            {
                throw new InvalidOperationException("Sale is not pending review.");
            }

            var target = request.Action.Trim().ToLowerInvariant() switch
            {
                "approve" => SaleWorkflowStatus.Approved,
                "observe" => SaleWorkflowStatus.Observed,
                "reject" => SaleWorkflowStatus.Rejected,
                _ => throw new InvalidOperationException("Action must be approve, observe or reject.")
            };

            if (target is SaleWorkflowStatus.Observed or SaleWorkflowStatus.Rejected && string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new InvalidOperationException("Reason is required for observe/reject.");
            }

            var previous = sale.Status;
            sale.Status = target;
            sale.UpdatedAtUtc = DateTime.UtcNow;
            sale.AddHistoryEntry(new SaleHistoryEntry(sale.UpdatedAtUtc, reviewer, previous, target, request.Reason?.Trim(), "review"));
            RefreshCollectionStatus(sale);
            
            SaveSale(sale);
            return sale;
        }
    }

    public SaleRecord AssignCollector(int id, AssignCollectorRequest request, string reviewer)
    {
        lock (_sync)
        {
            var sale = GetSaleById(id);
            if (sale == null)
            {
                throw new InvalidOperationException("Sale not found.");
            }

            sale.CollectorUserName = request.CollectorUserName.Trim();
            sale.UpdatedAtUtc = DateTime.UtcNow;
            sale.AddHistoryEntry(new SaleHistoryEntry(sale.UpdatedAtUtc, reviewer, sale.Status, sale.Status, request.Reason?.Trim(), "assign-collector"));
            
            SaveSale(sale);
            return sale;
        }
    }

    #endregion

    #region Collections

    public IReadOnlyList<CollectionSummary> GetCollectionPortfolio(string userName, bool manageAll)
    {
        lock (_sync)
        {
            var sales = LoadAllSales();
            RefreshCollectionStatuses(sales);
            
            return sales
                .Where(s => s.Status == SaleWorkflowStatus.Approved)
                .Where(s => manageAll || string.Equals(s.CollectorUserName, userName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.UpdatedAtUtc)
                .Select(MapCollectionSummary)
                .ToArray();
        }
    }

    public SaleRecord RegisterCollection(RegisterCollectionRequest request, string collectorUserName)
    {
        lock (_sync)
        {
            if (request.Amount <= 0)
            {
                throw new InvalidOperationException("Collection amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(request.Coordinates))
            {
                throw new InvalidOperationException("Coordinates are required for collection.");
            }

            var sale = GetSaleById(request.SaleId);
            if (sale == null)
            {
                throw new InvalidOperationException("Sale not found.");
            }

            if (sale.Status != SaleWorkflowStatus.Approved)
            {
                throw new InvalidOperationException("Only approved sales can receive collections.");
            }

            if (sale.RemainingAmount <= 0)
            {
                throw new InvalidOperationException("Sale is already fully collected.");
            }

            if (request.Amount > sale.RemainingAmount)
            {
                throw new InvalidOperationException("Collection amount cannot exceed remaining balance.");
            }

            var collections = LoadCollectionsForSale(sale.Id);
            var nextId = collections.Count == 0 ? 1 : collections.Max(c => c.Id) + 1;

            var entry = new CollectionEntry(
                Id: nextId,
                SaleId: sale.Id,
                Amount: request.Amount,
                Coordinates: request.Coordinates.Trim(),
                Notes: request.Notes?.Trim(),
                CollectedBy: collectorUserName,
                CollectedAtUtc: request.CollectedAtUtc ?? DateTime.UtcNow,
                CapturedAtUtc: DateTime.UtcNow);

            sale.AddCollectionEntry(entry);
            sale.CollectedAmount += request.Amount;
            sale.FirstCollectionAtUtc ??= entry.CollectedAtUtc;
            sale.CollectorUserName ??= collectorUserName;
            sale.UpdatedAtUtc = DateTime.UtcNow;
            RefreshCollectionStatus(sale);

            if (sale.RemainingAmount <= 0)
            {
                var previous = sale.Status;
                sale.Status = SaleWorkflowStatus.Closed;
                sale.AddHistoryEntry(new SaleHistoryEntry(sale.UpdatedAtUtc, collectorUserName, previous, SaleWorkflowStatus.Closed, "Auto close after full collection", "auto-close"));
            }

            SaveSale(sale);
            SaveCollection(entry);
            return sale;
        }
    }

    public int ReassignPortfolio(ReassignPortfolioRequest request, string supervisorUserName)
    {
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(request.FromCollector) || string.IsNullOrWhiteSpace(request.ToCollector))
            {
                throw new InvalidOperationException("Both collectors are required.");
            }

            var fromCollector = request.FromCollector.Trim();
            var toCollector = request.ToCollector.Trim();
            var idSet = request.SaleIds?.ToHashSet() ?? [];

            var sales = LoadAllSales();
            var candidates = sales
                .Where(s => s.Status == SaleWorkflowStatus.Approved)
                .Where(s => s.CollectionStatus is CollectionWorkflowStatus.Pending or CollectionWorkflowStatus.Partial or CollectionWorkflowStatus.Overdue)
                .Where(s => string.Equals(s.CollectorUserName, fromCollector, StringComparison.OrdinalIgnoreCase))
                .Where(s => idSet.Count == 0 || idSet.Contains(s.Id))
                .ToArray();

            foreach (var sale in candidates)
            {
                sale.CollectorUserName = toCollector;
                sale.UpdatedAtUtc = DateTime.UtcNow;
                sale.AddHistoryEntry(new SaleHistoryEntry(sale.UpdatedAtUtc, supervisorUserName, sale.Status, sale.Status, request.Reason?.Trim(), "reassign-portfolio"));
                SaveSale(sale);
            }

            return candidates.Length;
        }
    }

    #endregion

    #region Dashboard & Sync

    public DashboardSummary GetDashboardSummary()
    {
        lock (_sync)
        {
            var sales = LoadAllSales();
            RefreshCollectionStatuses(sales);
            
            var totalSales = sales.Count;
            var approvedSales = sales.Where(s => s.Status == SaleWorkflowStatus.Approved || s.Status == SaleWorkflowStatus.Closed).ToArray();
            var totalSalesAmount = approvedSales.Sum(s => s.TotalAmount);
            var totalCollectedAmount = approvedSales.Sum(s => s.CollectedAmount);
            var totalPendingAmount = approvedSales.Sum(s => s.RemainingAmount);
            var pendingCollections = approvedSales.Count(s => s.CollectionStatus == CollectionWorkflowStatus.Pending);
            var paidCollections = approvedSales.Count(s => s.CollectionStatus == CollectionWorkflowStatus.Paid);
            var overdueCollections = approvedSales.Count(s => s.CollectionStatus == CollectionWorkflowStatus.Overdue);
            var activeSellers = sales.Select(s => s.SellerUserName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var activeCollectors = sales.Where(s => !string.IsNullOrWhiteSpace(s.CollectorUserName)).Select(s => s.CollectorUserName!).Distinct(StringComparer.OrdinalIgnoreCase).Count();

            return new DashboardSummary(
                TotalSales: totalSales,
                TotalSalesAmount: totalSalesAmount,
                TotalCollectedAmount: totalCollectedAmount,
                TotalPendingAmount: totalPendingAmount,
                PendingCollections: pendingCollections,
                PaidCollections: paidCollections,
                OverdueCollections: overdueCollections,
                ActiveSellers: activeSellers,
                ActiveCollectors: activeCollectors);
        }
    }

    public SyncPayload GetSyncPayload()
    {
        lock (_sync)
        {
            var sales = LoadAllSales();
            RefreshCollectionStatuses(sales);
            
            return new SyncPayload(
                CatalogVersion: GetCatalogVersion(),
                GeneratedAtUtc: DateTime.UtcNow,
                Zones: GetZones(false).ToArray(),
                Products: GetProducts(false).ToArray(),
                PaymentMethods: GetPaymentMethods(false).ToArray(),
                Clients: GetClients(false, null).ToArray(),
                Sales: sales.OrderByDescending(s => s.UpdatedAtUtc).ToArray());
        }
    }

    #endregion

    #region Private Helpers

    private List<SaleRecord> LoadAllSales()
    {
        var salesData = _excelService.ReadSheetAsync("Sales").GetAwaiter().GetResult();
        var itemsData = _excelService.ReadSheetAsync("SaleItems").GetAwaiter().GetResult();
        var historyData = _excelService.ReadSheetAsync("SaleHistory").GetAwaiter().GetResult();
        var collectionsData = _excelService.ReadSheetAsync("Collections").GetAwaiter().GetResult();

        var sales = new List<SaleRecord>();

        foreach (var saleRow in salesData)
        {
            var saleId = Convert.ToInt32(saleRow["Id"]);
            var sale = MapToSaleRecord(saleRow);

            // Cargar items
            var items = itemsData
                .Where(row => Convert.ToInt32(row["SaleId"]) == saleId)
                .Select(MapToSaleItem)
                .ToList();
            sale.SetItems(items);

            // Cargar history
            var history = historyData
                .Where(row => Convert.ToInt32(row["SaleId"]) == saleId)
                .Select(MapToSaleHistoryEntry)
                .OrderBy(h => h.TimestampUtc)
                .ToList();
            foreach (var entry in history)
            {
                sale.AddHistoryEntry(entry);
            }

            // Cargar collections
            var collections = collectionsData
                .Where(row => Convert.ToInt32(row["SaleId"]) == saleId)
                .Select(MapToCollectionEntry)
                .OrderBy(c => c.CollectedAtUtc)
                .ToList();
            foreach (var entry in collections)
            {
                sale.AddCollectionEntry(entry);
            }

            sales.Add(sale);
        }

        return sales;
    }

    private List<CollectionEntry> LoadCollectionsForSale(int saleId)
    {
        var collectionsData = _excelService.ReadSheetAsync("Collections").GetAwaiter().GetResult();
        return collectionsData
            .Where(row => Convert.ToInt32(row["SaleId"]) == saleId)
            .Select(MapToCollectionEntry)
            .ToList();
    }

    private void SaveSale(SaleRecord sale)
    {
        // Guardar venta
        var salesData = _excelService.ReadSheetAsync("Sales").GetAwaiter().GetResult();
        var existingRow = salesData.FirstOrDefault(row => Convert.ToInt32(row["Id"]) == sale.Id);

        var saleRowData = new Dictionary<string, object?>
        {
            ["Id"] = sale.Id,
            ["SaleNumber"] = sale.SaleNumber,
            ["ClientId"] = sale.ClientId,
            ["SellerUserName"] = sale.SellerUserName,
            ["CollectorUserName"] = sale.CollectorUserName,
            ["PaymentMethodCode"] = sale.PaymentMethodCode,
            ["CollectionDay"] = sale.CollectionDay,
            ["Notes"] = sale.Notes,
            ["Status"] = (int)sale.Status,
            ["CollectionStatus"] = (int)sale.CollectionStatus,
            ["Collectable"] = sale.Collectable,
            ["SellerCommissionPercent"] = sale.SellerCommissionPercent,
            ["CreatedAtUtc"] = sale.CreatedAtUtc.ToString("O"),
            ["UpdatedAtUtc"] = sale.UpdatedAtUtc.ToString("O"),
            ["FirstCollectionAtUtc"] = sale.FirstCollectionAtUtc?.ToString("O"),
            ["TotalAmount"] = sale.TotalAmount,
            ["CollectedAmount"] = sale.CollectedAmount,
            ["PrimaryCoordinates"] = sale.Evidence.PrimaryCoordinates,
            ["SecondaryCoordinates"] = sale.Evidence.SecondaryCoordinates,
            ["LocationUrl"] = sale.Evidence.LocationUrl,
            ["PhotoUrls"] = JsonSerializer.Serialize(sale.Evidence.PhotoUrls)
        };

        if (existingRow != null)
        {
            _excelService.UpdateRowsAsync("Sales",
                row => Convert.ToInt32(row["Id"]) == sale.Id,
                row =>
                {
                    foreach (var kvp in saleRowData)
                    {
                        row[kvp.Key] = kvp.Value;
                    }
                }).GetAwaiter().GetResult();
        }
        else
        {
            _excelService.AppendRowAsync("Sales", saleRowData).GetAwaiter().GetResult();
        }

        // Guardar items
        SaveSaleItems(sale.Id, sale.Items);

        // Guardar history
        SaveSaleHistory(sale.Id, sale.History);
    }

    private void SaveSaleItems(int saleId, IReadOnlyList<SaleItem> items)
    {
        // Eliminar items existentes
        _excelService.DeleteRowsAsync("SaleItems", row => Convert.ToInt32(row["SaleId"]) == saleId).GetAwaiter().GetResult();

        // Agregar items actuales
        foreach (var item in items)
        {
            var itemData = new Dictionary<string, object?>
            {
                ["SaleId"] = saleId,
                ["ProductId"] = item.ProductId,
                ["ProductCode"] = item.ProductCode,
                ["ProductName"] = item.ProductName,
                ["Quantity"] = item.Quantity,
                ["UnitPrice"] = item.UnitPrice,
                ["Subtotal"] = item.Subtotal
            };
            _excelService.AppendRowAsync("SaleItems", itemData).GetAwaiter().GetResult();
        }
    }

    private void SaveSaleHistory(int saleId, IReadOnlyList<SaleHistoryEntry> history)
    {
        // Eliminar historial existente
        _excelService.DeleteRowsAsync("SaleHistory", row => Convert.ToInt32(row["SaleId"]) == saleId).GetAwaiter().GetResult();

        // Agregar historial actual
        foreach (var entry in history)
        {
            var historyData = new Dictionary<string, object?>
            {
                ["SaleId"] = saleId,
                ["TimestampUtc"] = entry.TimestampUtc.ToString("O"),
                ["UserName"] = entry.UserName,
                ["FromStatus"] = (int)entry.From,
                ["ToStatus"] = (int)entry.To,
                ["Reason"] = entry.Reason,
                ["Action"] = entry.Action
            };
            _excelService.AppendRowAsync("SaleHistory", historyData).GetAwaiter().GetResult();
        }
    }

    private void SaveCollection(CollectionEntry collection)
    {
        var collectionData = new Dictionary<string, object?>
        {
            ["Id"] = collection.Id,
            ["SaleId"] = collection.SaleId,
            ["Amount"] = collection.Amount,
            ["Coordinates"] = collection.Coordinates,
            ["Notes"] = collection.Notes,
            ["CollectedBy"] = collection.CollectedBy,
            ["CollectedAtUtc"] = collection.CollectedAtUtc.ToString("O"),
            ["CapturedAtUtc"] = collection.CapturedAtUtc.ToString("O")
        };
        _excelService.AppendRowAsync("Collections", collectionData).GetAwaiter().GetResult();
    }

    private IReadOnlyList<SaleItem> BuildSaleItems(IReadOnlyList<CreateSaleItemRequest> requests)
    {
        if (requests.Count == 0)
        {
            throw new InvalidOperationException("At least one sale item is required.");
        }

        var products = GetProducts(false);
        var items = new List<SaleItem>(requests.Count);
        
        foreach (var request in requests)
        {
            if (request.Quantity <= 0)
            {
                throw new InvalidOperationException("Item quantity must be greater than zero.");
            }

            var product = products.FirstOrDefault(p => p.Id == request.ProductId && p.IsActive)
                ?? throw new InvalidOperationException($"Product {request.ProductId} not found or inactive.");

            var unitPrice = request.UnitPrice ?? product.Price;
            if (unitPrice <= 0)
            {
                throw new InvalidOperationException("Unit price must be greater than zero.");
            }

            var subtotal = unitPrice * request.Quantity;
            items.Add(new SaleItem(product.Id, product.Code, product.Name, request.Quantity, unitPrice, subtotal));
        }

        return items;
    }

    private static SaleEvidence BuildEvidence(SaleEvidenceRequest request, bool allowIncomplete)
    {
        var coordinates = request.PrimaryCoordinates?.Trim() ?? string.Empty;
        var photos = request.PhotoUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .ToArray();

        if (!allowIncomplete)
        {
            if (string.IsNullOrWhiteSpace(coordinates))
            {
                throw new InvalidOperationException("Primary coordinates are required.");
            }

            if (photos.Length == 0)
            {
                throw new InvalidOperationException("At least one photo is required.");
            }
        }

        return new SaleEvidence(coordinates, request.SecondaryCoordinates?.Trim(), request.LocationUrl?.Trim(), photos);
    }

    private static string NormalizeCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Code is required.");
        }

        return value.Trim().ToUpperInvariant();
    }

    private static void EnsureUniqueCode(IEnumerable<string> existingCodes, string code, string label)
    {
        var normalized = NormalizeCode(code);
        if (existingCodes.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Duplicate {label}: {normalized}");
        }
    }

    private void EnsureZoneExists(string code)
    {
        var zones = GetZones(false);
        if (!zones.Any(z => z.Code == code && z.IsActive))
        {
            throw new InvalidOperationException($"Zone not found or inactive: {code}");
        }
    }

    private void EnsurePaymentMethodExists(string code)
    {
        var paymentMethods = GetPaymentMethods(false);
        if (!paymentMethods.Any(m => m.Code == code && m.IsActive))
        {
            throw new InvalidOperationException($"Payment method not found or inactive: {code}");
        }
    }

    private static CollectionSummary MapCollectionSummary(SaleRecord sale)
    {
        return new CollectionSummary(
            SaleId: sale.Id,
            SaleNumber: sale.SaleNumber,
            ClientId: sale.ClientId,
            SellerUserName: sale.SellerUserName,
            CollectorUserName: sale.CollectorUserName,
            CollectionStatus: sale.CollectionStatus,
            TotalAmount: sale.TotalAmount,
            CollectedAmount: sale.CollectedAmount,
            RemainingAmount: sale.RemainingAmount,
            CollectionDay: sale.CollectionDay,
            UpdatedAtUtc: sale.UpdatedAtUtc);
    }

    private void RefreshCollectionStatuses(List<SaleRecord> sales)
    {
        foreach (var sale in sales)
        {
            RefreshCollectionStatus(sale);
        }
    }

    private static void RefreshCollectionStatus(SaleRecord sale)
    {
        if (sale.Status is SaleWorkflowStatus.Rejected)
        {
            sale.CollectionStatus = CollectionWorkflowStatus.Uncollectible;
            return;
        }

        if (sale.Status is not (SaleWorkflowStatus.Approved or SaleWorkflowStatus.Closed))
        {
            sale.CollectionStatus = CollectionWorkflowStatus.Pending;
            return;
        }

        if (sale.RemainingAmount <= 0)
        {
            sale.CollectionStatus = CollectionWorkflowStatus.Paid;
            return;
        }

        if (sale.CollectedAmount > 0)
        {
            sale.CollectionStatus = CollectionWorkflowStatus.Partial;
            return;
        }

        var overdueLimit = sale.CreatedAtUtc.AddDays(7);
        sale.CollectionStatus = DateTime.UtcNow > overdueLimit
            ? CollectionWorkflowStatus.Overdue
            : CollectionWorkflowStatus.Pending;
    }

    private void TouchCatalogVersion()
    {
        // Incrementar versión (se puede almacenar en una hoja separada o en configuración)
        // Por simplicidad, podemos usar un contador en memoria o persistirlo
    }

    private int GetCatalogVersion()
    {
        // Por ahora retornamos 1, pero se podría persistir en una hoja de configuración
        return 1;
    }

    #endregion

    #region Mappers

    private static Zone MapToZone(Dictionary<string, object?> row)
    {
        return new Zone(
            Id: Convert.ToInt32(row["Id"]),
            Code: row["Code"]?.ToString() ?? string.Empty,
            Name: row["Name"]?.ToString() ?? string.Empty,
            IsActive: Convert.ToBoolean(row["IsActive"]));
    }

    private static Product MapToProduct(Dictionary<string, object?> row)
    {
        return new Product(
            Id: Convert.ToInt32(row["Id"]),
            Code: row["Code"]?.ToString() ?? string.Empty,
            Name: row["Name"]?.ToString() ?? string.Empty,
            Price: Convert.ToDecimal(row["Price"]),
            IsActive: Convert.ToBoolean(row["IsActive"]));
    }

    private static PaymentMethod MapToPaymentMethod(Dictionary<string, object?> row)
    {
        return new PaymentMethod(
            Id: Convert.ToInt32(row["Id"]),
            Code: row["Code"]?.ToString() ?? string.Empty,
            Name: row["Name"]?.ToString() ?? string.Empty,
            IsActive: Convert.ToBoolean(row["IsActive"]));
    }

    private static Client MapToClient(Dictionary<string, object?> row)
    {
        return new Client(
            Id: Convert.ToInt32(row["Id"]),
            FullName: row["FullName"]?.ToString() ?? string.Empty,
            Mobile: row["Mobile"]?.ToString() ?? string.Empty,
            Phone: row["Phone"]?.ToString(),
            ZoneCode: row["ZoneCode"]?.ToString() ?? string.Empty,
            CollectionDay: row["CollectionDay"]?.ToString() ?? string.Empty,
            Address: row["Address"]?.ToString() ?? string.Empty,
            CreatedBy: row["CreatedBy"]?.ToString() ?? string.Empty,
            CreatedAtUtc: DateTime.Parse(row["CreatedAtUtc"]?.ToString() ?? DateTime.UtcNow.ToString("O")),
            UpdatedAtUtc: DateTime.Parse(row["UpdatedAtUtc"]?.ToString() ?? DateTime.UtcNow.ToString("O")),
            IsActive: Convert.ToBoolean(row["IsActive"]));
    }

    private static SaleRecord MapToSaleRecord(Dictionary<string, object?> row)
    {
        var photoUrlsJson = row["PhotoUrls"]?.ToString() ?? "[]";
        var photoUrls = JsonSerializer.Deserialize<string[]>(photoUrlsJson) ?? Array.Empty<string>();

        return new SaleRecord
        {
            Id = Convert.ToInt32(row["Id"]),
            SaleNumber = row["SaleNumber"]?.ToString() ?? string.Empty,
            ClientId = Convert.ToInt32(row["ClientId"]),
            SellerUserName = row["SellerUserName"]?.ToString() ?? string.Empty,
            CollectorUserName = row["CollectorUserName"]?.ToString(),
            PaymentMethodCode = row["PaymentMethodCode"]?.ToString() ?? string.Empty,
            CollectionDay = row["CollectionDay"]?.ToString() ?? string.Empty,
            Notes = row["Notes"]?.ToString(),
            Status = (SaleWorkflowStatus)Convert.ToInt32(row["Status"]),
            CollectionStatus = (CollectionWorkflowStatus)Convert.ToInt32(row["CollectionStatus"]),
            Collectable = Convert.ToBoolean(row["Collectable"]),
            SellerCommissionPercent = Convert.ToDecimal(row["SellerCommissionPercent"]),
            CreatedAtUtc = DateTime.Parse(row["CreatedAtUtc"]?.ToString() ?? DateTime.UtcNow.ToString("O")),
            UpdatedAtUtc = DateTime.Parse(row["UpdatedAtUtc"]?.ToString() ?? DateTime.UtcNow.ToString("O")),
            FirstCollectionAtUtc = row["FirstCollectionAtUtc"]?.ToString() is string dateStr ? DateTime.Parse(dateStr) : null,
            TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
            CollectedAmount = Convert.ToDecimal(row["CollectedAmount"]),
            Evidence = new SaleEvidence(
                PrimaryCoordinates: row["PrimaryCoordinates"]?.ToString() ?? string.Empty,
                SecondaryCoordinates: row["SecondaryCoordinates"]?.ToString(),
                LocationUrl: row["LocationUrl"]?.ToString(),
                PhotoUrls: photoUrls)
        };
    }

    private static SaleItem MapToSaleItem(Dictionary<string, object?> row)
    {
        return new SaleItem(
            ProductId: Convert.ToInt32(row["ProductId"]),
            ProductCode: row["ProductCode"]?.ToString() ?? string.Empty,
            ProductName: row["ProductName"]?.ToString() ?? string.Empty,
            Quantity: Convert.ToInt32(row["Quantity"]),
            UnitPrice: Convert.ToDecimal(row["UnitPrice"]),
            Subtotal: Convert.ToDecimal(row["Subtotal"]));
    }

    private static SaleHistoryEntry MapToSaleHistoryEntry(Dictionary<string, object?> row)
    {
        return new SaleHistoryEntry(
            TimestampUtc: DateTime.Parse(row["TimestampUtc"]?.ToString() ?? DateTime.UtcNow.ToString("O")),
            UserName: row["UserName"]?.ToString() ?? string.Empty,
            From: (SaleWorkflowStatus)Convert.ToInt32(row["FromStatus"]),
            To: (SaleWorkflowStatus)Convert.ToInt32(row["ToStatus"]),
            Reason: row["Reason"]?.ToString(),
            Action: row["Action"]?.ToString() ?? string.Empty);
    }

    private static CollectionEntry MapToCollectionEntry(Dictionary<string, object?> row)
    {
        return new CollectionEntry(
            Id: Convert.ToInt32(row["Id"]),
            SaleId: Convert.ToInt32(row["SaleId"]),
            Amount: Convert.ToDecimal(row["Amount"]),
            Coordinates: row["Coordinates"]?.ToString() ?? string.Empty,
            Notes: row["Notes"]?.ToString(),
            CollectedBy: row["CollectedBy"]?.ToString() ?? string.Empty,
            CollectedAtUtc: DateTime.Parse(row["CollectedAtUtc"]?.ToString() ?? DateTime.UtcNow.ToString("O")),
            CapturedAtUtc: DateTime.Parse(row["CapturedAtUtc"]?.ToString() ?? DateTime.UtcNow.ToString("O")));
    }

    #endregion
}
