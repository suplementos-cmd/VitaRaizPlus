# ═══════════════════════════════════════════════════════════════════════════════
# Script de Respaldo del Excel de SalesCobrosGeo
# ═══════════════════════════════════════════════════════════════════════════════
# Descripción: Crea respaldos automáticos del archivo Excel con timestamp
# Uso: .\backup-excel.ps1
# ═══════════════════════════════════════════════════════════════════════════════

param(
    [Parameter(HelpMessage = "Ruta al archivo Excel (opcional)")]
    [string]$ExcelPath = "$PSScriptRoot\..\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx",
    
    [Parameter(HelpMessage = "Directorio de backups (opcional)")]
    [string]$BackupDir = "$PSScriptRoot\..\Backups\Excel",
    
    [Parameter(HelpMessage = "Número de backups a mantener (0 = ilimitado)")]
    [int]$MaxBackups = 30
)

# Funciones auxiliares
function Write-Info($message) {
    Write-Host "[INFO] $message" -ForegroundColor Cyan
}

function Write-Success($message) {
    Write-Host "[OK] $message" -ForegroundColor Green
}

function Write-Warning($message) {
    Write-Host "[WARN] $message" -ForegroundColor Yellow
}

function Write-Error($message) {
    Write-Host "[ERROR] $message" -ForegroundColor Red
}

# Main
try {
    Write-Info "Iniciando respaldo de Excel..."
    
    # Verificar que existe el archivo fuente
    if (-not (Test-Path $ExcelPath)) {
        Write-Error "No se encontró el archivo Excel en: $ExcelPath"
        exit 1
    }
    
    # Crear directorio de backups si no existe
    if (-not (Test-Path $BackupDir)) {
        Write-Info "Creando directorio de backups: $BackupDir"
        New-Item -ItemType Directory -Path $BackupDir | Out-Null
    }
    
    # Generar nombre de backup con timestamp
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFileName = "SalesCobrosGeo_backup_$timestamp.xlsx"
    $backupPath = Join-Path $BackupDir $backupFileName
    
    # Copiar archivo
    Write-Info "Copiando archivo..."
    Copy-Item -Path $ExcelPath -Destination $backupPath -Force
    
    # Verificar que se copió correctamente
    if (Test-Path $backupPath) {
        $sourceSize = (Get-Item $ExcelPath).Length
        $backupSize = (Get-Item $backupPath).Length
        
        if ($sourceSize -eq $backupSize) {
            Write-Success "Respaldo creado exitosamente: $backupFileName"
            Write-Info "Tamaño: $([math]::Round($backupSize / 1KB, 2)) KB"
        } else {
            Write-Warning "El tamaño del backup no coincide con el original"
        }
    } else {
        Write-Error "No se pudo verificar el backup creado"
        exit 1
    }
    
    # Limpiar backups antiguos si se especificó límite
    if ($MaxBackups -gt 0) {
        Write-Info "Verificando límite de backups (máximo: $MaxBackups)..."
        
        $backups = Get-ChildItem -Path $BackupDir -Filter "SalesCobrosGeo_backup_*.xlsx" |
                   Sort-Object CreationTime -Descending
        
        $backupCount = $backups.Count
        Write-Info "Backups existentes: $backupCount"
        
        if ($backupCount -gt $MaxBackups) {
            $toDelete = $backups | Select-Object -Skip $MaxBackups
            $deleteCount = $toDelete.Count
            
            Write-Warning "Eliminando $deleteCount backup(s) antiguo(s)..."
            
            foreach ($backup in $toDelete) {
                Write-Info "  Eliminando: $($backup.Name)"
                Remove-Item $backup.FullName -Force
            }
            
            Write-Success "Limpieza completada"
        }
    }
    
    # Resumen
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
    Write-Success "RESPALDO COMPLETADO EXITOSAMENTE"
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
    Write-Host "Archivo fuente : " -NoNewline; Write-Host $ExcelPath -ForegroundColor White
    Write-Host "Backup creado  : " -NoNewline; Write-Host $backupPath -ForegroundColor White
    Write-Host "Directorio     : " -NoNewline; Write-Host $BackupDir -ForegroundColor White
    
    $allBackups = Get-ChildItem -Path $BackupDir -Filter "SalesCobrosGeo_backup_*.xlsx"
    Write-Host "Total backups  : " -NoNewline; Write-Host $allBackups.Count -ForegroundColor White
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
    
    exit 0
}
catch {
    Write-Error "Error durante el respaldo: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
