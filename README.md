# VitaRaizPlus

Sistema de ventas y cobros con geolocalizacion.

Este repositorio incluye una propuesta funcional y una base tecnica para el MVP:
- API ASP.NET Core
- Web MVC
- Estructura inicial para app Android offline-first

## Documentacion funcional
- `docs/arquitectura.md`
- `docs/roles-y-pantallas.md`
- `docs/flujos-negocio.md`
- `docs/mvp-plan.md`
- `docs/block-0-checklist.md`
- `docs/block-1-auth.md`

## Estructura de codigo
- `src/SalesCobrosGeo.Api`
- `src/SalesCobrosGeo.Web`
- `src/SalesCobrosGeo.Shared`
- `mobile/android`

## Estado
- Bloque 0: base de solucion completada.
- Bloque 1: autenticacion, roles y auditoria base completados.

## Build
```powershell
./scripts/build.ps1
```

## Run
```powershell
./scripts/run-api.ps1
./scripts/run-web.ps1
```
