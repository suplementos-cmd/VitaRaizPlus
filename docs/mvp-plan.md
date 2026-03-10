# Plan MVP (8-10 semanas)

## Fase 1 (Semanas 1-2): Base técnica

- Estructura backend + DB
- Login y RBAC
- Catálogo básico de usuarios, clientes y productos

## Fase 2 (Semanas 3-5): Operación principal

- Registro de ventas
- Registro de cobros con geolocalización
- Sincronización offline/online en Android

## Fase 3 (Semanas 6-8): Dashboard y control

- Dashboard web de ventas/cobros
- Reportes por rol
- Auditoría de eventos

## Fase 4 (Semanas 9-10): Endurecimiento

- Pruebas E2E
- Optimización de consumo de datos
- Seguridad y despliegue productivo

## Estrategia de ahorro de datos para móvil

- Sincronización incremental (solo cambios)
- Compresión de payload (gzip)
- Imágenes y adjuntos opcionales/calidad reducida
- Cache local con caducidad
- Reintentos exponenciales y envío por lotes
