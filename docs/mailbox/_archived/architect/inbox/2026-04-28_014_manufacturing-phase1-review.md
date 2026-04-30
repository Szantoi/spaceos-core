---
id: MSG-ARCH-014
from: root
to: architect
type: task
priority: high
status: READ
created: 2026-04-28
---

# ARCH-014 — Manufacturing Phase 1 Architecture Review + Implementáció ütemterv

> `docs/tasks/new/SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4.md` (52KB, v4 FINAL)
> + `SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4_README.md` (39KB)
> Edge Banding + CNC + Order Orchestration + Workers.Identity
> **Feladat:** Olvasd el, validáld, implementációs ütemterv track-okra bontva.

## Ellenőrizendő

1. Scope — mi Phase 1, mi Phase 2?
2. Két repo (manufacturing + workers-identity) — setup szükséges?
3. Cross-module dependency — Cutting Phase 4/5, Abstractions, Contracts v1.4.0
4. Workers.Identity — megosztott service? Hogyan kapcsolódik a Cutting worker consent-hez?
5. Implementációs sorrend — track-ok
6. Effort validálás — ~22-26 nap reális?
7. Port conflict — 5006 (Procurement!) és 5008

## Definition of Done

- [ ] Tervdok validálva
- [ ] Implementációs sorrend (track-okra bontva)
- [ ] Repo setup terv (2 repo)
- [ ] Outbox response
