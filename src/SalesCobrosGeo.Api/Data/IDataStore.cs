namespace SalesCobrosGeo.Api.Data;

/// <summary>
/// Interfaz abstracta para almacenamiento de datos
/// Fase 6: Preparación para migración a Oracle
/// Permite intercambiar implementaciones (Excel ↔ Oracle) sin cambiar lógica de negocio
/// </summary>
public interface IDataStore
{
    /// <summary>
    /// Lee todos los registros de una tabla/hoja
    /// </summary>
    Task<IReadOnlyList<Dictionary<string, object?>>> ReadAllAsync(string tableName);
    
    /// <summary>
    /// Lee un registro específico por su clave primaria
    /// </summary>
    Task<Dictionary<string, object?>?> ReadByIdAsync(string tableName, string idColumn, object idValue);
    
    /// <summary>
    /// Inserta un nuevo registro
    /// </summary>
    Task<Dictionary<string, object?>> InsertAsync(string tableName, Dictionary<string, object?> data);
    
    /// <summary>
    /// Actualiza registros que cumplan con un predicado
    /// </summary>
    Task UpdateAsync(string tableName, Func<Dictionary<string, object?>, bool> predicate, Action<Dictionary<string, object?>> updater);
    
    /// <summary>
    /// Elimina registros que cumplan con un predicado
    /// </summary>
    Task<int> DeleteAsync(string tableName, Func<Dictionary<string, object?>, bool> predicate);
    
    /// <summary>
    /// Lee registros que cumplan con un predicado
    /// </summary>
    Task<IReadOnlyList<Dictionary<string, object?>>> QueryAsync(string tableName, Func<Dictionary<string, object?>, bool> predicate);
    
    /// <summary>
    /// Ejecuta una operación en una transacción
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
}
