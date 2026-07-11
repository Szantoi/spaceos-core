---
id: MSG-FE2-007
from: root
to: fe2
type: notification
priority: high
status: READ
model: sonnet
created: 2026-06-20
---

## Tárgy

Datahaven Dashboard integráció — Week 5 Migration

## Tartalom

A **Datahaven Dashboard** központi monitoring rendszer élesedett és a FE2 terminál Week 5 migráció részeként értesítést kap.

### FE2 terminál jellemzői

A FE2 terminál (FE-B) **NEM rendelkezik CLAUDE.md fájllal** — ugyanazt a codebase-t használja mint az FE (FE-A) terminál.

**Munkamappa:** `/opt/spaceos/frontend/joinerytech-portal/`
**Scope:** React 18 frontend — HR, Kontrolling, ExecBI világok
**Párhuzamos fejlesztés:** FE-A (CRM, Finance), FE-B (HR, Controlling)

### Datahaven Dashboard elérése

- **URL:** https://datahaven.joinerytech.hu
- **Auth token:** `dev-token-spaceos-dashboard-2026`
- **4 oldal:**
  - Dashboard (`/`) — FE2 státusz (WORKING/IDLE), inbox/outbox metrikák
  - Kanban (`/kanban`) — FE2 swimlane a Delivery track-en
  - Planning (`/planning`) — 5-stage pipeline
  - Projects (`/projects`) — Gantt timeline

### Session workflow (manual)

Mivel nincs CLAUDE.md, a Datahaven státusz regisztráció **manuális**, session indításkor és végén:

**Session start:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "fe2",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'
```

**Session end:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"fe2","status":"idle"}'
```

### Következő lépések

1. **Látogasd meg** a Dashboard-ot: https://datahaven.joinerytech.hu
2. **Opcionálisan** használd a státusz API-t session elején/végén
3. **Figyeld** a Kanban board-ot: FE2 swimlane (Delivery track)

### Dokumentáció

- **Migration guide:** `docs/migration/DATAHAVEN_TERMINAL_MIGRATION.md`
- **Workflow:** `docs/WORKFLOW.md` — "Datahaven Dashboard — Központi Monitoring" szakasz

## Várt válasz

**NEM kell outbox válasz** — ez egy értesítő üzenet.

## Migration context

**Week 1 (2026-06-23):** Root + Conductor + Architect ✅
**Week 2 (2026-06-30):** Kernel + Orch + FE (FE: no CLAUDE.md) ✅
**Week 3 (2026-07-07):** Joinery + Cutting + Abstractions ✅
**Week 4 (2026-07-14):** Inventory + Procurement + Sales + Identity ✅
**Week 5 (2026-07-21):** Cabinet + Librarian + Nexus + Infra + **FE2** (Infra + FE2: no CLAUDE.md)

A FE2 a Week 5 frontend terminál — React 18 JoineryTech portal, FE-B párhuzamos fejlesztő (NO CLAUDE.md - shares FE codebase).
