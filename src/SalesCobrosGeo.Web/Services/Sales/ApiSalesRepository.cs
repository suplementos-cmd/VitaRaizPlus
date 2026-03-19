using SalesCobrosGeo.Web.Models.Maintenance;
using SalesCobrosGeo.Web.Models.Sales;
using SalesCobrosGeo.Api.Data;
using System.Net.Http.Json;

namespace SalesCobrosGeo.Web.Services.Sales;

/// <summary>
/// Implementación de ISalesRepository que consume la API
/// Fase 2: Unificación de datos transaccionales (Web → API → Excel)
/// </summary>
public sealed class ApiSalesRepository : ISalesRepository
{
    private readonly HttpClient _httpClient;
    private readonly ExcelDataService _excelService;
    private readonly ILogger<ApiSalesRepository> _logger;

    public ApiSalesRepository(
        HttpClient httpClient,
        ExcelDataService excelService,
        ILogger<ApiSalesRepository> logger)
    {
        _httpClient = httpClient;
        _excelService = excelService;
        _logger = logger;
    }

    #region Catalogs (direct Excel access for now)

    public SalesCatalogs GetCatalogs()
    {
        try
        {
            // Leer catálogos desde Excel (compartido con Web)
            var zones = _excelService.ReadSheetAsync("Zones").GetAwaiter().GetResult()
                .Where(row => GetBool(row, "IsActive") == true)
                .Select(row => new CatalogOption(GetString(row, "Code") ?? "", GetString(row, "Name") ?? ""))
                .ToList();
            
            var paymentMethods = _excelService.ReadSheetAsync("PaymentMethods").GetAwaiter().GetResult()
                .Where(row => GetBool(row, "IsActive") == true)
                .Select(row => new CatalogOption(GetString(row, "Code") ?? "", GetString(row, "Name") ?? ""))
                .ToList();
            
            var collectionDays = _excelService.ReadSheetAsync("WeekDays").GetAwaiter().GetResult()
                .Where(row => GetBool(row, "IsActive") == true)
                .Select(row => new CatalogOption(GetString(row, "Code") ?? "", GetString(row, "Label") ?? ""))
                .ToList();
            
            var products = _excelService.ReadSheetAsync("Products").GetAwaiter().GetResult()
                .Where(row => GetBool(row, "IsActive") == true)
                .Select(row => new ProductOption(
                    GetString(row, "Code") ?? "", 
                    GetString(row, "Name") ?? "", 
                    GetDecimal(row, "Price") ?? 0))
                .ToList();
            
            var users = _excelService.ReadSheetAsync("Users").GetAwaiter().GetResult()
                .Where(row => GetBool(row, "IsActive") == true && GetString(row, "UserName") != "admin")
                .Select(row => new CatalogOption(GetString(row, "UserName") ?? "", GetString(row, "DisplayName") ?? ""))
                .ToList();
            
            var saleStatuses = _excelService.ReadSheetAsync("SaleStatuses").GetAwaiter().GetResult()
                .Where(row => GetBool(row, "IsActive") == true)
                .Select(row => new CatalogOption(GetString(row, "Code") ?? "", GetString(row, "Label") ?? ""))
                .ToList();

            return new SalesCatalogs(zones, paymentMethods, collectionDays, products, users, users, saleStatuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener catálogos para ventas");
            return new SalesCatalogs([], [], [], [], [], [], []);
        }
    }

    public IReadOnlyList<MaintenanceCatalogRecord> GetMaintenanceCatalog(string section)
    {
        try
        {
            var sheetName = ResolveSheetName(section);
            if (string.IsNullOrEmpty(sheetName))
            {
                return Array.Empty<MaintenanceCatalogRecord>();
            }

            var rows = _excelService.ReadSheetAsync(sheetName).GetAwaiter().GetResult();
            return rows.Select(row => new MaintenanceCatalogRecord(
                (long)(GetInt(row, "Id") ?? 0),
                section,
                GetString(row, "Code") ?? "",
                GetString(row, "Name") ?? GetString(row, "Label") ?? "",
                GetDecimal(row, "Price"),
                GetBool(row, "IsActive") ?? true
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener catálogo de mantenimiento {Section}", section);
            return Array.Empty<MaintenanceCatalogRecord>();
        }
    }

    public MaintenanceCatalogRecord SaveMaintenanceCatalogItem(MaintenanceCatalogSaveInput input)
    {
        // TODO: Implement via API
        _logger.LogWarning("SaveMaintenanceCatalogItem no implementado aún en API");
        throw new NotImplementedException("Método pendiente de implementación en API");
    }

    public bool DeleteMaintenanceCatalogItem(string section, long id)
    {
        // TODO: Implement via API
        _logger.LogWarning("DeleteMaintenanceCatalogItem no implementado aún en API");
        throw new NotImplementedException("Método pendiente de implementación en API");
    }

    private string? ResolveSheetName(string section)
    {
        return section switch
        {
            "zones" => "Zones",
            "products" => "Products",
            "payments" => "PaymentMethods",
            "weekdays" => "WeekDays",
            "sale_statuses" => "SaleStatuses",
            "collection_statuses" => "CollectionStatuses",
            _ => null
        };
    }

    #endregion

    #region Sales

    public IReadOnlyList<SaleRecord> GetAll()
    {
        try
        {
            var response = _httpClient.GetFromJsonAsync<List<SaleRecord>>("api/sales").GetAwaiter().GetResult();
            return (IReadOnlyList<SaleRecord>?)response ?? Array.Empty<SaleRecord>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las ventas desde API");
            return Array.Empty<SaleRecord>();
        }
    }

    public SaleRecord? GetById(string idV)
    {
        try
        {
            var response = _httpClient.GetFromJsonAsync<SaleRecord>($"api/sales/{idV}").GetAwaiter().GetResult();
            return response;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener venta {IdV} desde API", idV);
            return null;
        }
    }

    public SaleRecord Create(SaleFormInput input)
    {
        try
        {
            var response = _httpClient.PostAsJsonAsync("api/sales", input).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            
            var sale = response.Content.ReadFromJsonAsync<SaleRecord>().GetAwaiter().GetResult();
            if (sale == null)
            {
                throw new Exception("API retornó null después de crear venta");
            }
            
            return sale;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear venta en API");
            throw;
        }
    }

    public SaleRecord Update(string idV, SaleFormInput input)
    {
        try
        {
            var response = _httpClient.PutAsJsonAsync($"api/sales/{idV}", input).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            
            var sale = response.Content.ReadFromJsonAsync<SaleRecord>().GetAwaiter().GetResult();
            if (sale == null)
            {
                throw new Exception("API retornó null después de actualizar venta");
            }
            
            return sale;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar venta {IdV} en API", idV);
            throw;
        }
    }

    #endregion

    #region Collections

    public IReadOnlyList<CollectionRecord> GetCollections(string? profile = null, string? idV = null)
    {
        try
        {
            List<CollectionRecord> collections;
            
            if (!string.IsNullOrEmpty(idV))
            {
                // Obtener cobros de una venta específica
                var response = _httpClient.GetFromJsonAsync<List<CollectionRecord>>($"api/sales/{idV}/collections").GetAwaiter().GetResult();
                collections = response ?? [];
            }
            else
            {
                // Obtener todos los cobros
                var response = _httpClient.GetFromJsonAsync<List<CollectionRecord>>("api/sales/collections").GetAwaiter().GetResult();
                collections = response ?? [];
            }

            // Filtrar por profile (cobrador) si se especifica
            if (!string.IsNullOrEmpty(profile))
            {
                collections = collections.Where(c => c.Usuario.Equals(profile, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return collections;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cobros desde API");
            return Array.Empty<CollectionRecord>();
        }
    }

    public CollectionRecord? GetCollectionById(string idCc)
    {
        try
        {
            var response = _httpClient.GetFromJsonAsync<CollectionRecord>($"api/sales/collections/{idCc}").GetAwaiter().GetResult();
            return response;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cobro {IdCc} desde API", idCc);
            return null;
        }
    }

    public CollectionRecord RegisterCollection(CollectionFormInput input)
    {
        try
        {
            var response = _httpClient.PostAsJsonAsync("api/sales/collections", input).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            
            var collection = response.Content.ReadFromJsonAsync<CollectionRecord>().GetAwaiter().GetResult();
            if (collection == null)
            {
                throw new Exception("API retornó null después de registrar cobro");
            }
            
            return collection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar cobro en API");
            throw;
        }
    }

    public CollectionRecord UpdateCollection(string idCc, CollectionFormInput input)
    {
        try
        {
            var response = _httpClient.PutAsJsonAsync($"api/sales/collections/{idCc}", input).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            
            var collection = response.Content.ReadFromJsonAsync<CollectionRecord>().GetAwaiter().GetResult();
            if (collection == null)
            {
                throw new Exception("API retornó null después de actualizar cobro");
            }
            
            return collection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar cobro {IdCc} en API", idCc);
            throw;
        }
    }

    #endregion

    #region Collector Portfolio

    public IReadOnlyList<CollectorPortfolioItem> GetCollectorPortfolio(string? profile)
    {
        try
        {
            var endpoint = string.IsNullOrEmpty(profile)
                ? "api/sales/portfolio"
                : $"api/sales/portfolio/{profile}";
            
            var response = _httpClient.GetFromJsonAsync<List<CollectorPortfolioItem>>(endpoint).GetAwaiter().GetResult();
            return (IReadOnlyList<CollectorPortfolioItem>?)response ?? Array.Empty<CollectorPortfolioItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener portafolio desde API");
            return Array.Empty<CollectorPortfolioItem>();
        }
    }

    public CollectorPortfolioItem? GetPortfolioItem(string idV, string? profile)
    {
        try
        {
            var query = string.IsNullOrEmpty(profile) ? "" : $"?collector={profile}";
            var response = _httpClient.GetFromJsonAsync<CollectorPortfolioItem>($"api/sales/portfolio/item/{idV}{query}").GetAwaiter().GetResult();
            return response;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener item {IdV} del portafolio desde API", idV);
            return null;
        }
    }

    public IReadOnlyList<CatalogOption> GetCollectorProfiles()
    {
        try
        {
            // Obtener usuarios con rol de cobrador
            var users = _excelService.ReadSheetAsync("Users").GetAwaiter().GetResult();
            return users
                .Where(row => GetBool(row, "IsActive") == true && GetString(row, "UserName") != "admin")
                .Select(row => new CatalogOption(GetString(row, "UserName") ?? "", GetString(row, "DisplayName") ?? ""))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener perfiles de cobradores");
            return Array.Empty<CatalogOption>();
        }
    }

    #endregion

    #region Helper Methods

    private string? GetString(Dictionary<string, object?> row, string key)
    {
        return row.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private int? GetInt(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is int intValue)
            return intValue;

        return int.TryParse(value.ToString(), out var result) ? result : null;
    }

    private decimal? GetDecimal(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is decimal decValue)
            return decValue;

        if (value is double dblValue)
            return (decimal)dblValue;

        return decimal.TryParse(value.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private bool? GetBool(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is bool boolValue)
            return boolValue;

        return bool.TryParse(value.ToString(), out var result) ? result : null;
    }

    #endregion
}
