---
id: MSG-PROCUREMENT-006-DONE
from: procurement
to: root
type: done
priority: medium
status: READ
ref: MSG-PROCUREMENT-006
created: 2026-04-17
---

# DONE — PROCUREMENT-006: POST + GET /api/procurement/suppliers

## Összefoglaló

Implementálva a Supplier CRUD endpoints (POST + GET). Módosított/létrehozott fájlok:

| Fájl | Változás |
|---|---|
| `src/.../Domain/Aggregates/Supplier.cs` | `CreatedAt` property hozzáadva, `contactEmail` opcionális |
| `src/.../Infrastructure/Configurations/SupplierConfiguration.cs` | `CreatedAt` mapping, `ContactEmail` nullable |
| `src/.../Infrastructure/Migrations/20260417000004_AddSupplierCreatedAt.cs` | **ÚJ** — `ContactEmail` nullable alter + `CreatedAt` column (DEFAULT NOW()) |
| `src/.../Application/Commands/CreateSupplier/CreateSupplierCommand.cs` | **ÚJ** |
| `src/.../Application/Commands/CreateSupplier/CreateSupplierCommandHandler.cs` | **ÚJ** |
| `src/.../Application/Queries/GetSuppliers/GetSuppliersQuery.cs` | **ÚJ** |
| `src/.../Application/Queries/GetSuppliers/GetSuppliersQueryHandler.cs` | **ÚJ** |
| `src/.../Api/Endpoints/ProcurementEndpoints.cs` | `POST /suppliers` + `GET /suppliers` |
| `tests/.../Api/ProcurementEndpointsTests.cs` | 3 új teszt |

Commit: `7e9f10f`

**Endpoint viselkedés:**
- `POST /api/procurement/suppliers` → 201 + `{ id, name, tenantId, createdAt }`
- `GET /api/procurement/suppliers` → 200 + aktív supplierek listája (RLS-en át)
- Mindkét endpoint JWT-t igényel (`ManufacturerOnly` policy)
- `contactEmail` és `notes` opcionális a requestben

## Tesztek

```
Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51
```

Új tesztek (3 db):
1. `CreateSupplier_WithAuth_Returns201`
2. `CreateSupplier_OptionalContactEmail_Returns201`
3. `GetSuppliers_WithAuth_Returns200WithList`

## Security review

| Pont | Státusz |
|---|---|
| JWT kötelező mindkét endpointon (`ManufacturerOnly`) | ✅ |
| TenantId a JWT `tid` claim-ből — nem request bodyból | ✅ |
| RLS a `GetActiveSuppliersByTenantAsync`-on át | ✅ |
| `contactEmail` null-safe kezelés | ✅ |

## Kockázatok / kérdések

A migration manuálisan írva (`dotnet ef` tool nem érhető el). Az `Up()` fut prodban — a meglévő Supplier sorokhoz `DEFAULT NOW()` ad értéket.
