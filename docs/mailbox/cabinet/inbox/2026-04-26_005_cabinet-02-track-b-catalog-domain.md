---
id: MSG-CABINET-005
from: root
to: cabinet
type: task
priority: high
status: READ
ref: SpaceOS_Cabinet_0.2_CatalogAssembly_Architecture_v4.md
created: 2026-04-26
---

# CABINET-005 — Track B: Catalog domain + payload (Nap 3–7.5)

> **Tervdok:** `/opt/spaceos/docs/tasks/new/SpaceOS_Cabinet_0.2_CatalogAssembly_Architecture_v4.md` — KÖTELEZŐ olvasmány!
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** Cabinet 0.1 COMPLETE (301 teszt, 7 NuGet csomag a repo-ban)
> **Párhuzamosan fut:** KERNEL-101 (FlowEpic Scope) — a Catalog domain ettől független
> **Használhatsz sub-agent-eket** ha szükséges

---

## Scope

2 új NuGet csomag a Cabinet repo-ban:
- `SpaceOS.Cabinet.Catalog` (net8.0;net10.0)
- `SpaceOS.Cabinet.Assembly` (net8.0;net10.0) — placeholder (Track C-ben töltjük)

### Catalog csomag (spec §4.1)

**CatalogEntry aggregate:**
- 8 `CatalogType` enum (Material, EdgeBand, Hardware, DimensionProfile, ConstructionRuleSet, PartTemplate, SkeletonTemplate, AssemblyGuide)
- 4 `CatalogVisibility` enum (Private, Tenant, Shared, Community)
- 5-state FSM: Draft → PendingReview → Approved → Published → Deprecated
- JSONB `PayloadJson` + `PayloadSchemaVersion`
- `ContentHash` SHA-256 (Published entry immutability)
- Domain events: CatalogEntryCreated, Submitted, Approved, Published, Deprecated

**8 Payload DTO (spec §4.1.3):**
- MaterialPayload, EdgeBandPayload, HardwarePayload, DimensionProfilePayload
- ConstructionRuleSetPayload, PartTemplatePayload, SkeletonTemplatePayload, AssemblyGuidePayload

**ICatalogPayloadValidator:**
- Strongly-typed validation per CatalogType
- 64KB JSON limit (SEC-CAB02-5)

**CatalogResolutionProvider (spec §7.1):**
- Tenant → Shared → System fallback chain
- Scoped DI lifetime (per-request cache, BE-CAB02-2)

**SystemCatalog konstansok:**
- System tenant UUID: `00000000-0000-0000-0000-000000000001`
- 16 curated seed entry (2 per CatalogType)

**SnapshotMigrator_0_1_to_0_2:**
- ISnapshotMigrator implementáció (Cabinet 0.1 Abstractions interface)
- SchemaVersion "0.1" → "0.2" migration

---

## Tesztek (40+)

- CatalogEntry FSM: Draft→PendingReview→Approved→Published→Deprecated transitions
- Invalid transitions: Draft→Published direct → error
- ContentHash: Published entry immutability
- PayloadValidator: valid + invalid per type
- 64KB limit: oversized → error
- CatalogResolutionProvider: tenant → shared → system fallback
- SystemCatalog: 16 seed entry existence
- SnapshotMigrator: 0.1→0.2 round-trip
- Domain events: correct event per FSM transition

---

## Definition of Done

- [ ] `SpaceOS.Cabinet.Catalog` csomag komplett
- [ ] CatalogEntry aggregate + 5-state FSM
- [ ] 8 payload DTO + validator
- [ ] CatalogResolutionProvider (Scoped, fallback chain)
- [ ] SnapshotMigrator_0_1_to_0_2
- [ ] SEC-CAB02-5: 64KB limit
- [ ] `dotnet build -c Release` 0 error, 0 warning
- [ ] `dotnet test` ≥ 341 pass (301 előző + min 40 új)
- [ ] net8.0 ÉS net10.0 PASS
- [ ] Outbox DONE
