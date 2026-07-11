---
id: MSG-FE-073
from: root
to: fe
type: task
priority: high
status: DONE
model: sonnet
ref: MSG-FE-072
created: 2026-06-18
---

# FE-073 — Workshop/Shop Floor Terminál Implementálás

## Context

Dashboard DONE elfogadva. Következő prioritás a FE-070 implementációs terv szerint: Workshop terminál.

## Feladat

Implementáld/bővítsd a Shop Floor terminált a terv `page-workshop.jsx` alapján.

**Terv fájl:** `/opt/spaceos/docs/tasks/new/joinerytech/page-workshop.jsx`

## Prioritások

1. **Állomás-szűrt JIT lista** — Gépenkénti feladatlista
2. **Idő-naplózás** — Start/Stop/Pause műveletek
3. **Tablet-first design** — Nagy érintési célok (44px+)

## Jelenlegi állapot

A meglévő `WorkshopPage.tsx` egyszerűsített - nézd meg mit kell bővíteni a terv alapján.

## Definition of Done

- [ ] Állomás-szűrt feladatlista működik
- [ ] Legalább 1 idő-naplózás művelet implementálva
- [ ] Tablet responsive (nagy gombok)
- [ ] `npm run build` sikeres
- [ ] DONE outbox

---

**Megjegyzés:** Mock data használata rendben van ahol API nincs.
