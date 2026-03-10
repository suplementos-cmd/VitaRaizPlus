# Block 2, 3 and 4 implementation

## Block 2 - Catalogs
Implemented in-memory catalog management with versioning:
- Zones
- Products
- Payment methods
- Snapshot endpoint for mobile sync

Endpoints:
- GET `/api/catalogs/snapshot`
- GET/POST/PUT `/api/catalogs/zones`
- GET/POST/PUT `/api/catalogs/products`
- GET/POST/PUT `/api/catalogs/payment-methods`

Catalog writes are restricted to admin users.

## Block 3 - Clients and Sales registration
Implemented:
- Client CRUD (role-based)
- Sale creation with items, coordinates and photos
- Validation for mandatory evidence when sending sale for review
- Draft update endpoint for seller/supervisor

Endpoints:
- GET/POST/PUT `/api/clients`
- GET `/api/clients/{id}`
- GET `/api/sales/mine`
- POST `/api/sales`
- PUT `/api/sales/{id}/draft`
- GET `/api/sales/{id}`

## Block 4 - Sales supervision
Implemented supervision flow:
- Review queue for registered/corrected sales
- Review action: approve / observe / reject
- Collector reassignment
- Change history timeline per sale

Endpoints:
- GET `/api/sales/review`
- GET `/api/sales/team`
- POST `/api/sales/{id}/review`
- POST `/api/sales/{id}/assign-collector`

## Notes
- Persistence is in-memory for now (next step: database).
- Status workflow: Draft -> Registered/Corrected -> Approved/Observed/Rejected.
