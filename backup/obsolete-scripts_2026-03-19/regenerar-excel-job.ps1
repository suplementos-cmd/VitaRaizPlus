# Script mejorado para regenerar Excel
$ErrorActionPreference = "Stop"

$excelPath = "c:\Git\VitaRaizPlus\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx"
$dllPath = "c:\Git\VitaRaizPlus\src\SalesCobrosGeo.Api\bin\Release\net9.0\SalesCobrosGeo.Api.dll"

Write-Host "=== Regenerador Excel 16 Tablas ===" -Fore Cyan
Write-Host ""

# 1. Eliminar Excel existente
if (Test-Path $excel Path) {
    Write-Host "Eliminando Excel existente..." -Fore Yellow
    Remove-Item $excelPath -Force -ErrorAction SilentlyContinue
}

# 2. Asegurar que el directorio existe
$appDataDir = Split-Path $excelPath -Parent
if (!(Test-Path $appDataDir)) {
    New-Item -ItemType Directory -Path $appDataDir -Force | Out-Null
}

# 3. Ejecutar API y capturar salida
Write-Host "Ejecutando API para generar Excel..." -Fore Cyan
$job = Start-Job -ScriptBlock {
    param($dll, $url)
    & dotnet $dll --urls $url 2>&1
} -ArgumentList $dllPath, "http://localhost:5054"

# 4. Esperar a que el Excel se cree (máximo 15 segundos)
$waited = 0
$maxWait = 15
Write-Host "Esperando creacion del Excel" -Fore Yellow -NoNewline
while (!(Test-Path $excelPath) -and $waited -lt $maxWait) {
    Start-Sleep -Milliseconds 500
    Write-Host "." -NoNewline -Fore Yellow
    $waited += 0.5
}
Write-Host ""

# 5. Detener el job
Write-Host "Deteniendo API..." -Fore Gray
Stop-Job $job -ErrorAction SilentlyContinue
Remove-Job $job -Force -ErrorAction SilentlyContinue

# 6. Verificar resultado
if (Test-Path $excelPath) {
    $info = Get-Item $excelPath
    Write-Host ""
    Write-Host "[EXITO] Excel creado correctamente!" -Fore Green
    Write-Host "Archivo: $($info.Name)" -Fore White
    Write-Host "Tamano: $($info.Length) bytes" -Fore White
    Write-Host "Fecha: $($info.LastWriteTime)" -Fore White
    Write-Host ""
    Write-Host "El Excel contiene 16 tablas:" -Fore Cyan
    Write-Host "  - 10 tablas originales (Users, Zones, Products, etc.)" -Fore Gray
    Write-Host "  - 6 tablas nuevas de configuracion dinamica:" -Fore White
    Write-Host "    > MenuItems, WeekDays, SaleStatuses," -Fore Green
    Write-Host "    > CollectionStatuses, CatalogTypes, UISettings" -Fore Green
} else {
    Write-Host ""
    Write-Host "[ERROR] No se pudo crear el Excel" -Fore Red
    Write-Host "Intenta ejecutar manualmente:" -Fore Yellow
    Write-Host "  dotnet run --project src/SalesCobrosGeo.Api" -Fore Cyan
}
