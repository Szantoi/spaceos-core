---
id: MSG-FE-068
from: root
to: fe
type: task
priority: critical
status: DONE
model: sonnet
created: 2026-06-17
---

# FE-068 — JoineryTech UI Design Implementálás

## Context

A `/opt/spaceos/docs/tasks/new/joinerytech/` mappában van a teljes UI terv:
- **CLAUDE.md** - projekt kontextus, FSM-ek, státuszkezelés
- **PROJECT_STATUS.md** - részletes állapot
- **page-*.jsx** - React komponensek (prototípus)
- **data-*.js** - adat struktúrák
- **screenshots/** - képernyőképek

A cél: ezt a tervet alkalmazni a `frontend/joinerytech-portal/` React projektre.

## Feladat

1. **Olvasd be a tervet:**
   - `/opt/spaceos/docs/tasks/new/joinerytech/CLAUDE.md`
   - `/opt/spaceos/docs/tasks/new/joinerytech/PROJECT_STATUS.md`

2. **Elemezd a különbségeket:**
   - Jelenlegi frontend: `frontend/joinerytech-portal/src/`
   - Terv komponensei: `docs/tasks/new/joinerytech/page-*.jsx`

3. **Készíts tervet:**
   - Melyik komponenseket kell átvenni/adaptálni
   - Milyen sorrendben (prioritás: login → dashboard → fő workflow)

4. **Kezdd az implementálást:**
   - Lépésenként, build+test minden lépésnél
   - `npm run build` után a dist automatikusan frissül a https://joinerytech.hu/ oldalon

## Fontos

- A backend API-k még nem mind készek - használj mock/stub-ot ahol kell
- A Keycloak auth működik
- A https://joinerytech.hu/ élő oldal - óvatosan a változtatásokkal

## Definition of Done

- [ ] UI terv elemzése kész
- [ ] Implementációs terv dokumentálva
- [ ] Legalább 1 fő komponens implementálva és deployolva
- [ ] DONE outbox a részletekkel
