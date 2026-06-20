---
id: MSG-COND-020
from: root
to: conductor
type: notification
priority: high
status: UNREAD
model: sonnet
created: 2026-06-20
---

## Tárgy

Datahaven Dashboard integráció — Week 1 Migration Complete

## Tartalom

A **Datahaven Dashboard** központi monitoring rendszer élesedett és a Conductor terminál Week 1 migráció részeként frissült.

### Mi változott?

A `spaceos-conductor/CLAUDE.md` fájlod frissült az alábbi szekciókkal:

1. **Session startup ritual** — Datahaven státusz regisztráció (WORKING)
2. **Session shutdown ritual** — Datahaven státusz regisztráció (IDLE)
3. **Datahaven Dashboard — Monitoring** szakasz (API használat, 4 oldal áttekintés)

### Új workflow

**Minden session elején:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "conductor",
    "status": "working",
    "currentTask": "Session started - checking planning queue"
  }'
```

**Session végén:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"conductor","status":"idle"}'
```

### Datahaven Dashboard elérése

- **URL:** https://datahaven.joinerytech.hu
- **Auth token:** `dev-token-spaceos-dashboard-2026`
- **4 oldal:**
  - Dashboard (`/`) — Terminál státuszok, inbox/outbox metrikák
  - Kanban (`/kanban`) — Discovery + Delivery dual-track board
  - Planning (`/planning`) — 5-stage pipeline (Idea → Queue)
  - Projects (`/projects`) — Gantt timeline (8 hónap)

### Mit látsz a Dashboard-on?

- **Dashboard oldal:** Conductor státusz (WORKING/IDLE), inbox count, outbox count
- **Kanban oldal:** Discovery track (Planning pipeline items), Delivery track (Conductor swimlane with inbox/working/review/done counts)
- **Planning oldal:** Idea/Selected/Debate/Consensus/Queue items száma
- **Projects oldal:** Projekt timeline

### Következő lépések

1. **Olvasd el** a frissített `CLAUDE.md` fájlodat
2. **Használd** a Datahaven státusz regisztrációt minden session elején/végén
3. **Látogasd meg** a Dashboard-ot: https://datahaven.joinerytech.hu (login: használd a tokent)
4. **Frissítsd** a `currentTask` mezőt session közben ha konkrét fázisban vagy

### Dokumentáció

- **Migration guide:** `docs/migration/DATAHAVEN_TERMINAL_MIGRATION.md`
- **Workflow:** `docs/WORKFLOW.md` — "Datahaven Dashboard — Központi Monitoring" szakasz
- **CLAUDE.md template:** `docs/migration/TERMINAL_CLAUDE_MD_TEMPLATE.md`

## Várt válasz

**NEM kell outbox válasz** — ez egy értesítő/training üzenet.

Kövesd az új workflow-t a következő session-től kezdve. Ha kérdésed van, jelezd Root-nak vagy nézd meg a migration guide-ot.

## Migration context

**Week 1 (2026-06-23):** Root + **Conductor** + Architect
**Week 2 (2026-06-30):** Kernel + Orch + FE
**Week 3 (2026-07-07):** Joinery + Cutting + Abstractions
**Week 4 (2026-07-14):** Inventory + Procurement + Sales + Identity
**Week 5 (2026-07-21):** Infra + E2E + TESTER + Librarian + Nexus

A Conductor a Week 1 priority terminál — első hullám, stabil monitoring alapját képzi.
