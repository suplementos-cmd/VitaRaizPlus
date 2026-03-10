# Arquitectura propuesta

## Objetivo
Diseñar una plataforma única para web y Android con control por roles, geolocalización y dashboard de negocio.

## Componentes

1. **Backend API (monolito modular)**
   - Autenticación JWT
   - Autorización RBAC (Role Based Access Control)
   - Módulos: usuarios, clientes, ventas, cobros, rutas, reportes
   - Base de datos relacional (PostgreSQL)

2. **Aplicación Web (React + Vite sugerido)**
   - Panel administrativo
   - Dashboard gerencial
   - Gestión de usuarios, rutas, clientes, políticas de cobro

3. **Aplicación Android (Kotlin + Jetpack Compose sugerido)**
   - Módulos para vendedor/cobrador
   - Soporte offline-first
   - Sincronización por lotes cuando hay red

4. **Servicios transversales**
   - Auditoría de acciones
   - Notificaciones (push + email)
   - Registro de eventos de ubicación

## Seguridad y control de acceso

- JWT con refresh token
- Permisos granulares por rol y módulo
- Registro de auditoría por acción crítica
- Cifrado en tránsito (HTTPS)
- Cifrado local para cache sensible en móvil

## Modelo de despliegue inicial

- Backend: contenedor Docker + Nginx reverse proxy
- DB: PostgreSQL administrado
- Web: despliegue estático (CDN)
- Android: distribución interna para pruebas (QA) y luego Play Store
