# Migrate-DataToExcel.ps1
# Script para migrar datos existentes de SQLite/JSON a Excel
# Fecha: 2026-03-19
# Fase 5: Migración de Datos

<#
.SYNOPSIS
    Migra datos de SQLite y archivos JSON a Excel como fuente única de datos.

.DESCRIPTION
    Este script lee datos de:
    - SQLite (security.db en Web/App_Data)
    - Archivos JSON (ventas.json, cobros.json si existen)
    Y los migra a las hojas correspondientes en Excel:
    - Users → Sheet "Users"
    - Sales → Sheet "Sales"
    - Collections → Sheet "Collections"

.PARAMETER ExcelPath
    Ruta al archivo Excel destino. Por defecto: API/App_Data/SalesCobrosGeo.xlsx

.PARAMETER WebAppDataPath
    Ruta a la carpeta App_Data de Web. Por defecto: Web/App_Data

.PARAMETER BackupOriginal
    Si se debe hacer backup de los archivos originales. Por defecto: $true

.EXAMPLE
    .\Migrate-DataToExcel.ps1 -ExcelPath "C:\Data\SalesCobrosGeo.xlsx"
#>

param(
    [string]$ExcelPath = "$PSScriptRoot\..\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx",
    [string]$WebAppDataPath = "$PSScriptRoot\..\src\SalesCobrosGeo.Web\App_Data",
    [bool]$BackupOriginal = $true
)

# Verificar módulo ImportExcel
if (-not (Get-Module -ListAvailable -Name ImportExcel)) {
    Write-Host "Instalando módulo ImportExcel..." -ForegroundColor Yellow
    Install-Module -Name ImportExcel -Scope CurrentUser -Force
}

Import-Module ImportExcel

# ═══════════════════════════════════════════════════════════════════════════════
# FUNCIONES AUXILIARES
# ═══════════════════════════════════════════════════════════════════════════════

function Write-MigrationLog {
    param([string]$Message, [string]$Level = "INFO")
    
    $color = switch ($Level) {
        "ERROR" { "Red" }
        "WARN" { "Yellow" }
        "SUCCESS" { "Green" }
        default { "White" }
    }
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Backup-OriginalFiles {
    param([string]$WebAppDataPath)
    
    $backupDate = Get-Date -Format "yyyy-MM-dd_HHmmss"
    $backupPath = "$PSScriptRoot\..\backup\data-backup_$backupDate"
    
    New-Item -ItemType Directory -Path $backupPath -Force | Out-Null
    
    # Backup SQLite
    $sqliteFiles = Get-ChildItem -Path $WebAppDataPath -Filter "*.db*"
    foreach ($file in $sqliteFiles) {
        Copy-Item $file.FullName -Destination $backupPath
        Write-MigrationLog "Backup: $($file.Name)" -Level "INFO"
    }
    
    # Backup JSON si existen
    $jsonFiles = Get-ChildItem -Path $WebAppDataPath -Filter "*.json" -ErrorAction SilentlyContinue
    foreach ($file in $jsonFiles) {
        Copy-Item $file.FullName -Destination $backupPath
        Write-MigrationLog "Backup: $($file.Name)" -Level "INFO"
    }
    
    Write-MigrationLog "Backup completado en: $backupPath" -Level "SUCCESS"
    return $backupPath
}

function Read-SqliteTable {
    param(
        [string]$DbPath,
        [string]$TableName
    )
    
    if (-not (Test-Path $DbPath)) {
        Write-MigrationLog "SQLite no encontrado: $DbPath" -Level "WARN"
        return @()
    }
    
    try {
        # Usar System.Data.SQLite o ejecutar sqlite3.exe
        $connectionString = "Data Source=$DbPath;Version=3;Read Only=True;"
        $connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
        $connection.Open()
        
        $command = $connection.CreateCommand()
        $command.CommandText = "SELECT * FROM $TableName"
        
        $adapter = New-Object System.Data.SQLite.SQLiteDataAdapter($command)
        $dataTable = New-Object System.Data.DataTable
        $adapter.Fill($dataTable)
        
        $connection.Close()
        
        return $dataTable
    }
    catch {
        Write-MigrationLog "Error al leer tabla $TableName : $_" -Level "ERROR"
        return @()
    }
}

function Read-JsonFile {
    param([string]$JsonPath)
    
    if (-not (Test-Path $JsonPath)) {
        Write-MigrationLog "JSON no encontrado: $JsonPath" -Level "WARN"
        return @()
    }
    
    try {
        $data = Get-Content $JsonPath -Raw | ConvertFrom-Json
        Write-MigrationLog "Leídos $($data.Count) registros de $JsonPath" -Level "INFO"
        return $data
    }
    catch {
        Write-MigrationLog "Error al leer JSON: $_" -Level "ERROR"
        return @()
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# MIGRACIÓN: USERS
# ═══════════════════════════════════════════════════════════════════════════════

function Migrate-Users {
    param(
        [string]$ExcelPath,
        [string]$SqlitePath
    )
    
    Write-MigrationLog "═══ Migrando Usuarios ═══" -Level "INFO"
    
    # Leer usuarios existentes de Excel (para no duplicar)
    $existingUsers = @()
    if (Test-Path $ExcelPath) {
        try {
            $existingUsers = Import-Excel -Path $ExcelPath -WorksheetName "Users" -ErrorAction SilentlyContinue
        }
        catch {
            # Hoja no existe aún
        }
    }
    
    $existingUsernames = $existingUsers | ForEach-Object { $_.UserName }
    
    # Leer de SQLite
    $dbUsers = Read-SqliteTable -DbPath $SqlitePath -TableName "AspNetUsers"
    
    $migratedCount = 0
    foreach ($dbUser in $dbUsers) {
        if ($existingUsernames -contains $dbUser.UserName) {
            Write-MigrationLog "Usuario ya existe: $($dbUser.UserName)" -Level "WARN"
            continue
        }
        
        # Convertir a formato Excel
        $excelUser = [PSCustomObject]@{
            UserName = $dbUser.UserName
            Password = $dbUser.PasswordHash  # NOTA: Debería ser rehashed
            DisplayName = $dbUser.FullName ?? $dbUser.UserName
            Role = $dbUser.Role ?? "Vendedor"
            IsActive = $true
        }
        
        # Append a Excel
        $excelUser | Export-Excel -Path $ExcelPath -WorksheetName "Users" -Append -AutoSize
        $migratedCount++
    }
    
    Write-MigrationLog "Migrados $migratedCount usuarios" -Level "SUCCESS"
}

# ═══════════════════════════════════════════════════════════════════════════════
# MIGRACIÓN: SALES (desde JSON)
# ═══════════════════════════════════════════════════════════════════════════════

function Migrate-SalesFromJson {
    param(
        [string]$ExcelPath,
        [string]$JsonPath
    )
    
    Write-MigrationLog "═══ Migrando Ventas desde JSON ═══" -Level "INFO"
    
    $salesData = Read-JsonFile -JsonPath $JsonPath
    if ($salesData.Count -eq 0) {
        Write-MigrationLog "No hay ventas para migrar" -Level "WARN"
        return
    }
    
    $migratedCount = 0
    foreach ($sale in $salesData) {
        $excelSale = [PSCustomObject]@{
            IdV = $sale.IdV ?? "V$(Get-Random -Minimum 100000 -Maximum 999999)"
            NumVenta = $sale.NumVenta ?? 0
            FechaVenta = $sale.FechaVenta ?? (Get-Date)
            NombreCliente = $sale.NombreCliente ?? ""
            Celular = $sale.Celular ?? ""
            Telefono = $sale.Telefono
            Zona = $sale.Zona ?? ""
            FormaPago = $sale.FormaPago ?? ""
            DiaCobro = $sale.DiaCobro ?? ""
            FotoCliente = $sale.FotoCliente
            FotoFachada = $sale.FotoFachada
            FotoContrato = $sale.FotoContrato
            ObservacionVenta = $sale.ObservacionVenta
            Vendedor = $sale.Vendedor ?? ""
            Usuario = $sale.Usuario ?? ""
            Cobrador = $sale.Cobrador ?? ""
            Coordenadas = $sale.Coordenadas ?? ""
            UrlUbicacion = $sale.UrlUbicacion
            FechaPrimerCobro = $sale.FechaPrimerCobro
            Estado = $sale.Estado ?? "PENDIENTE"
            FechaActu = Get-Date
            Estado2 = $sale.Estado2 ?? "OPEN"
            ComisionVendedorPct = $sale.ComisionVendedorPct ?? 0
            Cobrar = $sale.Cobrar ?? "OK"
            FotoAdd1 = $sale.FotoAdd1
            FotoAdd2 = $sale.FotoAdd2
            Coordenadas2 = $sale.Coordenadas2
            ProductosCodigos = ""  # Serializar productos si existen
            ProductosCantidades = ""
            ProductosPrecios = ""
            ImporteTotal = $sale.ImporteTotal ?? 0
        }
        
        # Serializar productos
        if ($sale.Productos) {
            $excelSale.ProductosCodigos = ($sale.Productos | ForEach-Object { $_.ProductCode }) -join "|"
            $excelSale.ProductosCantidades = ($sale.Productos | ForEach-Object { $_.Quantity }) -join "|"
            $excelSale.ProductosPrecios = ($sale.Productos | ForEach-Object { $_.UnitPrice }) -join "|"
        }
        
        $excelSale | Export-Excel -Path $ExcelPath -WorksheetName "Sales" -Append -AutoSize
        $migratedCount++
    }
    
    Write-MigrationLog "Migradas $migratedCount ventas" -Level "SUCCESS"
}

# ═══════════════════════════════════════════════════════════════════════════════
# MIGRACIÓN: COLLECTIONS (desde JSON)
# ═══════════════════════════════════════════════════════════════════════════════

function Migrate-CollectionsFromJson {
    param(
        [string]$ExcelPath,
        [string]$JsonPath
    )
    
    Write-MigrationLog "═══ Migrando Cobros desde JSON ═══" -Level "INFO"
    
    $collectionsData = Read-JsonFile -JsonPath $JsonPath
    if ($collectionsData.Count -eq 0) {
        Write-MigrationLog "No hay cobros para migrar" -Level "WARN"
        return
    }
    
    $migratedCount = 0
    foreach ($collection in $collectionsData) {
        $excelCollection = [PSCustomObject]@{
            IdCc = $collection.IdCc ?? "C$(Get-Random -Minimum 100000 -Maximum 999999)"
            IdV = $collection.IdV ?? ""
            NumVenta = $collection.NumVenta ?? 0
            NombreCliente = $collection.NombreCliente ?? ""
            ImporteCobro = $collection.ImporteCobro ?? 0
            FechaCobro = $collection.FechaCobro ?? (Get-Date)
            ObservacionCobro = $collection.ObservacionCobro
            FechaCaptura = $collection.FechaCaptura ?? (Get-Date)
            EstadoCc = $collection.EstadoCc ?? "PARCIAL"
            Usuario = $collection.Usuario ?? ""
            Zona = $collection.Zona ?? ""
            DiaCobroPrevisto = $collection.DiaCobroPrevisto ?? ""
            DiaCobrado = $collection.DiaCobrado ?? ""
            CoordenadasCobro = $collection.CoordenadasCobro
            FotoCobro = $collection.FotoCobro
            ImporteAbonado = $collection.ImporteAbonado ?? 0
            ImporteRestante = $collection.ImporteRestante ?? 0
            ImporteTotal = $collection.ImporteTotal ?? 0
        }
        
        $excelCollection | Export-Excel -Path $ExcelPath -WorksheetName "Collections" -Append -AutoSize
        $migratedCount++
    }
    
    Write-MigrationLog "Migrados $migratedCount cobros" -Level "SUCCESS"
}

# ═══════════════════════════════════════════════════════════════════════════════
# MAIN EXECUTION
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  SCRIPT DE MIGRACIÓN DE DATOS A EXCEL - FASE 5" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Verificar archivos
if (-not (Test-Path $ExcelPath)) {
    Write-MigrationLog "Excel no encontrado: $ExcelPath" -Level "ERROR"
    Write-MigrationLog "Ejecute primero la API para crear el archivo Excel" -Level "WARN"
    exit 1
}

# Backup
if ($BackupOriginal) {
    $backupPath = Backup-OriginalFiles -WebAppDataPath $WebAppDataPath
}

# Migrar Usuarios
$sqlitePath = Join-Path $WebAppDataPath "security.db"
# Migrate-Users -ExcelPath $ExcelPath -SqlitePath $sqlitePath  # Descomentado cuando se use

# Migrar Ventas
$salesJsonPath = Join-Path $WebAppDataPath "ventas.json"
if (Test-Path $salesJsonPath) {
    Migrate-SalesFromJson -ExcelPath $ExcelPath -JsonPath $salesJsonPath
}

# Migrar Cobros
$collectionsJsonPath = Join-Path $WebAppDataPath "cobros.json"
if (Test-Path $collectionsJsonPath) {
    Migrate-CollectionsFromJson -ExcelPath $ExcelPath -JsonPath $collectionsJsonPath
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  MIGRACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "Siguientes pasos:" -ForegroundColor Yellow
Write-Host "  1. Verificar datos en Excel: $ExcelPath" -ForegroundColor White
Write-Host "  2. Probar API y Web con los datos migrados" -ForegroundColor White
Write-Host "  3. Si todo funciona, eliminar archivos SQLite/JSON originales" -ForegroundColor White
Write-Host ""
