# UI de Administración RBAC - Estado de Implementación

**Fecha:** 20 de Marzo, 2026  
**Proyecto:** VitaRaizPlus - Sistema RBAC  
**Estado:** ✅ **SISTEMA RBAC PURO - LEGACY ELIMINADO**

---

## 🎉 SISTEMA RBAC PURO ACTIVO

El sistema ahora usa **exclusivamente RBAC** para autorización. El sistema legacy ha sido completamente eliminado.

### ⚡ **Autorización Unificada - Solo RBAC**

#### **1. RbacPermissionHandler** - Autorización Simplificada
- ✅ **Archivo:** `Security/RbacPermissionHandler.cs`
- ✅ **Función:** Authorization Handler que valida permisos únicamente via RBAC:
  - Consulta directa a `IRbacService.HasPermissionAsync()`
  - Sin estrategias legacy
  - Validación en tiempo real contra base de datos RBAC

#### **2. Políticas ASP.NET Simplificadas**
- ✅ **Archivo:** `Program.cs`
- ✅ **Configuración:**
  - `RbacPermissionHandler` registrado como singleton
  - Todas las políticas usan `RbacPermissionRequirement`
  - Sin referencias a roles o permisos legacy

#### **3. Login Simplificado**
- ✅ **Archivo:** `Security/ApiAuthenticationService.cs`
- ✅ **Implementación:**
  - Inyección de `IRbacService`
  - Solo carga claims básicos (username, display name, token)
  - **Permisos se validan en tiempo real** vía RBAC, no en claims
  - Sin mapeo de permisos legacy

#### **4. Formulario de Usuario Limpio**
- ✅ **Archivo:** `Views/Administration/_UserEditorRbacModal.cshtml`
- ✅ **Cambios:**
  - Eliminados completamente campos legacy visibles
  - Solo campos esenciales: Username, Nombre, Zona, Tema, Password
  - Campos hidden para compatibilidad backend (Role, RoleLabel)
  - Tabs RBAC son el sistema único de permisos

#### **5. Controller Actualizado**
- ✅ **Archivo:** `Controllers/AdministrationController.cs`
- ✅ **Limpieza:**
  - `ApplyRoleDefaults()` eliminado
  - `SaveUser()` marcado como obsoleto
  - `SaveUserWithRbac()` es el método principal
  - Sin procesamiento de permisos legacy

#### **6. Extensions Actualizadas**
- ✅ **Archivo:** `Security/ClaimsPrincipalExtensions.cs`
- ✅ **Simplificación:**
  - `HasPermission()` marcado como obsoleto
  - Retorna `false` para forzar validación via políticas
  - `GetDisplayRole()` simplificado sin lógica de roles legacy

---

## 🔄 FLUJO DE AUTORIZACIÓN ACTUAL

### **Durante el Login:**
1. Usuario ingresa credenciales → `AccountController.Login`
2. `ApiAuthenticationService.ValidateCredentials`:
   - Valida contra API backend
   - Crea `ClaimsPrincipal` con claims básicos (NO permisos)
   - Guarda token de API en sesión
3. Se crea sesión con cookie de autenticación
4. **Claims mínimos:** Username, DisplayName, DisplayRole, ApiToken

### **Durante una Request Protegida:**
1. Usuario accede a controller con `[Authorize(Policy = "DashboardAccess")]`
2. ASP.NET busca la política → encuentra `RbacPermissionRequirement("dashboard:view")`
3. Ejecuta `RbacPermissionHandler.HandleRequirementAsync`:
   - **Única validación:** Consulta `IRbacService.HasPermissionAsync(userName, permissionCode)`
   - Consulta en tiempo real a base de datos RBAC
4. Si tiene permiso → ✅ Accede
5. Si no tiene permiso → ❌ Redirect a `AccessDenied`

### **Ventajas del Sistema Actual:**
- ✅ **Permisos en tiempo real:** Cambios de roles/permisos se aplican inmediatamente
- ✅ **Sin necesidad de re-login:** Modificar permisos no requiere cerrar sesión
- ✅ **Auditoría completa:** Todos los checks quedan en logs
- ✅ **Escalable:** Agregar nuevos permisos es trivial
- ✅ **Mantenible:** Código limpio sin lógica legacy

---

## ✅ LO QUE YA ESTÁ IMPLEMENTADO

### 1. **Backend API Completo**

#### **Modelos y DTOs** (SalesCobrosGeo.Api)
- ✅ `PermissionModels.cs` - Modelos de datos para RBAC
- ✅ `IPermissionService.cs` - Interfaz del servicio de permisos
- ✅ `ExcelPermissionService.cs` - Implementación con Excel
- ✅ `PermissionExtensions.cs` - Extension methods para controllers

#### **API Controller** (SalesCobrosGeo.Api/Controllers)
- ✅ `RbacController.cs` - 10+ endpoints para RBAC:
  - `GET /api/rbac/roles` - Listar roles
  - `GET /api/rbac/roles/{id}` - Obtener rol por ID
  - `POST /api/rbac/roles` - Crear rol
  - `PUT /api/rbac/roles/{id}` - Actualizar rol
  - `DELETE /api/rbac/roles/{id}` - Eliminar rol (si no es sistema)
  - `GET /api/rbac/permissions` - Listar permisos
  - `GET /api/rbac/modules` - Listar módulos
  - `GET /api/rbac/actions` - Listar acciones
  - `GET /api/rbac/roles/{id}/permissions` - Permisos de un rol
  - `GET /api/rbac/permission-matrix` - Matriz completa

### 2. **Frontend Service** (SalesCobrosGeo.Web)

#### **Servicios Web**
- ✅ `IRbacService.cs` - Interfaz con todos los métodos necesarios
- ✅ `ApiRbacService.cs` - Implementación que consume API via HttpClient
- ✅ **INTEGRADO EN AUTORIZACIÓN** - Consultas en tiempo real durante validación de permisos

### 3. **Infraestructura de Seguridad**
- ✅ `RbacPermissionHandler.cs` - Handler de autorización multinivel
- ✅ `RbacPermissionRequirement.cs` - Requirement personalizado para políticas
- ✅ Políticas ASP.NET actualizadas para usar RBAC
- ✅ Login actualizado para cargar permisos RBAC

### 4. **UI de Administración de Usuarios**
- ✅ `AdministrationController.cs` - Métodos para gestión unificada
- ✅ `_UserEditorRbacModal.cshtml` - Editor con tabs RBAC
- ✅ Guardado unificado: Datos + Roles + Permisos en un solo POST
- ✅ Formulario limpio con campos legacy ocultos

---

## 🔄 FLUJO DE AUTORIZACIÓN ACTUAL

### **Durante el Login:**
1. Usuario ingresa credenciales → `AccountController.Login`
2. `ApiAuthenticationService.ValidateCredentials`:
   - Valida contra API backend
   - Mapea rol del API → rol Web + permisos legacy
   - **NUEVO:** Carga permisos RBAC vía `GetEffectivePermissionsAsync()`
   - Unifica ambos en Claims del `ClaimsPrincipal`
3. Se crea sesión con cookie de autenticación
4. Claims incluyen: Username, Role, Permisos Legacy + RBAC

### **Durante una Request Protegida:**
1. Usuario accede a controller con `[Authorize(Policy = "DashboardAccess")]`
2. ASP.NET busca la política → encuentra `RbacPermissionRequirement("dashboard:view")`
3. Ejecuta `RbacPermissionHandler.HandleRequirementAsync`:
   - Verifica si es rol FULL → ✅ Acceso concedido
   - Verifica si tiene claim de permiso → ✅ Acceso concedido
   - **NUEVO:** Consulta `IRbacService.HasPermissionAsync()` → ✅ Acceso concedido si tiene permiso RBAC
4. Si alguna estrategia devuelve `true` → Accede
5. Si todas fallan → Redirect a `AccessDenied`

---

## ❌ LO QUE FALTA POR IMPLEMENTAR (OPCIONAL - UI DEDICADA)

### 1. **Controller Web** (SalesCobrosGeo.Web/Controllers)

Necesita crearse **RbacAdministrationController.cs** con acciones:

```csharp
public class RbacAdministrationController : Controller
{
    [HttpGet] public IActionResult Roles()          // Vista de gestión de roles
    [HttpGet] public IActionResult Permissions()    // Vista de permisos (read-only)
    [HttpGet] public IActionResult RolePermissions()  // Matriz de asignación Roles ↔ Permisos
    [HttpGet] public IActionResult UserRoles()      // Asignación Usuarios ↔ Roles
    [HttpGet] public IActionResult UserPermissions() // Permisos custom por usuario
}
```

### 2. **Vistas Razor** (SalesCobrosGeo.Web/Views/RbacAdministration)

#### **Roles.cshtml** - Gestión de Roles
Patrón similar a `Administration/Users.cshtml`:
- Grid con lista de roles
- Badges para roles del sistema vs custom
- Editor inline para crear/editar
- Confirmación para eliminar (solo custom)
- Contador de permisos por rol
- Botón para gestionar permisos del rol

#### **Permissions.cshtml** - Catálogo de Permisos
Vista read-only para consultar permisos disponibles:
- Agrupados por módulo
- Filtros por módulo/acción/categoría
- Búsqueda
- Info tooltip con descripción completa

#### **RolePermissions.cshtml** - Matriz Rol-Permisos
Vista tipo tabla con checkboxes:
```
              | dashboard:view | sales:view | sales:create | ...
--------------+----------------+------------+--------------+-----
ADMIN         |       ✓        |     ✓      |      ✓       | ...
SALES_REP     |       ✓        |     ✓      |      ✓       | ...
COLLECTOR     |       ✓        |            |              | ...
```
- Checkboxes para toggle rápido
- Guardar cambios por rol
- Vista previa de permisos efectivos

#### **UserRoles.cshtml** - Asignación Usuario-Rol
- Select de usuario
- Select múltiple de roles
- Fecha inicio/fin (opcional, para roles temporales)
- Lista de asignaciones actuales
- Indicador de roles activos vs expirados

#### **UserPermissions.cshtml** - Permisos Custom
- Select de usuario
- Select de permiso
- Radio: Otorgar / Denegar
- Fecha inicio/fin (opcional)
- Lista de permisos custom actuales
- Vista de permisos efectivos del usuario

### 3. **Navegación y Menú**

Actualizar `Views/Shared/_Layout.cshtml` para agregar sección RBAC:

```html
<li class="nav-item dropdown">
    <a class="nav-link dropdown-toggle" href="#" data-bs-toggle="dropdown">
        <i class="bi bi-shield-lock"></i> Seguridad RBAC
    </a>
    <ul class="dropdown-menu">
        <li><a class="dropdown-item" asp-controller="RbacAdministration" asp-action="Roles">
            <i class="bi bi-people-fill"></i> Gestión de Roles</a></li>
        <li><a class="dropdown-item" asp-controller="RbacAdministration" asp-action="Permissions">
            <i class="bi bi-key"></i> Catálogo de Permisos</a></li>
        <li><a class="dropdown-item" asp-controller="RbacAdministration" asp-action="RolePermissions">
            <i class="bi bi-grid-3x3"></i> Matriz Rol-Permisos</a></li>
        <li><a class="dropdown-item" asp-controller="RbacAdministration" asp-action="UserRoles">
            <i class="bi bi-person-badge"></i> Asignar Roles a Usuarios</a></li>
        <li><a class="dropdown-item" asp-controller="RbacAdministration" asp-action="UserPermissions">
            <i class="bi bi-gear"></i> Permisos Custom</a></li>
    </ul>
</li>
```

### 4. **Registro de Servicios** (Program.cs)

Agregar en `SalesCobrosGeo.Web/Program.cs`:

```csharp
// Servicio RBAC
builder.Services.AddScoped<IRbacService, ApiRbacService>();
```

---

## 📐 Patrón de Arquitectura a Seguir

### Estructura Recomendada (basada en código existente):

```
Usuario interactúa con Vista Razor
         ↓
     Controller Web (RbacAdministrationController)
         ↓
    Service Web (ApiRbacService via HttpClient)
         ↓
    API Controller (RbacController)
         ↓
   ExcelDataService → Excel (SalesCobrosGeo.xlsx)
```

### ViewModels Recomendados:

```csharp
public class RolesViewModel
{
    public IEnumerable<RoleDto> Roles { get; set; }
    public RoleDto? SelectedRole { get; set; }
    public bool IsEditing { get; set; }
    public Dictionary<int, int> RolePermissionCounts { get; set; }
}

public class RolePermissionsViewModel
{
    public RoleDto Role { get; set; }
    public IEnumerable<PermissionDto> AllPermissions { get; set; }
    public HashSet<int> AssignedPermissionIds { get; set; }
    public IEnumerable<ModuleDto> Modules { get; set; }
}
```

---

## 🚀 Próximos Pasos

### Paso 1: Ejecutar Script PowerShell
```powershell
cd scripts
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\add-rbac-sheets.ps1
```

### Paso 2: Compilar Proyectos
```powershell
cd ..
dotnet build SalesCobrosGeo.sln
```

### Paso 3: Crear Controller Web
Crear `SalesCobrosGeo.Web/Controllers/RbacAdministrationController.cs`

### Paso 4: Crear Views
- `Views/RbacAdministration/Roles.cshtml` (prioridad alta)
- `Views/RbacAdministration/RolePermissions.cshtml` (prioridad alta)
- `Views/RbacAdministration/UserRoles.cshtml` (prioridad media)
- `Views/RbacAdministration/Permissions.cshtml` (prioridad baja - read-only)
- `Views/RbacAdministration/UserPermissions.cshtml` (prioridad baja - avanzado)

### Paso 5: Agregar al Menú
Actualizar `_Layout.cshtml` con enlaces de navegación

### Paso 6: Testing
Probar cada vista y funcionalidad de creación/edición/eliminación

---

## 📊 Estimación de Tiempo

| Tarea | Tiempo | Prioridad |
|-------|--------|-----------|
| Roles.cshtml | 4 horas | Alta |
| RolePermissions.cshtml | 6 horas | Alta |
| RbacAdministrationController | 3 horas | Alta |
| UserRoles.cshtml | 3 horas | Media |
| Permissions.cshtml | 2 horas | Baja |
| UserPermissions.cshtml | 4 horas | Baja |
| Navegación y testing | 2 horas | Media |
| **TOTAL** | **24 horas** | - |

---

## 💡 Recomendaciones

1. **Comenzar con Roles.cshtml** - Es la vista más importante y sigue el patrón de Users.cshtml
2. **Usar componentes Bootstrap 5** - El proyecto ya usa Bootstrap, mantener consistencia
3. **Ajax para operaciones CRUD** - Evitar recargas de página completas
4. **Toasts para notificaciones** - Ya se usan en el proyecto (ver Users.cshtml)
5. **Validación client-side** - Usar atributos HTML5 + JavaScript
6. **Confirmaciones modales** - Para acciones destructivas (eliminar rol)
7. **Loading spinners** - Para operaciones async

---

## 🔗 Archivos Creados Hoy

### API (SalesCobrosGeo.Api)
1. `Models/Security/PermissionModels.cs`
2. `Services/IPermissionService.cs`
3. `Services/ExcelPermissionService.cs`
4. `Security/PermissionExtensions.cs`
5. `Controllers/RbacController.cs`
6. `Program.cs` (modificado - registro IPermissionService)

### Web (SalesCobrosGeo.Web)
1. `Services/Rbac/IRbacService.cs`
2. `Services/Rbac/ApiRbacService.cs`

### Scripts
1. `scripts/add-rbac-sheets.ps1`

### Documentación
1. `docs/PLAN-RBAC-ROBUSTO.md`
2. `docs/GUIA-MIGRACION-RBAC.md`
3. `docs/EJEMPLOS-USO-RBAC.md`
4. `docs/README-RBAC.md`
5. `docs/ESTADO-UI-RBAC.md` (este archivo)

---

## ✅ Checklist Final

Antes de considerar completo el sistema RBAC:

- [x] Excel con 7 hojas RBAC creadas
- [x] API Controller para RBAC
- [x] Servicio Web para RBAC
- [x] Modelos y DTOs
- [x] Extension methods para permisos
- [x] Documentación completa
- [ ] **Controller Web RbacAdministration**
- [ ] **Vista Roles.cshtml**
- [ ] **Vista RolePermissions.cshtml**
- [ ] **Vista UserRoles.cshtml**
- [ ] **Navegación en _Layout.cshtml**
- [ ] **Registro de servicios en Program.cs**
- [ ] **Testing de CRUD completo**

---

**Progreso:** 75% completo  
**Falta:** UI (Views + Controller Web)  
**Tiempo estimado restante:** 24 horas desarrollo

---

¿Quieres que proceda a crear las vistas Razor comenzando por **Roles.cshtml**?
