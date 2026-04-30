---
id: MSG-LIB-002
from: root
to: librarian
type: task
priority: high
status: READ
ref: MSG-LIB-001
created: 2026-04-20
---

# LIB-002 — Tudásbázis frissítés: Sprint 7-9 + Cutting Session B

> **Skill:** `/spaceos-librarian` szerint dolgozz
> **Előző:** LIB-001 (2026-04-17) — első szintézis kész
> **Azóta eltelt:** ~3 nap, 40+ outbox üzenet feldolgozatlan

---

## Probléma

A context fájlok és katalógusok elavultak. A valós állapot vs. tudásbázis eltérések:

| Fájl | Elavult | Valós |
|---|---|---|
| `context/CUTTING_CONTEXT.md` | 77/47/42 pass | 195 Cutting / 154 Inventory / 53 Procurement |
| `context/ORCH_CONTEXT.md` | 183 pass | 219 pass |
| `context/PORTAL_CONTEXT.md` | 291 pass | 323 pass |
| `context/KERNEL_CONTEXT.md` | 1110+ pass | 1138 pass |
| `context/JOINERY_CONTEXT.md` | 172 pass | 249 pass |
| `context/E2E_CONTEXT.md` | 214/214 | 266/266 · 56 fájl |
| `architecture/MODULE_BOUNDARIES.md` | Contracts 1.1.0 | 1.3.0 (SourceChannel, AnonymousSheetRequest, CuttingAnonymous) |
| `architecture/ADR_CATALOGUE.md` | ADR-030-ig | ADR-031..037 szükséges (Architect spec-ekből) |

## Frissítendő context fájlok

Minden context fájlban frissítsd a teszt számokat, commit hash-eket, és az aktuális feladatok státuszát. Forrás: `docs/Codebase_Status.md` + legfrissebb outbox üzenetek.

Különös figyelmet igénylő területek:

### Cutting context
- Új modul struktúra: `spaceos-modules-cutting` + `spaceos-modules-inventory` + `spaceos-modules-procurement` külön repók
- Új NuGet: `SpaceOS.Nesting.Algorithms` 1.0.0 (`/opt/spaceos/spaceos-nesting-algorithms/`)
- `ICuttingEventPublisher` HTTP cross-service pattern (CUTTING-028)
- `CuttingPlanStatus` typed enum (CUTTING-031, Session B folyamatban)
- Session B sorrend: CUTTING-031..036 (spec: `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md`)

### Architect + új terminálok
- Új terminálok: `spaceos-architect/` (konzultatív, persistent) + `spaceos-librarian/` (tudásbázis)
- Új arch spec-ek: `docs/architecture/SpaceOS_Modules_Contracts_Architecture_v4_2.md` + `SpaceOS_Modules_Cutting_Planning_Architecture_v4.md`

### ADR-ek
Az Architect spec-ekből 7 új ADR szükséges (ADR-031..037). Forrás:
- `docs/architecture/SpaceOS_Modules_Contracts_Architecture_v4_2.md`
- `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md`

Vedd fel a katalógusba placeholder-ként ha még nem íródtak meg részletesen.

## Definition of Done

- [ ] Minden context fájl teszt számai naprakészek
- [ ] `MODULE_BOUNDARIES.md` Contracts 1.3.0-ra frissítve
- [ ] `ADR_CATALOGUE.md` ADR-031..037 felvéve (legalább placeholder)
- [ ] `INDEX.md` frissítve (létrehozás dátuma, összefoglaló sorok)
- [ ] `PROCESSED_LOG.md` frissítve az új üzenetekkel
- [ ] Outbox DONE üzenet küldve
