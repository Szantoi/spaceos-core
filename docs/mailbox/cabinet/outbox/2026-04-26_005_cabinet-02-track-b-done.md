---
id: MSG-CABINET-005-DONE
from: cabinet
to: root
type: done
priority: high
status: READ
ref: MSG-CABINET-005
created: 2026-04-26
---

# CABINET-005 DONE -- Track B: Catalog domain + payload

## Eredmeny

### 2 uj NuGet csomag letrehozva

| Csomag | Framework | Statusz |
|---|---|---|
| SpaceOS.Cabinet.Catalog | net8.0;net10.0 | KOMPLETT |
| SpaceOS.Cabinet.Assembly | net8.0;net10.0 | placeholder (Track C) |

### Catalog csomag -- KOMPLETT

#### CatalogEntry aggregate + 5-state FSM
- Draft -> Submitted -> Approved -> Published -> Deprecated
- Submitted -> Rejected (terminal)
- Minden atmeneten Version++, UpdatedAt, UpdatedBy frissites
- Domain events: CatalogEntryCreated, Submitted, Approved, Rejected, Published, Deprecated

#### Enums
- CatalogType (8 ertek): HorizontalRole, MaterialThickness, JointType, EdgeBandingRule, HardwareSet, BackPanelStandard, RasterStandard, ConstructionTemplate
- CatalogVisibility (4 ertek): Private, Shared, Community, Curated
- CatalogLifecycleState (6 ertek): Draft, Submitted, Approved, Published, Deprecated, Rejected

#### 8 Payload DTO + Validator
- HorizontalRolePayloadV1, MaterialThicknessPayloadV1, JointTypePayloadV1, EdgeBandingRulePayloadV1
- HardwareSetPayloadV1, BackPanelStandardPayloadV1, RasterStandardPayloadV1, ConstructionTemplatePayloadV1
- CatalogPayloadSchemas mapping: (CatalogType, schemaVersion) -> DTO type
- CatalogPayloadValidator: System.Text.Json strict deserialization

#### CatalogResolutionProvider (Scoped, fallback chain)
- 6-level precedence: Skeleton-pin -> Template -> Tenant-private -> Shared -> Community -> Curated
- Cabinet 0.2: Tenant-private es Curated aktiv, tobbi ures
- Per-request cache Dictionary<(tenantId, type), CatalogEntry?>

#### SystemCatalog konstansok
- TenantId: 00000000-0000-0000-0000-000000000001
- ActorUserId: 00000000-0000-0000-0000-000000000002
- 16 curated seed entry (2 per CatalogType)

#### SnapshotMigrator_0_1_to_0_2
- ISnapshotMigrator implementacio
- SchemaVersion "0.1" -> "0.2"
- Uj mezok: roleAssignments (ures array), pinnedCatalogEntries (ures object)

### Security rules enforced

- SEC-CAB02-5: 64KB payload limit
- DB-CAB02-2: Version optimistic locking
- DB-CAB02-3: Curated visibility csak SystemCatalog.TenantId-vel
- DB-CAB02-8: ContentHash SHA-256 immutability
- PayloadSchemaVersion regex: ^[a-z][a-z0-9_]*/v\d+$

## Definition of Done checklist

- [x] SpaceOS.Cabinet.Catalog csomag komplett
- [x] CatalogEntry aggregate + 5-state FSM
- [x] 8 payload DTO + validator
- [x] CatalogResolutionProvider (Scoped, fallback chain)
- [x] SnapshotMigrator_0_1_to_0_2
- [x] SEC-CAB02-5: 64KB limit
- [x] `dotnet build -c Release` 0 error, 0 warning
- [x] `dotnet test` 353 pass (301 elozo + 52 uj) -- tulteljesiti a 341 celszamot
- [x] net8.0 ES net10.0 PASS

## Build & Test

```
Build succeeded. 0 Warning(s), 0 Error(s)
net8.0:  Passed! Failed: 0, Passed: 353, Skipped: 0
net10.0: Passed! Failed: 0, Passed: 353, Skipped: 0
```

## Teszt bontas

| Terulet | Teszt szam |
|---|---|
| Geometry | 76 |
| Abstractions | 23 |
| Domain | 95 |
| Machining | 31 |
| Construction | 43 |
| Semantics | 28 |
| CrossCutting/Smoke | 5 |
| **Catalog (uj)** | **52** |
| **Osszesen** | **353** |
