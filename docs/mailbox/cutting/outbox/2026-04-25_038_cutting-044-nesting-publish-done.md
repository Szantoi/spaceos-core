---
id: MSG-CUTTING-044-DONE
from: cutting
to: root
type: done
priority: high
status: READ
ref: MSG-CUTTING-044
created: 2026-04-25
---

## Összefoglaló

CUTTING-044: Nesting integráció a Publish flow-ba + PlanNestingSnapshot enrichment.

**Új Application fájlok:**
- `Application/Services/PanelSourceService.cs` — Inventory stock + offcut összegyűjtés → `AvailablePanel` lista, graceful degradation

**Módosított Application fájlok:**
- `Commands/PublishCuttingPlan/PublishCuttingPlanCommandHandler.cs` — teljes nesting integráció:
  - CuttingJob-ok → NestingPart mapping (GrainDirection → CanRotate: None=true, Vertical/Horizontal=false)
  - PanelSourceService → elérhető panelek
  - INestingStrategy.ComputeAsync → NestingResult
  - PlanNestingSnapshot mentés (waste + placement + yield + algorithm)

**Módosított Domain fájl:**
- `Domain/Entities/PlanNestingSnapshot.cs` — új mezők: `PlacementsJson` (jsonb), `YieldPercent` (decimal), `WasteAreaMm2` (long), `Algorithm` (string)

**Módosított Infrastructure fájlok:**
- `Configurations/PlanNestingSnapshotConfiguration.cs` — új oszlopok EF config
- `Extensions/ServiceCollectionExtensions.cs` — `PanelSourceService` DI regisztráció

**Migration:** `AddPlanNestingSnapshotEnrichment` — PlacementsJson, YieldPercent, WasteAreaMm2, Algorithm oszlopok

## Tesztek

**303/303 pass** (293 → +10 új):

PublishCuttingPlanNestingTests.cs (10 teszt):
1. `Publish_HappyPath_SavesNestingSnapshot` ✅
2. `Publish_NestingResult_PlacementsCountMatchParts` ✅
3. `Publish_YieldPercent_GreaterThanZero` ✅
4. `Publish_GrainDirectionNone_CanRotateTrue` ✅
5. `Publish_GrainDirectionVertical_CanRotateFalse` ✅
6. `PanelSourceService_FetchesStockAndOffcuts` ✅
7. `Publish_DoesNotCallReserveAsync` ✅ (reserve is separate)
8. `Publish_SnapshotContainsAlgorithmName` ✅ (FFDH)
9. `Publish_EmptyDaySlots_SkipsNesting` ✅
10. `Publish_AlreadyPublished_ReturnsInvalid` ✅

Contracts: 10/10 pass.

## Security review

- PanelSourceService: Inventory hívások try/catch-be csomagolva ✅
- PlanNestingSnapshot TenantId oszlop megmarad (RLS) ✅
- GrainDirection enum: ismeretlen érték → None fallback ✅
- PlacementsJson nem tartalmaz PII/tenant adatot (csak koordináták) ✅

## Kockázatok / kérdések

- **ReserveAsync nem hívódik Publish-kor**: a task spec említi a ReserveAsync-et a publish flow-ban, de a jelenlegi architektúra szerint a `POST /{planId}/reserve-panels` egy külön endpoint. A Publish csak a nesting-et futtatja és a snapshot-ot menti. Ha root mást szeretne, jelezze.
- **PanelSourceService az old `IInventoryProvider`-t használja** (`SpaceOS.Modules.Inventory.Contracts.Providers.IInventoryProvider`), mert a Contracts v1.3.0 `IInventoryProvider` `GetStockAsync`/`GetUsableOffcutsAsync` implementációja `NotSupportedException`-t dob a ContractsInventoryHttpAdapter-ben. Ez szándékos — az old adapter a helyes HTTP hívásokat tartalmazza.
