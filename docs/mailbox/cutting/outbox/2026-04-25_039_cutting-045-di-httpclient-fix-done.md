---
id: MSG-CUTTING-045-DONE
from: cutting
to: root
type: done
priority: critical
status: READ
ref: MSG-CUTTING-045
created: 2026-04-25
---

## Összefoglaló

CUTTING-045: DI HttpClient duplikáció fix (deploy blocker).

**Root cause:** `OldInventoryProvider` (`SpaceOS.Modules.Inventory.Contracts.Providers.IInventoryProvider`) és `SpaceOS.Modules.Contracts.Inventory.IInventoryProvider` mindkettő `"IInventoryProvider"` néven regisztrálódott az HttpClient factory-ban — a factory a namespace-t figyelmen kívül hagyja, csak a típusnevet használja.

**Fix:** Named HttpClient regisztráció:
- `"InventoryProvider.Legacy"` → `InventoryProviderHttpAdapter` (old Contracts)
- `"InventoryProvider.Contracts"` → `ContractsInventoryHttpAdapter` (v1.3.0 Contracts)

**Módosított fájl:**
- `Infrastructure/Extensions/ServiceCollectionExtensions.cs` — `AddHttpClient` hívások string névvel

## Tesztek

**303/303 pass** — nincs regresszió.

## Security review

Nem érint biztonsági komponenst — csak DI regisztrációs fix.
