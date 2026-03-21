<#
.SYNOPSIS
    Agrega hojas RBAC (Roles, Permisos, Módulos, Acciones) al Excel existente SalesCobrosGeo.xlsx

.DESCRIPTION
    Script que agrega 7 nuevas hojas al archivo Excel existente para implementar
    un sistema RBAC robusto sin cambiar la estructura actual.
    
    Hojas a crear:
    1. Modules - Módulos del sistema
    2. Actions - Acciones disponibles (CRUD + Scope)
    3. Permissions - Permisos (Módulo + Acción)
    4. Roles - Roles dinámicos
    5. RolePermissions - Mapeo Roles → Permisos
    6. UserRoles - Usuarios → Roles (con fechas)
    7. UserPermissions - Permisos custom por usuario

.PARAMETER ExcelPath
    Ruta al archivo Excel. Por defecto: ..\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx

.PARAMETER BackupFirst
    Crear backup antes de modificar (por defecto: $true)

.EXAMPLE
    .\add-rbac-sheets.ps1
    
.EXAMPLE
    .\add-rbac-sheets.ps1 -BackupFirst $true
#>

param(
    [string]$ExcelPath = "..\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx",
    [bool]$BackupFirst = $true
)

# Importar módulo ImportExcel
if (-not (Get-Module -ListAvailable -Name ImportExcel)) {
    Write-Host "[ERROR] Módulo ImportExcel no está instalado" -ForegroundColor Red
    Write-Host "Ejecuta: Install-Module -Name ImportExcel -Scope CurrentUser" -ForegroundColor Yellow
    exit 1
}

Import-Module ImportExcel

# Verificar que el archivo existe
$resolvedPath = Resolve-Path $ExcelPath -ErrorAction SilentlyContinue
if (-not $resolvedPath) {
    Write-Host "[ERROR] No se encontró el archivo Excel: $ExcelPath" -ForegroundColor Red
    exit 1
}

$excelFile = $resolvedPath.Path
Write-Host "Trabajando con: $excelFile" -ForegroundColor Cyan

# Crear backup si se solicita
if ($BackupFirst) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupPath = "$excelFile.backup.$timestamp"
    Copy-Item -Path $excelFile -Destination $backupPath -Force
    Write-Host "[OK] Backup creado: $backupPath" -ForegroundColor Green
}

# ====================================================================================
# HOJA 1: Modules (Módulos del Sistema)
# ====================================================================================
Write-Host "`n[1/7] Creando hoja: Modules..." -ForegroundColor Yellow

$modules = @(
    [PSCustomObject]@{ Id=1; Code="DASHBOARD";    Name="Dashboard";        Description="Tableros y reportes generales";      IsActive=$true }
    [PSCustomObject]@{ Id=2; Code="SALES";        Name="Ventas";           Description="Gestión de ventas";                  IsActive=$true }
    [PSCustomObject]@{ Id=3; Code="COLLECTIONS";  Name="Cobros";           Description="Gestión de cobranza";                IsActive=$true }
    [PSCustomObject]@{ Id=4; Code="MAINTENANCE";  Name="Mantenimiento";    Description="Catálogos y configuración";          IsActive=$true }
    [PSCustomObject]@{ Id=5; Code="ADMIN";        Name="Administración";   Description="Usuarios y seguridad";               IsActive=$true }
    [PSCustomObject]@{ Id=6; Code="REPORTS";      Name="Reportes";         Description="Reportes avanzados";                 IsActive=$true }
    [PSCustomObject]@{ Id=7; Code="CLIENTS";      Name="Clientes";         Description="Gestión de clientes";                IsActive=$true }
    [PSCustomObject]@{ Id=8; Code="PRODUCTS";     Name="Productos";        Description="Catálogo de productos";              IsActive=$true }
    [PSCustomObject]@{ Id=9; Code="ZONES";        Name="Zonas";            Description="Gestión de zonas geográficas";       IsActive=$true }
)

$modules | Export-Excel -Path $excelFile -WorksheetName "Modules" -AutoSize -TableName "ModulesTable" -TableStyle Medium2
Write-Host "   [OK] Hoja 'Modules' creada con $($modules.Count) registros" -ForegroundColor Green

# ====================================================================================
# HOJA 2: Actions (Acciones del Sistema)
# ====================================================================================
Write-Host "`n[2/7] Creando hoja: Actions..." -ForegroundColor Yellow

$actions = @(
    # Acciones CRUD
    [PSCustomObject]@{ Id=1;  Code="VIEW";     Name="Ver";             Description="Ver/Listar registros";                      Category="READ";     IsActive=$true }
    [PSCustomObject]@{ Id=2;  Code="CREATE";   Name="Crear";           Description="Crear nuevos registros";                    Category="WRITE";    IsActive=$true }
    [PSCustomObject]@{ Id=3;  Code="UPDATE";   Name="Editar";          Description="Modificar registros existentes";            Category="WRITE";    IsActive=$true }
    [PSCustomObject]@{ Id=4;  Code="DELETE";   Name="Eliminar";        Description="Eliminar registros";                        Category="WRITE";    IsActive=$true }
    
    # Acciones de Datos
    [PSCustomObject]@{ Id=5;  Code="EXPORT";   Name="Exportar";        Description="Exportar datos a Excel/PDF";                Category="READ";     IsActive=$true }
    [PSCustomObject]@{ Id=6;  Code="IMPORT";   Name="Importar";        Description="Importar datos desde archivo";              Category="WRITE";    IsActive=$true }
    [PSCustomObject]@{ Id=7;  Code="PRINT";    Name="Imprimir";        Description="Imprimir reportes";                         Category="READ";     IsActive=$true }
    
    # Acciones de Workflow
    [PSCustomObject]@{ Id=8;  Code="APPROVE";  Name="Aprobar";         Description="Aprobar registros pendientes";              Category="WORKFLOW"; IsActive=$true }
    [PSCustomObject]@{ Id=9;  Code="REJECT";   Name="Rechazar";        Description="Rechazar registros";                        Category="WORKFLOW"; IsActive=$true }
    [PSCustomObject]@{ Id=10; Code="CANCEL";   Name="Cancelar";        Description="Cancelar operaciones";                      Category="WORKFLOW"; IsActive=$true }
    
    # Scopes de Datos (Row-Level Security)
    [PSCustomObject]@{ Id=11; Code="OWN";      Name="Solo Propias";    Description="Solo registros creados por el usuario";     Category="SCOPE";    IsActive=$true }
    [PSCustomObject]@{ Id=12; Code="ZONE";     Name="De Mi Zona";      Description="Solo registros de la zona asignada";        Category="SCOPE";    IsActive=$true }
    [PSCustomObject]@{ Id=13; Code="ALL";      Name="Todas";           Description="Todos los registros del sistema";           Category="SCOPE";    IsActive=$true }
    
    # Acciones Administrativas
    [PSCustomObject]@{ Id=14; Code="MANAGE";   Name="Gestionar";       Description="Gestión completa del recurso";              Category="ADMIN";    IsActive=$true }
    [PSCustomObject]@{ Id=15; Code="CONFIG";   Name="Configurar";      Description="Configurar parámetros del sistema";         Category="ADMIN";    IsActive=$true }
)

$actions | Export-Excel -Path $excelFile -WorksheetName "Actions" -AutoSize -TableName "ActionsTable" -TableStyle Medium2
Write-Host "   [OK] Hoja 'Actions' creada con $($actions.Count) registros" -ForegroundColor Green

# ====================================================================================
# HOJA 3: Permissions (Permisos = Módulo + Acción)
# ====================================================================================
Write-Host "`n[3/7] Creando hoja: Permissions..." -ForegroundColor Yellow

$permissions = @(
    # Dashboard (Id=1)
    [PSCustomObject]@{ Id=1;   Code="dashboard:view";         ModuleId=1;  ActionId=1;   Description="Ver dashboard";                           IsActive=$true }
    [PSCustomObject]@{ Id=2;   Code="dashboard:export";       ModuleId=1;  ActionId=5;   Description="Exportar dashboard";                      IsActive=$true }
    
    # Sales (Id=2)
    [PSCustomObject]@{ Id=3;   Code="sales:view";             ModuleId=2;  ActionId=1;   Description="Ver listado de ventas";                   IsActive=$true }
    [PSCustomObject]@{ Id=4;   Code="sales:create";           ModuleId=2;  ActionId=2;   Description="Crear nuevas ventas";                     IsActive=$true }
    [PSCustomObject]@{ Id=5;   Code="sales:update";           ModuleId=2;  ActionId=3;   Description="Editar ventas existentes";                IsActive=$true }
    [PSCustomObject]@{ Id=6;   Code="sales:delete";           ModuleId=2;  ActionId=4;   Description="Eliminar ventas";                         IsActive=$true }
    [PSCustomObject]@{ Id=7;   Code="sales:export";           ModuleId=2;  ActionId=5;   Description="Exportar ventas a Excel";                 IsActive=$true }
    [PSCustomObject]@{ Id=8;   Code="sales:approve";          ModuleId=2;  ActionId=8;   Description="Aprobar ventas";                          IsActive=$true }
    [PSCustomObject]@{ Id=9;   Code="sales:cancel";           ModuleId=2;  ActionId=10;  Description="Cancelar ventas";                         IsActive=$true }
    [PSCustomObject]@{ Id=10;  Code="sales:own";              ModuleId=2;  ActionId=11;  Description="Solo ver/editar ventas propias";          IsActive=$true }
    [PSCustomObject]@{ Id=11;  Code="sales:zone";             ModuleId=2;  ActionId=12;  Description="Ventas de la zona del usuario";           IsActive=$true }
    [PSCustomObject]@{ Id=12;  Code="sales:all";              ModuleId=2;  ActionId=13;  Description="Todas las ventas del sistema";            IsActive=$true }
    
    # Collections (Id=3)
    [PSCustomObject]@{ Id=13;  Code="collections:view";       ModuleId=3;  ActionId=1;   Description="Ver listado de cobros";                   IsActive=$true }
    [PSCustomObject]@{ Id=14;  Code="collections:create";     ModuleId=3;  ActionId=2;   Description="Registrar cobros";                        IsActive=$true }
    [PSCustomObject]@{ Id=15;  Code="collections:update";     ModuleId=3;  ActionId=3;   Description="Editar cobros";                           IsActive=$true }
    [PSCustomObject]@{ Id=16;  Code="collections:delete";     ModuleId=3;  ActionId=4;   Description="Eliminar cobros";                         IsActive=$true }
    [PSCustomObject]@{ Id=17;  Code="collections:export";     ModuleId=3;  ActionId=5;   Description="Exportar cobros";                         IsActive=$true }
    [PSCustomObject]@{ Id=18;  Code="collections:own";        ModuleId=3;  ActionId=11;  Description="Solo cobros propios";                     IsActive=$true }
    [PSCustomObject]@{ Id=19;  Code="collections:zone";       ModuleId=3;  ActionId=12;  Description="Cobros de la zona";                       IsActive=$true }
    [PSCustomObject]@{ Id=20;  Code="collections:all";        ModuleId=3;  ActionId=13;  Description="Todos los cobros";                        IsActive=$true }
    
    # Maintenance (Id=4)
    [PSCustomObject]@{ Id=21;  Code="maintenance:view";       ModuleId=4;  ActionId=1;   Description="Ver catálogos";                           IsActive=$true }
    [PSCustomObject]@{ Id=22;  Code="maintenance:update";     ModuleId=4;  ActionId=3;   Description="Editar catálogos";                        IsActive=$true }
    
    # Admin (Id=5)
    [PSCustomObject]@{ Id=23;  Code="admin:view";             ModuleId=5;  ActionId=1;   Description="Ver usuarios y configuración";            IsActive=$true }
    [PSCustomObject]@{ Id=24;  Code="admin:create";           ModuleId=5;  ActionId=2;   Description="Crear usuarios";                          IsActive=$true }
    [PSCustomObject]@{ Id=25;  Code="admin:update";           ModuleId=5;  ActionId=3;   Description="Editar usuarios/roles";                   IsActive=$true }
    [PSCustomObject]@{ Id=26;  Code="admin:delete";           ModuleId=5;  ActionId=4;   Description="Eliminar usuarios";                       IsActive=$true }
    [PSCustomObject]@{ Id=27;  Code="admin:manage";           ModuleId=5;  ActionId=14;  Description="Gestión completa de seguridad";           IsActive=$true }
    
    # Reports (Id=6)
    [PSCustomObject]@{ Id=28;  Code="reports:view";           ModuleId=6;  ActionId=1;   Description="Ver reportes";                            IsActive=$true }
    [PSCustomObject]@{ Id=29;  Code="reports:export";         ModuleId=6;  ActionId=5;   Description="Exportar reportes";                       IsActive=$true }
    [PSCustomObject]@{ Id=30;  Code="reports:print";          ModuleId=6;  ActionId=7;   Description="Imprimir reportes";                       IsActive=$true }
    
    # Clients (Id=7)
    [PSCustomObject]@{ Id=31;  Code="clients:view";           ModuleId=7;  ActionId=1;   Description="Ver clientes";                            IsActive=$true }
    [PSCustomObject]@{ Id=32;  Code="clients:create";         ModuleId=7;  ActionId=2;   Description="Crear clientes";                          IsActive=$true }
    [PSCustomObject]@{ Id=33;  Code="clients:update";         ModuleId=7;  ActionId=3;   Description="Editar clientes";                         IsActive=$true }
    [PSCustomObject]@{ Id=34;  Code="clients:delete";         ModuleId=7;  ActionId=4;   Description="Eliminar clientes";                       IsActive=$true }
    [PSCustomObject]@{ Id=35;  Code="clients:zone";           ModuleId=7;  ActionId=12;  Description="Clientes de la zona";                     IsActive=$true }
    [PSCustomObject]@{ Id=36;  Code="clients:all";            ModuleId=7;  ActionId=13;  Description="Todos los clientes";                      IsActive=$true }
    
    # Products (Id=8)
    [PSCustomObject]@{ Id=37;  Code="products:view";          ModuleId=8;  ActionId=1;   Description="Ver productos";                           IsActive=$true }
    [PSCustomObject]@{ Id=38;  Code="products:create";        ModuleId=8;  ActionId=2;   Description="Crear productos";                         IsActive=$true }
    [PSCustomObject]@{ Id=39;  Code="products:update";        ModuleId=8;  ActionId=3;   Description="Editar productos";                        IsActive=$true }
    [PSCustomObject]@{ Id=40;  Code="products:delete";        ModuleId=8;  ActionId=4;   Description="Eliminar productos";                      IsActive=$true }
    
    # Zones (Id=9)
    [PSCustomObject]@{ Id=41;  Code="zones:view";             ModuleId=9;  ActionId=1;   Description="Ver zonas";                               IsActive=$true }
    [PSCustomObject]@{ Id=42;  Code="zones:create";           ModuleId=9;  ActionId=2;   Description="Crear zonas";                             IsActive=$true }
    [PSCustomObject]@{ Id=43;  Code="zones:update";           ModuleId=9;  ActionId=3;   Description="Editar zonas";                            IsActive=$true }
    [PSCustomObject]@{ Id=44;  Code="zones:delete";           ModuleId=9;  ActionId=4;   Description="Eliminar zonas";                          IsActive=$true }
)

$permissions | Export-Excel -Path $excelFile -WorksheetName "Permissions" -AutoSize -TableName "PermissionsTable" -TableStyle Medium2
Write-Host "   [OK] Hoja 'Permissions' creada con $($permissions.Count) registros" -ForegroundColor Green

# ====================================================================================
# HOJA 4: Roles (Roles Dinámicos)
# ====================================================================================
Write-Host "`n[4/7] Creando hoja: Roles..." -ForegroundColor Yellow

$roles = @(
    [PSCustomObject]@{ Id=1; Code="ADMIN";             Name="Administrador";         Description="Acceso total al sistema";                                IsActive=$true; IsSystem=$true }
    [PSCustomObject]@{ Id=2; Code="SALES_MANAGER";     Name="Gerente Ventas";        Description="Gestiona equipo de ventas y ve todas las ventas";        IsActive=$true; IsSystem=$true }
    [PSCustomObject]@{ Id=3; Code="SALES_REP";         Name="Vendedor";              Description="Crea y gestiona ventas propias";                         IsActive=$true; IsSystem=$true }
    [PSCustomObject]@{ Id=4; Code="COLL_SUPERVISOR";   Name="Supervisor Cobros";     Description="Supervisa cobradores y ve todos los cobros";             IsActive=$true; IsSystem=$true }
    [PSCustomObject]@{ Id=5; Code="COLLECTOR";         Name="Cobrador";              Description="Registra cobros propios";                                IsActive=$true; IsSystem=$true }
    [PSCustomObject]@{ Id=6; Code="SALES_COLLECTOR";   Name="Ventas + Cobros";       Description="Hace ventas y cobra (vendedor con capacidad de cobro)";  IsActive=$true; IsSystem=$false }
    [PSCustomObject]@{ Id=7; Code="AUDITOR";           Name="Auditor";               Description="Solo lectura de todo el sistema";                        IsActive=$true; IsSystem=$false }
    [PSCustomObject]@{ Id=8; Code="REPORTS_VIEWER";    Name="Visor Reportes";        Description="Solo puede ver reportes";                                IsActive=$true; IsSystem=$false }
)

$roles | Export-Excel -Path $excelFile -WorksheetName "Roles" -AutoSize -TableName "RolesTable" -TableStyle Medium2
Write-Host "   [OK] Hoja 'Roles' creada con $($roles.Count) registros" -ForegroundColor Green

# ====================================================================================
# HOJA 5: RolePermissions (Roles -> Permisos)
# ====================================================================================
Write-Host "`n Creando hoja: RolePermissions..." -ForegroundColor Yellow

$rolePermissions = @(
    # ========== ADMIN (RoleId=1): Todos los permisos ==========
    # Dashboard
    [PSCustomObject]@{ Id=1;   RoleId=1; PermissionId=1;  IsGranted=$true }  # dashboard:view
    [PSCustomObject]@{ Id=2;   RoleId=1; PermissionId=2;  IsGranted=$true }  # dashboard:export
    
    # Sales - FULL
    [PSCustomObject]@{ Id=3;   RoleId=1; PermissionId=3;  IsGranted=$true }  # sales:view
    [PSCustomObject]@{ Id=4;   RoleId=1; PermissionId=4;  IsGranted=$true }  # sales:create
    [PSCustomObject]@{ Id=5;   RoleId=1; PermissionId=5;  IsGranted=$true }  # sales:update
    [PSCustomObject]@{ Id=6;   RoleId=1; PermissionId=6;  IsGranted=$true }  # sales:delete
    [PSCustomObject]@{ Id=7;   RoleId=1; PermissionId=7;  IsGranted=$true }  # sales:export
    [PSCustomObject]@{ Id=8;   RoleId=1; PermissionId=8;  IsGranted=$true }  # sales:approve
    [PSCustomObject]@{ Id=9;   RoleId=1; PermissionId=9;  IsGranted=$true }  # sales:cancel
    [PSCustomObject]@{ Id=10;  RoleId=1; PermissionId=12; IsGranted=$true }  # sales:all
    
    # Collections - FULL
    [PSCustomObject]@{ Id=11;  RoleId=1; PermissionId=13; IsGranted=$true }  # collections:view
    [PSCustomObject]@{ Id=12;  RoleId=1; PermissionId=14; IsGranted=$true }  # collections:create
    [PSCustomObject]@{ Id=13;  RoleId=1; PermissionId=15; IsGranted=$true }  # collections:update
    [PSCustomObject]@{ Id=14;  RoleId=1; PermissionId=16; IsGranted=$true }  # collections:delete
    [PSCustomObject]@{ Id=15;  RoleId=1; PermissionId=17; IsGranted=$true }  # collections:export
    [PSCustomObject]@{ Id=16;  RoleId=1; PermissionId=20; IsGranted=$true }  # collections:all
    
    # Maintenance
    [PSCustomObject]@{ Id=17;  RoleId=1; PermissionId=21; IsGranted=$true }  # maintenance:view
    [PSCustomObject]@{ Id=18;  RoleId=1; PermissionId=22; IsGranted=$true }  # maintenance:update
    
    # Admin
    [PSCustomObject]@{ Id=19;  RoleId=1; PermissionId=23; IsGranted=$true }  # admin:view
    [PSCustomObject]@{ Id=20;  RoleId=1; PermissionId=24; IsGranted=$true }  # admin:create
    [PSCustomObject]@{ Id=21;  RoleId=1; PermissionId=25; IsGranted=$true }  # admin:update
    [PSCustomObject]@{ Id=22;  RoleId=1; PermissionId=26; IsGranted=$true }  # admin:delete
    [PSCustomObject]@{ Id=23;  RoleId=1; PermissionId=27; IsGranted=$true }  # admin:manage
    
    # Reports
    [PSCustomObject]@{ Id=24;  RoleId=1; PermissionId=28; IsGranted=$true }  # reports:view
    [PSCustomObject]@{ Id=25;  RoleId=1; PermissionId=29; IsGranted=$true }  # reports:export
    [PSCustomObject]@{ Id=26;  RoleId=1; PermissionId=30; IsGranted=$true }  # reports:print
    
    # Clients
    [PSCustomObject]@{ Id=27;  RoleId=1; PermissionId=31; IsGranted=$true }  # clients:view
    [PSCustomObject]@{ Id=28;  RoleId=1; PermissionId=32; IsGranted=$true }  # clients:create
    [PSCustomObject]@{ Id=29;  RoleId=1; PermissionId=33; IsGranted=$true }  # clients:update
    [PSCustomObject]@{ Id=30;  RoleId=1; PermissionId=34; IsGranted=$true }  # clients:delete
    [PSCustomObject]@{ Id=31;  RoleId=1; PermissionId=36; IsGranted=$true }  # clients:all
    
    # Products
    [PSCustomObject]@{ Id=32;  RoleId=1; PermissionId=37; IsGranted=$true }  # products:view
    [PSCustomObject]@{ Id=33;  RoleId=1; PermissionId=38; IsGranted=$true }  # products:create
    [PSCustomObject]@{ Id=34;  RoleId=1; PermissionId=39; IsGranted=$true }  # products:update
    [PSCustomObject]@{ Id=35;  RoleId=1; PermissionId=40; IsGranted=$true }  # products:delete
    
    # Zones
    [PSCustomObject]@{ Id=36;  RoleId=1; PermissionId=41; IsGranted=$true }  # zones:view
    [PSCustomObject]@{ Id=37;  RoleId=1; PermissionId=42; IsGranted=$true }  # zones:create
    [PSCustomObject]@{ Id=38;  RoleId=1; PermissionId=43; IsGranted=$true }  # zones:update
    [PSCustomObject]@{ Id=39;  RoleId=1; PermissionId=44; IsGranted=$true }  # zones:delete
    
    # ========== SALES_MANAGER (RoleId=2): Gestión completa de ventas ==========
    [PSCustomObject]@{ Id=40;  RoleId=2; PermissionId=1;  IsGranted=$true }  # dashboard:view
    [PSCustomObject]@{ Id=41;  RoleId=2; PermissionId=3;  IsGranted=$true }  # sales:view
    [PSCustomObject]@{ Id=42;  RoleId=2; PermissionId=4;  IsGranted=$true }  # sales:create
    [PSCustomObject]@{ Id=43;  RoleId=2; PermissionId=5;  IsGranted=$true }  # sales:update
    [PSCustomObject]@{ Id=44;  RoleId=2; PermissionId=6;  IsGranted=$true }  # sales:delete
    [PSCustomObject]@{ Id=45;  RoleId=2; PermissionId=7;  IsGranted=$true }  # sales:export
    [PSCustomObject]@{ Id=46;  RoleId=2; PermissionId=8;  IsGranted=$true }  # sales:approve
    [PSCustomObject]@{ Id=47;  RoleId=2; PermissionId=9;  IsGranted=$true }  # sales:cancel
    [PSCustomObject]@{ Id=48;  RoleId=2; PermissionId=12; IsGranted=$true }  # sales:all (ve todas)
    [PSCustomObject]@{ Id=49;  RoleId=2; PermissionId=31; IsGranted=$true }  # clients:view
    [PSCustomObject]@{ Id=50;  RoleId=2; PermissionId=36; IsGranted=$true }  # clients:all
    [PSCustomObject]@{ Id=51;  RoleId=2; PermissionId=37; IsGranted=$true }  # products:view
    [PSCustomObject]@{ Id=52;  RoleId=2; PermissionId=28; IsGranted=$true }  # reports:view
    [PSCustomObject]@{ Id=53;  RoleId=2; PermissionId=29; IsGranted=$true }  # reports:export
    
    # ========== SALES_REP (RoleId=3): Vendedor - solo sus ventas ==========
    [PSCustomObject]@{ Id=54;  RoleId=3; PermissionId=1;  IsGranted=$true }  # dashboard:view
    [PSCustomObject]@{ Id=55;  RoleId=3; PermissionId=3;  IsGranted=$true }  # sales:view
    [PSCustomObject]@{ Id=56;  RoleId=3; PermissionId=4;  IsGranted=$true }  # sales:create
    [PSCustomObject]@{ Id=57;  RoleId=3; PermissionId=5;  IsGranted=$true }  # sales:update
    [PSCustomObject]@{ Id=58;  RoleId=3; PermissionId=7;  IsGranted=$true }  # sales:export
    [PSCustomObject]@{ Id=59;  RoleId=3; PermissionId=10; IsGranted=$true }  # sales:own (SOLO PROPIAS)
    [PSCustomObject]@{ Id=60;  RoleId=3; PermissionId=31; IsGranted=$true }  # clients:view
    [PSCustomObject]@{ Id=61;  RoleId=3; PermissionId=32; IsGranted=$true }  # clients:create
    [PSCustomObject]@{ Id=62;  RoleId=3; PermissionId=35; IsGranted=$true }  # clients:zone
    [PSCustomObject]@{ Id=63;  RoleId=3; PermissionId=37; IsGranted=$true }  # products:view
    
    # ========== COLL_SUPERVISOR (RoleId=4): Supervisor de Cobros ==========
    [PSCustomObject]@{ Id=64;  RoleId=4; PermissionId=1;  IsGranted=$true }  # dashboard:view
    [PSCustomObject]@{ Id=65;  RoleId=4; PermissionId=13; IsGranted=$true }  # collections:view
    [PSCustomObject]@{ Id=66;  RoleId=4; PermissionId=14; IsGranted=$true }  # collections:create
    [PSCustomObject]@{ Id=67;  RoleId=4; PermissionId=15; IsGranted=$true }  # collections:update
    [PSCustomObject]@{ Id=68;  RoleId=4; PermissionId=16; IsGranted=$true }  # collections:delete
    [PSCustomObject]@{ Id=69;  RoleId=4; PermissionId=17; IsGranted=$true }  # collections:export
    [PSCustomObject]@{ Id=70;  RoleId=4; PermissionId=20; IsGranted=$true }  # collections:all (ve todos)
    [PSCustomObject]@{ Id=71;  RoleId=4; PermissionId=3;  IsGranted=$true }  # sales:view
    [PSCustomObject]@{ Id=72;  RoleId=4; PermissionId=12; IsGranted=$true }  # sales:all (para ver pendientes)
    [PSCustomObject]@{ Id=73;  RoleId=4; PermissionId=31; IsGranted=$true }  # clients:view
    [PSCustomObject]@{ Id=74;  RoleId=4; PermissionId=36; IsGranted=$true }  # clients:all
    [PSCustomObject]@{ Id=75;  RoleId=4; PermissionId=28; IsGranted=$true }  # reports:view
    
    # ========== COLLECTOR (RoleId=5): Cobrador - solo sus cobros ==========
    [PSCustomObject]@{ Id=76;  RoleId=5; PermissionId=1;  IsGranted=$true }  # dashboard:view
    [PSCustomObject]@{ Id=77;  RoleId=5; PermissionId=13; IsGranted=$true }  # collections:view
    [PSCustomObject]@{ Id=78;  RoleId=5; PermissionId=14; IsGranted=$true }  # collections:create
    [PSCustomObject]@{ Id=79;  RoleId=5; PermissionId=15; IsGranted=$true }  # collections:update
    [PSCustomObject]@{ Id=80;  RoleId=5; PermissionId=18; IsGranted=$true }  # collections:own (SOLO PROPIOS)
    [PSCustomObject]@{ Id=81;  RoleId=5; PermissionId=3;  IsGranted=$true }  # sales:view (ver pendientes)
    [PSCustomObject]@{ Id=82;  RoleId=5; PermissionId=11; IsGranted=$true }  # sales:zone (de su zona)
    [PSCustomObject]@{ Id=83;  RoleId=5; PermissionId=31; IsGranted=$true }  # clients:view
    [PSCustomObject]@{ Id=84;  RoleId=5; PermissionId=35; IsGranted=$true }  # clients:zone
    
    # ========== SALES_COLLECTOR (RoleId=6): Ventas + Cobros ==========
    [PSCustomObject]@{ Id=85;  RoleId=6; PermissionId=1;  IsGranted=$true }  # dashboard:view
    [PSCustomObject]@{ Id=86;  RoleId=6; PermissionId=3;  IsGranted=$true }  # sales:view
    [PSCustomObject]@{ Id=87;  RoleId=6; PermissionId=4;  IsGranted=$true }  # sales:create
    [PSCustomObject]@{ Id=88;  RoleId=6; PermissionId=5;  IsGranted=$true }  # sales:update
    [PSCustomObject]@{ Id=89;  RoleId=6; PermissionId=7;  IsGranted=$true }  # sales:export
    [PSCustomObject]@{ Id=90;  RoleId=6; PermissionId=10; IsGranted=$true }  # sales:own
    [PSCustomObject]@{ Id=91;  RoleId=6; PermissionId=13; IsGranted=$true }  # collections:view
    [PSCustomObject]@{ Id=92;  RoleId=6; PermissionId=14; IsGranted=$true }  # collections:create
    [PSCustomObject]@{ Id=93;  RoleId=6; PermissionId=15; IsGranted=$true }  # collections:update
    [PSCustomObject]@{ Id=94;  RoleId=6; PermissionId=18; IsGranted=$true }  # collections:own
    [PSCustomObject]@{ Id=95;  RoleId=6; PermissionId=31; IsGranted=$true }  # clients:view
    [PSCustomObject]@{ Id=96;  RoleId=6; PermissionId=32; IsGranted=$true }  # clients:create
    [PSCustomObject]@{ Id=97;  RoleId=6; PermissionId=35; IsGranted=$true }  # clients:zone
    [PSCustomObject]@{ Id=98;  RoleId=6; PermissionId=37; IsGranted=$true }  # products:view
    
    # ========== AUDITOR (RoleId=7): Solo lectura de todo ==========
    [PSCustomObject]@{ Id=99;  RoleId=7; PermissionId=1;  IsGranted=$true }  # dashboard:view
    [PSCustomObject]@{ Id=100; RoleId=7; PermissionId=3;  IsGranted=$true }  # sales:view
    [PSCustomObject]@{ Id=101; RoleId=7; PermissionId=7;  IsGranted=$true }  # sales:export
    [PSCustomObject]@{ Id=102; RoleId=7; PermissionId=12; IsGranted=$true }  # sales:all
    [PSCustomObject]@{ Id=103; RoleId=7; PermissionId=13; IsGranted=$true }  # collections:view
    [PSCustomObject]@{ Id=104; RoleId=7; PermissionId=17; IsGranted=$true }  # collections:export
    [PSCustomObject]@{ Id=105; RoleId=7; PermissionId=20; IsGranted=$true }  # collections:all
    [PSCustomObject]@{ Id=106; RoleId=7; PermissionId=31; IsGranted=$true }  # clients:view
    [PSCustomObject]@{ Id=107; RoleId=7; PermissionId=36; IsGranted=$true }  # clients:all
    [PSCustomObject]@{ Id=108; RoleId=7; PermissionId=37; IsGranted=$true }  # products:view
    [PSCustomObject]@{ Id=109; RoleId=7; PermissionId=28; IsGranted=$true }  # reports:view
    [PSCustomObject]@{ Id=110; RoleId=7; PermissionId=29; IsGranted=$true }  # reports:export
    [PSCustomObject]@{ Id=111; RoleId=7; PermissionId=30; IsGranted=$true }  # reports:print
    
    # ========== REPORTS_VIEWER (RoleId=8): Solo reportes ==========
    [PSCustomObject]@{ Id=112; RoleId=8; PermissionId=1;  IsGranted=$true }  # dashboard:view
    [PSCustomObject]@{ Id=113; RoleId=8; PermissionId=28; IsGranted=$true }  # reports:view
    [PSCustomObject]@{ Id=114; RoleId=8; PermissionId=29; IsGranted=$true }  # reports:export
    [PSCustomObject]@{ Id=115; RoleId=8; PermissionId=30; IsGranted=$true }  # reports:print
)

$rolePermissions | Export-Excel -Path $excelFile -WorksheetName "RolePermissions" -AutoSize -TableName "RolePermissionsTable" -TableStyle Medium2
Write-Host "   [OK] Hoja 'RolePermissions' creada con $($rolePermissions.Count) registros" -ForegroundColor Green

# ====================================================================================
# HOJA 6: UserRoles (Usuarios -> Roles con fechas)
# ====================================================================================
Write-Host "`n[6/7] Creando hoja: UserRoles..." -ForegroundColor Yellow

$today = (Get-Date).ToString("yyyy-MM-dd")

$userRoles = @(
    # Admin tiene rol ADMIN permanentemente
    [PSCustomObject]@{ Id=1; UserName="admin";      RoleId=1; StartDate=$today; EndDate=$null; IsActive=$true }
    
    # Vendedor1 tiene rol SALES_REP
    [PSCustomObject]@{ Id=2; UserName="vendedor1";  RoleId=3; StartDate=$today; EndDate=$null; IsActive=$true }
    
    # Cobrador1 tiene rol COLLECTOR
    [PSCustomObject]@{ Id=3; UserName="cobrador1";  RoleId=5; StartDate=$today; EndDate=$null; IsActive=$true }
    
    # SupVentas tiene rol SALES_MANAGER
    [PSCustomObject]@{ Id=4; UserName="supventas";  RoleId=2; StartDate=$today; EndDate=$null; IsActive=$true }
    
    # SupCobros tiene rol COLL_SUPERVISOR
    [PSCustomObject]@{ Id=5; UserName="supcobros";  RoleId=4; StartDate=$today; EndDate=$null; IsActive=$true }
)

$userRoles | Export-Excel -Path $excelFile -WorksheetName "UserRoles" -AutoSize -TableName "UserRolesTable" -TableStyle Medium2
Write-Host "   [OK] Hoja 'UserRoles' creada con $($userRoles.Count) registros" -ForegroundColor Green

# ====================================================================================
# HOJA 7: UserPermissions (Permisos custom por usuario)
# ====================================================================================
Write-Host "`n[7/7] Creando hoja: UserPermissions..." -ForegroundColor Yellow

# Inicialmente vacía (se llena cuando se otorgan permisos custom)
$userPermissions = @(
    # Ejemplo: Dar a vendedor1 permiso temporal de ver reportes por 30 días
    # [PSCustomObject]@{ Id=1; UserName="vendedor1"; PermissionId=28; IsGranted=$true; StartDate=$today; EndDate="2026-04-20" }
    
    # Ejemplo: Quitar a admin el permiso de eliminar ventas (override)
    # [PSCustomObject]@{ Id=2; UserName="admin"; PermissionId=6; IsGranted=$false; StartDate=$today; EndDate=$null }
)

# Crear hoja vacía con encabezados
$emptyUserPermissions = @(
    [PSCustomObject]@{ Id=[int]$null; UserName=""; PermissionId=[int]$null; IsGranted=[bool]$null; StartDate=""; EndDate="" }
)

$emptyUserPermissions | Export-Excel -Path $excelFile -WorksheetName "UserPermissions" -AutoSize -TableName "UserPermissionsTable" -TableStyle Medium2 -ExcludeProperty @()

Write-Host "   [OK] Hoja 'UserPermissions' creada (vacia, lista para permisos custom)" -ForegroundColor Green

# ====================================================================================
# RESUMEN FINAL
# ====================================================================================
Write-Host "`n" -NoNewline
Write-Host "====================================================================" -ForegroundColor Cyan
Write-Host "                    PROCESO COMPLETADO                              " -ForegroundColor Cyan
Write-Host "====================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Archivo Excel:" -ForegroundColor Yellow
Write-Host "   $excelFile" -ForegroundColor White
Write-Host ""
Write-Host "Hojas creadas:" -ForegroundColor Yellow
Write-Host "   [OK] Modules            - $($modules.Count) modulos" -ForegroundColor Green
Write-Host "   [OK] Actions            - $($actions.Count) acciones" -ForegroundColor Green
Write-Host "   [OK] Permissions        - $($permissions.Count) permisos" -ForegroundColor Green
Write-Host "   [OK] Roles              - $($roles.Count) roles" -ForegroundColor Green
Write-Host "   [OK] RolePermissions    - $($rolePermissions.Count) asignaciones" -ForegroundColor Green
Write-Host "   [OK] UserRoles          - $($userRoles.Count) usuarios asignados" -ForegroundColor Green
Write-Host "   [OK] UserPermissions    - (vacia - lista para custom permisos)" -ForegroundColor Green
Write-Host ""
Write-Host "Siguiente paso:" -ForegroundColor Yellow
Write-Host "   1. Verificar datos en Excel" -ForegroundColor White
Write-Host "   2. Ejecutar aplicacion para que cargue nuevas hojas" -ForegroundColor White
Write-Host "   3. Implementar servicios de permisos en codigo C#" -ForegroundColor White
Write-Host ""
Write-Host "Documentacion:" -ForegroundColor Yellow
Write-Host "   Ver: docs/PLAN-RBAC-ROBUSTO.md" -ForegroundColor White
Write-Host ""
