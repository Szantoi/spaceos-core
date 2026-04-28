---
id: MSG-CABINET-010-DONE
from: cabinet
to: root
type: done
priority: high
status: READ
ref: MSG-CABINET-010
created: 2026-04-28
---

# CABINET-010 DONE — Cabinet 0.3 Track C: RuleEngine + Release (UTOLSÓ TRACK)

## Eredmény

### ConstructionRuleEngine Channel<T> párhuzamosítás

| Komponens | Tartalom |
|---|---|
| `ApplyAllAsync(skeleton, context, ct)` | Channel<T> producer-consumer: szabályok párhuzamosan futnak (Task.WhenAll), eredmények szekvenciálisan mergelődnek (ReadAllAsync) |
| `ApplyAllAsync(skeleton, context, tenantStandard, ct)` | TenantStandard-aware overload: RuleSeverityOverrides post-processing az advisories-on |
| `ApplyAll()` | `[Obsolete("Use ApplyAllAsync for parallel execution. ApplyAll will be removed in 1.0.", error: false)]` |
| SEC-CAB-4 | Per-rule timeout + engine timeout megtartva mindkét async overload-ban |
| A11 | Kivételek Critical advisory-ként kerülnek capture-re, soha nem propagálódnak |
| Channel kapacitás | `Math.Max(1, _rules.Count)` — nulla kapacitású channel nem keletkezik |
| Producer safety | `try/finally` garantálja `channel.Writer.Complete()` akár pre-cancelled token esetén is |

### NuGet version bump

| Csomag | Verzió |
|---|---|
| Geometry, Machining, Assembly, Semantics | **0.2.1** (patch — additive) |
| Abstractions, Domain, Construction, Catalog, Application, SpaceOS.Cabinet (meta) | **0.3.0** (minor) |

### Release artifacts

| Fájl | Tartalom |
|---|---|
| `snapshots/reference-0.3.json` | Cabinet 0.3 reference snapshot — TenantStandard + Federation + Channel<T> |
| `docs/adr/ADR-CAB03-001-channel-parallelism.md` | Channel<T> architektúra döntés dokumentálva |

## Definition of Done checklist

- [x] `ApplyAllAsync()` Channel<T> párhuzamos
- [x] `ApplyAllAsync(tenantStandard)` TenantStandard-aware overload
- [x] `ApplyAll()` sync `[Obsolete]`
- [x] NuGet version bump (0.2.1 + 0.3.0)
- [x] Reference snapshot 0.3
- [x] ADR-CAB03-001 doc
- [x] `dotnet build -c Release` 0 error, 0 warning
- [x] `dotnet test` 719 pass (669 előző + 50 új) — teljesíti a ≥719 célt
- [x] net8.0 ÉS net10.0 PASS
- [x] git push ✅ → `6d35998`
- [x] Outbox DONE ✅

## Build & Test

```
Build succeeded. 0 Warning(s), 0 Error(s)
net8.0:  Passed! Failed: 0, Passed: 719, Skipped: 0
net10.0: Passed! Failed: 0, Passed: 719, Skipped: 0
```

## Teszt bontás (új tesztek)

| Terület | Új tesztek |
|---|---|
| ConstructionRuleEngineAsyncTests (Channel<T>) | 24 |
| SmokePipelineTests (full pipeline) | 15 |
| VersionConsistencyTests (NuGet + reflection) | 11 |
| **Összesen új** | **50** |
| **Összesített Cabinet 0.3** | **719** |

## Cabinet 0.3 COMPLETE — összesített teszt bontás

| Track | Terület | Tesztek |
|---|---|---|
| 0.2 baseline | Geometry, Domain, Machining, Construction, Semantics, Catalog, Assembly, Application | 518 |
| Track A | TenantStandard, CatalogEntry Federation, Rating+Flag, SnapshotMigrator 0.2→0.3 | +81 |
| Track B | TenantStandard handlers, Federation handlers, Fingerprint, DI, EF configs | +70 |
| Track C | Channel<T> RuleEngine async, Smoke pipeline, Version consistency | +50 |
| **Összesen** | | **719** |

## Git

- Commit: `6d35998`
- Push: `origin/main` ✅
- 60 fájl módosítva/létrehozva, 5559 sor hozzáadva
