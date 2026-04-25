---
id: MSG-ARCH-011-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-008
created: 2026-04-25
---

## Összefoglalás

A `SpaceOS_Cabinet_0.1_CoreFoundation_Architecture_v4.md` (105KB, 1899 sor) áttanulmányozva és validálva. A tervdok **implementációra kész** — 4 review-n átment (v1→v4), 28 finding lezárva, nyitott kérdés nincs.

---

## 1. Scope validálás — A1–A11 axiómák

**Verdikt: TELJES LEFEDÉS ✅**

| Axióma | Tartalom | Spec szekció | Status |
|--------|----------|-------------|--------|
| A1 | Affin mátrix mindenhol | §4.1.1 `AffineTransform` VO | ✅ |
| A2 | Két reference frame | §4.1.4 `PartFrame` + `AssemblyFrame` | ✅ |
| A3 | BaseCuboid mint gyökér | §4.2.2 `BaseCuboid` entity | ✅ |
| A4 | Hátfal mint kubus-derivált | §4.2.2 `BackPanel` slot | ✅ |
| A5 | Default joint = face-edge butt | §4.2.4 `Connection.JointType` default | ✅ |
| A6 | Megmunkálás 3 Subject | §4.3 `MachiningSubject` (Plane/Edge/Connection) | ✅ |
| A7 | Szemantikus név derivált | §4.5 `SemanticInferenceService` | ✅ |
| A8 | Platform-független Core | §3.1 NuGet csomagok, nincs UI/CAD/DB dep | ✅ |
| A9 | TenantStandard port | §4.7 `ITenantStandardProvider` interface-only | ✅ |
| A10 | Szelektív újraszámítás | §7.1 `DependencyGraph` Kahn topo-sort | ✅ |
| A11 | Warning, sosem blokk | §4.4.4 `DesignAdvisory` + `AdvisorySeverity` | ✅ |

**Phase A (Cabinet 0.1):** A1–A11 — ez a spec
**Phase B (Cabinet 0.2):** A12–A16 (Catalog, Assembly, FlowEpic, TenantStandard impl, Marketplace) — NEM scope

---

## 2. NuGet struktúra validálás

**Verdikt: HELYES ✅** — jól szervezett, cirkuláris függés nincs.

| Csomag | Framework | Szerep |
|--------|-----------|--------|
| `Geometry` | netstandard2.1 | Max kompatibilitás (mobil, WASM, .NET*) |
| `Abstractions` | netstandard2.1 | Port interfészek |
| `Domain` | net8.0;net10.0 | Skeleton aggregate + entities |
| `Machining` | net8.0;net10.0 | Megmunkálás VO-k |
| `Construction` | net8.0;net10.0 | Szabálymotor + 10 default rule |
| `Semantics` | net8.0;net10.0 | Inferencia + cache |
| `Cabinet` (meta) | net8.0;net10.0 | Mindent behúz |

**Megjegyzés:** A multi-target `net8.0;net10.0` stratégia helyes — a .NET 10 SDK a VPS-en verificálva (2026-04-25). A `global.json` SDK pinning (DB-CAB-1) biztosítja a reprodukálhatóságot.

**A Cabinet NuGet-ek FÜGGETLENEK a meglévő SpaceOS stack-től** — nincs Kernel, Joinery, Cutting, EF Core, MediatR handler dependency. Ez tiszta domain library.

---

## 3. Repo setup terv

| Lépés | Parancs / feladat |
|-------|-------------------|
| Repo init | `spaceos-modules-cabinet` — VPS authoritative, `develop` branch |
| Solution | `dotnet new sln` + 7 csproj (Geometry, Abstractions, Domain, Machining, Construction, Semantics, Cabinet meta) + 1 test projekt |
| global.json | `{ "sdk": { "version": "10.0.203", "rollForward": "latestFeature" } }` |
| CLAUDE.md | Cabinet terminál szabályok (spec §16.2 agent utasítás alapján) |
| .claude/ | agents, skills, settings (SpaceOS standard minta) |
| CI | GitHub Actions: build + test (net8.0 + net10.0) minden push-ra; pack + publish tag-re |
| NuGet config | GitHub Packages feed (cabinetbilder-autocad fogyasztó) |
| VPS systemd | **NEM KELL** — a Cabinet pure NuGet library, nincs runtime service |

---

## 4. Implementációs sorrend javaslat

A spec §16.1 már részletes napi ütemtervet ad (21.75 nap, 8 track). Az ütemterv **konzisztens és helyes** — az alábbiakat javaslom a terminál inbox-hoz:

### Első inbox: Track A + B (Repo skeleton + Geometry)

**Scope: 3.5 nap**

1. Repo scaffold (sln, 7 csproj, global.json, .gitignore, CLAUDE.md, CI workflow)
2. NuGet csomag-konfiguráció (Description, metadata, multi-target)
3. `Geometry` csomag teljes implementálása:
   - `Vector3`, `AffineTransform` (SEC-CAB-2: NaN/Infinity guard)
   - `PartFrame`, `AssemblyFrame`, `PartDimension`, `AssemblyDimension` (SEC-CAB-3: MaxXxx)
   - `GeometryConstants` (BE-CAB-7: epsilon config)
   - `CabinetJsonOptions.Strict` (DB-CAB-8)
   - 50 unit teszt mindkét cél-frameworken

**DoD:** `dotnet build -c Release` 0 warning, `dotnet test -f net8.0` + `dotnet test -f net10.0` pass.

### Második inbox: Track C + D (Abstractions + Domain)

**Scope: 5.5 nap** (Nap 4–9.5)

- `Abstractions`: `ITenantStandardProvider`, `ISnapshotMigrator`, `IGeometryProjector`, `IPartCatalog`
- `Domain`: `Skeleton` aggregate, `BaseCuboid`, `Part` (SEC-CAB-1: internal ctor), `Connection`
- `SkeletonSnapshot` + `FromJson`/`FromSnapshot` (SEC-CAB-6: post-deserialize validation)
- `DependencyGraph` Kahn topo-sort (SEC-CAB-7: cycle detection)
- `IDomainEvent` + `SequenceNumber` (DB-CAB-7)
- 60 + 15 + 10 teszt + 5 sample snapshot

### Harmadik inbox: Track E + F (Machining + Construction)

**Scope: 4.5 nap** (Nap 8–11)

- `Machining`: `MachiningFeature`, `MachiningSubject`, `HardwareReference`
- `Construction`: `ConstructionRuleEngine` + 10 default rule + SEC-CAB-4 timeout/cap
- 25 + 40 teszt + Advisory privacy regex teszt

### Negyedik inbox: Track G + H (Semantics + Cross-cutting + Release)

**Scope: 8.25 nap** (Nap 12–21.75)

- `Semantics`: `SemanticInferenceService` + cache (DB-CAB-6: lockless ConcurrentDictionary + LRU)
- Cross-cutting: determinism teszt, smoke teszt, threat model doc, deprecation policy, BenchmarkDotNet
- Meta package, multi-target verifikáció, GitHub Actions tag teszt
- Final integration: 230+ teszt zöld
- Release: `v0.1.0-alpha.1` tag

---

## 5. Effort validálás

**Verdikt: 21.75 nap REÁLIS ✅**

| Komponens | Nap | Megjegyzés |
|-----------|-----|-----------|
| Base domain (v1) | 12.0 | 7 NuGet csomag + 230 teszt — reális egy komplex domain library-hez |
| v2 review delta (DB-CAB) | 3.75 | Schema versioning, migration policy, determinism — önálló tasks |
| v3 security delta (SEC-CAB) | 3.5 | 1 CRITICAL + 5 HIGH — biztonsági tesztek is benne |
| v4 backend delta (BE-CAB) | 2.5 | Algorithmic policy, thread-safety doc, BenchmarkDotNet |
| **Total** | **21.75** | |

**Kockázat:** A becslés agent-szinten ~25-30 napra nyúlhat (a spec §16.3-ban is megemlítve). A Track-bontás viszont párhuzamosítható — több agent esetén ~14 nap.

---

## 6. Cross-module dependency

**Verdikt: NINCS CROSS-MODULE DEPENDENCY ✅**

A Cabinet 0.1 **teljesen független** a meglévő SpaceOS moduloktól:

| Modul | Kapcsolat Cabinet-tel |
|-------|----------------------|
| Kernel | Nincs — Cabinet nem használ EF Core, JWT, RLS |
| Joinery | Nincs — Cabinet nem ismeri a DoorOrder-t |
| Cutting | Nincs — Cabinet nem ismeri a CuttingPlan-t |
| Abstractions (SpaceOS) | Nincs — Cabinet saját Abstractions csomagja van |
| Inventory | Nincs — Cabinet nem kommunikál készlettel |

**Egyetlen integráció:** `cabinetbilder-autocad` (Windows, .NET 10) — de ez a Cabinet 0.1 RELEASE UTÁN jön, ~1.5 napos feladat (spec §16.4).

**Jövőbeli integráció (Cabinet 0.2+):** A `SkeletonSnapshot` JSON a Kernel `IParametricProduct` interfészen keresztül kerül majd a SpaceOS pipeline-ba — de ez a Cabinet 0.2 design session scope-ja.

---

## 7. Észrevételek (nem blokkolók)

| # | Észrevétel | Hatás |
|---|-----------|-------|
| 1 | A .NET 10 SDK telepítve a VPS-en (spec megjegyzi: "verifikálva 2026-04-25") — de a meglévő .NET 8 service-ek nem érintettek (parallel install) | Nincs kockázat |
| 2 | A spec §16.2 agent utasítás tartalmazza a `git push`-t — a Cabinet reponak léteznie kell a VPS-en mielőtt az agent elindul | Repo init a Track A Nap 1 feladata |
| 3 | A 230+ teszt target ambiciózus de reális — a Cutting modul 284 tesztnél tart hasonló komplexitással | Figyelni kell a teszt-minőségre, ne legyen "teszt-infláció" |
| 4 | A `MaxPartsPerSkeleton = 500` a Doorstar pilot-hoz elegendő (tipikus szekrény 30-60 Part), de gardróbsor-gyártóknak szűk lehet | Tenant config Cabinet 0.2-ben jön |
