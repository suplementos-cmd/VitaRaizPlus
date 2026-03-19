namespace SalesCobrosGeo.Api.Data;

/// <summary>
/// Implementación de IDataStore usando Excel (EPPlus) como backend
/// Fase 6: Implementación actual - Excel
/// Puede ser reemplazado por OracleDataStore en el futuro sin cambiar código de negocio
/// </summary>
public sealed class ExcelDataStoreAdapter : IDataStore
{
    private readonly ExcelDataService _excelService;
    private readonly ILogger<ExcelDataStoreAdapter> _logger;

    public ExcelDataStoreAdapter(ExcelDataService excelService, ILogger<ExcelDataStoreAdapter> logger)
    {
        _excelService = excelService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> ReadAllAsync(string tableName)
    {
        try
        {
            var rows = await _excelService.ReadSheetAsync(tableName);
            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al leer todos los registros de {TableName}", tableName);
            return Array.Empty<Dictionary<string, object?>>();
        }
    }

    public async Task<Dictionary<string, object?>?> ReadByIdAsync(string tableName, string idColumn, object idValue)
    {
        try
        {
            var rows = await _excelService.ReadSheetAsync(tableName);
            return rows.FirstOrDefault(row =>
            {
                if (!row.TryGetValue(idColumn, out var value))
                    return false;
                
                return value?.ToString() == idValue?.ToString();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al leer registro {IdValue} de {TableName}", idValue, tableName);
            return null;
        }
    }

    public async Task<Dictionary<string, object?>> InsertAsync(string tableName, Dictionary<string, object?> data)
    {
        try
        {
            await _excelService.AppendRowAsync(tableName, data);
            _logger.LogInformation("Registro insertado en {TableName}", tableName);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al insertar registro en {TableName}", tableName);
            throw;
        }
    }

    public async Task UpdateAsync(string tableName, Func<Dictionary<string, object?>, bool> predicate, Action<Dictionary<string, object?>> updater)
    {
        try
        {
            await _excelService.UpdateRowsAsync(tableName, predicate, updater);
            _logger.LogInformation("Registros actualizados en {TableName}", tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar registros en {TableName}", tableName);
            throw;
        }
    }

    public async Task<int> DeleteAsync(string tableName, Func<Dictionary<string, object?>, bool> predicate)
    {
        try
        {
            await _excelService.DeleteRowsAsync(tableName, predicate);
            // Excel no retorna count, asumir 1 por ahora
            _logger.LogInformation("Registros eliminados de {TableName}", tableName);
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar registros de {TableName}", tableName);
            throw;
        }
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> QueryAsync(string tableName, Func<Dictionary<string, object?>, bool> predicate)
    {
        try
        {
            var rows = await _excelService.ReadSheetAsync(tableName);
            return rows.Where(predicate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar {TableName}", tableName);
            return Array.Empty<Dictionary<string, object?>>();
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        // Excel no soporta transacciones reales
        // Simplemente ejecutar la operación
        // En OracleDataStore esto usaría BEGIN TRANSACTION / COMMIT / ROLLBACK
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante operación transaccional");
            throw;
        }
    }
}
