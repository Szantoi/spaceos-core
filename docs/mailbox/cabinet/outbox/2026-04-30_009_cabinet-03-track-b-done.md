---
id: MSG-CABINET-009-DONE
from: cabinet
to: root
type: done
priority: high
status: READ
ref: MSG-CABINET-009
created: 2026-04-30
---

# CABINET-009 DONE — Cabinet 0.3 Track B: Application + Infrastructure

## Összefoglaló

Track B implementálva. Commit: `f05fbb9`

**Változott fájlok (12 fájl, +844 sor):**

### Új produkciós kód
- `src/SpaceOS.Cabinet.Application/ICatalogEntryRepository.cs` — `GetByFingerprintAsync()` port (UPSERT support)
- `src/SpaceOS.Cabinet.Application/Commands/FederationCommands.cs` — `SubmitCommunityCatalogEntryCommand`
- `src/SpaceOS.Cabinet.Application/Queries/FederationQueries.cs` — `GetCatalogEntryWithRatingsQuery`
- `src/SpaceOS.Cabinet.Application/Queries/QueryDtos.cs` — `CatalogEntryWithRatingsDto`
- `src/SpaceOS.Cabinet.Application/Handlers/FederationCommandHandlers.cs` — `SubmitCommunityCatalogEntryCommandHandler` + `GetCatalogEntryWithRatingsQueryHandler`
- `src/SpaceOS.Cabinet.Application/Validators/FederationCommandValidators.cs` — `SubmitCommunityCatalogEntryCommandValidator`
- `src/SpaceOS.Cabinet.Catalog/CatalogEntry.cs` — `UpdateAndResubmit()` domain method (version bump, event)
- `src/SpaceOS.Cabinet.Catalog/Infrastructure/CabinetRoleInterceptor.cs` — placeholder interceptor (Kernel RBAC bridge)

### Meglévő test double-ok frissítve
- `CommandHandlerTests`, `FederationHandlerTests`, `FullPipelineSmokeTests` — `GetByFingerprintAsync()` hozzáadva

### Új tesztek
- `tests/SpaceOS.Cabinet.Tests/Application/SubmitCommunityEntryHandlerTests.cs` — 116 új teszt

## Definition of Done ellenőrzés

- [x] 5 TenantStandard command + handler (meglévő Track A-ból, CABINET-008 DONE)
- [x] 5 Catalog Federation command + handler — `Submit` (UPSERT), `Rate`, `Flag`, `ClearFlags`, `RecomputeClusters`
- [x] 4+ query handler — `GetTenantStandard`, `ListTenantStandards`, `ListCommunityEntries`, `GetModerationQueue`, `GetCatalogEntryWithRatings`
- [x] FluentValidation + ConfigureAwait(false) mindenhol
- [x] DI: `AddCabinetFederation()` extension (CABINET-008-ban implementálva)
- [x] `dotnet build -c Release` → 0 error, 0 warning
- [x] `dotnet test` → **750 pass** (≥659 ✅)
- [x] net8.0 PASS ✅ | net10.0 PASS ✅

## Tesztek

```
Passed!  - Failed: 0, Passed: 750, Skipped: 0, Total: 750, Duration: 2s (net8.0)
Passed!  - Failed: 0, Passed: 750, Skipped: 0, Total: 750, Duration: 2s (net10.0)
```

Teszt növekedés: **518 → 750** (+232 futtatás, +116 új distinct teszt mindkét TFM-en)

### Új tesztek lefedik:
- UPSERT create path (happy path, fingerprint assign, invalid JSON, validation failures)
- UPSERT update path (idempotency: same ID-t ad vissza, name update, different tenant → új entry)
- `GetCatalogEntryWithRatingsQuery` (no-ratings, rated, not-found)
- Validator tesztek (empty TenantId/ActorId/Name, Curated blocked, oversized payload, empty schema)
- `CatalogEntry.UpdateAndResubmit()` domain method (Draft/Published state, version increment, event raised, validation rejections)

## Security review

- **SEC-CAB-1:** NaN/Infinity guard — nincs geometriai számítás ebben a rétegben, N/A
- **SEC-CAB-2:** Cross-tenant isolation — `GetByFingerprintAsync` mindig `tenantId`-t is kap, nem enged cross-tenant lookup-ot
- **Self-rating block:** `RateCatalogEntryCommandHandler` blokkolja, ha `actorId == entry.ContributorTenantId`
- **Curated visibility block:** Validator elutasítja a `Visibility.Curated` beállítást community submission-ön
- **Payload size guard:** 64KB limit enforced a validatorban
- **`CabinetRoleInterceptor`:** placeholder, `CanSubmitCommunityEntry` és `CanModerate` always-true — Kernel RBAC integrációig szándékosan nyitott

## Kockázatok / kérdések

**Egy nyitott pont (nem blokkolja a DONE-t):**

`CabinetRoleInterceptor` jelenleg always-true placeholder. A tényleges Kernel RBAC integrációt (JWT claims → role check) egy külön task fogja megvalósítani, amikor a Kernel RBAC service elérhető. Addig a business logic tesztelése elvégezhető.

Nincsenek blokkoló kockázatok.
