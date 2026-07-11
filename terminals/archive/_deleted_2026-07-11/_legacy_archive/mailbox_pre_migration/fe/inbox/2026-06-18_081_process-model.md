---
id: MSG-FE-081
from: root
to: fe
type: task
priority: high
status: READ
model: sonnet
ref: MSG-FE-080
created: 2026-06-18
---

# FE-081 — Process Model (folyamat-motor) implementálás

## Feladat

Implementáld a folyamat-motor modellt és konstansokat a `page-process-model.jsx` terv alapján.

## Scope

A folyamat-motor alapjai:
1. **PROC_PALETTE** — 8 szín a folyamat vizualizációhoz
2. **PROC_ACTORS** — 6 szereplő (manufacturer, supplier, installer, designer, dealer, client)
3. **procActor(k)** — szereplő meta helper (label, icon, tint)
4. **SEG_META** — szegmens típusok (step, branch, parallel, loop)
5. **Segment factory-k** — newStep, newBranch, newParallel, newLoop
6. **Rekurzív flow helpers** — mapFlow, updateSeg, removeSeg, insertSeg, moveSeg
7. **Branch/Parallel kezelők** — addPath, removePath, addLane, removeLane
8. **allSteps(flow)** — összes step szegmens (loop target-ekhez)

## Meglévő alapok

Ellenőrizd, hogy a `FACILITIES` konstans létezik-e (a telephelyekhez).

## DoD

- [ ] `PROC_PALETTE` konstans (8 szín)
- [ ] `PROC_ACTORS` és `procActor` helper
- [ ] `SEG_META` szegmens típus metaadatok
- [ ] `segId(prefix)` egyedi ID generátor
- [ ] `newStep/newBranch/newParallel/newLoop` factory-k
- [ ] `mapFlow(flow, fn)` rekurzív helper
- [ ] `updateSeg/removeSeg` id-alapú mutátorok
- [ ] `insertSeg(flow, container, seg, afterId)` beszúró
- [ ] `moveSeg(flow, id, dir)` fel/le mozgató
- [ ] `addPath/removePath` branch kezelők
- [ ] `addLane/removeLane` parallel kezelők
- [ ] `allSteps(flow)` gyűjtő
- [ ] Minden exportálva `window`-ra
- [ ] `npm run build` sikeres
- [ ] DONE outbox

## Ref

Terv: `docs/tasks/new/joinerytech/page-process-model.jsx` (126 sor)

---

Timestamp: 2026-06-18 06:05 UTC
