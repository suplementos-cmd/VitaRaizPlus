# ═══════════════════════════════════════════════════════════════════════════════
# Script Manual para Crear Excel de SalesCobrosGeo
# ═══════════════════════════════════════════════════════════════════════════════
# Descripcion: Crea el archivo Excel directamente sin ejecutar la aplicacion
# Uso: powershell -ExecutionPolicy Bypass -File ./scripts/crear-excel-manual.ps1
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Creador Manual de Excel - SalesCobrosGeo" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$excelPath = "$PSScriptRoot\..\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx"
$appDataDir = "$PSScriptRoot\..\src\SalesCobrosGeo.Api\App_Data"

# Verificar si Excel ya existe
if (Test-Path $excelPath) {
    Write-Host "[ADVERTENCIA] El archivo Excel ya existe:" -ForegroundColor Yellow
    Write-Host "  $excelPath" -ForegroundColor White
    Write-Host ""
    $respuesta = Read-Host "¿Desea sobrescribirlo? (S/N)"
    
    if ($respuesta -notmatch "^[Ss]$") {
        Write-Host "[INFO] Operacion cancelada por el usuario" -ForegroundColor Cyan
        exit 0
    }
    
    Write-Host "[INFO] Creando backup del archivo existente..." -ForegroundColor Cyan
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupPath = "$excelPath.backup.$timestamp"
    Copy-Item -Path $excelPath -Destination $backupPath -Force
    Write-Host "[OK] Backup creado: $(Split-Path $backupPath -Leaf)" -ForegroundColor Green
}

# Crear directorio si no existe
if (-not (Test-Path $appDataDir)) {
    Write-Host "[INFO] Creando directorio App_Data..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $appDataDir -Force | Out-Null
    Write-Host "[OK] Directorio creado" -ForegroundColor Green
}

Write-Host ""
Write-Host "[INFO] Verificando paquete EPPlus..." -ForegroundColor Cyan

# Intentar cargar el ensamblado de EPPlus
$epplusPath = "$PSScriptRoot\..\src\SalesCobrosGeo.Api\bin\Debug\net9.0\EPPlus.dll"

if (-not (Test-Path $epplusPath)) {
    Write-Host "[ADVERTENCIA] EPPlus.dll no encontrado. Compilando proyecto..." -ForegroundColor Yellow
    
    try {
        Push-Location "$PSScriptRoot\.."
        dotnet restore SalesCobrosGeo.sln
        dotnet build src/SalesCobrosGeo.Api/SalesCobrosGeo.Api.csproj --configuration Debug
        Pop-Location
        
        if (-not (Test-Path $epplusPath)) {
            Write-Host "[ERROR] No se pudo compilar el proyecto" -ForegroundColor Red
            Write-Host ""
            Write-Host "Solucion alternativa:" -ForegroundColor Yellow
            Write-Host "1. Ejecuta: dotnet build" -ForegroundColor White
            Write-Host "2. Luego: dotnet run --project src/SalesCobrosGeo.Api" -ForegroundColor White
            Write-Host "   (El Excel se creara automaticamente al iniciar)" -ForegroundColor Gray
            exit 1
        }
    }
    catch {
        Write-Host "[ERROR] Error al compilar: $_" -ForegroundColor Red
        exit 1
    }
}

Write-Host "[OK] EPPlus encontrado" -ForegroundColor Green

try {
    # Cargar ensamblado EPPlus
    Add-Type -Path $epplusPath
    [OfficeOpenXml.ExcelPackage]::LicenseContext = [OfficeOpenXml.LicenseContext]::NonCommercial
    
    Write-Host ""
    Write-Host "[INFO] Creando archivo Excel..." -ForegroundColor Cyan
    
    # Crear Excel
    $package = New-Object OfficeOpenXml.ExcelPackage
    
    # Crear hojas
    $sheets = @(
        @{Name="Users"; Headers=@("UserName","Password","DisplayName","Role","IsActive")},
        @{Name="Zones"; Headers=@("Id","Code","Name","IsActive")},
        @{Name="Products"; Headers=@("Id","Code","Name","Price","IsActive")},
        @{Name="PaymentMethods"; Headers=@("Id","Code","Name","IsActive")},
        @{Name="Clients"; Headers=@("Id","FullName","Mobile","Phone","ZoneCode","CollectionDay","Address","CreatedBy","CreatedAtUtc","UpdatedAtUtc","IsActive")},
        @{Name="Sales"; Headers=@("Id","SaleNumber","ClientId","SellerUserName","CollectorUserName","PaymentMethodCode","CollectionDay","Notes","Status","CollectionStatus","Collectable","SellerCommissionPercent","CreatedAtUtc","UpdatedAtUtc","FirstCollectionAtUtc","TotalAmount","CollectedAmount","PrimaryCoordinates","SecondaryCoordinates","LocationUrl","PhotoUrls")},
        @{Name="SaleItems"; Headers=@("SaleId","ProductId","ProductCode","ProductName","Quantity","UnitPrice","Subtotal")},
        @{Name="SaleHistory"; Headers=@("SaleId","TimestampUtc","UserName","FromStatus","ToStatus","Reason","Action")},
        @{Name="Collections"; Headers=@("Id","SaleId","Amount","Coordinates","Notes","CollectedBy","CollectedAtUtc","CapturedAtUtc")},
        @{Name="AuditTrail"; Headers=@("Id","TimestampUtc","EventType","UserName","Description","Path","IpAddress","Coordinates","Metadata")}
    )
    
    foreach ($sheetInfo in $sheets) {
        Write-Host "  - Creando hoja: $($sheetInfo.Name)" -ForegroundColor Gray
        $ws = $package.Workbook.Worksheets.Add($sheetInfo.Name)
        
        # Agregar encabezados
        for ($i = 0; $i -lt $sheetInfo.Headers.Count; $i++) {
            $ws.Cells[1, $i + 1].Value = $sheetInfo.Headers[$i]
            $ws.Cells[1, $i + 1].Style.Font.Bold = $true
        }
    }
    
    Write-Host "[OK] 10 hojas creadas" -ForegroundColor Green
    
    # Agregar datos iniciales
    Write-Host ""
    Write-Host "[INFO] Agregando datos iniciales..." -ForegroundColor Cyan
    
    # Usuarios
    $usersSheet = $package.Workbook.Worksheets["Users"]
    $users = @(
        @("admin", "admin123", "Administrador", "Administrador", $true),
        @("vendedor1", "venta123", "Vendedor 1", "Vendedor", $true),
        @("cobrador1", "cobra123", "Cobrador 1", "Cobrador", $true),
        @("supventas", "super123", "Supervisor Ventas", "SupervisorVentas", $true),
        @("supcobros", "super123", "Supervisor Cobranza", "SupervisorCobranza", $true)
    )
    
    for ($i = 0; $i -lt $users.Count; $i++) {
        for ($j = 0; $j -lt $users[$i].Count; $j++) {
            $usersSheet.Cells[$i + 2, $j + 1].Value = $users[$i][$j]
        }
    }
    Write-Host "  - 5 usuarios agregados" -ForegroundColor Gray
    
    # Zonas
    $zonesSheet = $package.Workbook.Worksheets["Zones"]
    $zones = @(
        @(1, "CENTRO", "Zona Centro", $true),
        @(2, "NORTE", "Zona Norte", $true),
        @(3, "SUR", "Zona Sur", $true),
        @(4, "ESTE", "Zona Este", $true),
        @(5, "OESTE", "Zona Oeste", $true)
    )
    
    for ($i = 0; $i -lt $zones.Count; $i++) {
        for ($j = 0; $j -lt $zones[$i].Count; $j++) {
            $zonesSheet.Cells[$i + 2, $j + 1].Value = $zones[$i][$j]
        }
    }
    Write-Host "  - 5 zonas agregadas" -ForegroundColor Gray
    
    # Productos
    $productsSheet = $package.Workbook.Worksheets["Products"]
    $products = @(
        @(1, "VRP-001", "VitaRaiz Plus 30 caps", 120.00, $true),
        @(2, "VRP-002", "VitaRaiz Plus 60 caps", 210.00, $true),
        @(3, "VRP-003", "VitaRaiz Plus 90 caps", 290.00, $true)
    )
    
    for ($i = 0; $i -lt $products.Count; $i++) {
        for ($j = 0; $j -lt $products[$i].Count; $j++) {
            $productsSheet.Cells[$i + 2, $j + 1].Value = $products[$i][$j]
        }
    }
    Write-Host "  - 3 productos agregados" -ForegroundColor Gray
    
    # Formas de pago
    $paymentSheet = $package.Workbook.Worksheets["PaymentMethods"]
    $payments = @(
        @(1, "CONTADO", "Contado", $true),
        @(2, "CREDITO", "Credito", $true),
        @(3, "TRANSFER", "Transferencia", $true),
        @(4, "TARJETA", "Tarjeta", $true)
    )
    
    for ($i = 0; $i -lt $payments.Count; $i++) {
        for ($j = 0; $j -lt $payments[$i].Count; $j++) {
            $paymentSheet.Cells[$i + 2, $j + 1].Value = $payments[$i][$j]
        }
    }
    Write-Host "  - 4 formas de pago agregadas" -ForegroundColor Gray
    
    # Guardar archivo
    Write-Host ""
    Write-Host "[INFO] Guardando archivo..." -ForegroundColor Cyan
    $fileInfo = New-Object System.IO.FileInfo($excelPath)
    $package.SaveAs($fileInfo)
    $package.Dispose()
    
    Write-Host "[OK] Archivo guardado exitosamente" -ForegroundColor Green
    
    # Resumen
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  EXCEL CREADO EXITOSAMENTE" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Ubicacion: " -NoNewline
    Write-Host $excelPath -ForegroundColor White
    Write-Host ""
    Write-Host "Contenido:" -ForegroundColor Yellow
    Write-Host "  ✓ 10 hojas (tablas)" -ForegroundColor Gray
    Write-Host "  ✓ 5 usuarios iniciales" -ForegroundColor Gray
    Write-Host "  ✓ 5 zonas" -ForegroundColor Gray
    Write-Host "  ✓ 3 productos" -ForegroundColor Gray
    Write-Host "  ✓ 4 formas de pago" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Proximos pasos:" -ForegroundColor Yellow
    Write-Host "  1. Ejecuta: " -NoNewline
    Write-Host "dotnet run --project src/SalesCobrosGeo.Api" -ForegroundColor White
    Write-Host "  2. La API cargara los datos del Excel automaticamente" -ForegroundColor Gray
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "[ERROR] No se pudo crear el Excel: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Stack Trace:" -ForegroundColor Gray
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Solucion alternativa:" -ForegroundColor Yellow
    Write-Host "Ejecuta la aplicacion directamente y el Excel se creara automaticamente:" -ForegroundColor White
    Write-Host '  dotnet run --project src/SalesCobrosGeo.Api' -ForegroundColor Cyan
    exit 1
}
