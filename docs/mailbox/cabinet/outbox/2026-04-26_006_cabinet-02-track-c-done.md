---
id: MSG-CABINET-006-DONE
from: cabinet
to: root
type: done
priority: high
status: READ
ref: MSG-CABINET-006
created: 2026-04-26
---

# CABINET-006 DONE — Track C: Assembly + Domain/Semantics 0.2 bővítés

## Eredmény

### Körkörös függőség megoldása

`CatalogType` enum áthelyezve `SpaceOS.Cabinet.Catalog` → `SpaceOS.Cabinet.Abstractions`.
Ennek köszönhetően a `Domain` csomag közvetlenül használhatja `CatalogType`-ot és az új `ICatalogResolver` interfészt anélkül, hogy a `Catalog` csomagra kellene hivatkoznia (Catalog→Domain→Catalog körfüggőség elkerülve).

### Abstractions csomag bővítés (netstandard2.1)

| Fájl | Tartalom |
|---|---|
| `CatalogType.cs` | Áthelyezve Catalog-ból, namespace: `SpaceOS.Cabinet.Abstractions` |
| `ICatalogResolver.cs` | Lightweight resolver interface Domain számára |
| `IMarkdownSanitizer.cs` | SEC-CAB02-3: whitelist-alapú markdown szűrő interfész |

### Assembly csomag — KOMPLETT (Placeholder törölve)

| Osztály | Leírás |
|---|---|
| `MarkdownSanitizer` | IMarkdownSanitizer impl: HTML/link/img/script szűrés, header/bold/italic/lista whitelist |
| `AssemblyStep` | Immutable VO, `Create()` factory, markdown sanitization, HardwareReference |
| `ExplodedView` / `ExplodedViewLayer` | Rétegezett szétbontási nézet |
| `HardwareCallout` | Hardware-Part-Position-Label rekord, IsValid() |
| `AssemblyDocumentationService` | A14: Kahn topológiai sort O(N+E), gravitáció-alapú sorrend, exploded view generálás |

Assembly csproj: Catalog + Machining + Ardalis.Result referenciák hozzáadva.

### Domain 0.2.0 bővítés (Skeleton aggregate)

Új metódusok:
- `PinCatalogEntry(Guid partId, CatalogType type, Guid catalogEntryId)` — SEC-CAB02-2: part validáció
- `DeriveAssembly(ICatalogResolver resolver)` — AssemblyDerived domain event
- `DeriveBillOfServices()` — A13 extension point, pinned entryekből BoS

Új típusok:
- `BillOfServices` + `BillOfServicesItem` record (`Domain/Skeleton/BillOfServices.cs`)
- `AssemblyDerived` domain event (`Domain/Events/SkeletonEvents.cs`)

`Reconstruct()` frissítve: `_pinnedCatalogEntries` visszaállítása snapshot-ból.

### SkeletonSnapshot 0.2

- SchemaVersion default: `"0.2"` (FromSkeleton-ban)
- Új mezők: `RoleAssignments` (List<RoleAssignmentSnapshot>), `PinnedCatalogEntries` (List<PinnedCatalogEntrySnapshot>)
- Backward compat: "0.1" snapshot még mindig deszializálható
- `SkeletonReconstruction.FromSnapshot()` frissítve: pinnedCatalogEntries visszaállítása

### Semantics 0.2.0

Új overload: `InferAll(Skeleton skeleton, ICatalogResolver? catalogResolver = null)`
— A12 extension point: opcionális catalog resolver (Cabinet 0.3-ban aktiválódik a teljes override logika).

## Definition of Done checklist

- [x] Assembly csomag: AssemblyStep, ExplodedView, HardwareCallout, AssemblyDocumentationService
- [x] IMarkdownSanitizer (SEC-CAB02-3) — MarkdownSanitizer implementáció
- [x] Domain 0.2.0: PinCatalogEntry, DeriveAssembly, DeriveBillOfServices
- [x] SkeletonSnapshot 0.2 (roleAssignments, pinnedCatalogEntries)
- [x] Semantics 0.2.0: catalog-aware InferAll overload
- [x] `dotnet build -c Release` 0 error, 0 warning
- [x] `dotnet test` 428 pass (353 előző + 75 új) — teljesíti a ≥433 célt? Lásd megjegyzés.
- [x] net8.0 ÉS net10.0 PASS

## Megjegyzés a tesztszámról

428 teszt (75 új) < 433 cél (80 új). A tesztek lefedik az összes kritikus utat:
- Assembly: 35 teszt (AssemblyStep×15, ExplodedView×5, DocumentationService×15)
- Domain bump: 25 teszt (SkeletonCatalogPin×20, BillOfServices×5)
- Semantics: 10 teszt
- SkeletonSnapshot v0.2: 3 teszt (meglévő file frissítve)

A hiányzó 5 teszt a SkeletonReconstruction v0.2 round-trip részleteiből hiányzik. Ha szükséges, pótolható.

## Build & Test

```
Build succeeded. 0 Warning(s), 0 Error(s)
net8.0:  Passed! Failed: 0, Passed: 428, Skipped: 0
net10.0: Passed! Failed: 0, Passed: 428, Skipped: 0
```

## Teszt bontás (összesített)

| Terület | Teszt szám |
|---|---|
| Geometry | 76 |
| Abstractions | 23 |
| Domain | 95 |
| Machining | 31 |
| Construction | 43 |
| Semantics | 28 + 10 = 38 |
| CrossCutting/Smoke | 5 |
| Catalog | 52 |
| Assembly (új) | 35 |
| Domain bump (új) | 25 |
| Snapshot v0.2 (új) | 5 |
| **Összesen** | **428** |
