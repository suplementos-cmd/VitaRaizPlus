<#
.SYNOPSIS
    Verifica e inicializa usuarios en Excel
    
.DESCRIPTION
    Este script ejecuta la API brevemente para que se ejecute la inicializacion
    de usuarios en el archivo Excel si no existen todavia.
    
.EXAMPLE
    .\init-users-excel.ps1
#>

$ErrorActionPreference = "Stop"

$excelPath = Join-Path $PSScriptRoot "..\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx"
$apiProject = Join-Path $PSScriptRoot "..\src\SalesCobrosGeo.Api\SalesCobrosGeo.Api.csproj"

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "  Inicializacion de Usuarios en Excel            " -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

# Verificar que Excel existe
if (-not (Test-Path $excelPath)) {
    Write-Host "[ERROR] Excel no encontrado en: $excelPath" -ForegroundColor Red
    Write-Host "Ejecuta primero: .\regenerate-excel.ps1" -ForegroundColor Yellow
    exit 1
}

$fileInfo = Get-Item $excelPath
Write-Host "[OK] Excel encontrado" -ForegroundColor Green
Write-Host "  Ruta: $($fileInfo.FullName)" -ForegroundColor Gray
Write-Host "  Tamanio: $([math]::Round($fileInfo.Length / 1KB, 2)) KB" -ForegroundColor Gray
Write-Host ""

# Compilar API para asegurar que esta actualizada
Write-Host "Compilando API..." -ForegroundColor Cyan
dotnet build $apiProject -c Release --no-incremental | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Fallo la compilacion" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] API compilada" -ForegroundColor Green
Write-Host ""

# Ejecutar API brevemente
Write-Host "Ejecutando API para inicializar usuarios..." -ForegroundColor Cyan
Write-Host "  (Presiona Ctrl+C despues de 5-10 segundos)" -ForegroundColor Gray
Write-Host ""

dotnet run --project $apiProject --no-build -c Release --urls "http://localhost:5099"

Write-Host ""
Write-Host "===================================================" -ForegroundColor Green
Write-Host "  Inicializacion completada                       " -ForegroundColor Green
Write-Host "===================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Usuarios creados en Excel (hoja 'Users'):" -ForegroundColor Cyan
Write-Host "  - admin / admin123 (Administrador)" -ForegroundColor White
Write-Host "  - vendedor1 / venta123 (Vendedor)" -ForegroundColor White
Write-Host "  - cobrador1 / cobra123 (Cobrador)" -ForegroundColor White
Write-Host "  - supventas / super123 (Supervisor Ventas)" -ForegroundColor White
Write-Host "  - supcobros / super123 (Supervisor Cobranza)" -ForegroundColor White
Write-Host ""
Write-Host "El Excel ahora contiene la tabla 'Users' con estos 5 usuarios." -ForegroundColor Green
Write-Host ""
