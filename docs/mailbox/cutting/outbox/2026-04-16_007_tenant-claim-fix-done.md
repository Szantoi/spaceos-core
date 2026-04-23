---
id: MSG-CUTTING-007-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-007
created: 2026-04-16
---

## Összefoglaló

`GetTenantId` claim javítva mindhárom repóban: `"tenant_id"` → `"tid"`.
`MapInboundClaims = false` beállítva mindhárom `Program.cs`-ben.

### Változott fájlok

**spaceos-modules-cutting** (commit `79d16a2`):
- `src/SpaceOS.Modules.Cutting.Api/Endpoints/CuttingEndpoints.cs` — `"tenant_id"` → `"tid"`
- `src/SpaceOS.Modules.Cutting.Api/Program.cs` — `MapInboundClaims = false`
- `tests/SpaceOS.Modules.Cutting.Tests/Api/TestAuthHandler.cs` — `"tenant_id"` → `"tid"`

**spaceos-modules-inventory** (commit `b67d9bc`):
- `src/SpaceOS.Modules.Inventory.Api/Endpoints/InventoryEndpoints.cs` — `"tenant_id"` → `"tid"`
- `src/SpaceOS.Modules.Inventory.Api/Program.cs` — `MapInboundClaims = false`
- `tests/SpaceOS.Modules.Inventory.Tests/Api/TestAuthHandler.cs` — `"tenant_id"` → `"tid"`

**spaceos-modules-procurement** (commit `1fba5e2`):
- `src/SpaceOS.Modules.Procurement.Api/Endpoints/ProcurementEndpoints.cs` — `"tenant_id"` → `"tid"`
- `src/SpaceOS.Modules.Procurement.Api/Program.cs` — `MapInboundClaims = false`
- `tests/SpaceOS.Modules.Procurement.Tests/Api/TestAuthHandler.cs` — `"tenant_id"` → `"tid"`

## Tesztek

| Repo | Teszt | Eredmény |
|---|---|---|
| spaceos-modules-inventory | 47 | ✅ mind zöld |
| spaceos-modules-procurement | 42 | ✅ mind zöld |
| spaceos-modules-cutting | 64 (54 + 10 contracts) | ✅ mind zöld |
| **Összesen** | **153** | **✅** |

## Security review

- `MapInboundClaims = false` megakadályozza, hogy az MS middleware a `"tid"` claimet hosszú URI-ra nevezze át
- A javítás összhangban van a Kernel `ClaimsTenantResolver.cs` implementációjával
- Minden endpoint `[Authorize(Policy = "ManufacturerOnly")]` — változatlan

## Kockázatok / megjegyzések

Nincs kockázat. A fix egyetlen sor per fájl, a tesztek lefedik az auth path-ot.
