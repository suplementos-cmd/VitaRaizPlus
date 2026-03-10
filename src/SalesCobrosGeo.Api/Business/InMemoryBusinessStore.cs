using SalesCobrosGeo.Api.Contracts.Catalogs;
using SalesCobrosGeo.Api.Contracts.Clients;
using SalesCobrosGeo.Api.Contracts.Collections;
using SalesCobrosGeo.Api.Contracts.Sales;

namespace SalesCobrosGeo.Api.Business;

public sealed class InMemoryBusinessStore : IBusinessStore
{
    private readonly Lock _sync = new();
    private readonly List<Zone> _zones =
    [
        new(1, "CENTRO", "Zona Centro", true),
        new(2, "NORTE", "Zona Norte", true),
        new(3, "SUR", "Zona Sur", true)
    ];

    private readonly List<Product> _products =
    [
        new(1, "VRP-001", "VitaRaiz Plus 30 caps", 120m, true),
        new(2, "VRP-002", "VitaRaiz Plus 60 caps", 210m, true)
    ];

    private readonly List<PaymentMethod> _paymentMethods =
    [
        new(1, "CONTADO", "Contado", true),
        new(2, "CREDITO", "Credito", true),
        new(3, "TRANSFER", "Transferencia", true)
    ];

    private readonly List<Client> _clients = [];
    private readonly List<SaleRecord> _sales = [];
    private int _catalogVersion = 1;
    private int _clientIdentity;
    private int _saleIdentity;
    private int _collectionIdentity;

    public CatalogSnapshot GetCatalogSnapshot()
    {
        lock (_sync)
        {
            return new CatalogSnapshot(
                _catalogVersion,
                _zones.ToArray(),
                _products.ToArray(),
                _paymentMethods.ToArray());
        }
    }

    public IReadOnlyList<Zone> GetZones(bool includeInactive)
    {
        lock (_sync)
        {
            return _zones.Where(x => includeInactive || x.IsActive).ToArray();
        }
    }

    public IReadOnlyList<Product> GetProducts(bool includeInactive)
    {
        lock (_sync)
        {
            return _products.Where(x => includeInactive || x.IsActive).ToArray();
        }
    }

    public IReadOnlyList<PaymentMethod> GetPaymentMethods(bool includeInactive)
    {
        lock (_sync)
        {
            return _paymentMethods.Where(x => includeInactive || x.IsActive).ToArray();
        }
    }

    public Zone AddZone(CreateZoneRequest request)
    {
        lock (_sync)
        {
            EnsureUniqueCode(_zones.Select(x => x.Code), request.Code, "zone code");
            var nextId = _zones.Count == 0 ? 1 : _zones.Max(x => x.Id) + 1;
            var zone = new Zone(nextId, NormalizeCode(request.Code), request.Name.Trim(), request.IsActive);
            _zones.Add(zone);
            TouchCatalogVersion();
            return zone;
        }
    }

    public Zone UpdateZone(int id, UpdateZoneRequest request)
    {
        lock (_sync)
        {
            var index = _zones.FindIndex(x => x.Id == id);
            if (index < 0)
            {
                throw new InvalidOperationException("Zone not found.");
            }

            var normalizedCode = NormalizeCode(request.Code);
            EnsureUniqueCode(_zones.Where(x => x.Id != id).Select(x => x.Code), normalizedCode, "zone code");

            var updated = new Zone(id, normalizedCode, request.Name.Trim(), request.IsActive);
            _zones[index] = updated;
            TouchCatalogVersion();
            return updated;
        }
    }

    public Product AddProduct(CreateProductRequest request)
    {
        lock (_sync)
        {
            EnsureUniqueCode(_products.Select(x => x.Code), request.Code, "product code");
            var nextId = _products.Count == 0 ? 1 : _products.Max(x => x.Id) + 1;
            var product = new Product(nextId, NormalizeCode(request.Code), request.Name.Trim(), request.Price, request.IsActive);
            _products.Add(product);
            TouchCatalogVersion();
            return product;
        }
    }

    public Product UpdateProduct(int id, UpdateProductRequest request)
    {
        lock (_sync)
        {
            var index = _products.FindIndex(x => x.Id == id);
            if (index < 0)
            {
                throw new InvalidOperationException("Product not found.");
            }

            var normalizedCode = NormalizeCode(request.Code);
            EnsureUniqueCode(_products.Where(x => x.Id != id).Select(x => x.Code), normalizedCode, "product code");

            var updated = new Product(id, normalizedCode, request.Name.Trim(), request.Price, request.IsActive);
            _products[index] = updated;
            TouchCatalogVersion();
            return updated;
        }
    }

    public PaymentMethod AddPaymentMethod(CreatePaymentMethodRequest request)
    {
        lock (_sync)
        {
            EnsureUniqueCode(_paymentMethods.Select(x => x.Code), request.Code, "payment method code");
            var nextId = _paymentMethods.Count == 0 ? 1 : _paymentMethods.Max(x => x.Id) + 1;
            var method = new PaymentMethod(nextId, NormalizeCode(request.Code), request.Name.Trim(), request.IsActive);
            _paymentMethods.Add(method);
            TouchCatalogVersion();
            return method;
        }
    }

    public PaymentMethod UpdatePaymentMethod(int id, UpdatePaymentMethodRequest request)
    {
        lock (_sync)
        {
            var index = _paymentMethods.FindIndex(x => x.Id == id);
            if (index < 0)
            {
                throw new InvalidOperationException("Payment method not found.");
            }

            var normalizedCode = NormalizeCode(request.Code);
            EnsureUniqueCode(_paymentMethods.Where(x => x.Id != id).Select(x => x.Code), normalizedCode, "payment method code");

            var updated = new PaymentMethod(id, normalizedCode, request.Name.Trim(), request.IsActive);
            _paymentMethods[index] = updated;
            TouchCatalogVersion();
            return updated;
        }
    }

    public IReadOnlyList<Client> GetClients(bool includeInactive, string? zoneCode)
    {
        lock (_sync)
        {
            var normalizedZone = string.IsNullOrWhiteSpace(zoneCode) ? null : NormalizeCode(zoneCode);
            return _clients
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
            return _clients.FirstOrDefault(x => x.Id == id);
        }
    }

    public Client AddClient(CreateClientRequest request, string createdBy)
    {
        lock (_sync)
        {
            var zoneCode = NormalizeCode(request.ZoneCode);
            EnsureZoneExists(zoneCode);

            var now = DateTime.UtcNow;
            var client = new Client(
                Id: ++_clientIdentity,
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

            _clients.Add(client);
            return client;
        }
    }

    public Client UpdateClient(int id, UpdateClientRequest request, string updatedBy, bool canManageAll)
    {
        lock (_sync)
        {
            var index = _clients.FindIndex(x => x.Id == id);
            if (index < 0)
            {
                throw new InvalidOperationException("Client not found.");
            }

            var current = _clients[index];
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

            _clients[index] = updated;
            return updated;
        }
    }

    public IReadOnlyList<SaleRecord> GetSalesForUser(string userName, bool manageAll)
    {
        lock (_sync)
        {
            RefreshCollectionStatuses();
            return _sales
                .Where(s => manageAll || string.Equals(s.SellerUserName, userName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.CreatedAtUtc)
                .ToArray();
        }
    }

    public IReadOnlyList<SaleRecord> GetSalesForReview()
    {
        lock (_sync)
        {
            return _sales
                .Where(s => s.Status is SaleWorkflowStatus.Registered or SaleWorkflowStatus.Corrected)
                .OrderByDescending(s => s.UpdatedAtUtc)
                .ToArray();
        }
    }

    public SaleRecord? GetSaleById(int id)
    {
        lock (_sync)
        {
            return _sales.FirstOrDefault(x => x.Id == id);
        }
    }

    public SaleRecord AddSale(CreateSaleRequest request, string userName, bool canRegisterDirectly)
    {
        lock (_sync)
        {
            var client = _clients.FirstOrDefault(c => c.Id == request.ClientId && c.IsActive);
            if (client is null)
            {
                throw new InvalidOperationException("Client not found or inactive.");
            }

            var normalizedPayment = NormalizeCode(request.PaymentMethodCode);
            EnsurePaymentMethodExists(normalizedPayment);

            var items = BuildSaleItems(request.Items);
            var evidence = BuildEvidence(request.Evidence, request.IsDraft);
            var now = DateTime.UtcNow;

            var sale = new SaleRecord
            {
                Id = ++_saleIdentity,
                SaleNumber = $"VTA-{now:yyyyMMdd}-{_saleIdentity:D5}",
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

            sale.Items.AddRange(items);
            sale.History.Add(new SaleHistoryEntry(now, userName, SaleWorkflowStatus.Draft, sale.Status, null, "create"));

            _sales.Add(sale);
            return sale;
        }
    }

    public SaleRecord UpdateSaleDraft(int id, UpdateSaleDraftRequest request, string userName, bool canManageAll)
    {
        lock (_sync)
        {
            var sale = _sales.FirstOrDefault(s => s.Id == id) ?? throw new InvalidOperationException("Sale not found.");

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
            if (!_clients.Any(c => c.Id == request.ClientId && c.IsActive))
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
            sale.Items.Clear();
            sale.Items.AddRange(BuildSaleItems(request.Items));
            sale.TotalAmount = sale.Items.Sum(i => i.Subtotal);
            sale.Status = nextStatus;
            sale.UpdatedAtUtc = DateTime.UtcNow;
            sale.History.Add(new SaleHistoryEntry(sale.UpdatedAtUtc, userName, previousStatus, nextStatus, request.ChangeReason?.Trim(), "update-draft"));

            return sale;
        }
    }

    public SaleRecord ReviewSale(int id, ReviewSaleRequest request, string reviewer)
    {
        lock (_sync)
        {
            var sale = _sales.FirstOrDefault(s => s.Id == id) ?? throw new InvalidOperationException("Sale not found.");
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
            sale.History.Add(new SaleHistoryEntry(sale.UpdatedAtUtc, reviewer, previous, target, request.Reason?.Trim(), "review"));
            RefreshCollectionStatus(sale);
            return sale;
        }
    }

    public SaleRecord AssignCollector(int id, AssignCollectorRequest request, string reviewer)
    {
        lock (_sync)
        {
            var sale = _sales.FirstOrDefault(s => s.Id == id) ?? throw new InvalidOperationException("Sale not found.");
            sale.CollectorUserName = request.CollectorUserName.Trim();
            sale.UpdatedAtUtc = DateTime.UtcNow;
            sale.History.Add(new SaleHistoryEntry(sale.UpdatedAtUtc, reviewer, sale.Status, sale.Status, request.Reason?.Trim(), "assign-collector"));
            return sale;
        }
    }

    public IReadOnlyList<CollectionSummary> GetCollectionPortfolio(string userName, bool manageAll)
    {
        lock (_sync)
        {
            RefreshCollectionStatuses();
            return _sales
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

            var sale = _sales.FirstOrDefault(s => s.Id == request.SaleId) ?? throw new InvalidOperationException("Sale not found.");
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

            var entry = new CollectionEntry(
                Id: ++_collectionIdentity,
                SaleId: sale.Id,
                Amount: request.Amount,
                Coordinates: request.Coordinates.Trim(),
                Notes: request.Notes?.Trim(),
                CollectedBy: collectorUserName,
                CollectedAtUtc: request.CollectedAtUtc ?? DateTime.UtcNow,
                CapturedAtUtc: DateTime.UtcNow);

            sale.Collections.Add(entry);
            sale.CollectedAmount += request.Amount;
            sale.FirstCollectionAtUtc ??= entry.CollectedAtUtc;
            sale.CollectorUserName ??= collectorUserName;
            sale.UpdatedAtUtc = DateTime.UtcNow;
            RefreshCollectionStatus(sale);

            if (sale.RemainingAmount <= 0)
            {
                var previous = sale.Status;
                sale.Status = SaleWorkflowStatus.Closed;
                sale.History.Add(new SaleHistoryEntry(sale.UpdatedAtUtc, collectorUserName, previous, SaleWorkflowStatus.Closed, "Auto close after full collection", "auto-close"));
            }

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

            var candidates = _sales
                .Where(s => s.Status == SaleWorkflowStatus.Approved)
                .Where(s => s.CollectionStatus is CollectionWorkflowStatus.Pending or CollectionWorkflowStatus.Partial or CollectionWorkflowStatus.Overdue)
                .Where(s => string.Equals(s.CollectorUserName, fromCollector, StringComparison.OrdinalIgnoreCase))
                .Where(s => idSet.Count == 0 || idSet.Contains(s.Id))
                .ToArray();

            foreach (var sale in candidates)
            {
                sale.CollectorUserName = toCollector;
                sale.UpdatedAtUtc = DateTime.UtcNow;
                sale.History.Add(new SaleHistoryEntry(sale.UpdatedAtUtc, supervisorUserName, sale.Status, sale.Status, request.Reason?.Trim(), "reassign-portfolio"));
            }

            return candidates.Length;
        }
    }

    public DashboardSummary GetDashboardSummary()
    {
        lock (_sync)
        {
            RefreshCollectionStatuses();
            var totalSales = _sales.Count;
            var approvedSales = _sales.Where(s => s.Status == SaleWorkflowStatus.Approved || s.Status == SaleWorkflowStatus.Closed).ToArray();
            var totalSalesAmount = approvedSales.Sum(s => s.TotalAmount);
            var totalCollectedAmount = approvedSales.Sum(s => s.CollectedAmount);
            var totalPendingAmount = approvedSales.Sum(s => s.RemainingAmount);
            var pendingCollections = approvedSales.Count(s => s.CollectionStatus == CollectionWorkflowStatus.Pending);
            var paidCollections = approvedSales.Count(s => s.CollectionStatus == CollectionWorkflowStatus.Paid);
            var overdueCollections = approvedSales.Count(s => s.CollectionStatus == CollectionWorkflowStatus.Overdue);
            var activeSellers = _sales.Select(s => s.SellerUserName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var activeCollectors = _sales.Where(s => !string.IsNullOrWhiteSpace(s.CollectorUserName)).Select(s => s.CollectorUserName!).Distinct(StringComparer.OrdinalIgnoreCase).Count();

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
            RefreshCollectionStatuses();
            return new SyncPayload(
                CatalogVersion: _catalogVersion,
                GeneratedAtUtc: DateTime.UtcNow,
                Zones: _zones.ToArray(),
                Products: _products.ToArray(),
                PaymentMethods: _paymentMethods.ToArray(),
                Clients: _clients.ToArray(),
                Sales: _sales.OrderByDescending(s => s.UpdatedAtUtc).ToArray());
        }
    }

    private IReadOnlyList<SaleItem> BuildSaleItems(IReadOnlyList<CreateSaleItemRequest> requests)
    {
        if (requests.Count == 0)
        {
            throw new InvalidOperationException("At least one sale item is required.");
        }

        var items = new List<SaleItem>(requests.Count);
        foreach (var request in requests)
        {
            if (request.Quantity <= 0)
            {
                throw new InvalidOperationException("Item quantity must be greater than zero.");
            }

            var product = _products.FirstOrDefault(p => p.Id == request.ProductId && p.IsActive)
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
        if (!_zones.Any(z => z.Code == code && z.IsActive))
        {
            throw new InvalidOperationException($"Zone not found or inactive: {code}");
        }
    }

    private void EnsurePaymentMethodExists(string code)
    {
        if (!_paymentMethods.Any(m => m.Code == code && m.IsActive))
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

    private void RefreshCollectionStatuses()
    {
        foreach (var sale in _sales)
        {
            RefreshCollectionStatus(sale);
        }
    }

    private void RefreshCollectionStatus(SaleRecord sale)
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
        _catalogVersion++;
    }
}
