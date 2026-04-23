---
id: MSG-K024
from: architect
to: kernel
type: task
status: UNREAD
priority: P0
sprint: "Sprint D · Phase 3A"
ref: "/opt/spaceos/docs/SpaceOS_Phase3A_Architecture_v3.md"
---

# Sprint D · Phase 3A — Spatial BIM Core + Modules.Joinery + 4D Timeline

## Összefoglaló

**6 fejlesztői terület, ~10 fejlesztői nap.** Blokkoló feltétel: Phase 2 DoD teljes (T-01..T-08 zöld).

**Referencia ügyfél:** Doorstar Kft. — első Modules.Joinery derivátum.

**Kumulált finding:** 5C + 10H + 7M = 22 finding beépítve (BE-P3A-01..11 + SEC-P3A-01..11).

> ⚠️ MINDIG hivatkozd az eredeti dokumentumot: `/opt/spaceos/docs/SpaceOS_Phase3A_Architecture_v3.md` — a teljes kódspecifikáció ott van, beleértve az összes C# kódot, DDL-t és DoD gate-et.

**Implementációs sorrend:**
```
Nap 1–2:  PA-01 Domain layer (aggregátumok, entitások, VO-k, domain events)
Nap 3:    PA-02 BVH Domain Service (IBvhTreeService, BvhQueryService, cycle+depth guard)
Nap 4–5:  PA-03 Application CQRS (5 handler + FluentValidation validators)
Nap 6–7:  PA-04 Infrastructure (EF Core config, IBvhRepository, Migrations 0016–0019)
Nap 8:    PA-05 API endpoints (RegisterPhysicalSpace, RegisterSpatialElement, LinkTask, 4D queries)
Nap 9–10: PA-06 Tesztek (≥35 új teszt) + DoD gate-ek
```

---

## PA-01 · Domain Layer (Nap 1–2)

### `TenantScopedEntity` base class (BE-P3A-07)

`Domain/Common/TenantScopedEntity.cs` — `abstract class`, `TenantId: Guid` protected set.
Minden új entitás ebből örököl.

### `PhysicalSpace` aggregate root (BE-P3A-01)

`Domain/Aggregates/PhysicalSpace.cs`:
- Properties: `TenantId`, `FacilityId` (VO), `Dimensions` (VO), `Origin` (VO), `SpaceType`, `CellSizeMm`, `RegistrationHash`
- **NINCS `_nodes` navigation** — BE-P3A-01 fix (eager load eltávolítva)
- `static Register(...)` factory → `PhysicalSpaceRegisteredEvent` raiseol
- `ComputeRegistrationHash()` — SHA-256, input: `tenantId|facilityId|W|H|D|X|Y|Z|type|cellSizeMm|timestampMs` (SEC-P3A-05)

### `BvhNode` entity (BE-P3A-03)

`Domain/Entities/BvhNode.cs` — `TenantScopedEntity`:
- `PhysicalSpaceId`, `ParentId?` (null = root), `BoundingBox` (VO), `IsLeaf`, `ElementId?`
- **NINCS `_children` navigation** — BE-P3A-03 fix; gyerekek: `IBvhRepository.GetChildrenAsync()`

### `SpatialElement` entity

`Domain/Entities/SpatialElement.cs` — `TenantScopedEntity`:
- `BvhLeafId`, `FlowEpicId` (NOT NULL), `TradeType`, `ElementType`, `IsArchived`
- `TradeType` enum: `door|window|cabinet|wall|opening|shelf` — **'other' nincs** (SEC-P3A-06)

### `SpatialTaskLink` join entity

`Domain/Entities/SpatialTaskLink.cs` — `TenantScopedEntity`:
- `FlowTaskId`, `SpatialElementId`, `WorkPhase`
- `WorkPhase` enum: `measurement|cutting|edging|assembly|finishing|installation` — **'other' nincs** (SEC-P3A-06)

### Value Objects

`Domain/ValueObjects/BoundingBox.cs` — `sealed record(MinX,MinY,MinZ,MaxX,MaxY,MaxZ)`:
- `Intersects(other)` — 3 tengelyen AABB overlap check (6 összehasonlítás)

`Domain/ValueObjects/SpatialGrid.cs` — BE-P3A-06:
- `From(PhysicalSpace)` factory — `WidthCells = ceil(W/CellSizeMm)`, `DepthCells = ceil(D/CellSizeMm)`
- `GetIntersectingCells(BoundingBox query)` — colMin/colMax/rowMin/rowMax számítás, `GridCell` yield

`Domain/ValueObjects/DimensionVector.cs` — `sealed record(WidthMm, HeightMm, DepthMm)`

`Domain/ValueObjects/Point3D.cs` — `sealed record(X, Y, Z)`

### Domain Events (BE-P3A-08)

`Domain/Events/`:
- `PhysicalSpaceRegisteredEvent` — `(PhysicalSpaceId, TenantId, FacilityId, SpaceType, W, H, D)`
- `SpatialElementRegisteredEvent` — `(ElementId, PhysicalSpaceId, FlowEpicId, TradeType)`
- `SpatialCollisionDetectedEvent` — `(ElementIdA, ElementIdB, IntersectionVolume: BoundingBox)`

### `FlowEpic` extension — 4D state

`ComputeSpatialState()` a meglévő `FlowEpic` aggregátumba:
```
All ClosedDone → ClosedDone
Any InDev      → InDev
Any WaitingForInput → WaitingForInput
egyéb          → BacklogReady
```

---

## PA-02 · BVH Domain Service (Nap 3)

### `IBvhTreeService` — belső interfész (BE-P3A-02)

`Application/Services/IBvhTreeService.cs`:
- `InsertElementAsync(physicalSpaceId, elementBox, flowEpicId, tradeType, elementType, ct)` → `Result<Guid>`
- `QueryIntersectingAsync(physicalSpaceId, query, ct)` → `Result<IReadOnlyList<Guid>>`
- **NEM nyilvános API endpoint** — csak `RegisterSpatialElementCommandHandler` hívja

### `BvhQueryService` — async, repository-driven (BE-P3A-01 + BE-P3A-03 + SEC-P3A-03 + SEC-P3A-07)

`Domain/Services/BvhQueryService.cs`:
- `QueryIntersectingAsync` → `IBvhRepository.GetRootAsync()` → `TraverseBvhAsync()` rekurzió
- `TraverseBvhAsync` paraméterei: `node, query, results, visited: HashSet<Guid>, depth, ct`
- **Cycle guard:** `!visited.Add(node.Id)` → `DomainException("BVH cycle detected")`
- **Depth guard:** `depth > 32` → `DomainException("BVH max depth exceeded")` (SEC-P3A-07)
- Gyerekek: `IBvhRepository.GetChildrenAsync(node.Id, ct)` — nincs navigation property
- `ConfigureAwait(false)` minden async hívásban

---

## PA-03 · Application CQRS (Nap 4–5)

### 5 handler

**`RegisterPhysicalSpaceCommandHandler`:**
- `_facilityRepo.ExistsAsync(cmd.FacilityId, ct)` → `Result.NotFound` ha nem létezik
- `PhysicalSpace.Register(...)` factory hívás
- `_spaceRepo.AddAsync` → `_unitOfWork.CommitAsync` → `_dispatcher.DispatchAsync(space.PopDomainEvents())`
- Golden Rule 4: domain events dispatch COMMIT UTÁN

**`RegisterSpatialElementCommandHandler`:**
- Validálja a PhysicalSpace létezését és TenantId egyezést
- `_bvhTreeService.InsertElementAsync(...)` → BVH fa belső kezelése
- Collision detection: ha intersecting elements találhatók → `SpatialCollisionDetectedEvent`

**`LinkTaskToElementCommandHandler`** (SEC-P3A-02):
- `_taskRepo.GetByIdAsync` + `_elementRepo.GetByIdAsync`
- **Defense-in-depth:** `task.TenantId != element.TenantId` → `Result.Forbidden("Cross-tenant spatial link rejected.")`
- DB trigger szintén véd (TR_SpatialTaskLinks_TenantCheck)

**`GetSpatialSnapshotAtTQueryHandler`** (BE-P3A-05):
- SQL: spec §5.4 snapshot query (DISTINCT ON, AuditEvents join, `try_cast_uuid`, `@at` timestamp filter)
- Returns: `Result<PagedList<SpatialContractDto>>` — `LIMIT @pageSize OFFSET @offset`
- `SpatialContractDto`: `ElementId, BoundingBox, TradeType, FsmStateAtT, ReachedAt` — **ElementType NINCS** (ADR-008)

**`GetSpatialTimelineEventsQueryHandler`:**
- SQL: spec §5.4 playback query (AuditEvents join, ordered ASC)
- Returns: `Result<List<SpatialTimelineEventDto>>`

### FluentValidation validators (BE-P3A-04)

`Application/Commands/Validators/`:
- `RegisterPhysicalSpaceCommandValidator`:
  - `FacilityId.NotEmpty()`
  - `WidthMm/DepthMm`: `> 0`, `<= 100_000`
  - `HeightMm`: `> 0`, `<= 30_000`
  - `CellSizeMm`: `>= 100`, `<= 5_000`
  - `SpaceType.IsInEnum()`
- `RegisterSpatialElementCommandValidator`:
  - `PhysicalSpaceId.NotEmpty()`, `FlowEpicId.NotEmpty()`, `TradeType.IsInEnum()`
  - `ElementType.NotEmpty().MaximumLength(50)`
  - `BoundingBox.MinX < MaxX`, `MinY < MaxY`, `MinZ < MaxZ`
- `LinkTaskToElementCommandValidator`:
  - `FlowTaskId.NotEmpty()`, `SpatialElementId.NotEmpty()`, `WorkPhase.IsInEnum()`

---

## PA-04 · Infrastructure (Nap 6–7)

### IBvhRepository

`Application/Ports/IBvhRepository.cs`:
- `GetRootAsync(physicalSpaceId, ct)` → `BvhNode?`
- `GetChildrenAsync(parentId, ct)` → `IReadOnlyList<BvhNode>`
- `AddAsync(node, ct)`, `UpdateAsync(node, ct)`

### EF Core konfiguráció (BE-P3A-09)

`Infrastructure/Persistence/Configurations/BvhNodeConfiguration.cs`:
- `builder.OwnsOne(n => n.BoundingBox, bb => { ... })` — 6 oszlopra (MinX..MaxZ) — `HasColumnName` explicit
- Self-ref FK: `HasOne<BvhNode>().WithMany().HasForeignKey(n => n.ParentId).OnDelete(Cascade).IsRequired(false)`
- **Nincs `HasMany(_children)`** — navigation property nem létezik

`Infrastructure/Persistence/Configurations/PhysicalSpaceConfiguration.cs`:
- `OwnsOne(s => s.Dimensions)` — WidthMm, HeightMm, DepthMm
- `OwnsOne(s => s.Origin)` — OriginX, OriginY, OriginZ
- `SpaceType`: `HasConversion<string>().HasMaxLength(20)`

### Migrations 0016–0019

**Migration 0016** — `PhysicalSpaces`:
- Tábla az §6.2 DDL szerint (Id, TenantId, FacilityId, összes méret, SpaceType CHECK, CellSizeMm CHECK, RegistrationHash)
- `spaceos_schema_owner` ownership + `spaceos_app` GRANT
- RLS: `ENABLE + FORCE ROW LEVEL SECURITY` + `ps_tenant_isolation` policy
- Trigger: `TR_PhysicalSpaces_CellSizeImmutable` — CellSizeMm változtatás tiltva ha már van BvhNode
- `IX_PhysicalSpaces_TenantId`, `IX_PhysicalSpaces_FacilityId`, `IX_PhysicalSpaces_Active` (partial)
- `suppressTransaction: true`

**Migration 0017** — `BvhNodes`:
- `CK_BvhNodes_NoSelfLoop` CHECK: `ParentId IS NULL OR ParentId != Id`
- `CK_BvhNodes_LeafElement` CHECK: `(IsLeaf=true) OR (ElementId IS NULL)`
- `spaceos_schema_owner` + RLS FORCE + `bvh_tenant_isolation` policy
- Trigger: `TR_BvhNodes_DepthLimit` — recursive CTE depth check (max 32)
- `IX_BvhNodes_PhysicalSpaceId`, `IX_BvhNodes_ParentId` (partial, NOT NULL), `IX_BvhNodes_ElementId` (partial, IsLeaf=true)
- `suppressTransaction: true`

**Migration 0018** — `SpatialElements`:
- TradeType CHECK: `IN ('door','window','cabinet','wall','opening','shelf')` — 'other' NINCS
- `UQ_SpatialElements_BvhLeafId` UNIQUE constraint
- FK visszafelé: `ALTER TABLE "BvhNodes" ADD CONSTRAINT "FK_BvhNodes_SpatialElement"` — SET NULL on delete
- `SpatialContractsView` létrehozása (ElementType NINCS benne — ADR-008)
- `GRANT SELECT ON "SpatialContractsView" TO spaceos_app`
- `IX_SpatialElements_TenantId`, `IX_SpatialElements_FlowEpicId`, `IX_SpatialElements_TradeType` (partial)
- `suppressTransaction: true`

**Migration 0019** — `SpatialTaskLinks`:
- WorkPhase CHECK: `IN ('measurement','cutting','edging','assembly','finishing','installation')`
- `UQ_SpatialTaskLinks_TaskElement` UNIQUE: `(FlowTaskId, SpatialElementId)`
- RLS FORCE + `stl_tenant_isolation` policy
- Trigger: `TR_SpatialTaskLinks_TenantCheck` — cross-tenant reject
- `try_cast_uuid(text)` helper function létrehozása (IMMUTABLE plpgsql)
- `IX_SpatialTaskLinks_FlowTaskId`, `IX_SpatialTaskLinks_SpatialElementId`
- `suppressTransaction: true`

---

## PA-05 · API Endpoints (Nap 8)

**Nyilvános endpointok** (OpenAPI spec-ben szerepelnek):
- `POST /api/spaces` → `RegisterPhysicalSpaceCommandHandler`
- `POST /api/spaces/{id}/elements` → `RegisterSpatialElementCommandHandler`
- `POST /api/elements/{id}/links` → `LinkTaskToElementCommandHandler`
- `GET /api/spaces/{id}/timeline?at={T}&page=1&pageSize=50` → `GetSpatialSnapshotAtTQueryHandler`
- `GET /api/spaces/{id}/timeline/events` → `GetSpatialTimelineEventsQueryHandler`

**BELSŐ — NEM szerepel OpenAPI spec-ben** (BE-P3A-02):
- `InsertBvhNodeCommandHandler` — nincs API route
- `QueryBvhIntersectingQueryHandler` — nincs API route

---

## PA-06 · Tesztek (Nap 9–10)

### Domain unit tesztek (≥20 db)

- `BoundingBox.Intersects()` — 8 eset: hit + miss minden tengelyen (X/Y/Z)
- `BvhQueryService` cycle guard: A→B→A → `DomainException`
- `BvhQueryService` depth guard: 33. szinten → `DomainException`
- `BvhQueryService` compile gate: `node.Children` navigation property NEM LÉTEZIK
- `SpatialGrid.GetIntersectingCells()` — belső / határ / külső / átfedő AABB
- `PhysicalSpace.RegistrationHash` — azonos input → azonos hash; 1 mezőben diff → más hash
- `FlowEpic.ComputeSpatialState()` — 5 kombináció (all done, any InDev, any WaitingForInput, mixed, backlog)
- Parallel safety: két IN_DEV Task átfedő AABB → `SpatialCollisionDetectedEvent`
- Domain events dispatch tesztek (3 event handler)

### Validátor tesztek (≥5 db)

- `RegisterPhysicalSpaceCommandValidator` — valid + boundary invalid (0mm, 100001mm, CellSize<100)
- `RegisterSpatialElementCommandValidator` — MinX >= MaxX → validation error

### Security / integration tesztek (≥10 db)

- Cross-tenant `SpatialTaskLink` INSERT → `cross_tenant_link_rejected`
- BvhNode self-loop INSERT → `CK_BvhNodes_NoSelfLoop` violation
- `CellSizeMm` UPDATE miután BvhNode létezik → `cell_size_immutable`
- `SpatialContractsView` SELECT → `ElementType` kolumna NINCS az eredményben
- Malformed `flowTaskId` → sor kihagyva, nem exception
- `TradeType = 'other'` INSERT → CHECK constraint violation
- Spatial query hamis TenantId → 0 sor
- `tableowner` ellenőrzés: `SELECT tableowner FROM pg_tables WHERE tablename = 'BvhNodes'` → `spaceos_schema_owner`

---

## Definition of Done

### Migration gate-ek
- [ ] 0016 fut — PhysicalSpaces + RLS FORCE + owner + CellSize trigger
- [ ] 0017 fut — BvhNodes + self-ref + NoSelfLoop CHECK + depth trigger
- [ ] 0018 fut — SpatialElements + visszafelé FK + SpatialContractsView + owner
- [ ] 0019 fut — SpatialTaskLinks + cross-tenant trigger + try_cast_uuid
- [ ] `EXPLAIN ANALYZE` minden query endpointon — Seq Scan nincs

### Domain gate-ek
- [ ] `BoundingBox.Intersects()` — 8 eset zöld
- [ ] `BvhQueryService` — nincs `node.Children` (compile gate)
- [ ] `BvhQueryService` cycle + depth guard tesztek zöldek
- [ ] `SpatialGrid.GetIntersectingCells()` — 4 eset zöld
- [ ] `PhysicalSpace.RegistrationHash` — determinisztikus + avalanche
- [ ] `FlowEpic.ComputeSpatialState()` — 5 kombináció zöld
- [ ] Domain events handler tesztek zöldek

### API gate-ek
- [ ] `GetSpatialSnapshotAtT` → `PagedList<SpatialContractDto>` (nem `List<T>`)
- [ ] `InsertBvhNodeCommandHandler` NEM szerepel OpenAPI spec-ben
- [ ] `SpatialContractDto`-ban `ElementType` NINCS

### Security gate-ek
- [ ] `BvhNodes` tableowner → `spaceos_schema_owner`
- [ ] Cross-tenant link → `cross_tenant_link_rejected`
- [ ] BvhNode self-loop → constraint violation
- [ ] CellSizeMm change guard — trigger
- [ ] `TradeType = 'other'` → CHECK violation
- [ ] Hamis TenantId → 0 sor

### Összesített
- [ ] Meglévő **1049 teszt** zöld
- [ ] Phase 3A új tesztek: **≥ 35 db**
- [ ] 0 build warning (xUnit1051 kivételével)
- [ ] `ConfigureAwait(false)` minden production async call-ban
- [ ] `dotnet list package --vulnerable` → 0 high/critical
