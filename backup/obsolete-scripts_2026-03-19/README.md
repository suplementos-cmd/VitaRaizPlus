# Scripts Obsoletos - Backup 2026-03-19

Archivos movidos desde `scripts/` por ser obsoletos o duplicados.

---

## 🗑️ Scripts Obsoletos

### cleanup-obsolete-files.ps1
- **Razón**: Los archivos que intenta eliminar ya no existen (ya fueron limpiados)
- **Archivos objetivo**: JsonSalesRepository.cs, InMemoryUserStore.cs, etc.
- **Estado**: Ya están en `backup/obsolete-files/2026-03-19_142117/`
- **Resultado**: Script sin propósito, limpieza ya completada

---

## ⚠️ Scripts Duplicados

### crear-excel-manual.ps1
- **Razón**: Duplica lógica de `ExcelDataInitializer.cs`
- **Problema**: Script manual puede crear Excel con esquema diferente al código C#
- **Riesgo**: Desincronización entre script y código fuente
- **Reemplazo**: Usar `regenerate-excel.ps1` que ejecuta API (usa código C# oficial)

### regenerar-excel-16-tablas.ps1
- **Razón**: Funcionalidad duplicada con `regenerate-excel.ps1`
- **Problema**: Puerto hardcoded (5053)
- **Reemplazo**: `regenerate-excel.ps1` (más reciente, usa configuración dinámica)

### regenerar-excel-job.ps1
- **Razón**: Funcionalidad duplicada con `regenerate-excel.ps1`
- **Problema**: Complejidad innecesaria (PowerShell Jobs), puerto hardcoded (5054)
- **Reemplazo**: `regenerate-excel.ps1` (método más simple y estándar)

### RegenerarExcel.csx
- **Razón**: Funcionalidad duplicada con `regenerate-excel.ps1`
- **Problema**: Requiere `dotnet-script` (no instalado por defecto)
- **Tipo**: C# Script (no PowerShell)
- **Reemplazo**: `regenerate-excel.ps1` (usa herramientas estándar)

---

## ✅ Scripts Activos Mantenidos

Solo quedaron los scripts esenciales en `scripts/`:

1. **build.ps1** - Compilar solución
2. **run-api.ps1** - Ejecutar API
3. **run-web.ps1** - Ejecutar Web
4. **backup-excel.ps1** - Backup Excel
5. **restore-excel.ps1** - Restaurar Excel
6. **regenerate-excel.ps1** - Regenerar Excel (canónico)
7. **Migrate-DataToExcel.ps1** - Migración SQLite→Excel (Fase 5)
8. **init-users-excel.ps1** - Inicializar usuarios
9. **minify-css.ps1** - Minificar CSS

---

## 📊 Impacto

- **Scripts eliminados**: 5
- **Scripts activos**: 9 (8 PowerShell + 1 Markdown)
- **Reducción**: 36% menos archivos
- **Claridad**: Una sola forma canonical de regenerar Excel
- **Mantenibilidad**: Lógica centralizada en código C#, no en scripts

---

**Fecha de limpieza**: 2026-03-19  
**Análisis completo**: Ver `ANALISIS-SCRIPTS.md` en raíz del proyecto
