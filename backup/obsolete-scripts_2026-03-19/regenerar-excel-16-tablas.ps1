# Regenerar archivo Excel con las 16 tablas (10 originales + 6 nuevas)
# Asegurarse de que no haya procesos usando el archivo

$excelPath = "c:\Git\VitaRaizPlus\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx"

Write-Host "=== Regenerador de Excel con 16 tablas ===" -ForegroundColor Cyan
Write-Host ""

# Verificar si el archivo existe
if (Test-Path $excelPath) {
    Write-Host "Excel actual encontrado:" -ForegroundColor Yellow
    Get-Item $excelPath | Format-List Name, Length, LastWriteTime
    
    Write-Host "Eliminando archivo existente..." -ForegroundColor Yellow
    try {
        Remove-Item $excelPath -Force
        Write-Host "[OK] Archivo eliminado" -ForegroundColor Green
    } catch {
        Write-Host "[ERROR] No se pudo eliminar: $_" -ForegroundColor Red
        Write-Host "Posible causa: archivo abierto en Excel" -ForegroundColor Yellow
        exit 1
    }
}

# Ejecutar API brevemente para generar Excel
Write-Host ""
Write-Host "Iniciando API para generar Excel..." -ForegroundColor Cyan
$apiProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "c:\Git\VitaRaizPlus\src\SalesCobrosGeo.Api\bin\Release\net9.0\SalesCobrosGeo.Api.dll", "--urls", "http://localhost:5053" `
    -PassThru `
    -WindowStyle Hidden

# Esperar a que el archivo se cree
Write-Host "Esperando creacion del archivo Excel..." -ForegroundColor Yellow
$maxWait = 10
$waited = 0
while (-not (Test-Path $excelPath) -and $waited -lt $maxWait) {
    Start-Sleep -Seconds 1
    $waited++
    Write-Host "." -NoNewline
}
Write-Host ""

# Detener API
if ($apiProcess -and !$apiProcess.HasExited) {
    Stop-Process -Id $apiProcess.Id -Force
    Write-Host "[OK] API detenida" -ForegroundColor Green
}

# Verificar resultado
if (Test-Path $excelPath) {
    Write-Host ""
    Write-Host "[EXITO] Excel creado con 16 tablas!" -ForegroundColor Green
    Get-Item $excelPath | Format-List Name, Length, LastWriteTime
    
    Write-Host ""
    Write-Host "Tablas creadas:" -ForegroundColor Cyan
    Write-Host "  [Originales]" -ForegroundColor White
    Write-Host "  1. Users" -ForegroundColor Gray
    Write-Host "  2. Zones" -ForegroundColor Gray
    Write-Host "  3. Products" -ForegroundColor Gray
    Write-Host "  4. PaymentMethods" -ForegroundColor Gray
    Write-Host "  5. Clients" -ForegroundColor Gray
    Write-Host "  6. Sales" -ForegroundColor Gray
    Write-Host "  7. SaleItems" -ForegroundColor Gray
    Write-Host "  8. SaleHistory" -ForegroundColor Gray
    Write-Host "  9. Collections" -ForegroundColor Gray
    Write-Host "  10. AuditTrail" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  [Configuracion Dinamica]" -ForegroundColor White
    Write-Host "  11. MenuItems" -ForegroundColor Green
    Write-Host "  12. WeekDays" -ForegroundColor Green
    Write-Host "  13. SaleStatuses" -ForegroundColor Green
    Write-Host "  14. CollectionStatuses" -ForegroundColor Green
    Write-Host "  15. CatalogTypes" -ForegroundColor Green
    Write-Host "  16. UISettings" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[ERROR] Excel no se creo" -ForegroundColor Red
    exit 1
}
