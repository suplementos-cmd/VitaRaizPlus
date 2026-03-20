using SalesCobrosGeo.Api.Data;
using SalesCobrosGeo.Api.Initialization;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => 
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger("ExcelGenerator");

var excelPath = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx");
excelPath = Path.GetFullPath(excelPath);

logger.LogInformation("=== Regenerador de Excel ===");
logger.LogInformation("");

if (File.Exists(excelPath))
{
    logger.LogInformation("Eliminando Excel existente: {Path}", excelPath);
    File.Delete(excelPath);
}

logger.LogInformation("Creando Excel con estructura...");
var excelService = new ExcelDataService(excelPath);

logger.LogInformation("Inicializando datos...");
ExcelDataInitializer.Initialize(excelService, logger);

logger.LogInformation("");
logger.LogInformation("=== ✅ Excel creado exitosamente ===");
logger.LogInformation("Ubicación: {Path}", excelPath);

if (File.Exists(excelPath))
{
    var fileInfo = new FileInfo(excelPath);
    logger.LogInformation("Tamaño: {Size} bytes", fileInfo.Length);
}
