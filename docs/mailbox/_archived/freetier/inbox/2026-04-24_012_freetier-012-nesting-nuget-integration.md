---
id: MSG-FREETIER-012
from: root
to: freetier
type: task
priority: high
status: READ
created: 2026-04-24
---

# FREETIER-012 — Nesting NuGet integráció (stub → valós FFDH/Guillotine)

> A `NestingEngineService` jelenleg stub FFDH-t használ. A `SpaceOS.Nesting.Algorithms` NuGet csomag (v1.1.0) a `/opt/spaceos/spaceos-nesting-algorithms/` mappában van — FFDH + Guillotine algoritmusokkal, 32 teszt.
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Használhatsz sub-agent-eket** ha szükséges

---

## Feladat

1. **NuGet csomag referencia** — a `SpaceOS.Nesting.Algorithms` projekt source-ból `ProjectReference` vagy lokális NuGet pack+install:

```bash
# Opció A: ProjectReference (egyszerűbb)
# SpaceOS.FreeTier.Infrastructure.csproj-ba:
# <ProjectReference Include="../../../spaceos-nesting-algorithms/src/SpaceOS.Nesting.Algorithms.csproj" />

# Opció B: NuGet pack
cd /opt/spaceos/spaceos-nesting-algorithms
dotnet pack -c Release -o /opt/spaceos/local-nuget/
# Majd nuget.config-ban local feed hozzáadás
```

2. **NestingEngineService** — stub lecserélése a valós algoritmusra:
   - Input mapping: `NestingInput` → algoritmus input
   - Algoritmus futtatás: FFDH vagy Guillotine (konfigurálható)
   - Output mapping: algoritmus eredmény → `NestingResultDto` (sheets[].placements[])
   - `SemaphoreSlim(10)` guard megtartása (D-18)

3. **Ellenőrizd a NuGet API-t:**
```bash
cd /opt/spaceos/spaceos-nesting-algorithms
grep -r "public.*class\|public.*interface\|public.*static" src/ | grep -v "/obj/" | head -20
```

## Tesztek (+3)

1. Valós FFDH: 1 sheet, 3 part → placements[].length == 3, x/y koordináták validak
2. Yield > 0% (a stub 5.18%-ot adott, a valós jobb kell legyen)
3. Guillotine: ha támogatott, yield összehasonlítás FFDH-val

## Definition of Done

- [ ] `SpaceOS.Nesting.Algorithms` integrálva (ProjectReference vagy NuGet)
- [ ] `NestingEngineService` valós FFDH/Guillotine algoritmust használ
- [ ] Placement koordináták valósak (nem sor-rendben pakolás)
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 176 pass
- [ ] Outbox DONE
