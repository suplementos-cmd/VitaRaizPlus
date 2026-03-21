# Script para corregir automáticamente el rol del usuario admin
# Convierte FULL → Administrador en el Excel

param(
    [string]$ExcelPath = "$PSScriptRoot\..\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx"
)

Write-Host "=== Corrector Automático de Rol Admin ===" -ForegroundColor Cyan
Write-Host ""

# Verificar que el archivo Excel existe
if (-not (Test-Path $ExcelPath)) {
    Write-Host "ERROR: No se encontró el archivo Excel en: $ExcelPath" -ForegroundColor Red
    exit 1
}

Write-Host "Archivo Excel encontrado: $ExcelPath" -ForegroundColor Green
Write-Host ""

# Cargar el módulo ImportExcel
try {
    Import-Module ImportExcel -ErrorAction Stop
} catch {
    Write-Host "ERROR: El módulo ImportExcel no está instalado" -ForegroundColor Red
    Write-Host "Instálalo con: Install-Module -Name ImportExcel -Scope CurrentUser" -ForegroundColor Yellow
    exit 1
}

Write-Host "Leyendo usuarios del Excel..." -ForegroundColor Cyan

try {
    # Leer la hoja Users
    $users = Import-Excel -Path $ExcelPath -WorksheetName "Users"
    
    # Buscar el usuario admin
    $adminIndex = -1
    for ($i = 0; $i -lt $users.Count; $i++) {
        if ($users[$i].UserName -eq "admin") {
            $adminIndex = $i
            break
        }
    }
    
    if ($adminIndex -eq -1) {
        Write-Host "ERROR: No se encontró el usuario 'admin'" -ForegroundColor Red
        exit 1
    }
    
    $adminUser = $users[$adminIndex]
    $currentRole = $adminUser.Role
    
    Write-Host "Usuario admin encontrado:" -ForegroundColor Green
    Write-Host "  Rol actual: $currentRole" -ForegroundColor White
    Write-Host ""
    
    # Verificar si necesita corrección
    if ($currentRole -eq "Administrador") {
        Write-Host "✓ El rol ya es correcto: 'Administrador'" -ForegroundColor Green
        Write-Host "No se requiere ninguna acción." -ForegroundColor Green
        exit 0
    }
    
    if ($currentRole -ne "FULL" -and $currentRole -ne "Administrador") {
        Write-Host "ADVERTENCIA: El rol es '$currentRole', esperaba 'FULL' o 'Administrador'" -ForegroundColor Yellow
        Write-Host "¿Deseas cambiarlo a 'Administrador'? (S/N)" -ForegroundColor Yellow
        $respuesta = Read-Host
        if ($respuesta -ne "S" -and $respuesta -ne "s") {
            Write-Host "Operación cancelada." -ForegroundColor Yellow
            exit 0
        }
    }
    
    Write-Host "Corrigiendo rol de 'FULL' → 'Administrador'..." -ForegroundColor Cyan
    
    # Actualizar el rol
    $users[$adminIndex].Role = "Administrador"
    
    # Guardar de vuelta al Excel
    $users | Export-Excel -Path $ExcelPath -WorksheetName "Users" -AutoSize -TableName "Users"
    
    Write-Host ""
    Write-Host "✓ Rol corregido exitosamente!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Siguiente paso:" -ForegroundColor Cyan
    Write-Host "  1. Reinicia la API (Ctrl+C y .\scripts\run-api.ps1)" -ForegroundColor White
    Write-Host "  2. Cierra sesión en la web" -ForegroundColor White
    Write-Host "  3. Vuelve a iniciar sesión con: admin / admin123" -ForegroundColor White
    Write-Host ""
    Write-Host "Ahora deberías ver todas las vistas!" -ForegroundColor Green
    
} catch {
    Write-Host "ERROR al procesar el archivo Excel:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Asegúrate de que el archivo Excel no esté abierto en otra aplicación" -ForegroundColor Yellow
    exit 1
}
