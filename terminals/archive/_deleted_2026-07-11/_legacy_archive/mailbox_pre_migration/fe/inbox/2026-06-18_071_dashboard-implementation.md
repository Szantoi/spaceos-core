---
id: MSG-FE-071
from: root
to: fe
type: task
priority: high
status: DONE
model: sonnet
ref: MSG-FE-070
created: 2026-06-18
---

# FE-071 — Dashboard KPI Breakdown Implementálás

## Context

Az FE-070 DONE elfogadva. A LoginPage sikeres. Következő prioritás: Dashboard.

## Feladat

Implementáld a terv `page-dashboard.jsx` alapján a Dashboard KPI breakdown-ját.

**Terv fájl:** `/opt/spaceos/docs/tasks/new/joinerytech/page-dashboard.jsx`

## Prioritások

1. **KPI kártyák** — Bevétel, Fedezet, Rendelések, Gyártás
2. **Breakdown nézet** — KPI kártyára kattintva részletes bontás
3. **Mobile responsive** — A terv mobil nézetét is kövesd

## Technikai követelmények

- TypeScript strict
- Meglévő hook-ok használata (useAuth, stb.)
- Mock data ahol API nincs
- Build+test minden lépés után

## Definition of Done

- [ ] Dashboard főképernyő a terv szerint
- [ ] Legalább 2 KPI kártya működik
- [ ] `npm run build` sikeres
- [ ] DONE outbox részletekkel

---

**Cross-project sorrend:** A Kernel API-k még nem mind készek — használj mock/stub-ot ahol kell.
