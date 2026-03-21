using System;
using System.Linq;
using OfficeOpenXml;

// Script para verificar permisos RBAC del usuario admin directamente del Excel

var excelPath = @"c:\Git\VitaRaizPlus\src\SalesCobrosGeo.Api\App_Data\SalesCobrosGeo.xlsx";

if (!File.Exists(excelPath))
{
    Console.WriteLine($"ERROR: Excel no encontrado en {excelPath}");
    return;
}

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

using var package = new ExcelPackage(new FileInfo(excelPath));

// 1. Verificar usuario admin
Console.WriteLine("=== USUARIO ADMIN ===");
var usersSheet = package.Workbook.Worksheets["Users"];
if (usersSheet != null)
{
    for (int row = 2; row <= usersSheet.Dimension.End.Row; row++)
    {
        var userName = usersSheet.Cells[row, 1].Value?.ToString();
        if (userName?.ToLower() == "admin")
        {
            Console.WriteLine($"Usuario: {userName}");
            Console.WriteLine($"Nombre completo: {usersSheet.Cells[row, 2].Value}");
            Console.WriteLine($"Activo: {usersSheet.Cells[row, 4].Value}");
            break;
        }
    }
}

// 2. Verificar roles RBAC
Console.WriteLine("\n=== ROLES RBAC ===");
var rolesSheet = package.Workbook.Worksheets["Roles"];
var roles = new System.Collections.Generic.Dictionary<int, string>();
if (rolesSheet != null)
{
    for (int row = 2; row <= rolesSheet.Dimension.End.Row; row++)
    {
        var id = Convert.ToInt32(rolesSheet.Cells[row, 1].Value ?? 0);
        var code = rolesSheet.Cells[row, 2].Value?.ToString() ?? "";
        var name = rolesSheet.Cells[row, 3].Value?.ToString() ?? "";
        var isActive = rolesSheet.Cells[row, 5].Value?.ToString()?.ToLower() == "true";
        
        if (isActive)
        {
            roles[id] = $"{code} - {name}";
            Console.WriteLine($"  Rol {id}: {code} - {name}");
        }
    }
}

// 3. Verificar asignación de roles al usuario admin
Console.WriteLine("\n=== ROLES DEL USUARIO ADMIN ===");
var userRolesSheet = package.Workbook.Worksheets["UserRoles"];
var adminRoleIds = new System.Collections.Generic.List<int>();
if (userRolesSheet != null)
{
    for (int row = 2; row <= userRolesSheet.Dimension.End.Row; row++)
    {
        var userName = userRolesSheet.Cells[row, 2].Value?.ToString();
        if (userName?.ToLower() == "admin")
        {
            var roleId = Convert.ToInt32(userRolesSheet.Cells[row, 3].Value ?? 0);
            var isActive = userRolesSheet.Cells[row, 6].Value?.ToString()?.ToLower() != "false";
            
            if (isActive && roles.ContainsKey(roleId))
            {
                adminRoleIds.Add(roleId);
                Console.WriteLine($"  ✓ Rol {roleId}: {roles[roleId]}");
            }
        }
    }
}

if (adminRoleIds.Count == 0)
{
    Console.WriteLine("  ⚠ NO TIENE ROLES ASIGNADOS");
}

// 4. Verificar permisos
Console.WriteLine("\n=== PERMISOS DISPONIBLES ===");
var permissionsSheet = package.Workbook.Worksheets["Permissions"];
var permissions = new System.Collections.Generic.Dictionary<int, (string Code, string Desc)>();
if (permissionsSheet != null)
{
    for (int row = 2; row <= permissionsSheet.Dimension.End.Row; row++)
    {
        var id = Convert.ToInt32(permissionsSheet.Cells[row, 1].Value ?? 0);
        var code = permissionsSheet.Cells[row, 2].Value?.ToString() ?? "";
        var desc = permissionsSheet.Cells[row, 6].Value?.ToString() ?? "";
        
        permissions[id] = (code, desc);
        
        if (code.Contains("VIEW"))
        {
            Console.WriteLine($"  Permiso {id}: {code}");
        }
    }
}

// 5. Obtener permisos de los roles del admin
Console.WriteLine("\n=== PERMISOS DE LOS ROLES DEL ADMIN ===");
var rolePermissionsSheet = package.Workbook.Worksheets["RolePermissions"];
var adminPermissionIds = new System.Collections.Generic.HashSet<int>();
if (rolePermissionsSheet != null)
{
    for (int row = 2; row <= rolePermissionsSheet.Dimension.End.Row; row++)
    {
        var roleId = Convert.ToInt32(rolePermissionsSheet.Cells[row, 2].Value ?? 0);
        if (adminRoleIds.Contains(roleId))
        {
            var permId = Convert.ToInt32(rolePermissionsSheet.Cells[row, 3].Value ?? 0);
            var isGranted = rolePermissionsSheet.Cells[row, 4].Value?.ToString()?.ToLower() != "false";
            
            if (isGranted && permissions.ContainsKey(permId))
            {
                adminPermissionIds.Add(permId);
                Console.WriteLine($"  ✓ {permissions[permId].Code}");
            }
        }
    }
}

if (adminPermissionIds.Count == 0)
{
    Console.WriteLine("  ⚠ NO TIENE PERMISOS DESDE ROLES");
}

// 6. Verificar permisos críticos
Console.WriteLine("\n=== VERIFICACIÓN DE PERMISOS CRÍTICOS ===");
var criticalPermissions = new[] { "DASHBOARD.VIEW", "SALES.VIEW", "COLLECTIONS.VIEW", "ADMINISTRATION.VIEW" };
foreach (var critPerm in criticalPermissions)
{
    var perm = permissions.FirstOrDefault(p => p.Value.Code == critPerm);
    if (perm.Value.Code != null)
    {
        var hasIt = adminPermissionIds.Contains(perm.Key);
        var icon = hasIt ? "✓" : "✗";
        Console.WriteLine($"  {icon} {critPerm}");
    }
    else
    {
        Console.WriteLine($"  ? {critPerm} (no existe en BD)");
    }
}

Console.WriteLine("\n=== FIN ===");
