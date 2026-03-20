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
            _logger.LogInformation("Obteniendo catálogos para ventas desde Excel");
            
            // Leer catálogos desde Excel (compartido con Web)
            var zones = _excelService.ReadSheetAsync("Zones").GetAwaiter().GetResult()
                .Where(row => GetBool(row, "IsActive") == true)
                .Select(row => new CatalogOption(
                    GetString(row, "Code") ?? "", 
                    GetString(row, "Name") ?? GetString(row, "Label") ?? ""))
                .ToList();
            
            var paymentMethods = _excelService.ReadSheetAsync("PaymentMethods").GetAwaiter().GetResult()
                .Where(row => GetBool(row, "IsActive") == true)
                .Select(row => new CatalogOption(
                    GetString(row, "Code") ?? "", 
                    GetString(row, "Name") ?? GetString(row, "Label") ?? ""))
                .ToList();
            
            var collectionDays = _excelService.ReadSheetAsync("WeekDays").GetAwaiter().GetResult()
                .Where(row => GetBool(row, "IsActive") == true)
                .Select(row => new CatalogOption(
                    GetString(row, "Code") ?? "", 
                    GetString(row, "Label") ?? GetString(row, "Name") ?? ""))
                .ToList();
            
            var products = _excelService.ReadSheetAsync("Products").GetAwaiter().GetResult()
                .Where(row => GetBool(row, "IsActive") == true)
                .Select(row => new ProductOption(
                    GetString(row, "Code") ?? "", 
                    GetString(row, "Name") ?? GetString(row, "Label") ?? "", 
                    GetDecimal(row, "Price") ?? 0))
                .ToList();
            
            // Cargar usuarios y filtrar por rol
            var allUsers = _excelService.ReadSheetAsync("Users").GetAwaiter().GetResult()
                .Where(row => GetBool(row, "IsActive") == true)
                .ToList();
            
            var sellers = allUsers
                .Where(row => 
                {
                    var role = GetString(row, "Role");
                    return role == "Vendedor" || role == "SupervisorVentas" || role == "Administrador";
                })
                .Select(row => new CatalogOption(
                    GetString(row, "UserName") ?? "", 
                    GetString(row, "DisplayName") ?? ""))
                .ToList();
            
            var collectors = allUsers
                .Where(row => 
                {
                    var role = GetString(row, "Role");
                    return role == "Cobrador" || role == "SupervisorCobranza" || role == "Administrador";
                })
                .Select(row => new CatalogOption(
                    GetString(row, "UserName") ?? "", 
                    GetString(row, "DisplayName") ?? ""))
                .ToList();
            
            var saleStatuses = _excelService.ReadSheetAsync("SaleStatuses").GetAwaiter().GetResult()
                .Where(row => GetBool(row, "IsActive") == true)
                .Select(row => new CatalogOption(
                    GetString(row, "Code") ?? "", 
                    GetString(row, "Label") ?? GetString(row, "Name") ?? ""))
                .ToList();

            _logger.LogInformation("Catálogos cargados: {Zones} zonas, {PaymentMethods} formas pago, {Days} días, {Products} productos, {Sellers} vendedores, {Collectors} cobradores, {Statuses} estados", 
                zones.Count, paymentMethods.Count, collectionDays.Count, products.Count, sellers.Count, collectors.Count, saleStatuses.Count);

            return new SalesCatalogs(zones, paymentMethods, collectionDays, products, sellers, collectors, saleStatuses);
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
            
            // Filtrar vendedores/cobradores por rol
            if (section == "vendedores")
            {
                rows = rows.Where(row => 
                {
                    var role = GetString(row, "Role");
                    return role == "Vendedor" || role == "SupervisorVentas" || role == "Administrador";
                }).ToList();
                _logger.LogInformation("Catálogo vendedores: {RowCount} registros después de filtrar por rol", rows.Count);
            }
            else if (section == "cobradores")
            {
                rows = rows.Where(row => 
                {
                    var role = GetString(row, "Role");
                    return role == "Cobrador" || role == "SupervisorCobranza" || role == "Administrador";
                }).ToList();
                _logger.LogInformation("Catálogo cobradores: {RowCount} registros después de filtrar por rol", rows.Count);
            }
            
            // Log columnas disponibles de la primera fila para depuración
            if (rows.Count > 0)
            {
                var firstRow = rows[0];
                _logger.LogInformation("Columnas disponibles en {Section}: {Columns}", section, string.Join(", ", firstRow.Keys));
            }
            else
            {
                _logger.LogWarning("Catálogo {Section} no tiene registros", section);
            }
            
            return rows.Select(row => new MaintenanceCatalogRecord(
                (long)(GetInt(row, "Id") ?? 0),
                section,
                GetString(row, "Code") ?? GetString(row, "UserName") ?? "",
                GetString(row, "Name") ?? GetString(row, "Label") ?? GetString(row, "DisplayName") ?? "",
                GetDecimal(row, "Price"),
                GetBool(row, "IsActive") ?? true,
                new Dictionary<string, object?>(row) // Todas las columnas
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
        try
        {
            var sheetName = ResolveSheetName(input.Section);
            if (string.IsNullOrEmpty(sheetName))
            {
                throw new ArgumentException($"Sección '{input.Section}' no válida", nameof(input));
            }

            var isUpdate = input.Id.HasValue && input.Id.Value > 0;

            if (isUpdate)
            {
                // Actualizar registro existente
                _excelService.UpdateRowsAsync(sheetName, 
                    row => (GetInt(row, "Id") ?? 0) == input.Id.Value,
                    row =>
                    {
                        // Actualizar todos los campos dinámicamente (excepto Id)
                        foreach (var field in input.Fields.Where(f => f.Key != "Id"))
                        {
                            row[field.Key] = field.Value;
                        }
                        // Asegurar compatibilidad Name/Label
                        if (input.Fields.ContainsKey("Name") && !input.Fields.ContainsKey("Label"))
                        {
                            row["Label"] = input.Fields["Name"];
                        }
                    }).GetAwaiter().GetResult();

                _logger.LogInformation("Catálogo actualizado: {Section} - ID {Id}", input.Section, input.Id);
            }
            else
            {
                // Crear nuevo registro - obtener siguiente ID
                var existingData = _excelService.ReadSheetAsync(sheetName).GetAwaiter().GetResult();
                var maxId = existingData.Count > 0
                    ? existingData.Max(row => GetInt(row, "Id") ?? 0)
                    : 0;
                var newId = maxId + 1;

                // Crear fila con todos los campos dinámicos
                var newRow = new Dictionary<string, object?> { ["Id"] = newId };
                foreach (var field in input.Fields.Where(f => f.Key != "Id"))
                {
                    newRow[field.Key] = field.Value;
                }
                // Asegurar compatibilidad Name/Label
                if (input.Fields.ContainsKey("Name") && !input.Fields.ContainsKey("Label"))
                {
                    newRow["Label"] = input.Fields["Name"];
                }

                _excelService.AppendRowAsync(sheetName, newRow).GetAwaiter().GetResult();
                input.Id = newId;

                _logger.LogInformation("Catálogo creado: {Section} - ID {Id}", input.Section, newId);
            }

            return new MaintenanceCatalogRecord(
                input.Id ?? 0,
                input.Section,
                input.Fields.TryGetValue("Code", out var code) ? code?.ToString() ?? "" : "",
                input.Fields.TryGetValue("Name", out var name) ? name?.ToString() ?? "" : 
                    input.Fields.TryGetValue("Label", out var label) ? label?.ToString() ?? "" : "",
                input.Fields.TryGetValue("Price", out var price) && price is decimal d ? d : null,
                input.Fields.TryGetValue("IsActive", out var active) && active is bool b ? b : true,
                new Dictionary<string, object?>(input.Fields)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar ítem de catálogo {Section}", input.Section);
            throw;
        }
    }

    public bool DeleteMaintenanceCatalogItem(string section, long id)
    {
        try
        {
            var sheetName = ResolveSheetName(section);
            if (string.IsNullOrEmpty(sheetName))
            {
                throw new ArgumentException($"Sección '{section}' no válida", nameof(section));
            }

            _excelService.DeleteRowsAsync(sheetName,
                row => (GetInt(row, "Id") ?? 0) == id
            ).GetAwaiter().GetResult();

            _logger.LogInformation("Catálogo eliminado: {Section} - ID {Id}", section, id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar ítem de catálogo {Section} ID {Id}", section, id);
            return false;
        }
    }

    private string? ResolveSheetName(string section)
    {
        return section switch
        {
            // Catálogos de negocio
            "zonas" => "Zones",
            "productos" => "Products",
            "formas-pago" => "PaymentMethods",
            "dias-cobro" => "WeekDays",
            "estatus-venta" => "SaleStatuses",
            "estatus-cobro-grupos" => "CollectionStatuses",
            
            // Catálogos de configuración
            "menu-items" => "MenuItems",
            "tipos-catalogo" => "CatalogTypes",
            "config-ui" => "UISettings",
            
            // Personal (filtrado de Users)
            "vendedores" => "Users",
            "cobradores" => "Users",
            
            // Backward compatibility
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
            _logger.LogInformation("Enviando venta a API: Cliente {Cliente}, Productos: {ProductCount}", 
                input.NombreCliente, 
                input.Productos?.Count ?? 0);
            
            var response = _httpClient.PostAsJsonAsync("api/sales", input).GetAwaiter().GetResult();
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                _logger.LogError("Error de API al crear venta. Status: {Status}, Contenido: {Content}", 
                    response.StatusCode, 
                    errorContent);
                
                // Intentar extraer mensaje de error
                try
                {
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(errorContent);
                    if (errorObj != null && errorObj.TryGetValue("message", out var msg))
                    {
                        throw new InvalidOperationException($"Error de API: {msg}");
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // Si no se puede deserializar, usar el contenido crudo
                }
                
                throw new InvalidOperationException($"Error al crear venta en API (Status: {response.StatusCode})");
            }
            
            response.EnsureSuccessStatusCode();
            
            var sale = response.Content.ReadFromJsonAsync<SaleRecord>().GetAwaiter().GetResult();
            if (sale == null)
            {
                _logger.LogError("API retornó null después de crear venta");
                throw new Exception("API retornó null después de crear venta");
            }
            
            _logger.LogInformation("Venta creada exitosamente: {IdV}", sale.IdV);
            return sale;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error HTTP al crear venta en API");
            throw new InvalidOperationException("Error de conexión con el servidor. Verifique que la API esté ejecutándose.", ex);
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
                .Where(row => 
                {
                    var isActive = GetBool(row, "IsActive") == true;
                    var userName = GetString(row, "UserName");
                    var role = GetString(row, "Role");
                    return isActive && 
                           userName != "admin" && 
                           (role == "Cobrador" || role == "SupervisorCobranza");
                })
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
