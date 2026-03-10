# VitaRaizPlus - Diseño base del sistema

Este repositorio contiene una propuesta inicial para construir un **sistema de ventas y cobros con geolocalización** que funcione tanto en:

- Aplicación móvil Android (enfoque ahorro de datos)
- Sistema web en navegador

Incluye:

1. Arquitectura sugerida (backend, web, móvil y seguridad)
2. Modelo de roles con pantallas por rol
3. Flujos de ventas y cobranza geolocalizada
4. Recomendaciones de optimización de datos para móvil
5. Backlog MVP por fases

## Documentos

- `docs/arquitectura.md`
- `docs/roles-y-pantallas.md`
- `docs/flujos-negocio.md`
- `docs/mvp-plan.md`

## Siguiente paso recomendado

Construir un MVP con:

- Backend API REST + JWT + control de permisos por rol
- Web Admin/Dashboard
- App Android para vendedor/cobrador con sincronización offline
