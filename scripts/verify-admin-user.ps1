# Script para verificar y corregir el usuario admin
# Verifica que el usuario admin existe y tiene el rol Administrador correcto

param(
    [string]$ExcelPath = "$PSScriptRoot\..\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx"
)

Write-Host "=== Verificador de Usuario Admin ===" -ForegroundColor Cyan
Write-Host ""

# Verificar que el archivo Excel existe
if (-not (Test-Path $ExcelPath)) {
    Write-Host "ERROR: No se encontró el archivo Excel en: $ExcelPath" -ForegroundColor Red
    Write-Host "Ejecuta primero la API para que se cree el archivo Excel inicial." -ForegroundColor Yellow
    exit 1
}

Write-Host "Archivo Excel encontrado: $ExcelPath" -ForegroundColor Green
Write-Host ""

# Cargar el módulo ImportExcel si está disponible
try {
    Import-Module ImportExcel -ErrorAction Stop
    Write-Host "Módulo ImportExcel cargado correctamente" -ForegroundColor Green
} catch {
    Write-Host "ADVERTENCIA: El módulo ImportExcel no está instalado" -ForegroundColor Yellow
    Write-Host "Para instalar: Install-Module -Name ImportExcel -Scope CurrentUser" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Mostrando información básica del archivo..." -ForegroundColor Cyan
    
    $fileInfo = Get-Item $ExcelPath
    Write-Host "  Tamaño: $($fileInfo.Length) bytes"
    Write-Host "  Última modificación: $($fileInfo.LastWriteTime)"
    Write-Host ""
    Write-Host "No se puede leer el contenido sin el módulo ImportExcel" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Leyendo usuarios del Excel..." -ForegroundColor Cyan

try {
    # Leer la hoja Users
    $users = Import-Excel -Path $ExcelPath -WorksheetName "Users"
    
    Write-Host "Total de usuarios encontrados: $($users.Count)" -ForegroundColor Green
    Write-Host ""
    
    # Buscar el usuario admin
    $adminUser = $users | Where-Object { $_.UserName -eq "admin" }
    
    if ($null -eq $adminUser) {
        Write-Host "ERROR: No se encontró el usuario 'admin'" -ForegroundColor Red
        Write-Host ""
        Write-Host "Usuarios existentes:" -ForegroundColor Yellow
        $users | Select-Object UserName, DisplayName, Role, IsActive | Format-Table
        Write-Host ""
        Write-Host "SOLUCIÓN: Reinicia la API para que se creen los usuarios iniciales" -ForegroundColor Cyan
        exit 1
    }
    
    Write-Host "Usuario admin encontrado:" -ForegroundColor Green
    Write-Host "  UserName: $($adminUser.UserName)" -ForegroundColor White
    Write-Host "  DisplayName: $($adminUser.DisplayName)" -ForegroundColor White
    Write-Host "  Role: $($adminUser.Role)" -ForegroundColor $(if ($adminUser.Role -eq "Administrador") { "Green" } else { "Red" })
    Write-Host "  IsActive: $($adminUser.IsActive)" -ForegroundColor $(if ($adminUser.IsActive) { "Green" } else { "Red" })
    Write-Host "  Password: $(if ($adminUser.Password) { '[Configurado]' } else { '[NO CONFIGURADO]' })" -ForegroundColor $(if ($adminUser.Password) { "Green" } else { "Red" })
    Write-Host ""
    
    # Verificar el rol
    if ($adminUser.Role -ne "Administrador") {
        Write-Host "ERROR: El usuario admin tiene rol incorrecto: '$($adminUser.Role)'" -ForegroundColor Red
        Write-Host "       Debería ser: 'Administrador'" -ForegroundColor Yellow
        Write-Host ""
        
        # Detectar si pusieron el rol Web en vez del rol API
        if ($adminUser.Role -eq "FULL") {
            Write-Host "Detectado error común: Pusiste 'FULL' en vez de 'Administrador'" -ForegroundColor Magenta
            Write-Host "  FULL = Rol Web (interno de la aplicación)" -ForegroundColor White
            Write-Host "  Administrador = Rol API (debe ir en el Excel)" -ForegroundColor White
            Write-Host ""
        }
        
        Write-Host "Para corregir manualmente:" -ForegroundColor Cyan
        Write-Host "  1. Abre el archivo Excel: $ExcelPath"
        Write-Host "  2. Ve a la hoja 'Users'"
        Write-Host "  3. Busca la fila del usuario 'admin'"
        Write-Host "  4. Cambia la columna 'Role' de '$($adminUser.Role)' a 'Administrador'"
        Write-Host "  5. Guarda el archivo"
        Write-Host "  6. Reinicia la aplicación"
        Write-Host ""
        Write-Host "O regenera el Excel:" -ForegroundColor Cyan
        Write-Host "  Remove-Item '$ExcelPath' -Force"
        Write-Host "  .\scripts\run-api.ps1"
        exit 1
    }
    
    # Verificar IsActive
    if (-not $adminUser.IsActive) {
        Write-Host "ERROR: El usuario admin está inactivo" -ForegroundColor Red
        Write-Host ""
        Write-Host "Para corregir manualmente:" -ForegroundColor Cyan
        Write-Host "  1. Abre el archivo Excel: $ExcelPath"
        Write-Host "  2. Ve a la hoja 'Users'"
        Write-Host "  3. Busca la fila del usuario 'admin'"
        Write-Host "  4. Cambia la columna 'IsActive' a TRUE"
        Write-Host "  5. Guarda el archivo"
        Write-Host "  6. Reinicia la aplicación"
        exit 1
    }
    
    Write-Host "✓ El usuario admin está configurado correctamente" -ForegroundColor Green
    Write-Host ""
    Write-Host "Mapeo de permisos esperado:" -ForegroundColor Cyan
    Write-Host "  Rol Web: FULL" -ForegroundColor White
    Write-Host "  Permisos:" -ForegroundColor White
    Write-Host "    - dashboard:view" -ForegroundColor White
    Write-Host "    - sales:view" -ForegroundColor White
    Write-Host "    - collections:view" -ForegroundColor White
    Write-Host "    - maintenance:view" -ForegroundColor White
    Write-Host "    - administration:view" -ForegroundColor White
    Write-Host ""
    Write-Host "Si aún no ves todas las vistas:" -ForegroundColor Yellow
    Write-Host "  1. Cierra sesión completamente"
    Write-Host "  2. Cierra el navegador (para limpiar cookies)"
    Write-Host "  3. Reinicia la API si estaba corriendo"
    Write-Host "  4. Vuelve a iniciar sesión con: admin / admin123"
    Write-Host "  5. Visita: http://localhost:puerto/Account/DiagnosticPermissions"
    Write-Host "     para ver tus permisos actuales"
    Write-Host ""
    
    # Mostrar todos los usuarios
    Write-Host "Todos los usuarios en el sistema:" -ForegroundColor Cyan
    $users | Select-Object UserName, DisplayName, Role, IsActive | Format-Table -AutoSize
    
} catch {
    Write-Host "ERROR al leer el archivo Excel:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Asegúrate de que:" -ForegroundColor Yellow
    Write-Host "  - El archivo Excel no esté abierto en otra aplicación"
    Write-Host "  - Tienes permisos para leer el archivo"
    exit 1
}

Write-Host ""
Write-Host "Verificación completada" -ForegroundColor Green
