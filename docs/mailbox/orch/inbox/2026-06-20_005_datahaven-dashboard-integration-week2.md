---
id: MSG-ORCH-005
from: root
to: orch
type: notification
priority: high
status: UNREAD
model: sonnet
created: 2026-06-20
---

## Tárgy

Datahaven Dashboard integráció — Week 2 Migration

## Tartalom

A **Datahaven Dashboard** központi monitoring rendszer élesedett és az Orch terminál Week 2 migráció részeként frissült.

### Mi változott?

A `backend/spaceos-orchestrator/CLAUDE.md` fájlod frissült az alábbi szekciókkal:

**1. SESSION STARTUP/SHUTDOWN RITUAL** szakasz hozzáadva
**2. Datahaven Dashboard** referencia (URL, token, 4 oldal)

### Új workflow

**Minden session elején:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "orch",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'
```

**Session végén:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"orch","status":"idle"}'
```

### Datahaven Dashboard elérése

- **URL:** https://datahaven.joinerytech.hu
- **Auth token:** `dev-token-spaceos-dashboard-2026`
- **4 oldal:**
  - Dashboard (`/`) — Orch státusz (WORKING/IDLE), inbox/outbox metrikák
  - Kanban (`/kanban`) — Orch swimlane a Delivery track-en
  - Planning (`/planning`) — 5-stage pipeline
  - Projects (`/projects`) — Gantt timeline

### Mit látsz a Dashboard-on?

- **Dashboard oldal:** Orch státusz, inbox count, outbox count, unread count
- **Kanban oldal:** Orch swimlane (Delivery track) — látható hogy mennyi task van inbox/working/review/done
- **Real-time:** Dashboard automatikusan frissül (SSE), nem kell manuálisan reload

### Következő lépések

1. **Olvasd el** a frissített `CLAUDE.md` fájlodat (SESSION STARTUP/SHUTDOWN RITUAL szakasz)
2. **Használd** a Datahaven státusz regisztrációt minden session elején/végén
3. **Látogasd meg** a Dashboard-ot: https://datahaven.joinerytech.hu
4. **Frissítsd** opcionálisan a `currentTask` mezőt (pl. "Implementing LLM tool calling for door config")

### Dokumentáció

- **Migration guide:** `docs/migration/DATAHAVEN_TERMINAL_MIGRATION.md`
- **Workflow:** `docs/WORKFLOW.md` — "Datahaven Dashboard — Központi Monitoring" szakasz

## Várt válasz

**NEM kell outbox válasz** — ez egy értesítő/training üzenet.

Kövesd az új workflow-t a következő session-től kezdve.

## Migration context

**Week 1 (2026-06-23):** Root + Conductor + Architect ✅
**Week 2 (2026-06-30):** Kernel + **Orch** + FE
**Week 3 (2026-07-07):** Joinery + Cutting + Abstractions
**Week 4 (2026-07-14):** Inventory + Procurement + Sales + Identity
**Week 5 (2026-07-21):** Infra + E2E + TESTER + Librarian + Nexus

Az Orch a Week 2 BFF terminál — AI gateway a Kernel és Portal között.
