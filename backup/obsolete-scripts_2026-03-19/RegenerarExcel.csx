// Script C# para regenerar Excel desde consola
using SalesCobrosGeo.Api.Data;
using SalesCobrosGeo.Api.Initialization;
using Microsoft.Extensions.Logging;

// Configurar logging básico
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger<Program>();

// Ruta al archivo Excel
var excelPath = @"c:\Git\VitaRaizPlus\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx";

logger.LogInformation("=== Regenerador de Excel con 16 tablas ===");
logger.LogInformation("");

// Eliminar archivo existente si existe
if (File.Exists(excelPath))
{
    logger.LogInformation("Eliminando Excel existente...");
    File.Delete(excelPath);
}

// Crear servicio de Excel
using var excelService = new ExcelDataService(excelPath);

// Inicializar datos
ExcelDataInitializer.Initialize(excelService, logger);

// Verificar resultado
if (File.Exists(excelPath))
{
    var fileInfo = new FileInfo(excelPath);
    logger.LogInformation("");
    logger.LogInformation("[EXITO] Excel creado con 16 tablas!");
    logger.LogInformation($"Archivo: {fileInfo.Name}");
    logger.LogInformation($"Tamaño: {fileInfo.Length} bytes");
    logger.LogInformation($"Fecha: {fileInfo.LastWriteTime}");
}
else
{
    logger.LogError("[ERROR] El Excel no se creó");
    return 1;
}

return 0;
