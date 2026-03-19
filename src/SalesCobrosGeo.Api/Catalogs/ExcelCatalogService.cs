using SalesCobrosGeo.Api.Data;

namespace SalesCobrosGeo.Api.Catalogs;

/// <summary>
/// Servicio para acceso a catálogos dinámicos almacenados en Excel
/// </summary>
public sealed class ExcelCatalogService : ICatalogService
{
    private readonly ExcelDataService _excelService;
    private readonly ILogger<ExcelCatalogService> _logger;

    public ExcelCatalogService(ExcelDataService excelService, ILogger<ExcelCatalogService> logger)
    {
        _excelService = excelService;
        _logger = logger;
    }

    #region Menu Items

    public async Task<IReadOnlyList<MenuItem>> GetMenuItemsAsync(bool includeInactive = false)
    {
        var rows = await _excelService.ReadSheetAsync("MenuItems");
        var items = rows.Select(ParseMenuItem).Where(m => m != null).Select(m => m!).ToList();

        if (!includeInactive)
        {
            items = items.Where(m => m.IsActive).ToList();
        }

        return items.OrderBy(m => m.SortOrder).ToList();
    }

    public async Task<IReadOnlyList<MenuItem>> GetMenuItemsForPlatformAsync(string platform)
    {
        var allItems = await GetMenuItemsAsync();
        return allItems.Where(m => m.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase) || 
                                    m.Platform.Equals("Both", StringComparison.OrdinalIgnoreCase))
                       .ToList();
    }

    private static MenuItem? ParseMenuItem(Dictionary<string, object?> row)
    {
        try
        {
            return new MenuItem(
                Id: Convert.ToInt32(row["Id"]),
                Code: row["Code"]?.ToString() ?? "",
                Label: row["Label"]?.ToString() ?? "",
                IconSvg: row["IconSvg"]?.ToString(),
                Controller: row["Controller"]?.ToString(),
                Action: row["Action"]?.ToString(),
                RequiredPolicy: row["RequiredPolicy"]?.ToString(),
                SortOrder: row["SortOrder"] != null ? Convert.ToInt32(row["SortOrder"]) : 0,
                IsActive: row["IsActive"]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false,
                ParentId: row["ParentId"]?.ToString() is string pid && !string.IsNullOrEmpty(pid) ? Convert.ToInt32(pid) : null,
                Platform: row["Platform"]?.ToString() ?? "Web"
            );
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion

    #region Week Days

    public async Task<IReadOnlyList<WeekDay>> GetWeekDaysAsync(bool includeInactive = false)
    {
        var rows = await _excelService.ReadSheetAsync("WeekDays");
        var days = rows.Select(ParseWeekDay).Where(d => d != null).Select(d => d!).ToList();

        if (!includeInactive)
        {
            days = days.Where(d => d.IsActive).ToList();
        }

        return days.OrderBy(d => d.SortOrder).ToList();
    }

    public async Task<WeekDay?> GetWeekDayByCodeAsync(string code)
    {
        var days = await GetWeekDaysAsync();
        return days.FirstOrDefault(d => d.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<WeekDay?> GetTodayWeekDayAsync()
    {
        var today = DateTime.Now.DayOfWeek;
        var codeMap = new Dictionary<DayOfWeek, string>
        {
            [DayOfWeek.Monday] = "LUNES",
            [DayOfWeek.Tuesday] = "MARTES",
            [DayOfWeek.Wednesday] = "MIERCOLES",
            [DayOfWeek.Thursday] = "JUEVES",
            [DayOfWeek.Friday] = "VIERNES",
            [DayOfWeek.Saturday] = "SABADO",
            [DayOfWeek.Sunday] = "DOMINGO"
        };

        if (codeMap.TryGetValue(today, out var code))
        {
            return await GetWeekDayByCodeAsync(code);
        }

        return null;
    }

    private static WeekDay? ParseWeekDay(Dictionary<string, object?> row)
    {
        try
        {
            return new WeekDay(
                Id: Convert.ToInt32(row["Id"]),
                Code: row["Code"]?.ToString() ?? "",
                Name: row["Name"]?.ToString() ?? "",
                ShortCode: row["ShortCode"]?.ToString() ?? "",
                SortOrder: row["SortOrder"] != null ? Convert.ToInt32(row["SortOrder"]) : 0,
                IsActive: row["IsActive"]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false
            );
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion

    #region Sale Statuses

    public async Task<IReadOnlyList<SaleStatus>> GetSaleStatusesAsync(bool includeInactive = false)
    {
        var rows = await _excelService.ReadSheetAsync("SaleStatuses");
        var statuses = rows.Select(ParseSaleStatus).Where(s => s != null).Select(s => s!).ToList();

        if (!includeInactive)
        {
            statuses = statuses.Where(s => s.IsActive).ToList();
        }

        return statuses.OrderBy(s => s.SortOrder).ToList();
    }

    public async Task<SaleStatus?> GetSaleStatusByCodeAsync(string code)
    {
        var statuses = await GetSaleStatusesAsync();
        return statuses.FirstOrDefault(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    private static SaleStatus? ParseSaleStatus(Dictionary<string, object?> row)
    {
        try
        {
            return new SaleStatus(
                Id: Convert.ToInt32(row["Id"]),
                Code: row["Code"]?.ToString() ?? "",
                Name: row["Name"]?.ToString() ?? "",
                ColorClass: row["ColorClass"]?.ToString() ?? "",
                IconSvg: row["IconSvg"]?.ToString(),
                SortOrder: row["SortOrder"] != null ? Convert.ToInt32(row["SortOrder"]) : 0,
                IsActive: row["IsActive"]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false,
                IsFinal: row["IsFinal"]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false
            );
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion

    #region Collection Statuses

    public async Task<IReadOnlyList<CollectionStatus>> GetCollectionStatusesAsync(bool includeInactive = false)
    {
        var rows = await _excelService.ReadSheetAsync("CollectionStatuses");
        var statuses = rows.Select(ParseCollectionStatus).Where(s => s != null).Select(s => s!).ToList();

        if (!includeInactive)
        {
            statuses = statuses.Where(s => s.IsActive).ToList();
        }

        return statuses.OrderBy(s => s.Priority).ToList();
    }

    public async Task<CollectionStatus?> GetCollectionStatusByCodeAsync(string code)
    {
        var statuses = await GetCollectionStatusesAsync();
        return statuses.FirstOrDefault(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    private static CollectionStatus? ParseCollectionStatus(Dictionary<string, object?> row)
    {
        try
        {
            return new CollectionStatus(
                Id: Convert.ToInt32(row["Id"]),
                Code: row["Code"]?.ToString() ?? "",
                Name: row["Name"]?.ToString() ?? "",
                ColorClass: row["ColorClass"]?.ToString() ?? "",
                Priority: row["Priority"] != null ? Convert.ToInt32(row["Priority"]) : 0,
                IsActive: row["IsActive"]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false
            );
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion

    #region Catalog Types

    public async Task<IReadOnlyList<CatalogType>> GetCatalogTypesAsync(bool includeInactive = false)
    {
        var rows = await _excelService.ReadSheetAsync("CatalogTypes");
        var types = rows.Select(ParseCatalogType).Where(t => t != null).Select(t => t!).ToList();

        if (!includeInactive)
        {
            types = types.Where(t => t.IsActive).ToList();
        }

        return types.OrderBy(t => t.SortOrder).ToList();
    }

    public async Task<CatalogType?> GetCatalogTypeByCodeAsync(string code)
    {
        var types = await GetCatalogTypesAsync();
        return types.FirstOrDefault(t => t.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    private static CatalogType? ParseCatalogType(Dictionary<string, object?> row)
    {
        try
        {
            return new CatalogType(
                Id: Convert.ToInt32(row["Id"]),
                Code: row["Code"]?.ToString() ?? "",
                Name: row["Name"]?.ToString() ?? "",
                Description: row["Description"]?.ToString() ?? "",
                IconClass: row["IconClass"]?.ToString() ?? "",
                Category: row["Category"]?.ToString() ?? "",
                SortOrder: row["SortOrder"] != null ? Convert.ToInt32(row["SortOrder"]) : 0,
                IsActive: row["IsActive"]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false
            );
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion

    #region UI Settings

    public async Task<UISetting?> GetUISettingAsync(string category, string key)
    {
        var settings = await GetUISettingsByCategoryAsync(category);
        return settings.FirstOrDefault(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<UISetting>> GetUISettingsByCategoryAsync(string category)
    {
        var rows = await _excelService.ReadSheetAsync("UISettings");
        var settings = rows.Select(ParseUISetting).Where(s => s != null).Select(s => s!).ToList();

        return settings.Where(s => s.IsActive && 
                                   s.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                      .ToList();
    }

    private static UISetting? ParseUISetting(Dictionary<string, object?> row)
    {
        try
        {
            return new UISetting(
                Id: Convert.ToInt32(row["Id"]),
                Category: row["Category"]?.ToString() ?? "",
                Key: row["Key"]?.ToString() ?? "",
                Value: row["Value"]?.ToString() ?? "",
                Description: row["Description"]?.ToString() ?? "",
                IsActive: row["IsActive"]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false
            );
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion
}
