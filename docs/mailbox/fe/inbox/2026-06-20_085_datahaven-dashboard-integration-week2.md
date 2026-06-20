---
id: MSG-FE-085
from: root
to: fe
type: notification
priority: high
status: UNREAD
model: sonnet
created: 2026-06-20
---

## Tárgy

Datahaven Dashboard integráció — Week 2 Migration

## Tartalom

A **Datahaven Dashboard** központi monitoring rendszer élesedett. Az FE terminál Week 2 migráció részeként értesítést kap az új monitoring rendszerről.

**Megjegyzés:** Az FE terminál nem rendelkezik CLAUDE.md fájllal, így nincs automatikus session startup ritual frissítés. Az alábbiakat manuálisan kell implementálni session elején/végén.

### Új workflow (implementálandó)

**Minden session elején:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "fe",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'
```

**Session végén:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"fe","status":"idle"}'
```

### Datahaven Dashboard elérése

- **URL:** https://datahaven.joinerytech.hu
- **Auth token:** `dev-token-spaceos-dashboard-2026`
- **4 oldal:**
  - Dashboard (`/`) — FE státusz (WORKING/IDLE), inbox/outbox metrikák
  - Kanban (`/kanban`) — FE swimlane a Delivery track-en
  - Planning (`/planning`) — 5-stage pipeline
  - Projects (`/projects`) — Gantt timeline

### Mit látsz a Dashboard-on?

- **Dashboard oldal:** FE terminál státusza, inbox/outbox count
- **Kanban oldal:** FE swimlane — inbox/working/review/done oszlopok
- **Real-time updates:** SSE real-time frissítés

### Következő lépések

1. **Használd** a fenti curl parancsokat session elején/végén
2. **Látogasd meg** a Dashboard-ot: https://datahaven.joinerytech.hu
3. **Opcionális:** Frissítsd a `currentTask` mezőt session közben
4. **Jövőbeli:** FE terminál CLAUDE.md létrehozása (ha szükséges)

### Dokumentáció

- **Migration guide:** `docs/migration/DATAHAVEN_TERMINAL_MIGRATION.md`
- **Workflow:** `docs/WORKFLOW.md` — "Datahaven Dashboard — Központi Monitoring" szakasz
- **Template:** `docs/migration/TERMINAL_CLAUDE_MD_TEMPLATE.md` — használható CLAUDE.md létrehozásához

## Várt válasz

**NEM kell outbox válasz** — ez egy értesítő/training üzenet.

Implementáld a státusz regisztrációt a következő session-től kezdve.

## Migration context

**Week 1 (2026-06-23):** Root + Conductor + Architect ✅
**Week 2 (2026-06-30):** Kernel + Orch + **FE**
**Week 3 (2026-07-07):** Joinery + Cutting + Abstractions
**Week 4 (2026-07-14):** Inventory + Procurement + Sales + Identity
**Week 5 (2026-07-21):** Infra + E2E + TESTER + Librarian + Nexus

Az FE a Week 2 frontend terminál — React UI fejlesztés a JoineryTech Portal-hoz.
