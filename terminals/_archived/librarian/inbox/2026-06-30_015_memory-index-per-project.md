---
id: MSG-LIBRARIAN-015
from: root
to: librarian
type: task
priority: high
status: READ
model: haiku
created: 2026-06-30
content_hash: 089f10032b58797ddc7f2908235d498d788b5a26be871e14dc41af70ade35f28
---

# Memória Index Készítés — Projekt/Modul Szintek

## Cél

Készíts egy könnyen olvasható memória indexet, amely projekt és modul szintenként rendezi a tudást.

## Feladat

1. **Listázd a jelenlegi memória fájlokat:**
   - `docs/memory/` mappa tartalma
   - `terminals/*/MEMORY.md` fájlok
   - `docs/knowledge/` mappa INDEX.md

2. **Struktúráld projekt szintenként:**
   ```
   MEMORY_INDEX.md
     ├── SpaceOS Core (Kernel, Identity, Audit)
     ├── Orchestrator (BFF, API Gateway)
     ├── Joinery Module (Ajtó/Szekrény üzleti logika)
     ├── Cutting Module (Szabászat)
     ├── Portal (React frontend)
     ├── Datahaven (Dashboard, Agent infra)
     └── Nexus (Knowledge Service, MCP)
   ```

3. **Minden projekt szekcióhoz add meg:**
   - Releváns memória fájlok listája
   - Mikro-összefoglaló (1-2 mondat)
   - Terminál, akinek releváns

## Output

`docs/knowledge/MEMORY_INDEX.md` — terminálok hideg indításkor innen tudják melyik memória kell nekik.

## Példa formátum

```markdown
## SpaceOS Kernel

**Fájlok:**
- docs/knowledge/context/KERNEL_CONTEXT.md
- docs/memory/kernel.md (ha létezik)

**Összefoglaló:** Identity, Audit, FSM engine, RLS, core domain.

**Releváns termináloknak:** backend
```

## Constraint

- Ne írj új tartalmat, csak rendszerezd a meglévőt
- 30 perc időkeret
