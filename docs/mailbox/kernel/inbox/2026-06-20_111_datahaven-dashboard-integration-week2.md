---
id: MSG-KERNEL-111
from: root
to: kernel
type: notification
priority: high
status: UNREAD
model: sonnet
created: 2026-06-20
---

## Tárgy

Datahaven Dashboard integráció — Week 2 Migration

## Tartalom

A **Datahaven Dashboard** központi monitoring rendszer élesedett és a Kernel terminál Week 2 migráció részeként frissült.

### Mi változott?

A `backend/spaceos-kernel/CLAUDE.md` fájlod frissült az alábbi szekciókkal:

**1. SESSION STARTUP/SHUTDOWN RITUAL** szakasz hozzáadva
**2. Datahaven Dashboard** referencia (URL, token, 4 oldal)

### Új workflow

**Minden session elején:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "kernel",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'
```

**Session végén:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"kernel","status":"idle"}'
```

### Datahaven Dashboard elérése

- **URL:** https://datahaven.joinerytech.hu
- **Auth token:** `dev-token-spaceos-dashboard-2026`
- **4 oldal:**
  - Dashboard (`/`) — Kernel státusz (WORKING/IDLE), inbox/outbox metrikák
  - Kanban (`/kanban`) — Kernel swimlane a Delivery track-en (inbox/working/review/done)
  - Planning (`/planning`) — 5-stage pipeline (Idea → Queue)
  - Projects (`/projects`) — Gantt timeline (8 hónap)

### Mit látsz a Dashboard-on?

- **Dashboard oldal:** Kernel státusz, inbox count, outbox count, unread count
- **Kanban oldal:** Kernel swimlane (Delivery track) — inbox/working/review/done oszlopok
- **Session státusz:** WORKING amikor dolgozol, IDLE amikor befejezted

### Következő lépések

1. **Olvasd el** a frissített `CLAUDE.md` fájlodat (SESSION STARTUP/SHUTDOWN RITUAL szakasz)
2. **Használd** a Datahaven státusz regisztrációt minden session elején/végén
3. **Látogasd meg** a Dashboard-ot: https://datahaven.joinerytech.hu (login: használd a tokent)
4. **Frissítsd** opcionálisan a `currentTask` mezőt session közben (pl. "Building Kernel 0 errors")

### Dokumentáció

- **Migration guide:** `docs/migration/DATAHAVEN_TERMINAL_MIGRATION.md`
- **Workflow:** `docs/WORKFLOW.md` — "Datahaven Dashboard — Központi Monitoring" szakasz
- **Week 2 summary:** `docs/migration/WEEK_2_MIGRATION_SUMMARY.md` (hamarosan)

## Várt válasz

**NEM kell outbox válasz** — ez egy értesítő/training üzenet.

Kövesd az új workflow-t a következő session-től kezdve.

## Migration context

**Week 1 (2026-06-23):** Root + Conductor + Architect ✅
**Week 2 (2026-06-30):** **Kernel** + Orch + FE
**Week 3 (2026-07-07):** Joinery + Cutting + Abstractions
**Week 4 (2026-07-14):** Inventory + Procurement + Sales + Identity
**Week 5 (2026-07-21):** Infra + E2E + TESTER + Librarian + Nexus

A Kernel a Week 2 product core terminál — backend foundation az egész rendszernek.
