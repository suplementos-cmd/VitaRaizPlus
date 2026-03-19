# 🪟 Solución de Problemas - Windows PowerShell

## ❌ Error: Script no firmado digitalmente

```
PSSecurityException: El archivo no está firmado digitalmente.
No se puede ejecutar este script en el sistema actual.
```

## ✅ Soluciones

### Opción 1: Ejecutar con Bypass (Recomendado - Temporal)

Ejecuta los scripts prefijando con política de bypass:

```powershell
# Compilar proyecto
powershell -ExecutionPolicy Bypass -File ./scripts/build.ps1

# Ejecutar API
powershell -ExecutionPolicy Bypass -File ./scripts/run-api.ps1

# Ejecutar Web
powershell -ExecutionPolicy Bypass -File ./scripts/run-web.ps1

# Backup Excel
powershell -ExecutionPolicy Bypass -File ./scripts/backup-excel.ps1
```

### Opción 2: Cambiar Política para Usuario Actual (Recomendado - Permanente)

Abre PowerShell **como Administrador** y ejecuta:

```powershell
# Ver política actual
Get-ExecutionPolicy -List

# Cambiar para el usuario actual (no requiere admin)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# O cambiar para todo el sistema (requiere admin)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope LocalMachine
```

**Explicación de políticas:**
- `Restricted` (por defecto) - No permite ningún script
- `RemoteSigned` - Permite scripts locales, requiere firma para descargados
- `Unrestricted` - Permite todos los scripts (NO recomendado)
- `Bypass` - Omite todas las restricciones (solo para sesión actual)

### Opción 3: Desbloquear Archivos Específicos

Si descargaste el repositorio de internet:

```powershell
# En la raíz del proyecto
Get-ChildItem -Path ./scripts -Recurse | Unblock-File
```

### Opción 4: Usar Comandos Directos (Sin Scripts)

#### Compilar:
```powershell
dotnet build SalesCobrosGeo.sln --configuration Release
```

#### Ejecutar API:
```powershell
cd src/SalesCobrosGeo.Api
dotnet run
```

#### Ejecutar Web:
```powershell
cd src/SalesCobrosGeo.Web
dotnet run
```

## 🔍 Verificar Configuración Actual

```powershell
# Ver política actual
Get-ExecutionPolicy

# Ver todas las políticas
Get-ExecutionPolicy -List

# Ver si un archivo específico está bloqueado
Get-Item ./scripts/build.ps1 | Select-Object -ExpandProperty Attributes
```

## ⚡ Solución Rápida para Este Proyecto

Ejecuta esto **UNA SOLA VEZ** (como usuario normal, no requiere admin):

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force
```

Luego podrás ejecutar todos los scripts normalmente:

```powershell
./scripts/build.ps1
./scripts/run-api.ps1
./scripts/run-web.ps1
```

## 🛡️ Seguridad

- **RemoteSigned** es seguro: solo permite scripts locales o firmados
- **Bypass** es temporal: solo afecta la sesión actual
- **NO uses Unrestricted**: permite cualquier script sin restricciones

## 📞 ¿Aún tienes problemas?

Contacta al administrador del sistema o revisa:
https://docs.microsoft.com/powershell/module/microsoft.powershell.security/set-executionpolicy
