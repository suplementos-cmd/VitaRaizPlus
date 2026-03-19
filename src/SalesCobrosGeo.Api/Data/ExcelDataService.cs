using OfficeOpenXml;
using System.Collections.Concurrent;

namespace SalesCobrosGeo.Api.Data;

/// <summary>
/// Servicio centralizado para acceso a datos en Excel.
/// Proporciona operaciones thread-safe de lectura/escritura.
/// </summary>
public sealed class ExcelDataService : IDisposable
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly FileSystemWatcher? _watcher;
    private volatile bool _disposed;

    public ExcelDataService(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        
        // Configurar licencia de EPPlus (modo no comercial)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        // Asegurar que el archivo existe
        EnsureExcelFileExists();

        // Opcional: Monitorear cambios en el archivo (para sincronización futura)
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                _watcher = new FileSystemWatcher(directory)
                {
                    Filter = Path.GetFileName(_filePath),
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };
            }
        }
        catch
        {
            // Si falla el watcher, continuamos sin él
        }
    }

    private void EnsureExcelFileExists()
    {
        if (File.Exists(_filePath))
            return;

        // Crear directorio si no existe
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Crear archivo Excel con estructura inicial
        using var package = new ExcelPackage();
        
        // Core business tables
        CreateUsersSheet(package);
        CreateZonesSheet(package);
        CreateProductsSheet(package);
        CreatePaymentMethodsSheet(package);
        CreateClientsSheet(package);
        CreateSalesSheet(package);
        CreateSaleItemsSheet(package);
        CreateSaleHistorySheet(package);
        CreateCollectionsSheet(package);
        CreateAuditTrailSheet(package);
        
        // Dynamic configuration tables
        CreateMenuItemsSheet(package);
        CreateWeekDaysSheet(package);
        CreateSaleStatusesSheet(package);
        CreateCollectionStatusesSheet(package);
        CreateCatalogTypesSheet(package);
        CreateUISettingsSheet(package);

        package.SaveAs(new FileInfo(_filePath));
    }

    #region Sheet Definitions

    private static void CreateUsersSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("Users");
        sheet.Cells["A1"].Value = "UserName";
        sheet.Cells["B1"].Value = "Password";
        sheet.Cells["C1"].Value = "DisplayName";
        sheet.Cells["D1"].Value = "Role";
        sheet.Cells["E1"].Value = "IsActive";
        sheet.Cells["A1:E1"].Style.Font.Bold = true;
    }

    private static void CreateZonesSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("Zones");
        sheet.Cells["A1"].Value = "Id";
        sheet.Cells["B1"].Value = "Code";
        sheet.Cells["C1"].Value = "Name";
        sheet.Cells["D1"].Value = "IsActive";
        sheet.Cells["A1:D1"].Style.Font.Bold = true;
    }

    private static void CreateProductsSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("Products");
        sheet.Cells["A1"].Value = "Id";
        sheet.Cells["B1"].Value = "Code";
        sheet.Cells["C1"].Value = "Name";
        sheet.Cells["D1"].Value = "Price";
        sheet.Cells["E1"].Value = "IsActive";
        sheet.Cells["A1:E1"].Style.Font.Bold = true;
    }

    private static void CreatePaymentMethodsSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("PaymentMethods");
        sheet.Cells["A1"].Value = "Id";
        sheet.Cells["B1"].Value = "Code";
        sheet.Cells["C1"].Value = "Name";
        sheet.Cells["D1"].Value = "IsActive";
        sheet.Cells["A1:D1"].Style.Font.Bold = true;
    }

    private static void CreateClientsSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("Clients");
        sheet.Cells["A1"].Value = "Id";
        sheet.Cells["B1"].Value = "FullName";
        sheet.Cells["C1"].Value = "Mobile";
        sheet.Cells["D1"].Value = "Phone";
        sheet.Cells["E1"].Value = "ZoneCode";
        sheet.Cells["F1"].Value = "CollectionDay";
        sheet.Cells["G1"].Value = "Address";
        sheet.Cells["H1"].Value = "CreatedBy";
        sheet.Cells["I1"].Value = "CreatedAtUtc";
        sheet.Cells["J1"].Value = "UpdatedAtUtc";
        sheet.Cells["K1"].Value = "IsActive";
        sheet.Cells["A1:K1"].Style.Font.Bold = true;
    }

    private static void CreateSalesSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("Sales");
        sheet.Cells["A1"].Value = "Id";
        sheet.Cells["B1"].Value = "SaleNumber";
        sheet.Cells["C1"].Value = "ClientId";
        sheet.Cells["D1"].Value = "SellerUserName";
        sheet.Cells["E1"].Value = "CollectorUserName";
        sheet.Cells["F1"].Value = "PaymentMethodCode";
        sheet.Cells["G1"].Value = "CollectionDay";
        sheet.Cells["H1"].Value = "Notes";
        sheet.Cells["I1"].Value = "Status";
        sheet.Cells["J1"].Value = "CollectionStatus";
        sheet.Cells["K1"].Value = "Collectable";
        sheet.Cells["L1"].Value = "SellerCommissionPercent";
        sheet.Cells["M1"].Value = "CreatedAtUtc";
        sheet.Cells["N1"].Value = "UpdatedAtUtc";
        sheet.Cells["O1"].Value = "FirstCollectionAtUtc";
        sheet.Cells["P1"].Value = "TotalAmount";
        sheet.Cells["Q1"].Value = "CollectedAmount";
        sheet.Cells["R1"].Value = "PrimaryCoordinates";
        sheet.Cells["S1"].Value = "SecondaryCoordinates";
        sheet.Cells["T1"].Value = "LocationUrl";
        sheet.Cells["U1"].Value = "PhotoUrls";
        sheet.Cells["A1:U1"].Style.Font.Bold = true;
    }

    private static void CreateSaleItemsSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("SaleItems");
        sheet.Cells["A1"].Value = "SaleId";
        sheet.Cells["B1"].Value = "ProductId";
        sheet.Cells["C1"].Value = "ProductCode";
        sheet.Cells["D1"].Value = "ProductName";
        sheet.Cells["E1"].Value = "Quantity";
        sheet.Cells["F1"].Value = "UnitPrice";
        sheet.Cells["G1"].Value = "Subtotal";
        sheet.Cells["A1:G1"].Style.Font.Bold = true;
    }

    private static void CreateSaleHistorySheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("SaleHistory");
        sheet.Cells["A1"].Value = "SaleId";
        sheet.Cells["B1"].Value = "TimestampUtc";
        sheet.Cells["C1"].Value = "UserName";
        sheet.Cells["D1"].Value = "FromStatus";
        sheet.Cells["E1"].Value = "ToStatus";
        sheet.Cells["F1"].Value = "Reason";
        sheet.Cells["G1"].Value = "Action";
        sheet.Cells["A1:G1"].Style.Font.Bold = true;
    }

    private static void CreateCollectionsSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("Collections");
        sheet.Cells["A1"].Value = "Id";
        sheet.Cells["B1"].Value = "SaleId";
        sheet.Cells["C1"].Value = "Amount";
        sheet.Cells["D1"].Value = "Coordinates";
        sheet.Cells["E1"].Value = "Notes";
        sheet.Cells["F1"].Value = "CollectedBy";
        sheet.Cells["G1"].Value = "CollectedAtUtc";
        sheet.Cells["H1"].Value = "CapturedAtUtc";
        sheet.Cells["A1:H1"].Style.Font.Bold = true;
    }

    private static void CreateAuditTrailSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("AuditTrail");
        sheet.Cells["A1"].Value = "Id";
        sheet.Cells["B1"].Value = "TimestampUtc";
        sheet.Cells["C1"].Value = "EventType";
        sheet.Cells["D1"].Value = "UserName";
        sheet.Cells["E1"].Value = "Description";
        sheet.Cells["F1"].Value = "Path";
        sheet.Cells["G1"].Value = "IpAddress";
        sheet.Cells["H1"].Value = "Coordinates";
        sheet.Cells["I1"].Value = "Metadata";
        sheet.Cells["A1:I1"].Style.Font.Bold = true;
    }

    private static void CreateMenuItemsSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("MenuItems");
        sheet.Cells["A1"].Value = "Id";
        sheet.Cells["B1"].Value = "Code";
        sheet.Cells["C1"].Value = "Label";
        sheet.Cells["D1"].Value = "IconSvg";
        sheet.Cells["E1"].Value = "Controller";
        sheet.Cells["F1"].Value = "Action";
        sheet.Cells["G1"].Value = "RequiredPolicy";
        sheet.Cells["H1"].Value = "SortOrder";
        sheet.Cells["I1"].Value = "IsActive";
        sheet.Cells["J1"].Value = "ParentId";
        sheet.Cells["K1"].Value = "Platform";
        sheet.Cells["A1:K1"].Style.Font.Bold = true;
    }

    private static void CreateWeekDaysSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("WeekDays");
        sheet.Cells["A1"].Value = "Id";
        sheet.Cells["B1"].Value = "Code";
        sheet.Cells["C1"].Value = "Name";
        sheet.Cells["D1"].Value = "ShortCode";
        sheet.Cells["E1"].Value = "SortOrder";
        sheet.Cells["F1"].Value = "IsActive";
        sheet.Cells["A1:F1"].Style.Font.Bold = true;
    }

    private static void CreateSaleStatusesSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("SaleStatuses");
        sheet.Cells["A1"].Value = "Id";
        sheet.Cells["B1"].Value = "Code";
        sheet.Cells["C1"].Value = "Name";
        sheet.Cells["D1"].Value = "ColorClass";
        sheet.Cells["E1"].Value = "IconSvg";
        sheet.Cells["F1"].Value = "SortOrder";
        sheet.Cells["G1"].Value = "IsActive";
        sheet.Cells["H1"].Value = "IsFinal";
        sheet.Cells["A1:H1"].Style.Font.Bold = true;
    }

    private static void CreateCollectionStatusesSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("CollectionStatuses");
        sheet.Cells["A1"].Value = "Id";
        sheet.Cells["B1"].Value = "Code";
        sheet.Cells["C1"].Value = "Name";
        sheet.Cells["D1"].Value = "ColorClass";
        sheet.Cells["E1"].Value = "Priority";
        sheet.Cells["F1"].Value = "IsActive";
        sheet.Cells["A1:F1"].Style.Font.Bold = true;
    }

    private static void CreateCatalogTypesSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("CatalogTypes");
        sheet.Cells["A1"].Value = "Id";
        sheet.Cells["B1"].Value = "Code";
        sheet.Cells["C1"].Value = "Name";
        sheet.Cells["D1"].Value = "Description";
        sheet.Cells["E1"].Value = "IconClass";
        sheet.Cells["F1"].Value = "Category";
        sheet.Cells["G1"].Value = "SortOrder";
        sheet.Cells["H1"].Value = "IsActive";
        sheet.Cells["A1:H1"].Style.Font.Bold = true;
    }

    private static void CreateUISettingsSheet(ExcelPackage package)
    {
        var sheet = package.Workbook.Worksheets.Add("UISettings");
        sheet.Cells["A1"].Value = "Id";
        sheet.Cells["B1"].Value = "Category";
        sheet.Cells["C1"].Value = "Key";
        sheet.Cells["D1"].Value = "Value";
        sheet.Cells["E1"].Value = "Description";
        sheet.Cells["F1"].Value = "IsActive";
        sheet.Cells["A1:F1"].Style.Font.Bold = true;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Lee datos de una hoja específica.
    /// </summary>
    public async Task<List<Dictionary<string, object?>>> ReadSheetAsync(string sheetName)
    {
        ThrowIfDisposed();
        await _semaphore.WaitAsync();
        try
        {
            using var package = new ExcelPackage(new FileInfo(_filePath));
            var sheet = package.Workbook.Worksheets[sheetName];
            
            if (sheet == null)
            {
                return new List<Dictionary<string, object?>>();
            }

            var result = new List<Dictionary<string, object?>>();
            var rowCount = sheet.Dimension?.Rows ?? 0;
            var colCount = sheet.Dimension?.Columns ?? 0;

            if (rowCount < 2) // No hay datos, solo encabezados
            {
                return result;
            }

            // Leer encabezados
            var headers = new List<string>();
            for (int col = 1; col <= colCount; col++)
            {
                headers.Add(sheet.Cells[1, col].Text);
            }

            // Leer filas
            for (int row = 2; row <= rowCount; row++)
            {
                var rowData = new Dictionary<string, object?>();
                for (int col = 1; col <= colCount; col++)
                {
                    rowData[headers[col - 1]] = sheet.Cells[row, col].Value;
                }
                result.Add(rowData);
            }

            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Escribe datos en una hoja específica (reemplaza todo el contenido excepto encabezados).
    /// </summary>
    public async Task WriteSheetAsync(string sheetName, List<Dictionary<string, object?>> data)
    {
        ThrowIfDisposed();
        await _semaphore.WaitAsync();
        try
        {
            using var package = new ExcelPackage(new FileInfo(_filePath));
            var sheet = package.Workbook.Worksheets[sheetName];
            
            if (sheet == null)
            {
                throw new InvalidOperationException($"Sheet '{sheetName}' not found.");
            }

            // Limpiar datos existentes (mantener encabezados)
            if (sheet.Dimension != null && sheet.Dimension.Rows > 1)
            {
                sheet.DeleteRow(2, sheet.Dimension.Rows - 1);
            }

            // Escribir nuevos datos
            if (data.Count > 0)
            {
                var headers = new List<string>();
                for (int col = 1; col <= sheet.Dimension?.Columns; col++)
                {
                    headers.Add(sheet.Cells[1, col].Text);
                }

                int row = 2;
                foreach (var rowData in data)
                {
                    for (int col = 0; col < headers.Count; col++)
                    {
                        var key = headers[col];
                        if (rowData.TryGetValue(key, out var value))
                        {
                            sheet.Cells[row, col + 1].Value = value;
                        }
                    }
                    row++;
                }
            }

            await package.SaveAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Añade una fila a una hoja específica.
    /// </summary>
    public async Task AppendRowAsync(string sheetName, Dictionary<string, object?> rowData)
    {
        ThrowIfDisposed();
        await _semaphore.WaitAsync();
        try
        {
            using var package = new ExcelPackage(new FileInfo(_filePath));
            var sheet = package.Workbook.Worksheets[sheetName];
            
            if (sheet == null)
            {
                throw new InvalidOperationException($"Sheet '{sheetName}' not found.");
            }

            var rowCount = sheet.Dimension?.Rows ?? 1;
            var colCount = sheet.Dimension?.Columns ?? 0;
            var nextRow = rowCount + 1;

            // Leer encabezados
            var headers = new List<string>();
            for (int col = 1; col <= colCount; col++)
            {
                headers.Add(sheet.Cells[1, col].Text);
            }

            // Escribir nueva fila
            for (int col = 0; col < headers.Count; col++)
            {
                var key = headers[col];
                if (rowData.TryGetValue(key, out var value))
                {
                    sheet.Cells[nextRow, col + 1].Value = value;
                }
            }

            await package.SaveAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Actualiza filas que cumplan un criterio.
    /// </summary>
    public async Task UpdateRowsAsync(string sheetName, Func<Dictionary<string, object?>, bool> predicate, Action<Dictionary<string, object?>> updater)
    {
        ThrowIfDisposed();
        var data = await ReadSheetAsync(sheetName);
        
        foreach (var row in data.Where(predicate))
        {
            updater(row);
        }

        await WriteSheetAsync(sheetName, data);
    }

    /// <summary>
    /// Elimina filas que cumplan un criterio.
    /// </summary>
    public async Task DeleteRowsAsync(string sheetName, Func<Dictionary<string, object?>, bool> predicate)
    {
        ThrowIfDisposed();
        var data = await ReadSheetAsync(sheetName);
        var filtered = data.Where(r => !predicate(r)).ToList();
        await WriteSheetAsync(sheetName, filtered);
    }

    #endregion

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ExcelDataService));
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _watcher?.Dispose();
        _semaphore?.Dispose();
    }
}
