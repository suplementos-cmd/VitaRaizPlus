using System;
using System.Linq;
using OfficeOpenXml;

// Script para listar TODOS los permisos disponibles, especialmente los de VIEW

var excelPath = @"c:\Git\VitaRaizPlus\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx";

if (!File.Exists(excelPath))
{
    Console.WriteLine($"ERROR: Excel no encontrado en {excelPath}");
    return;
}

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

using var package = new ExcelPackage(new FileInfo(excelPath));

var permissionsSheet = package.Workbook.Worksheets["Permissions"];
if (permissionsSheet == null)
{
    Console.WriteLine("ERROR: Hoja Permissions no encontrada");
    return;
}

Console.WriteLine("=== TODOS LOS PERMISOS EN EL EXCEL ===\n");

for (int row = 2; row <= permissionsSheet.Dimension.End.Row; row++)
{
    var id = permissionsSheet.Cells[row, 1].Value;
    var code = permissionsSheet.Cells[row, 2].Value?.ToString() ?? "";
    var desc = permissionsSheet.Cells[row, 6].Value?.ToString() ?? "";
    
    Console.WriteLine($"{id,3}. {code,-30} | {desc}");
}

Console.WriteLine($"\nTotal: {permissionsSheet.Dimension.End.Row - 1} permisos");
Console.WriteLine("\n=== PERMISOS CRÍTICOS (VIEW) ===\n");

var criticalCodes = new[] {
    "dashboard:view",
    "sales:view",
    "collections:view",
    "maintenance:view",
    "administration:view"
};

foreach (var critCode in criticalCodes)
{
    bool found = false;
    for (int row = 2; row <= permissionsSheet.Dimension.End.Row; row++)
    {
        var code = permissionsSheet.Cells[row, 2].Value?.ToString() ?? "";
        if (code.Equals(critCode, StringComparison.OrdinalIgnoreCase))
        {
            var id = permissionsSheet.Cells[row, 1].Value;
            Console.WriteLine($"  ✓ encontrado: {critCode} (ID: {id})");
            found = true;
            break;
        }
    }
    if (!found)
    {
        Console.WriteLine($"  ✗ NO encontrado: {critCode}");
    }
}

Console.WriteLine("\n=== FIN ===");

Console.WriteLine("\n=== FIN ===");
