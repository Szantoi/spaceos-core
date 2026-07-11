---
id: MSG-ARCHITECT-002-DONE
from: architect
to: conductor
type: done
priority: high
status: READ
ref: MSG-ARCHITECT-002
created: 2026-06-21
content_hash: 19090d20e2da2029435fc323fcd2893991f7b772368b7966b1efec56e12647e7
---

# DONE: Projekt Koordináció Automatizálás Rendszer Tervezése

## Summary

A v1→v4 architektúra specifikáció elkészült a Project Automation System-hez.

**Output fájl:** `docs/tasks/new/SpaceOS_Project_Automation_Architecture_v4.md`

---

## Teljesített Definition of Done

- [x] **v1: YAML séma specifikáció** — Teljes task chain séma, dependencies, validációs szabályok
- [x] **v2: Auto-dispatch daemon terv** — Chokidar-alapú event-driven architektúra (nem cron), DONE matching logika, BLOCKED kezelés, párhuzamos task támogatás
- [x] **v3: Generator scripts terv** — TypeScript alapú skeleton generator, endpoint scaffolder, inbox template generator, meglévő fájl kezelés (skip/append/overwrite)
- [x] **v4: Implementation guide** — Fájl struktúra, track-ek, DoD checklist, konfiguráció, security considerations

---

## Főbb Döntések

| Kérdés | Döntés | Indoklás |
|--------|--------|----------|
| **Dispatcher trigger** | Chokidar (event-driven) | Azonnali reakció DONE-ra, nem kell cron-ra várni |
| **Generator nyelv** | TypeScript | Típusbiztos, Knowledge Service-be integrálható |
| **Template engine** | Nincs (string interpolation) | Nincs szükség over-engineering-re |
| **Daemon lokáció** | knowledge-service/src/pipeline/ | Meglévő infra (Chokidar, MCP, SSE) |
| **Meglévő fájl kezelés** | Default: SKIP + warning | Biztonságos, nem ír felül véletlenül |

---

## Prioritások (WSJF)

| P# | Feature | Effort | Státusz |
|----|---------|--------|---------|
| P0 | Task Chain YAML + Auto-dispatch | 3 nap | ✅ Specifikálva |
| P1 | Skeleton Generator | 2 nap | ✅ Specifikálva |
| P1 | Endpoint Scaffolder | 1 nap | ✅ Specifikálva |
| P2 | MCP Tools Integration | 2 nap | ✅ Specifikálva |
| P3 | Milestone Auto-planning | 3 nap | 📋 Későbbi iteráció |

---

## Új MCP Tools (6 db)

1. `create_project` — Projekt struktúra létrehozása
2. `get_project_status` — Projekt státusz lekérdezés
3. `dispatch_next` — Következő task manuális dispatch
4. `list_blocked` — BLOCKED task-ok listázása
5. `generate_skeleton` — Modul skeleton generálás
6. `generate_endpoint` — API endpoint scaffolding

---

## Implementation Tracks

| Track | Fájlok | Effort |
|-------|--------|--------|
| **A: YAML Processor** | projectDispatcher.ts, projectMatcher.ts | 1 nap |
| **B: Generator Core** | generateModule.ts, generateEndpoint.ts | 1.5 nap |
| **C: Templates** | templates/*.cs.tmpl | 0.5 nap |
| **D: MCP Integration** | projectTools.ts, mcp.ts | 1 nap |
| **E: Tests** | __tests__/pipeline/*.test.ts | 1 nap |

**Összesen:** ~5 nap implementáció (Backend terminál)

---

## Files Changed

- `docs/tasks/new/SpaceOS_Project_Automation_Architecture_v4.md` (új, ~750 sor)

---

## Next Steps

1. **Conductor review** — Specifikáció elfogadása
2. **Backend task kiadás** — Track A+B+C+D+E implementáció
3. **E2E teszt** — create_project → dispatch → DONE → auto-dispatch ciklus

---

**Megjegyzés:** A specifikáció IMPLEMENTÁCIÓRA KÉSZ státuszú. Minden DoD pont teljesítve, security considerations dokumentálva.
