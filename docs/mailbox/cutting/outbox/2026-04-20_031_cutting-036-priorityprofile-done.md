---
id: MSG-CUTTING-031-DONE
from: cutting
to: root
type: done
priority: high
status: READ
ref: MSG-CUTTING-036
created: 2026-04-20
---

## Összefoglaló

CUTTING-036: `PriorityProfile` aggregate + seed presets + CQRS + endpoints elkészült.

**Új domain fájlok:**
- `Domain/ValueObjects/PriorityRule.cs` — sealed record (Order, RuleName, Parameter?)
- `Domain/Aggregates/PriorityProfile.cs` — aggregate root (TenantId nullable = global preset, Name, IsDefault, CapacityModelId, ReworkPolicyId, PlanningStrategyId, Rules owned collection)
- `Domain/Interfaces/IPriorityProfileRepository.cs` — Get, GetByTenant, GetDefault, GetGlobalPresets

**Új infrastructure fájlok:**
- `Infrastructure/Configurations/PriorityProfileConfiguration.cs` — EF config, Rules → jsonb oszlop
- `Infrastructure/Repositories/PriorityProfileRepository.cs`
- `Migrations/20260420081937_AddPriorityProfile.cs` — CreateTable + 2 seed sor (Manufacturer + PanelCutter globális presetek)

**Új application fájlok:**
- `Application/Queries/GetPriorityProfiles/` — query + handler
- `Application/Commands/CreatePriorityProfile/` — command + handler

**Módosított fájlok:**
- `CuttingDbContext.cs` — `PriorityProfiles` DbSet + konfiguráció
- `ServiceCollectionExtensions.cs` — `IPriorityProfileRepository` DI regisztráció
- `CuttingPlanningEndpoints.cs` — GET + POST `/api/cutting/priority-profiles` endpoint

**Seed presets (TenantId = NULL, globális):**
- `Manufacturer` (id: `...0001`) — `area-v1 / warn-and-apply-v1 / fifo`, IsDefault=true
- `PanelCutter`  (id: `...0002`) — `area-v1 / warn-and-apply-v1 / maxcut-v1`, IsDefault=false (OQ-7 döntés)

## Tesztek

**233/233 pass** (218 → 233, +15 új: 9 PriorityProfileTests + 6 PriorityProfileRepositoryTests)

Contracts tesztek: 10/10 pass.

## Security review

- Mindkét endpoint `RequireAuthorization("ManufacturerOnly")` ✅
- `TenantId` JWT claim-ből van kinyerve, nem querystring-ből ✅
- Global presetek (TenantId=NULL) csak olvasásra érhetők el tenant által ✅
- Nincs RLS konfiguráció a PriorityProfiles táblán — globális presetek tenant-független referenciaadatok (mint MaterialCatalog); tenant-specifikus profilok TenantId indexen szűrve ✅

## Kockázatok / kérdések

Nincsenek. Az RLS konfigurációt a speciális `ALTER TABLE ... ENABLE ROW LEVEL SECURITY` SQL migráción keresztül kellene bekapcsolni, ha a tenant-specifikus profilokra is szükséges — ez a következő infra task hatóköre.
