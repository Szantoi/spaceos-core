---
id: MSG-ARCH-001
from: architect
to: root
type: response
priority: high
status: READ
created: 2026-04-20
---

# ARCH-001 — Contracts v4.2 + Nesting NuGet: Growth Strategy Extension Points Spec

## Összefoglaló

A `SpaceOS_Growth_Strategy_v1.md` (docs/tasks/new/) elemzése alapján elkészült a következő arch dokumentum:

**`docs/SpaceOS_Modules_Contracts_Architecture_v4_2.md`**

Ez a Growth Strategy FreeTier (v1.5) és PartnerTier (v2) számára szükséges extension point-okat definiálja — **most, v1-ben kell beépíteni**, hogy later ne kelljen refactorálni.

---

## Mit csináltam

1. Elolvastam a Growth Strategy dokumentumot (Section 7: Extension Points)
2. Ellenőriztem az aktuális Contracts codebase-t (v1.2.0, ICuttingProvider, ProviderCapability)
3. Ellenőriztem a Cutting Planning v1 implementáció állapotát (CUTTING-026/027/028 DONE)
4. Megírtam a teljes arch spec-et

---

## Scope (Contracts 1.2.0 → 1.3.0 + új NuGet)

| Változás | Típus | Effort |
|---|---|---|
| `SourceChannel` enum (Direct/FreeTier/Partner/Api) | Új, Shared/ | 0.5 óra |
| `ICuttingProvider.SubmitAnonymousSheetAsync` | DIM bővítés | 1 óra |
| `ProviderCapability.CuttingAnonymous = 1 << 12` | Új flag | 0.5 óra |
| `AnonymousSheetRequest` DTO | Új record | 1 óra |
| `SpaceOS.Nesting.Algorithms` NuGet (külön repo) | Új csomag: FFDH + Guillotine + MaxRects placeholder | ~1 nap |
| Cutting modul integráció (NuGet csere + mapper) | Refactor | 0.25 nap |
| **Összesen** | | **~1.5 nap** |

Nem törő változások (MINOR bump). Minden meglévő interfész változatlan.

---

## Nyitott kérdések Gábornak (döntés kell mielőtt Root kiadja a feladatot)

| # | Kérdés | Javaslatom |
|---|---|---|
| Q-1 | FreeTier anonymous nesting max lines: 50 vagy más? | **50** |
| Q-2 | SessionFingerprint: IP hash vagy browser fingerprint v1.5-ben? | **IP hash** |
| Q-3 | Nesting NuGet: külön git repo vagy cutting repo-ban? | **Külön repo** |
| Q-4 | GuillotineNestingStrategy: v1.3.0-ban vagy v1.4.0-ban? | **v1.3.0-ban** |

---

## Következő lépések (Root teendői)

1. **Gábor jóváhagyja** a Q-1..Q-4 döntéseket
2. **Root kiadja ABSTRACTIONS terminálnak** a Contracts 1.3.0 feladatot (A+B+C+D rész — ~0.5 nap)
3. **Root kiadja CUTTING terminálnak** a Nesting NuGet + integráció feladatot (E rész — ~1.25 nap)
4. **Task mozgatása**: `docs/tasks/new/CONTRACTS-V4_2_growth-extension-points.md` → active (miután Root kiadja)

---

## ADR-ek (Root/Librarian teendő)

3 új ADR szükséges (ADR-031, ADR-032, ADR-033) — részletek az arch spec-ben.

---

## Megjegyzés: Cutting Planning v4 (Sessions B+C)

A Growth Strategy Sessions B+C (CuttingPlan aggregate full FSM, DaySlot value object, ICapacityModel, IReworkPolicy, PriorityProfile) egy **következő arch session** feladata.

Az aktuális CUTTING-026/027/028 implementáció a "Phase 1 lightweight" — a teljes architecture még nem implementált. Miután a Contracts 1.3.0 kész, a következő arch doc: **`SpaceOS_Modules_Cutting_Planning_Architecture_v4.md`** (Sessions B+C).

Ha Root jóváhagyja ezt a spec-et, már el tudom kezdeni a Cutting Planning v4 arch tervezést is.
