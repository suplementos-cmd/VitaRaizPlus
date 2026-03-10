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
- `docs/block-2-3-4.md`
- `docs/block-5-6-7-8.md`

## Estructura de codigo
- `src/SalesCobrosGeo.Api`
- `src/SalesCobrosGeo.Web`
- `src/SalesCobrosGeo.Shared`
- `mobile/android`
- `scripts`

## Estado actual
- Bloques 0 y 1: base de solucion, autenticacion, roles y auditoria.
- Bloques 2, 3 y 4: catalogos, clientes/ventas y supervision de ventas.
- Bloques 5, 6, 7 y 8: cobranza, supervision de cartera, sync movil y dashboards web.

## Usuarios demo (password `demo123`)
- `vendedor.demo`
- `supventas.demo`
- `cobrador.demo`
- `supcobranza.demo`
- `admin.demo`

## Build
```powershell
./scripts/build.ps1
```

## Run
```powershell
./scripts/run-api.ps1
./scripts/run-web.ps1
```
