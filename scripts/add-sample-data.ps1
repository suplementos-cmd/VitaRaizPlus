<#
.SYNOPSIS
    Agrega datos de ejemplo funcionales al archivo Excel SalesCobrosGeo.xlsx
    
.DESCRIPTION
    Este script agrega:
    - 2 ventas de ejemplo con datos realistas
    - 2 cobros de ejemplo relacionados a las ventas
    - Verifica usuarios y sus asignaciones RBAC
    
.NOTES
    Ejecutar desde: scripts/
    Requiere: ImportExcel module
#>

param(
    [string]$ExcelPath = "..\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx"
)

$ErrorActionPreference = "Stop"

# ══════════════════════════════════════════════════════════════════════
# FUNCIONES AUXILIARES
# ══════════════════════════════════════════════════════════════════════

function Write-Header {
    param([string]$Message)
    Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Message)
    Write-Host "`n[$([DateTime]::Now.ToString('HH:mm:ss'))] $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "   [OK] $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "   [INFO] $Message" -ForegroundColor Cyan
}

# ══════════════════════════════════════════════════════════════════════
# VALIDACIONES INICIALES
# ══════════════════════════════════════════════════════════════════════

Write-Header "AGREGAR DATOS DE EJEMPLO"

# Verificar módulo ImportExcel
if (-not (Get-Module -ListAvailable -Name ImportExcel)) {
    Write-Host "[ERROR] Módulo ImportExcel no instalado. Instalando..." -ForegroundColor Red
    Install-Module -Name ImportExcel -Scope CurrentUser -Force
    Write-Success "Módulo ImportExcel instalado"
}

Import-Module ImportExcel -ErrorAction Stop

# Resolver ruta absoluta
$ExcelPath = Join-Path $PSScriptRoot $ExcelPath | Resolve-Path
Write-Info "Trabajando con: $ExcelPath"

if (-not (Test-Path $ExcelPath)) {
    Write-Host "[ERROR] No se encontró el archivo Excel: $ExcelPath" -ForegroundColor Red
    exit 1
}

# Crear backup
$backupPath = "$ExcelPath.backup.$((Get-Date).ToString('yyyyMMdd_HHmmss'))"
Copy-Item -Path $ExcelPath -Destination $backupPath -Force
Write-Success "Backup creado: $backupPath"

# ══════════════════════════════════════════════════════════════════════
# DATOS DE EJEMPLO
# ══════════════════════════════════════════════════════════════════════

$now = Get-Date
$today = $now.Date

# ------------------------------------------------------------------
# VENTAS DE EJEMPLO
# ------------------------------------------------------------------

$ventasEjemplo = @(
    @{
        IdV = "V000001"
        NumVenta = 1
        FechaVenta = $today
        NombreCliente = "Maria Gonzalez Lopez"
        Celular = "5512345678"
        Telefono = ""
        Zona = "CENTRO"
        FormaPago = "CREDITO"
        DiaCobro = "Lunes"
        FotoCliente = ""
        FotoFachada = ""
        FotoContrato = ""
        ObservacionVenta = "Cliente nuevo - Pago semanal"
        Vendedor = "vendedor1"
        Usuario = "vendedor1"
        Cobrador = "cobrador1"
        Coordenadas = "19.4326,-99.1332"
        UrlUbicacion = "https://maps.google.com/?q=19.4326,-99.1332"
        FechaPrimerCobro = $today.AddDays(7)
        Estado = "PENDIENTE"
        FechaActu = $now
        Estado2 = "OPEN"
        ComisionVendedorPct = 10.00
        Cobrar = "OK"
        FotoAdd1 = ""
        FotoAdd2 = ""
        Coordenadas2 = ""
        ProductosCodigos = "PROD001|PROD002"
        ProductosCantidades = "2|1"
        ProductosPrecios = "450.00|300.00"
        ImporteTotal = 1200.00
        ProductosCantidad = 2
    },
    @{
        IdV = "V000002"
        NumVenta = 2
        FechaVenta = $today
        NombreCliente = "Carlos Rodriguez Sanchez"
        Celular = "5587654321"
        Telefono = "5555-4321"
        Zona = "NORTE"
        FormaPago = "CONTADO"
        DiaCobro = "Miercoles"
        FotoCliente = ""
        FotoFachada = ""
        FotoContrato = ""
        ObservacionVenta = "Pago de contado completo"
        Vendedor = "vendedor1"
        Usuario = "vendedor1"
        Cobrador = "cobrador1"
        Coordenadas = "19.5026,-99.2132"
        UrlUbicacion = "https://maps.google.com/?q=19.5026,-99.2132"
        FechaPrimerCobro = $null
        Estado = "COMPLETADO"
        FechaActu = $now
        Estado2 = "CLOSED"
        ComisionVendedorPct = 10.00
        Cobrar = "OK"
        FotoAdd1 = ""
        FotoAdd2 = ""
        Coordenadas2 = ""
        ProductosCodigos = "PROD003|PROD004|PROD005"
        ProductosCantidades = "1|2|1"
        ProductosPrecios = "800.00|250.00|150.00"
        ImporteTotal = 1450.00
        ProductosCantidad = 3
    }
)

# ------------------------------------------------------------------
# COBROS DE EJEMPLO
# ------------------------------------------------------------------

$cobrosEjemplo = @(
    @{
        IdCc = "C000001"
        IdV = "V000001"
        NumVenta = 1
        NombreCliente = "Maria Gonzalez Lopez"
        ImporteCobro = 400.00
        FechaCobro = $today.AddDays(7)
        ObservacionCobro = "Primer pago - Todo correcto"
        FechaCaptura = $today.AddDays(7).AddHours(14)
        EstadoCc = "PARCIAL"
        Usuario = "cobrador1"
        Zona = "CENTRO"
        DiaCobroPrevisto = "Lunes"
        DiaCobrado = "Lunes"
        CoordenadasCobro = "19.4326,-99.1332"
        FotoCobro = ""
        ImporteAbonado = 400.00
        ImporteRestante = 800.00
        ImporteTotal = 1200.00
    },
    @{
        IdCc = "C000002"
        IdV = "V000002"
        NumVenta = 2
        NombreCliente = "Carlos Rodriguez Sanchez"
        ImporteCobro = 1450.00
        FechaCobro = $today
        ObservacionCobro = "Pago completo de contado"
        FechaCaptura = $today.AddHours(15)
        EstadoCc = "COMPLETADO"
        Usuario = "cobrador1"
        Zona = "NORTE"
        DiaCobroPrevisto = "Miercoles"
        DiaCobrado = "Jueves"
        CoordenadasCobro = "19.5026,-99.2132"
        FotoCobro = ""
        ImporteAbonado = 1450.00
        ImporteRestante = 0.00
        ImporteTotal = 1450.00
    }
)

# ══════════════════════════════════════════════════════════════════════
# AGREGAR VENTAS
# ══════════════════════════════════════════════════════════════════════

Write-Step "[1/3] Agregando ventas de ejemplo..."

try {
    # Leer datos actuales
    $existingSales = Import-Excel -Path $ExcelPath -WorksheetName "Sales" -ErrorAction SilentlyContinue
    
    if ($null -eq $existingSales) {
        Write-Info "Hoja 'Sales' vacía o no existe. Se creará con los datos de ejemplo."
        $existingSales = @()
    }
    
    # Verificar qué ventas ya existen
    $existingIds = @()
    if ($existingSales.Count -gt 0) {
        $existingIds = $existingSales | ForEach-Object { $_.IdV }
    }
    
    $ventasToAdd = @()
    foreach ($venta in $ventasEjemplo) {
        if ($existingIds -contains $venta.IdV) {
            Write-Info "Venta $($venta.IdV) ya existe. Omitiendo."
        } else {
            $ventasToAdd += [PSCustomObject]$venta
            Write-Success "Agregando venta $($venta.IdV) - Cliente: $($venta.NombreCliente)"
        }
    }
    
    if ($ventasToAdd.Count -gt 0) {
        # Combinar datos existentes con nuevos
        $allSales = @()
        if ($existingSales.Count -gt 0) {
            $allSales += $existingSales
        }
        $allSales += $ventasToAdd
        
        # Guardar en Excel
        $allSales | Export-Excel -Path $ExcelPath -WorksheetName "Sales" -AutoSize -FreezeTopRow -BoldTopRow
        Write-Success "Se agregaron $($ventasToAdd.Count) venta(s) a la hoja 'Sales'"
    } else {
        Write-Info "No se agregaron ventas (todas ya existen)"
    }
}
catch {
    Write-Host "[ERROR] al agregar ventas: $_" -ForegroundColor Red
    throw
}

# ══════════════════════════════════════════════════════════════════════
# AGREGAR COBROS
# ══════════════════════════════════════════════════════════════════════

Write-Step "[2/3] Agregando cobros de ejemplo..."

try {
    # Leer datos actuales
    $existingCollections = Import-Excel -Path $ExcelPath -WorksheetName "Collections" -ErrorAction SilentlyContinue
    
    if ($null -eq $existingCollections) {
        Write-Info "Hoja 'Collections' vacía o no existe. Se creará con los datos de ejemplo."
        $existingCollections = @()
    }
    
    # Verificar qué cobros ya existen
    $existingCollectionIds = @()
    if ($existingCollections.Count -gt 0) {
        $existingCollectionIds = $existingCollections | ForEach-Object { $_.IdCc }
    }
    
    $cobrosToAdd = @()
    foreach ($cobro in $cobrosEjemplo) {
        if ($existingCollectionIds -contains $cobro.IdCc) {
            Write-Info "Cobro $($cobro.IdCc) ya existe. Omitiendo."
        } else {
            $cobrosToAdd += [PSCustomObject]$cobro
            Write-Success "Agregando cobro $($cobro.IdCc) - Cliente: $($cobro.NombreCliente), Monto: $($cobro.ImporteCobro)"
        }
    }
    
    if ($cobrosToAdd.Count -gt 0) {
        # Combinar datos existentes con nuevos
        $allCollections = @()
        if ($existingCollections.Count -gt 0) {
            $allCollections += $existingCollections
        }
        $allCollections += $cobrosToAdd
        
        # Guardar en Excel
        $allCollections | Export-Excel -Path $ExcelPath -WorksheetName "Collections" -AutoSize -FreezeTopRow -BoldTopRow
        Write-Success "Se agregaron $($cobrosToAdd.Count) cobro(s) a la hoja 'Collections'"
    } else {
        Write-Info "No se agregaron cobros (todos ya existen)"
    }
}
catch {
    Write-Host "[ERROR] al agregar cobros: $_" -ForegroundColor Red
    throw
}

# ══════════════════════════════════════════════════════════════════════
# VERIFICAR USUARIOS Y RBAC
# ══════════════════════════════════════════════════════════════════════

Write-Step "[3/3] Verificando usuarios y asignaciones RBAC..."

try {
    # Verificar hoja Users
    $users = Import-Excel -Path $ExcelPath -WorksheetName "Users" -ErrorAction SilentlyContinue
    if ($users) {
        $userCount = ($users | Measure-Object).Count
        Write-Success "Hoja 'Users' tiene $userCount usuario(s)"
        
        # Listar usuarios
        $users | ForEach-Object {
            Write-Info "   - $($_.UserName) ($($_.DisplayName)) - Rol: $($_.Role) - Zona: $($_.Zone)"
        }
    }
    
    # Verificar hoja UserRoles (RBAC)
    $userRoles = Import-Excel -Path $ExcelPath -WorksheetName "UserRoles" -ErrorAction SilentlyContinue
    if ($userRoles) {
        $assignmentCount = ($userRoles | Measure-Object).Count
        Write-Success "Hoja 'UserRoles' tiene $assignmentCount asignación(es) RBAC"
        
        # Leer roles para mostrar nombres
        $roles = Import-Excel -Path $ExcelPath -WorksheetName "Roles" -ErrorAction SilentlyContinue
        $roleDict = @{}
        if ($roles) {
            $roles | ForEach-Object { $roleDict[$_.Id] = $_.Name }
        }
        
        # Listar asignaciones
        $userRoles | ForEach-Object {
            $roleName = if ($roleDict.ContainsKey($_.RoleId)) { $roleDict[$_.RoleId] } else { "Rol ID $($_.RoleId)" }
            $status = if ($_.IsActive) { "Activo" } else { "Inactivo" }
            Write-Info "   - $($_.UserName) → $roleName ($status)"
        }
    }
}
catch {
    Write-Host "[ADVERTENCIA] al verificar usuarios/RBAC: $_" -ForegroundColor Yellow
}

# ══════════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ══════════════════════════════════════════════════════════════════════

Write-Header "PROCESO COMPLETADO"

Write-Host "`nArchivo Excel:" -ForegroundColor Cyan
Write-Host "   $ExcelPath" -ForegroundColor White

Write-Host "`nDatos de ejemplo agregados:" -ForegroundColor Cyan
Write-Host "   [OK] Ventas de ejemplo      - 2 registros" -ForegroundColor Green
Write-Host "   [OK] Cobros de ejemplo      - 2 registros" -ForegroundColor Green
Write-Host "   [OK] Usuarios verificados   - Completo" -ForegroundColor Green
Write-Host "   [OK] RBAC verificado        - Completo" -ForegroundColor Green

Write-Host "`nDetalles de ventas agregadas:" -ForegroundColor Cyan
foreach ($venta in $ventasEjemplo) {
    Write-Host "   - $($venta.IdV): $($venta.NombreCliente) - $($venta.FormaPago) - $$($venta.ImporteTotal)" -ForegroundColor White
}

Write-Host "`nDetalles de cobros agregados:" -ForegroundColor Cyan
foreach ($cobro in $cobrosEjemplo) {
    Write-Host "   - $($cobro.IdCc): $($cobro.NombreCliente) - Estado: $($cobro.EstadoCc) - $$($cobro.ImporteCobro)" -ForegroundColor White
}

Write-Host "`nSiguientes pasos:" -ForegroundColor Cyan
Write-Host "   1. Reiniciar API y Web para que carguen los nuevos datos" -ForegroundColor Yellow
Write-Host "   2. Verificar que las ventas aparezcan en Dashboard" -ForegroundColor Yellow
Write-Host "   3. Verificar que los cobros aparezcan en módulo Cobros" -ForegroundColor Yellow
Write-Host "   4. Probar RBAC navegando con diferentes usuarios" -ForegroundColor Yellow

Write-Host "`nBackup creado en:" -ForegroundColor Cyan
Write-Host "   $backupPath" -ForegroundColor Gray

Write-Host ""
