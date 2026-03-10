# Block 1 - Authentication, Roles and Audit

## Implemented
- Custom bearer token authentication (in-memory tokens)
- Role-based authorization policies:
  - Authenticated
  - CanManageSales
  - CanCollect
  - CanSuperviseCollections
  - AdminOnly
- Demo users for each role
- Audit trail middleware for mutating API requests
- Admin endpoint to review recent audit records

## Demo users
- vendedor.demo / demo123
- supventas.demo / demo123
- cobrador.demo / demo123
- supcobranza.demo / demo123
- admin.demo / demo123

## Main endpoints
- POST `/api/auth/login`
- POST `/api/auth/logout`
- GET `/api/auth/me`
- GET `/api/sales/mine`
- GET `/api/sales/team`
- POST `/api/collections/register`
- POST `/api/collections/reassign`
- GET `/api/audit/recent`

## Note
This is the base security layer for MVP. Tokens and users are in-memory and should be replaced by persistent storage in upcoming blocks.
