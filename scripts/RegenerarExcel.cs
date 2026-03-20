using SalesCobrosGeo.Api.Data;
using SalesCobrosGeo.Api.Initialization;
using Microsoft.Extensions.Logging;

// Logging simple a consola
using var loggerFactory = LoggerFactory.Create(builder => 
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<Program>();

// Ruta al Excel
var excelPath = @"C:\Git\VitaRaizPlus\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx";

logger.LogInformation("=== Regenerador de Excel Manual ===");
logger.LogInformation("");

// Eliminar archivo existente si existe
if (File.Exists(excelPath))
{
    logger.LogInformation("Eliminando Excel existente...");
    File.Delete(excelPath);
}

// Crear servicio Excel - esto creará el archivo con todas las hojas
logger.LogInformation("Creando Excel con estructura...");
var excelService = new ExcelDataService(excelPath);

// Inicializar datos
logger.LogInformation("Inicializando datos...");
ExcelDataInitializer.Initialize(excelService, logger);

logger.LogInformation("");
logger.LogInformation("=== Excel creado exitosamente ===");
logger.LogInformation($"Ubicación: {excelPath}");

if (File.Exists(excelPath))
{
    var fileInfo = new FileInfo(excelPath);
    logger.LogInformation($"Tamaño: {fileInfo.Length} bytes");
}
