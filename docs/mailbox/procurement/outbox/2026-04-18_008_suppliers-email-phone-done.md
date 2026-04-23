---
id: MSG-PROCUREMENT-008-DONE
from: procurement
to: root
type: done
priority: medium
status: READ
ref: MSG-PROCUREMENT-008
created: 2026-04-18
---

# DONE — PROCUREMENT-008: BUG-001 Supplier email+phone mezők

## Root cause (megerősítve)

Kettős probléma:
1. **Field name mismatch**: Portal `email`-t küld, backend `ContactEmail`-t várt
2. **Hiányzó mező**: `Phone` teljesen hiányzott a domain/DB/API-ból

## Módosított fájlok

| Fájl | Változás |
|---|---|
| `src/.../Domain/Aggregates/Supplier.cs` | `ContactEmail` → `Email`, `Phone` hozzáadva |
| `src/.../Configurations/SupplierConfiguration.cs` | `Email` → `ContactEmail` kolumna (`HasColumnName`), `Phone` nullable varchar(50) |
| `src/.../Migrations/20260418000005_AddSupplierPhone.cs` | **ÚJ** — `Phone` oszlop hozzáadása |
| `src/.../Commands/CreateSupplier/CreateSupplierCommand.cs` | `ContactEmail` → `Email`, `Phone` hozzáadva |
| `src/.../Commands/CreateSupplier/CreateSupplierCommandHandler.cs` | frissítve |
| `src/.../Queries/GetSuppliers/GetSuppliersQuery.cs` | `SupplierResponse`: `ContactEmail` → `Email`, `Phone` hozzáadva |
| `src/.../Queries/GetSuppliers/GetSuppliersQueryHandler.cs` | frissítve |
| `src/.../Api/Endpoints/ProcurementEndpoints.cs` | `CreateSupplierRequest`: `ContactEmail` → `Email`, `Phone` hozzáadva |
| 3 tesztfájl | `Supplier.Create` hívások frissítve |

Commit: `cd5c542`

**Megközelítés:** `Email` property `HasColumnName("ContactEmail")`-lel mappel → nincs oszlop rename migráció, a meglévő adatok megmaradnak.

## Tesztek

```
Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51
```

## Security review

| Pont | Státusz |
|---|---|
| `email` és `phone` null-safe kezelés az API requestben | ✅ |
| Mező értékek a JWT tenant scope alá esnek | ✅ |

## Kockázatok / kérdések

A `Phone` migration (`AddSupplierPhone`) manuálisan fut VPS-en — jelezd INFRA-nak.
