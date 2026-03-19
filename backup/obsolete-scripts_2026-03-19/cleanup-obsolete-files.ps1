<#
.SYNOPSIS
    Elimina archivos obsoletos despues de migracion a Excel
    
.DESCRIPTION
    Este script elimina archivos legacy (JSON, InMemory stores) que ya no se usan
    despues de migrar a Excel como fuente de datos centralizada.
    Crea backup automatico antes de eliminar.
    
.EXAMPLE
    .\cleanup-obsolete-files.ps1
#>

$ErrorActionPreference = "Stop"

$rootPath = Split-Path $PSScriptRoot -Parent
$backupPath = Join-Path $PSScriptRoot "..\backup\obsolete-files\$(Get-Date -Format 'yyyy-MM-dd_HHmmss')"

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "  Limpieza de Archivos Obsoletos - Post Migracion" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

# Archivos a eliminar con sus rutas relativas
$filesToDelete = @(
    # Web - Codigo C#
    "src\SalesCobrosGeo.Web\Services\Sales\JsonSalesRepository.cs",
    "src\SalesCobrosGeo.Web\Security\InMemoryUserSessionTracker.cs",
    "src\SalesCobrosGeo.Web\Security\InMemoryApplicationUserService.cs",
    
    # Api - Codigo C#
    "src\SalesCobrosGeo.Api\Business\InMemoryBusinessStore.cs",
    "src\SalesCobrosGeo.Api\Security\InMemoryUserStore.cs",
    "src\SalesCobrosGeo.Api\Audit\InMemoryAuditTrailStore.cs",
    
    # Web - Datos JSON
    "src\SalesCobrosGeo.Web\App_Data\cobros.json",
    "src\SalesCobrosGeo.Web\App_Data\ventas.json"
)

# 1. Verificar que archivos existen
Write-Host "[1/4] Verificando archivos a eliminar..." -ForegroundColor Cyan
$existingFiles = @()
$missingFiles = @()

foreach ($file in $filesToDelete) {
    $fullPath = Join-Path $rootPath $file
    if (Test-Path $fullPath) {
        $existingFiles += $fullPath
        $fileInfo = Get-Item $fullPath
        Write-Host "  [OK] $file ($([math]::Round($fileInfo.Length / 1KB, 2)) KB)" -ForegroundColor Green
    }
    else {
        $missingFiles += $file
        Write-Host "  [SKIP] $file (no existe)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Resumen: $($existingFiles.Count) archivos a eliminar, $($missingFiles.Count) ya no existen" -ForegroundColor White
Write-Host ""

if ($existingFiles.Count -eq 0) {
    Write-Host "No hay archivos para eliminar. Limpieza ya realizada." -ForegroundColor Green
    exit 0
}

# 2. Crear backup
Write-Host "[2/4] Creando backup..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path $backupPath -Force | Out-Null

$backedUpCount = 0
foreach ($file in $existingFiles) {
    $relativePath = $file.Replace($rootPath, "").TrimStart("\")
    $backupFile = Join-Path $backupPath $relativePath
    $backupDir = Split-Path $backupFile -Parent
    
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    Copy-Item $file -Destination $backupFile -Force
    $backedUpCount++
}

Write-Host "  [OK] $backedUpCount archivos respaldados en:" -ForegroundColor Green
Write-Host "       $backupPath" -ForegroundColor Gray
Write-Host ""

# 3. Eliminar archivos
Write-Host "[3/4] Eliminando archivos obsoletos..." -ForegroundColor Cyan

$deletedCount = 0
foreach ($file in $existingFiles) {
    try {
        Remove-Item $file -Force
        $deletedCount++
        $fileName = Split-Path $file -Leaf
        Write-Host "  [DELETED] $fileName" -ForegroundColor Red
    }
    catch {
        Write-Host "  [ERROR] No se pudo eliminar: $file" -ForegroundColor Red
        Write-Host "          $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "  [OK] $deletedCount archivos eliminados" -ForegroundColor Green
Write-Host ""

# 4. Compilar proyecto para verificar
Write-Host "[4/4] Verificando compilacion..." -ForegroundColor Cyan
Write-Host ""

$solutionPath = Join-Path $rootPath "SalesCobrosGeo.sln"
$buildOutput = dotnet build $solutionPath -c Release --no-incremental 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "===================================================" -ForegroundColor Green
    Write-Host "  [OK] Limpieza completada exitosamente           " -ForegroundColor Green
    Write-Host "===================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Archivos eliminados:  $deletedCount" -ForegroundColor White
    Write-Host "Backup creado en:     $backupPath" -ForegroundColor White
    Write-Host "Compilacion:          Exitosa" -ForegroundColor Green
    Write-Host ""
    Write-Host "Reduccion de codigo:  ~880 lineas eliminadas" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Siguiente paso: Commit de cambios" -ForegroundColor Yellow
    Write-Host "  git add ." -ForegroundColor Gray
    Write-Host "  git commit -m 'Limpieza: Eliminar archivos obsoletos post-migracion Excel'" -ForegroundColor Gray
    Write-Host ""
}
else {
    Write-Host "===================================================" -ForegroundColor Red
    Write-Host "  [ERROR] Compilacion fallo despues de limpieza   " -ForegroundColor Red
    Write-Host "===================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Para revertir cambios, restaura el backup:" -ForegroundColor Yellow
    Write-Host "  Copy-Item '$backupPath\*' -Destination '$rootPath' -Recurse -Force" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Output de compilacion:" -ForegroundColor Yellow
    Write-Host $buildOutput -ForegroundColor Gray
    exit 1
}
