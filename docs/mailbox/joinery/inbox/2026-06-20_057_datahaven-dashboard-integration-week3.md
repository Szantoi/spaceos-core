---
id: MSG-JOINERY-057
from: root
to: joinery
type: notification
priority: high
status: UNREAD
model: sonnet
created: 2026-06-20
---

## Tárgy

Datahaven Dashboard integráció — Week 3 Migration

## Tartalom

A **Datahaven Dashboard** központi monitoring rendszer élesedett és a Joinery terminál Week 3 migráció részeként frissült.

### Mi változott?

A `backend/spaceos-modules-joinery/CLAUDE.md` fájlod frissült az alábbi szekciókkal:

**1. SESSION STARTUP/SHUTDOWN RITUAL** szakasz hozzáadva (a fájl elején)
**2. Datahaven Dashboard** referencia (URL, token, 4 oldal)

### Új workflow

**Minden session elején:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "joinery",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'
```

**Session végén:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"joinery","status":"idle"}'
```

### Datahaven Dashboard elérése

- **URL:** https://datahaven.joinerytech.hu
- **Auth token:** `dev-token-spaceos-dashboard-2026`
- **4 oldal:**
  - Dashboard (`/`) — Joinery státusz (WORKING/IDLE), inbox/outbox metrikák
  - Kanban (`/kanban`) — Joinery swimlane a Delivery track-en
  - Planning (`/planning`) — 5-stage pipeline
  - Projects (`/projects`) — Gantt timeline

### Mit látsz a Dashboard-on?

- **Dashboard oldal:** Joinery terminál státusza, inbox/outbox count, unread count
- **Kanban oldal:** Joinery swimlane (Delivery track) — inbox/working/review/done
- **Real-time:** Automatikus frissítés (SSE)

### Következő lépések

1. **Olvasd el** a frissített `CLAUDE.md` fájlodat (SESSION STARTUP/SHUTDOWN RITUAL szakasz)
2. **Használd** a Datahaven státusz regisztrációt minden session elején/végén
3. **Látogasd meg** a Dashboard-ot: https://datahaven.joinerytech.hu
4. **Opcionális:** Frissítsd a `currentTask` mezőt session közben

### Dokumentáció

- **Migration guide:** `docs/migration/DATAHAVEN_TERMINAL_MIGRATION.md`
- **Workflow:** `docs/WORKFLOW.md` — "Datahaven Dashboard — Központi Monitoring" szakasz

## Várt válasz

**NEM kell outbox válasz** — ez egy értesítő/training üzenet.

Kövesd az új workflow-t a következő session-től kezdve.

## Migration context

**Week 1 (2026-06-23):** Root + Conductor + Architect ✅
**Week 2 (2026-06-30):** Kernel + Orch + FE ✅
**Week 3 (2026-07-07):** **Joinery** + Cutting + Abstractions
**Week 4 (2026-07-14):** Inventory + Procurement + Sales + Identity
**Week 5 (2026-07-21):** Infra + E2E + TESTER + Librarian + Nexus

A Joinery a Week 3 domain modul — ajtók és bútorok parametrikus tervezése.
