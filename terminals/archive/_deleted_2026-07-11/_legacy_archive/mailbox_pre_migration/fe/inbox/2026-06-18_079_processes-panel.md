---
id: MSG-FE-079
from: root
to: fe
type: task
priority: high
status: READ
model: sonnet
ref: MSG-FE-077
created: 2026-06-18
---

# FE-079 — ProcessesPanel (Munkafolyamat) implementálás

## Feladat

Implementáld a Beállítások → Munkafolyamat → Folyamatok panelt a `page-process-panel.jsx` terv alapján.

## Scope

A munkafolyamat-kezelő rendszer:
1. **Kirendeltség-választó** — Facility selector tabs
2. **Folyamat kártyák** — Grid layout, minden kártya stats chip-ekkel
3. **CRUD műveletek** — Új, duplikálás, törlés
4. **ProcessEditor indítása** — A teljes folyam-szerkesztő megnyitása

## Meglévő alapok

Ellenőrizd, hogy léteznek-e:
- `sim.processes[]` tömb
- `FACILITIES` konstans
- `addProcess`, `duplicateProcess`, `removeProcess` akciók
- `processStepStats(proc)` helper

## DoD

- [ ] `ProcessesPanel` komponens
- [ ] Facility selector tabs működnek
- [ ] Process cards grid (3 oszlopos lg)
- [ ] Stats Chip komponens (fázis/lépés/elágazás/párhuzam/ciklus/külső)
- [ ] "Új folyamat" és "Üres folyamat" gombok
- [ ] Duplikálás és törlés (confirm)
- [ ] ProcessEditor modal/SlideOver indítás (ha létezik)
- [ ] `npm run build` sikeres
- [ ] DONE outbox

## Ref

Terv: `docs/tasks/new/joinerytech/page-process-panel.jsx` (90 sor)

---

Timestamp: 2026-06-18 06:32 UTC
