# Script para verificar configuración de login

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  DIAGNÓSTICO DE LOGIN - VitaRaizPlus" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar que Excel existe
$excelPath = Join-Path $PSScriptRoot "..\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx"
Write-Host "1. Verificando archivo Excel..." -ForegroundColor Yellow
if (Test-Path $excelPath) {
    Write-Host "   ✓ Excel encontrado: $excelPath" -ForegroundColor Green
    $excelSize = (Get-Item $excelPath).Length / 1KB
    Write-Host "   ✓ Tamaño: $([math]::Round($excelSize, 2)) KB" -ForegroundColor Green
} else {
    Write-Host "   ✗ Excel NO encontrado en: $excelPath" -ForegroundColor Red
    Write-Host "   → Ejecuta: .\scripts\regenerate-excel.ps1" -ForegroundColor Yellow
}
Write-Host ""

# 2. Verificar si la API está corriendo
Write-Host "2. Verificando API en http://localhost:5207..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5207/api/auth/login" -Method POST -ContentType "application/json" -Body '{"userName":"test","password":"test"}' -ErrorAction SilentlyContinue -TimeoutSec 2
    Write-Host "   ✓ API está respondiendo (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "   ✓ API está corriendo (responde con 401 Unauthorized - esperado)" -ForegroundColor Green
    } else {
        Write-Host "   ✗ API NO está corriendo o no responde" -ForegroundColor Red
        Write-Host "   → Ejecuta en otra terminal: .\scripts\run-api.ps1" -ForegroundColor Yellow
    }
}
Write-Host ""

# 3. Instrucciones para crear usuario admin
Write-Host "3. Usuario de prueba recomendado:" -ForegroundColor Yellow
Write-Host "   Usuario: admin" -ForegroundColor White
Write-Host "   Password: admin123" -ForegroundColor White
Write-Host ""
Write-Host "   Para crear el usuario, abre el Excel y agrega en la hoja 'Users':" -ForegroundColor Cyan
Write-Host "   - UserName: admin" -ForegroundColor White
Write-Host "   - Password: admin123" -ForegroundColor White
Write-Host "   - DisplayName: Administrador" -ForegroundColor White
Write-Host "   - Role: Administrador" -ForegroundColor White
Write-Host "   - RoleLabel: Administrador del Sistema" -ForegroundColor White
Write-Host "   - Zone: CENTRO" -ForegroundColor White
Write-Host "   - Theme: root" -ForegroundColor White
Write-Host "   - IsActive: TRUE" -ForegroundColor White
Write-Host "   - TwoFactorEnabled: FALSE" -ForegroundColor White
Write-Host ""

# 4. Verificar puertos
Write-Host "4. Puertos configurados:" -ForegroundColor Yellow
Write-Host "   - API:  http://localhost:5207" -ForegroundColor White
Write-Host "   - Web:  http://localhost:5208" -ForegroundColor White
Write-Host ""

# 5. Comandos útiles
Write-Host "5. Comandos útiles:" -ForegroundColor Yellow
Write-Host "   .\scripts\run-api.ps1      # Iniciar API" -ForegroundColor White
Write-Host "   .\scripts\run-web.ps1      # Iniciar Web" -ForegroundColor White
Write-Host "   .\scripts\verify-admin-user.ps1  # Verificar usuario admin" -ForegroundColor White
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
