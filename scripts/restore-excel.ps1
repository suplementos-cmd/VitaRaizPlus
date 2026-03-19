# ═══════════════════════════════════════════════════════════════════════════════
# Script de Restauración del Excel de SalesCobrosGeo
# ═══════════════════════════════════════════════════════════════════════════════
# Descripción: Restaura un backup del archivo Excel
# Uso: .\restore-excel.ps1 [-BackupFile <ruta_al_backup>]
# ═══════════════════════════════════════════════════════════════════════════════

param(
    [Parameter(HelpMessage = "Ruta al archivo de backup a restaurar")]
    [string]$BackupFile,
    
    [Parameter(HelpMessage = "Ruta de destino del Excel (opcional)")]
    [string]$ExcelPath = "$PSScriptRoot\..\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx",
    
    [Parameter(HelpMessage = "Directorio de backups (opcional)")]
    [string]$BackupDir = "$PSScriptRoot\..\Backups\Excel",
    
    [Parameter(HelpMessage = "Forzar restauración sin confirmación")]
    [switch]$Force
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
    Write-Info "Iniciando restauración de Excel..."
    
    # Si no se especificó archivo, mostrar lista de backups disponibles
    if (-not $BackupFile) {
        Write-Info "Buscando backups disponibles en: $BackupDir"
        
        if (-not (Test-Path $BackupDir)) {
            Write-Error "No existe el directorio de backups: $BackupDir"
            exit 1
        }
        
        $backups = Get-ChildItem -Path $BackupDir -Filter "SalesCobrosGeo_backup_*.xlsx" |
                   Sort-Object CreationTime -Descending
        
        if ($backups.Count -eq 0) {
            Write-Error "No se encontraron backups disponibles"
            exit 1
        }
        
        Write-Host ""
        Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
        Write-Host "BACKUPS DISPONIBLES" -ForegroundColor Yellow
        Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
        
        for ($i = 0; $i -lt $backups.Count; $i++) {
            $backup = $backups[$i]
            Write-Host "[$($i + 1)] " -NoNewline -ForegroundColor Yellow
            Write-Host "$($backup.Name) " -NoNewline -ForegroundColor White
            Write-Host "($([math]::Round($backup.Length / 1KB, 2)) KB, " -NoNewline -ForegroundColor Gray
            Write-Host "$($backup.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))" -NoNewline -ForegroundColor Gray
            Write-Host ")" -ForegroundColor Gray
        }
        
        Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
        Write-Host ""
        
        $selection = Read-Host "Seleccione el número del backup a restaurar (0 para cancelar)"
        
        if ($selection -eq "0" -or [string]::IsNullOrWhiteSpace($selection)) {
            Write-Warning "Restauración cancelada por el usuario"
            exit 0
        }
        
        $index = [int]$selection - 1
        
        if ($index -lt 0 -or $index -ge $backups.Count) {
            Write-Error "Selección inválida"
            exit 1
        }
        
        $BackupFile = $backups[$index].FullName
    }
    
    # Verificar que existe el archivo de backup
    if (-not (Test-Path $BackupFile)) {
        Write-Error "No se encontró el archivo de backup: $BackupFile"
        exit 1
    }
    
    # Crear backup del archivo actual antes de restaurar
    if (Test-Path $ExcelPath) {
        if (-not $Force) {
            Write-Warning "Se va a sobrescribir el archivo Excel existente"
            Write-Info "Origen: $BackupFile"
            Write-Info "Destino: $ExcelPath"
            
            $confirmation = Read-Host "¿Desea continuar? (S/N)"
            
            if ($confirmation -notmatch "^[Ss]$") {
                Write-Warning "Restauración cancelada por el usuario"
                exit 0
            }
        }
        
        Write-Info "Creando backup de seguridad del archivo actual..."
        $safetyBackup = "$ExcelPath.before-restore.$(Get-Date -Format 'yyyyMMdd_HHmmss').xlsx"
        Copy-Item -Path $ExcelPath -Destination $safetyBackup -Force
        Write-Success "Backup de seguridad creado: $(Split-Path $safetyBackup -Leaf)"
    }
    
    # Restaurar archivo
    Write-Info "Restaurando archivo..."
    Copy-Item -Path $BackupFile -Destination $ExcelPath -Force
    
    # Verificar que se restauró correctamente
    if (Test-Path $ExcelPath) {
        $backupSize = (Get-Item $BackupFile).Length
        $restoredSize = (Get-Item $ExcelPath).Length
        
        if ($backupSize -eq $restoredSize) {
            Write-Success "Archivo restaurado exitosamente"
            Write-Info "Tamaño: $([math]::Round($restoredSize / 1KB, 2)) KB"
        } else {
            Write-Warning "El tamaño del archivo restaurado no coincide con el backup"
        }
    } else {
        Write-Error "No se pudo verificar la restauración"
        exit 1
    }
    
    # Resumen
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
    Write-Success "RESTAURACIÓN COMPLETADA EXITOSAMENTE"
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
    Write-Host "Backup usado   : " -NoNewline; Write-Host $BackupFile -ForegroundColor White
    Write-Host "Archivo Excel  : " -NoNewline; Write-Host $ExcelPath -ForegroundColor White
    Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Gray
    Write-Warning "IMPORTANTE: Reinicie la aplicación para que los cambios surtan efecto"
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
    
    exit 0
}
catch {
    Write-Error "Error durante la restauración: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
