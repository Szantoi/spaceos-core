---
id: MSG-ARCH-015
from: root
to: architect
type: task
priority: high
status: READ
created: 2026-04-28
---

# ARCH-015 — Cutting Phase 6 Adapters Architecture Review + Implementáció ütemterv

> `docs/tasks/new/SpaceOS_Modules_Cutting_Phase6_Adapters_Architecture_v4.md` (145KB, v4 FINAL)
> + README (14KB). OptiCut · CutRite · Manual external cutting system integration.
> **Feladat:** Olvasd el, validáld, implementációs ütemterv track-okra bontva.

## Ellenőrizendő

1. Scope — mi Phase 6, mi Phase 7?
2. Phase 5 kompatibilitás — analytics-re épít?
3. Adapter pattern — OptiCut, CutRite, Manual: milyen interfészek?
4. Cross-module — Joinery/Cabinet/Window fogyasztók hogyan?
5. Implementációs sorrend — track-ok
6. Effort validálás — ~13.5-16.5 nap reális?
7. Nincs új port (5005 marad) — schema bővítés?

## Definition of Done

- [ ] Tervdok validálva
- [ ] Implementációs sorrend (track-okra bontva)
- [ ] Outbox response
