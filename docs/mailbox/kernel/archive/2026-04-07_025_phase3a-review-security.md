---
id: MSG-K025
from: architect
to: kernel
type: task
status: UNREAD
priority: P1
sprint: "Sprint D · Phase 3A Review"
ref: MSG-K024
---

# Phase 3A — Kód review + security ellenőrzés

A Phase 3A implementáció (Spatial BIM Core + Modules.Joinery + 4D Timeline) kész.
Kérlek végezd el az alábbi review és security ellenőrzéseket, majd küldd el az outbox-ba a találatokat.

---

## 1. Build + Test gate

```bash
cd /opt/spaceos/SpaceOS.Kerner
dotnet build --configuration Release
dotnet test
dotnet list package --vulnerable
```

Elvárt eredmény:
- `0 error, 0 warning`
- `814 pass, 0 fail`
- `0 high/critical vulnerability`

---

## 2. Kód review — Domain Layer

Ellenőrizd az alábbi fájlokat:

**Aggregates / Entities:**
- `SpaceOS.Kernel.Domain/Aggregates/PhysicalSpace.cs`
  - [ ] `static Register()` factory — nincs public constructor
  - [ ] `RegistrationHash` SHA-256 kiszámítása helyes (SpaceCode + SpaceType + Dimensions)
  - [ ] `AddDomainEvent(new PhysicalSpaceRegisteredEvent(...))` meghívva
  - [ ] `_nodes` navigációs property **nem létezik** (BE-P3A-01)

- `SpaceOS.Kernel.Domain/Entities/BvhNode.cs`
  - [ ] `_children` navigációs property **nem létezik** (BE-P3A-03)
  - [ ] `CreateRoot()` / `CreateLeaf()` factory metódusok

- `SpaceOS.Kernel.Domain/Entities/SpatialElement.cs`
- `SpaceOS.Kernel.Domain/Entities/SpatialTaskLink.cs`

**Value Objects:**
- `BoundingBox.cs` — `Intersects()` 6-tengelyes AABB logika helyes?
- `SpatialGrid.cs` — `GetIntersectingCells(BoundingBox)` lefedi az összes metsző cellát?
- `GridCell.cs`, `DimensionVector.cs`, `Point3D.cs` — `readonly record struct` típus

**Domain Service:**
- `SpaceOS.Kernel.Domain/Services/BvhQueryService.cs`
  - [ ] Cycle guard: `HashSet<Guid> visited` minden traversal-ban
  - [ ] Depth guard: mélység > 32 → `DomainException` (SEC-P3A-03)
  - [ ] `ConfigureAwait(false)` minden `await`-nél
  - [ ] IBvhRepository.GetChildrenAsync() hívás (nincs eager load)

---

## 3. Kód review — Application Layer

- `RegisterPhysicalSpaceCommandHandler.cs`
  - [ ] `IPhysicalSpaceRepository.ExistsWithHashAsync()` → 409 Conflict ha duplikált hash
  - [ ] `IBvhTreeService.InsertElementAsync()` hívva az elemek számára

- `RegisterSpatialElementCommandHandler.cs`
  - [ ] `IBvhTreeService.InsertElementAsync()` collision detektálás és `SpatialCollisionDetectedEvent` dispatch

- `LinkTaskToElementCommandHandler.cs`
  - [ ] Cross-tenant guard: `task.TenantId != element.TenantId` → `Result.Forbidden` (SEC-P3A-07)
  - [ ] `IFlowTaskLookup` interface hívása (nem direkt EF Core, BE-P3A alkalmazás réteg szabály)

- `GetSpatialSnapshotAtTQueryHandler.cs` + `GetSpatialTimelineEventsQueryHandler.cs`
  - [ ] Delegál `ISpatialQueryRepository`-ra (nincs EF Core az Application rétegben)

- `SpatialContractDto.cs`
  - [ ] `ElementType` property **nem létezik** (ADR-008 — szándékos, BE-P3A-10)

- FluentValidation validators (3 db):
  - [ ] Minden command-hoz van validator
  - [ ] Pagination: `pageSize` max 50

---

## 4. Kód review — Infrastructure Layer

**EF Core konfigurációk:**
- `PhysicalSpaceConfiguration.cs`
  - [ ] `OwnsOne` Dimensions (DimensionVector) → külön oszlopok, nem JSON
  - [ ] `OwnsOne` Origin (Point3D) → külön oszlopok
  - [ ] SpaceType: `HasConversion<string>()`, nem int
  - [ ] Nincs data annotation a domain entitásokon

- `BvhNodeConfiguration.cs`
  - [ ] `OwnsOne(BoundingBox)` → 6 int oszlop (MinX, MaxX, MinY, MaxY, MinZ, MaxZ)
  - [ ] Self-ref FK: `HasOne<BvhNode>().WithMany().HasForeignKey(n => n.ParentId).OnDelete(Cascade)`
  - [ ] `HasMany(_children)` **nem létezik** (BE-P3A-03)

- `SpatialElementConfiguration.cs`, `SpatialTaskLinkConfiguration.cs`

**Repository-k:**
- `SpatialQueryRepository.cs`
  - [ ] 4D snapshot query: `DISTINCT ON`, AuditEvents JOIN, `@at` timestamp filter, pagination
  - [ ] `try_cast_uuid` hívás minden Guid konverziónál a raw SQL-ben (SEC-P3A-01)
  - [ ] Timeline playback query helyes sorrendben

- `AppDbContext.cs`
  - [ ] 4 új DbSet: `PhysicalSpaces`, `BvhNodes`, `SpatialElements`, `SpatialTaskLinks`
  - [ ] 4 tenant query filter: `.HasQueryFilter(e => e.TenantId == _tenantId)` (vagy ekvivalens)

---

## 5. Security ellenőrzések

### SEC-P3A-01 — `try_cast_uuid` IMMUTABLE függvény
```sql
-- Migrációban megvan?
SELECT proname FROM pg_proc WHERE proname = 'try_cast_uuid';
```
- [ ] Migration 0019 tartalmazza a `CREATE OR REPLACE FUNCTION try_cast_uuid` DDL-t
- [ ] Az összes raw SQL query `try_cast_uuid()` hívással kezeli a malformed UUID-okat

### SEC-P3A-02 — RLS minden új táblán
```sql
SELECT relname, relrowsecurity, relforcerowsecurity
FROM pg_class
WHERE relname IN ('PhysicalSpaces','BvhNodes','SpatialElements','SpatialTaskLinks');
```
- [ ] `relforcerowsecurity = true` mind a 4 táblán

### SEC-P3A-03 — BVH mélység DB trigger
```sql
-- Migration 0017-ben megvan?
SELECT tgname FROM pg_trigger WHERE tgname = 'TR_BvhNodes_DepthCheck';
```
- [ ] `check_bvh_depth()` trigger létezik és max 32 rekurzív szintet enged

### SEC-P3A-04 — Table ownership
```sql
SELECT tablename, tableowner FROM pg_tables
WHERE tablename IN ('PhysicalSpaces','BvhNodes','SpatialElements','SpatialTaskLinks');
```
- [ ] Minden tábla owner: `spaceos_schema_owner`

### SEC-P3A-05 — Cross-tenant DB trigger
```sql
SELECT tgname FROM pg_trigger WHERE tgname = 'TR_SpatialTaskLinks_TenantCheck';
```
- [ ] Trigger létezik és cross-tenant INSERT-et blokkol

### SEC-P3A-06 — TradeType / WorkPhase CHECK constraints (no 'other')
```sql
SELECT conname, consrc FROM pg_constraint
WHERE conname LIKE '%TradeType%' OR conname LIKE '%WorkPhase%';
```
- [ ] `'other'` nem szerepel az allowed értékek között

### SEC-P3A-07 — SpatialContractsView — ElementType absent
```sql
SELECT column_name FROM information_schema.columns
WHERE table_name = 'SpatialContractsView';
```
- [ ] `element_type` / `ElementType` oszlop **nem létezik** a view-ban

### Reflection teszt futtatás
```bash
dotnet test --filter "FullyQualifiedName~SpatialSecurityTests"
```
- [ ] 5/5 pass

---

## 6. Ismert limitációk (nem blocker, Phase 3B scope)

Az alábbiak hiányoznak de szándékosan vannak kihagyva:
- `FlowEpic.ComputeSpatialState()` — FlowTask navigáció + FsmState integráció — Phase 3B
- Parallel collision teszt (két IN_DEV Task overlapping AABB) — Phase 3B
- Domain event handler assertion tesztek (PhysicalSpaceRegistered, SpatialCollisionDetected) — Phase 3B

---

## Elvárt outbox üzenet

Küldj vissza egy `type: response` üzenetet `ref: MSG-K025` hivatkozással, amely tartalmazza:
- Build + test eredmény (pass/fail count, warning count)
- Security ellenőrzések eredménye (minden pont ✅ vagy ⚠️ eltéréssel)
- Bármilyen eltérés a fentitől — ha talál hibát, részletezve
- `dotnet list package --vulnerable` kimenet
