<#
.SYNOPSIS
    Regenera el archivo Excel con los catalogos actualizados
    
.DESCRIPTION
    Este script elimina el archivo Excel existente y ejecuta la API 
    para regenerar el archivo con los datos mas recientes.
    
.EXAMPLE
    .\regenerate-excel.ps1
#>

$ErrorActionPreference = "Stop"

$excelPath = Join-Path $PSScriptRoot "..\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx"
$apiProject = Join-Path $PSScriptRoot "..\src\SalesCobrosGeo.Api\SalesCobrosGeo.Api.csproj"

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "  Regeneracion de Excel - SalesCobrosGeo          " -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar si el archivo existe
if (Test-Path $excelPath) {
    Write-Host "[OK] Excel encontrado: $excelPath" -ForegroundColor Yellow
    Write-Host "  Eliminando archivo antiguo..." -ForegroundColor Yellow
    Remove-Item $excelPath -Force
    Write-Host "  [OK] Archivo eliminado" -ForegroundColor Green
}
else {
    Write-Host "[INFO] Excel no existe - se creara uno nuevo" -ForegroundColor Cyan
}

Write-Host ""

# 2. Compilar el proyecto API (Release)
Write-Host "Compilando API en modo Release..." -ForegroundColor Cyan
dotnet build $apiProject -c Release --no-incremental
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Error al compilar la API" -ForegroundColor Red
    exit 1
}
Write-Host "[OK] API compilada exitosamente" -ForegroundColor Green
Write-Host ""

# 3. Ejecutar la API temporalmente para generar Excel
Write-Host "Ejecutando API para generar Excel..." -ForegroundColor Cyan
Write-Host "  (Se detendra automaticamente despues de 10 segundos)" -ForegroundColor Gray

$apiProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project `"$apiProject`" --no-build -c Release --urls http://localhost:5199" `
    -PassThru `
    -NoNewWindow

# Esperar 10 segundos para que la API inicie y genere el Excel
Start-Sleep -Seconds 10

# Detener la API
Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
Write-Host "[OK] API ejecutada y detenida" -ForegroundColor Green
Write-Host ""

# 4. Verificar que el Excel fue creado
Start-Sleep -Seconds 1
if (Test-Path $excelPath) {
    $fileInfo = Get-Item $excelPath
    Write-Host "===================================================" -ForegroundColor Green
    Write-Host "  [OK] Excel regenerado exitosamente              " -ForegroundColor Green
    Write-Host "===================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Ruta:    $excelPath" -ForegroundColor White
    Write-Host "Tamanio: $([math]::Round($fileInfo.Length / 1KB, 2)) KB" -ForegroundColor White
    Write-Host "Fecha:   $($fileInfo.LastWriteTime)" -ForegroundColor White
    Write-Host ""
    Write-Host "El archivo Excel contiene 16 tablas con catalogos dinamicos:" -ForegroundColor Cyan
    Write-Host "  - SaleStatuses (4 estados de venta)" -ForegroundColor Gray
    Write-Host "  - CollectionStatuses (9 estados de cobro - ACTUALIZADO)" -ForegroundColor Yellow
    Write-Host "  - PaymentMethods, ProductTypes, MenuItems, etc." -ForegroundColor Gray
    Write-Host ""
}
else {
    Write-Host "===================================================" -ForegroundColor Red
    Write-Host "  [ERROR] Excel no fue generado                   " -ForegroundColor Red
    Write-Host "===================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Verifica que el proyecto API se ejecute correctamente:" -ForegroundColor Yellow
    Write-Host "  dotnet run --project `"$apiProject`"" -ForegroundColor Gray
    exit 1
}
