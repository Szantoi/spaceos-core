---
id: MSG-CABINET-010
from: root
to: cabinet
type: task
priority: high
status: READ
ref: MSG-CABINET-009-DONE
created: 2026-04-28
---

# CABINET-010 — Cabinet 0.3 Track C: RuleEngine + BenchmarkDotNet + Release (Day 9–13)

> **Tervdok:** `/opt/spaceos/docs/tasks/active/SpaceOS_Cabinet_0_3_Federation_Architecture_v4.md`
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** CABINET-009 ✅ (669 teszt, Track B Application)
> **Ez az UTOLSÓ track — Cabinet 0.3 itt zárul!**
> **Használhatsz sub-agent-eket** ha szükséges

---

## Day 9–10: ConstructionRuleEngine Channel<T> párhuzamosítás

A tervdok szerint a `ConstructionRuleEngine.ApplyAllAsync()` az eddigi szekvenciális `foreach` helyett `Channel<T>` producer-consumer mintát használ:

```csharp
// Producer: szabályok párhuzamos futtatása
// Consumer: eredmények szekvenciális összefésülése
// BE-01: 30% teljesítményjavulás target (BenchmarkDotNet mérés)
// SEC-CAB-4: per-rule timeout + engine timeout megtartva
```

### Implementáció

1. `ApplyAllAsync()` — `Channel<ConstructionRuleResult>.CreateBounded(capacity)` + `Task.WhenAll` producer
2. `ApplyAll()` sync overload → `[Obsolete]` (BE-03 breaking change)
3. TenantStandard-aware: a rule engine a `TenantStandard` threshold-öket használja (ha van)

---

## Day 11: BenchmarkDotNet performance baseline

```csharp
// tests/SpaceOS.Cabinet.Benchmarks/
// ConstructionRuleEngineBenchmark.cs
// - Baseline: sequential ApplyAll (0.2 verzió)
// - Target: Channel<T> ApplyAllAsync (0.3 verzió)
// - Cél: ≥30% gyorsabb 500 Part Skeleton-on
```

---

## Day 12: NuGet version bump + Roslyn analyzer

- Geometry, Machining, Assembly, Semantics → **0.2.1** patch (additive)
- Abstractions, Domain, Construction, Catalog, Application, meta → **0.3.0** minor
- Roslyn analyzer: `SimilarityFingerprint` setter private enforced compile-time (SEC-02)
- ADR doc: `docs/adr/ADR-CAB03-001-channel-parallelism.md`

---

## Day 13: Smoke + Reference snapshot + Release prep

- Full pipeline smoke: Create Skeleton → TenantStandard → Construction → Federation → Snapshot 0.3
- Reference snapshot: `snapshots/reference-0.3.json`
- Determinism teszt: 10 run same hash
- Git push + tag candidate `v0.3.0-alpha.1`

---

## Tesztek (50+)

**RuleEngine Channel (20+):** parallel execution, timeout, bounded channel, TenantStandard-aware
**Benchmark (5+):** baseline vs channel, ≥30% improvement assertion
**NuGet/Roslyn (10+):** version consistency, fingerprint setter blocked
**Smoke (15+):** full pipeline, determinism, snapshot round-trip

## Definition of Done

- [ ] `ApplyAllAsync()` Channel<T> párhuzamos
- [ ] `ApplyAll()` sync `[Obsolete]`
- [ ] TenantStandard-aware rule engine
- [ ] BenchmarkDotNet: ≥30% gyorsabb
- [ ] NuGet version bump (0.2.1 + 0.3.0)
- [ ] Reference snapshot 0.3
- [ ] `dotnet build -c Release` 0 error, 0 warning
- [ ] `dotnet test` ≥ 719 pass (669 + 50 új)
- [ ] net8.0 ÉS net10.0 PASS
- [ ] git push
- [ ] Outbox DONE
