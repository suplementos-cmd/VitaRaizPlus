#!/usr/bin/env pwsh
# Script para verificar permisos RBAC del usuario admin

$ErrorActionPreference = "Stop"

Write-Host "=== Verificando permisos RBAC del usuario admin ===" -ForegroundColor Cyan

# Cargar ensamblado de Excel (EPPlus)
$projectPath = "$PSScriptRoot\..\src\SalesCobrosGeo.Api"
$excelPath = "$projectPath\App_Data\SalesCobrosGeo.xlsx"

if (-not (Test-Path $excelPath)) {
    Write-Host "ERROR: No se encuentra el Excel en: $excelPath" -ForegroundColor Red
    exit 1
}

Write-Host "`nArchivo Excel encontrado: $excelPath" -ForegroundColor Green

# Llamar a la API para obtener permisos efectivos
try {
    Write-Host "`nConsultando API para permisos efectivos del usuario 'admin'..." -ForegroundColor Yellow
    
    # Primero necesitamos autenticarnos
    $loginBody = @{
        Username = "admin"
        Password = "admin123"
    } | ConvertTo-Json
    
    $loginResponse = Invoke-RestMethod -Uri "http://localhost:5207/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    
    if ($loginResponse.success -and $loginResponse.token) {
        Write-Host "✓ Login exitoso, token obtenido" -ForegroundColor Green
        
        $headers = @{
            "Authorization" = "Bearer $($loginResponse.token)"
        }
        
        # Obtener permisos efectivos
        $permissionsResponse = Invoke-RestMethod -Uri "http://localhost:5207/api/rbac/users/admin/effective-permissions" -Method GET -Headers $headers
        
        Write-Host "`n=== Roles Activos ===" -ForegroundColor Cyan
        foreach ($role in $permissionsResponse.activeRoles) {
            Write-Host "  - [$($role.code)] $($role.name)" -ForegroundColor White
        }
        
        Write-Host "`n=== Permisos desde Roles ===" -ForegroundColor Cyan
        Write-Host "  Total: $($permissionsResponse.permissionsFromRoles.Count) permisos" -ForegroundColor White
        $permissionsResponse.permissionsFromRoles | ForEach-Object {
            Write-Host "    [$($_.code)] $($_.description)" -ForegroundColor Gray
        }
        
        Write-Host "`n=== Permisos Personalizados ===" -ForegroundColor Cyan
        if ($permissionsResponse.customPermissions.Count -gt 0) {
            foreach ($perm in $permissionsResponse.customPermissions) {
                $status = if ($perm.isGranted) { "GRANTED" } else { "DENIED" }
                Write-Host "  [$status] $($perm.permissionCode)" -ForegroundColor $(if ($perm.isGranted) { "Green" } else { "Red" })
            }
        } else {
            Write-Host "  (ninguno)" -ForegroundColor Gray
        }
        
        Write-Host "`n=== Permisos Efectivos Finales ===" -ForegroundColor Cyan
        Write-Host "  Total: $($permissionsResponse.effectivePermissions.Count) permisos" -ForegroundColor White
        $permissionsResponse.effectivePermissions | Sort-Object | ForEach-Object {
            Write-Host "    • $_" -ForegroundColor Yellow
        }
        
        # Verificar permisos clave
        Write-Host "`n=== Verificación de Permisos Clave ===" -ForegroundColor Cyan
        $requiredPermissions = @(
            "DASHBOARD.VIEW",
            "SALES.VIEW",
            "COLLECTIONS.VIEW",
            "ADMINISTRATION.VIEW"
        )
        
        foreach ($perm in $requiredPermissions) {
            $hasIt = $permissionsResponse.effectivePermissions -contains $perm
            $icon = if ($hasIt) { "✓" } else { "✗" }
            $color = if ($hasIt) { "Green" } else { "Red" }
            Write-Host "  $icon $perm" -ForegroundColor $color
        }
        
    } else {
        Write-Host "ERROR: Login falló" -ForegroundColor Red
        Write-Host $loginResponse | ConvertTo-Json -Depth 5
        exit 1
    }
    
} catch {
    Write-Host "`nERROR al consultar API:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "`nAsegúrate de que la API esté corriendo en http://localhost:5207" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n=== Verificación completa ===" -ForegroundColor Green
