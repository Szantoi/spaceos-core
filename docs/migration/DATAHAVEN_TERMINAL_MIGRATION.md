# Datahaven Dashboard Terminal Migration Guide

> Átállási útmutató minden SpaceOS terminál számára az új Datahaven Dashboard rendszerre

**Created:** 2026-06-20
**Version:** 1.0
**Status:** 🚧 Migration in Progress

---

## 📋 Executive Summary

### Mi változik?

**Előtte:**
- Terminálok nem látták egymást
- Nincs központi dashboard
- Nehéz átlátni a rendszer állapotát
- Mailbox csak fájlrendszer

**Utána:**
- **Datahaven Dashboard:** https://datahaven.joinerytech.hu
- Real-time visibility minden terminal állapotáról
- Kanban board a workflow-hoz
- Planning pipeline látható
- Project timeline Gantt chart-tal

---

## 🎯 Migration Célok

1. **Transparency:** Minden terminal látja a teljes rendszer állapotát
2. **Coordination:** Jobb koordináció terminálok között
3. **Monitoring:** Real-time status tracking
4. **Planning:** Látható planning pipeline
5. **Documentation:** Egységes dokumentáció minden terminalnak

---

## 🏗️ Új Rendszer Architektúra

```
┌─────────────────────────────────────────────────────┐
│         Datahaven Dashboard (React)                 │
│       https://datahaven.joinerytech.hu              │
│                                                     │
│  Dashboard │ Kanban │ Planning │ Projects          │
└─────────────────────┬───────────────────────────────┘
                      │
                      │ REST API
                      │
┌─────────────────────▼───────────────────────────────┐
│         Knowledge Service (Backend)                 │
│              Port 3456                              │
│                                                     │
│  • Dashboard API                                    │
│  • Kanban API                                       │
│  • Planning API                                     │
│  • Projects API                                     │
│  • SSE Events                                       │
│  • MCP Protocol                                     │
└─────────────────────┬───────────────────────────────┘
                      │
                      │ File System
                      │
┌─────────────────────▼───────────────────────────────┐
│           File System (Data Source)                 │
│                                                     │
│  • docs/mailbox/*/inbox/                           │
│  • docs/mailbox/*/outbox/                          │
│  • docs/planning/{ideas,queue}/                    │
│  • docs/tasks/{active,new,archive}/                │
│  • docs/memory/*.md                                 │
└─────────────────────────────────────────────────────┘
```

---

## 📱 Új Dashboard Funkciók

### 1. Dashboard Page

**Mit látsz:**
- **Global metrics:** Total inbox, outbox, unread, active sessions
- **Terminal grid:** 17 terminal kártya real-time status-szal
- **Saját terminal:** Inbox/outbox count, UNREAD messages, WORKING/IDLE status

**Mit csinálhatsz:**
- Gyors status check
- Látod ki dolgozik min
- UNREAD messages at-a-glance
- Click terminal → Kanban filtered view

### 2. Kanban Board

**Discovery Track (Planning):**
- Ideas → Selected → Debate → Consensus → Queue
- Látod mi van a planning pipeline-ban
- WIP metrics per stage

**Delivery Track (Execution):**
- **Saját swimlane:** Inbox → Working → Review → Done
- Látod saját task flow-dat
- Látod más terminálok workload-ját
- Bottleneck detection

### 3. Planning Pipeline

**Mit látsz:**
- 5 stage metrics (Ideas, Selected, Debate, Consensus, Queue)
- Planning items list
- Filter by status/priority
- Confidence scores

**Mire jó:**
- Látod mi jön pipeline-ból
- Tudod mi van queue-ban ready for pickup
- Planning transparency

### 4. Projects Timeline

**Mit látsz:**
- Gantt chart (8 months)
- Project bars with progress
- Milestones
- "Today" marker

**Mire jó:**
- Látod projekt timeline-t
- Progress tracking
- Deadline awareness
- Epic overview

---

## 🔧 Terminal Integration

### Amit minden terminal kap:

#### 1. **Dashboard Access**
```
URL: https://datahaven.joinerytech.hu
Token: dev-token-spaceos-dashboard-2026
```

#### 2. **Real-time Status Tracking**

Terminal status automatikusan tracked:
- **WORKING:** Session aktív, van task
- **IDLE:** Session nincs vagy inaktív

Status API:
```bash
# Register WORKING (session start)
POST /api/terminal/{terminal}/status
{ "state": "working", "taskId": "MSG-XXX" }

# Register IDLE (session end)
POST /api/terminal/{terminal}/status
{ "state": "idle" }

# Heartbeat
POST /api/terminal/{terminal}/status
{}
```

#### 3. **Inbox/Outbox Visibility**

Dashboard automatikusan scanneli:
- `docs/mailbox/{terminal}/inbox/*.md`
- `docs/mailbox/{terminal}/outbox/*.md`
- UNREAD status frontmatter-ből

Nincs extra munka, csak:
- Frontmatter YAML megfelelő formátumban
- `status: UNREAD|READ` mező

#### 4. **SSE Notifications**

Terminal feliratkozhat real-time event-ekre:
```bash
GET /api/mailbox/{terminal}/subscribe
```

Events:
- `wake_up` — Új UNREAD inbox message
- `terminal_status_change` — Más terminal status változás
- `planning_update` — Új item a planning pipeline-ban

---

## 📚 Dokumentáció Frissítések

### Minden terminal CLAUDE.md-be kerül:

#### **Session Startup Ritual bővítése:**

```markdown
## Session Startup Ritual

### 1. Check Inbox
\`\`\`bash
ls docs/mailbox/{terminal}/inbox/ | grep UNREAD
\`\`\`

### 2. Check Dashboard (ÚJ!)
**Dashboard URL:** https://datahaven.joinerytech.hu

**Mit nézz meg:**
- Saját terminal card: Inbox/Outbox/UNREAD count
- Global metrics: Hány terminal dolgozik
- Kanban board: Saját swimlane (Inbox → Working → Review → Done)

### 3. Register Status (ÚJ!)
\`\`\`bash
# Session start - register WORKING
curl -X POST http://localhost:3456/api/terminal/{terminal}/status \\
  -H "Content-Type: application/json" \\
  -d '{"state": "working", "taskId": "current-task-id"}'
\`\`\`

### 4. Read Memory
\`\`\`bash
cat docs/memory/{terminal}.md
\`\`\`

### 5. Load Context
(existing context loading steps)
\`\`\`
```

#### **Session End Ritual bővítése:**

```markdown
## Session End Ritual

### 1. Update Memory
\`\`\`bash
# Update docs/memory/{terminal}.md with session learnings
\`\`\`

### 2. Register IDLE (ÚJ!)
\`\`\`bash
curl -X POST http://localhost:3456/api/terminal/{terminal}/status \\
  -H "Content-Type: application/json" \\
  -d '{"state": "idle"}'
\`\`\`

### 3. Create Outbox (if DONE)
\`\`\`bash
# Create DONE outbox message with proper frontmatter
\`\`\`
```

---

## 🔄 Migration Steps per Terminal

### Phase 1: Documentation Update

**1. Update CLAUDE.md:**
- [ ] Add Dashboard URL section
- [ ] Add Session startup ritual updates
- [ ] Add Session end ritual updates
- [ ] Add Dashboard usage guide
- [ ] Add API endpoint reference

**2. Update Memory file:**
- [ ] Add "Dashboard Integration" section
- [ ] Note the terminal's current status
- [ ] Document any specific dashboard usage patterns

**3. Create Terminal Docs (if not exists):**
```
docs/terminals/{terminal}/
  README.md          ← Quick reference
  DASHBOARD.md       ← Dashboard usage guide
```

### Phase 2: Workflow Integration

**4. Update workflow scripts (if any):**
- [ ] Add status registration to session start scripts
- [ ] Add status registration to session end scripts
- [ ] Update inbox processing to check dashboard

**5. Test integration:**
- [ ] Start session → Check dashboard shows WORKING
- [ ] Process inbox → Check kanban updates
- [ ] End session → Check dashboard shows IDLE
- [ ] Create outbox → Check dashboard reflects

### Phase 3: Training & Calibration

**6. Send inbox message:**
```yaml
---
id: MSG-{TERMINAL}-DATAHAVEN-001
from: root
to: {terminal}
type: training
priority: high
status: UNREAD
model: sonnet
created: 2026-06-20
---

# Datahaven Dashboard Integration

A SpaceOS új központi monitoring rendszert kapott!

## Új Funkciók

**Dashboard URL:** https://datahaven.joinerytech.hu
**Auth Token:** dev-token-spaceos-dashboard-2026

### Mit látsz a dashboardon:
1. **Dashboard page:** Saját terminal card (inbox/outbox/UNREAD/status)
2. **Kanban board:** Saját swimlane (Inbox → Working → Review → Done)
3. **Planning:** Mi van a planning pipeline-ban
4. **Projects:** Project timeline Gantt chart-tal

## Új Workflow

### Session Start (FONTOS!):
\`\`\`bash
# Register WORKING status
curl -X POST http://localhost:3456/api/terminal/{terminal}/status \\
  -H "Content-Type: application/json" \\
  -d '{"state": "working", "taskId": "MSG-ID"}'
\`\`\`

### Session End:
\`\`\`bash
# Register IDLE status
curl -X POST http://localhost:3456/api/terminal/{terminal}/status \\
  -H "Content-Type: application/json" \\
  -d '{"state": "idle"}'
\`\`\`

## Dokumentáció

- Migration guide: `docs/migration/DATAHAVEN_TERMINAL_MIGRATION.md`
- Design brief: `docs/design/DATAHAVEN_UI_DESIGN_BRIEF.md`
- Dashboard README: `datahaven-web/client/README.md`

## Action Items

1. [ ] Read migration guide
2. [ ] Test dashboard login (use token above)
3. [ ] Update CLAUDE.md with new rituals
4. [ ] Test status registration API
5. [ ] Acknowledge in outbox: MSG-{TERMINAL}-DATAHAVEN-ACK

**Deadline:** Next session
**Questions:** docs/mailbox/root/inbox/

---
Root Terminal
\`\`\`

**7. Wait for acknowledgment:**
- [ ] Terminal reads message
- [ ] Terminal sends ACK outbox
- [ ] Terminal demonstrates dashboard usage

---

## 🎓 Training Points

### Dashboard Best Practices

**DO:**
- ✅ Check dashboard at session start
- ✅ Register WORKING status when starting
- ✅ Register IDLE when ending
- ✅ Use Kanban to see workflow
- ✅ Check Planning for upcoming work
- ✅ Monitor other terminals for coordination

**DON'T:**
- ❌ Forget to register status (dashboard shows stale data)
- ❌ Ignore UNREAD inbox messages
- ❌ Work in isolation (check dashboard for context)
- ❌ Skip documentation updates

### Common Scenarios

**Scenario 1: Starting a new task**
1. Check Dashboard → See inbox count
2. Register WORKING with task ID
3. Process task
4. Create DONE outbox
5. Dashboard auto-updates

**Scenario 2: Checking blockers**
1. Open Kanban board
2. See all terminals' swimlanes
3. Find blocked tasks (status=blocked)
4. Coordinate with blocked terminal
5. Resolve blocker

**Scenario 3: Planning review**
1. Open Planning page
2. See Queue stage
3. Pick next task from queue
4. Start working
5. Dashboard shows WORKING

---

## 📊 Terminal Status Matrix

| Terminal | Status | Last Update | Migration Phase |
|----------|--------|-------------|-----------------|
| kernel | 🟡 Pending | - | Phase 1 |
| orch | 🟡 Pending | - | Phase 1 |
| fe | 🟡 Pending | - | Phase 1 |
| joinery | 🟡 Pending | - | Phase 1 |
| abstractions | 🟡 Pending | - | Phase 1 |
| cutting | 🟡 Pending | - | Phase 1 |
| inventory | 🟡 Pending | - | Phase 1 |
| procurement | 🟡 Pending | - | Phase 1 |
| sales | 🟡 Pending | - | Phase 1 |
| identity | 🟡 Pending | - | Phase 1 |
| infra | 🟡 Pending | - | Phase 1 |
| e2e | 🟡 Pending | - | Phase 1 |
| architect | 🟡 Pending | - | Phase 1 |
| librarian | 🟡 Pending | - | Phase 1 |
| nexus | ✅ Complete | 2026-06-20 | Pilot |
| root | ✅ Complete | 2026-06-20 | Pilot |
| conductor | 🟡 Pending | - | Phase 1 |
| fe2 | 🟡 Pending | - | Phase 1 |
| cabinet | 🟡 Pending | - | Phase 1 |

**Legend:**
- ✅ Complete — Documentation updated, tested, acknowledged
- 🟢 In Progress — Migration started
- 🟡 Pending — Not started
- 🔴 Blocked — Issues to resolve

---

## 🚀 Rollout Plan

### Week 1: Pilot (DONE ✅)
- [x] Nexus terminal (test subject)
- [x] Root terminal
- [x] Documentation created
- [x] Dashboard deployed

### Week 2: Priority Terminals (Phase 1)
- [ ] Conductor (orchestrator)
- [ ] Librarian (knowledge management)
- [ ] Architect (planning)
- [ ] FE (high activity)

### Week 3: Backend Terminals (Phase 2)
- [ ] Kernel
- [ ] Orch
- [ ] Joinery
- [ ] Cutting
- [ ] Identity

### Week 4: Supporting Terminals (Phase 3)
- [ ] Abstractions
- [ ] Inventory
- [ ] Procurement
- [ ] Sales
- [ ] Infra
- [ ] E2E

### Week 5: Remaining Terminals (Phase 4)
- [ ] FE2
- [ ] Cabinet
- [ ] Orchestrator (if different from orch)

---

## 📖 API Reference

### Dashboard API

**GET /api/dashboard**
```json
{
  "timestamp": "2026-06-20T12:00:00Z",
  "metrics": {
    "totalInbox": 268,
    "totalOutbox": 333,
    "totalUnread": 9,
    "activeSessions": 2,
    "terminals": 17
  },
  "terminals": [
    {
      "name": "kernel",
      "inbox": 24,
      "outbox": 22,
      "unreadInbox": 0,
      "unreadOutbox": 0,
      "status": "idle",
      "lastActivity": null
    }
  ]
}
```

### Kanban API

**GET /api/kanban/snapshot**
```json
{
  "discovery": {
    "totals": {
      "ideas": 12,
      "selected": 3,
      "debate": 5,
      "consensus": 2,
      "queue": 3
    }
  },
  "delivery": {
    "swimlanes": [
      {
        "terminal": "kernel",
        "sessionActive": false,
        "totals": {
          "inbox": 24,
          "working": 0,
          "review": 0,
          "done": 22
        }
      }
    ]
  }
}
```

### Planning API

**GET /api/planning/items**
```json
{
  "items": [
    {
      "id": "plan-001.md",
      "title": "Planning Item Title",
      "status": "idea",
      "priority": "high",
      "createdAt": "2026-06-20T10:00:00Z",
      "confidence": 0.85
    }
  ],
  "metrics": {
    "ideas": 12,
    "selected": 3,
    "inDebate": 5,
    "consensus": 2,
    "queued": 3
  }
}
```

### Projects API

**GET /api/projects**
```json
{
  "projects": [
    {
      "id": "project-001",
      "name": "Project Name",
      "status": "active",
      "priority": "high",
      "startDate": "2026-06-01",
      "endDate": "2026-07-01",
      "progress": 65,
      "terminal": "kernel",
      "epic": "EPIC-001",
      "tasks": 10,
      "completedTasks": 6
    }
  ],
  "milestones": []
}
```

### Terminal Status API

**POST /api/terminal/:terminal/status**
```bash
# Register WORKING
curl -X POST http://localhost:3456/api/terminal/kernel/status \
  -H "Content-Type: application/json" \
  -d '{"state": "working", "taskId": "MSG-KERNEL-123"}'

# Register IDLE
curl -X POST http://localhost:3456/api/terminal/kernel/status \
  -H "Content-Type: application/json" \
  -d '{"state": "idle"}'

# Heartbeat (keep alive)
curl -X POST http://localhost:3456/api/terminal/kernel/status \
  -H "Content-Type: application/json" \
  -d '{}'
```

**GET /api/terminal/:terminal/status**
```json
{
  "terminal": "kernel",
  "status": {
    "state": "idle",
    "lastActivity": "2026-06-20T12:00:00Z",
    "taskId": null
  }
}
```

---

## 🐛 Troubleshooting

### Problem: Dashboard shows IDLE but terminal is WORKING

**Solution:**
- Terminal forgot to register WORKING status
- Fix: Call `POST /api/terminal/{terminal}/status` with `state: working`

### Problem: Dashboard shows stale inbox count

**Solution:**
- Dashboard cache (60s auto-refresh)
- Manual refresh button
- Wait for auto-refresh or click Refresh

### Problem: Cannot login to dashboard

**Solution:**
- Check token: `dev-token-spaceos-dashboard-2026`
- Check network: `curl https://datahaven.joinerytech.hu/health`
- Check backend: `systemctl status spaceos-knowledge`

### Problem: Status registration fails

**Solution:**
- Check backend running: `systemctl status spaceos-knowledge`
- Check endpoint: `curl http://localhost:3456/health`
- Check payload format (JSON)

---

## 📞 Support & Questions

**Migration Issues:** docs/mailbox/root/inbox/
**Technical Issues:** docs/mailbox/nexus/inbox/
**Design Feedback:** docs/design/DATAHAVEN_UI_DESIGN_BRIEF.md

**Documentation:**
- Migration guide: `docs/migration/DATAHAVEN_TERMINAL_MIGRATION.md` (this file)
- Dashboard README: `datahaven-web/client/README.md`
- Design brief: `docs/design/DATAHAVEN_UI_DESIGN_BRIEF.md`
- Nexus memory: `docs/memory/nexus.md`

---

**Version:** 1.0
**Last Update:** 2026-06-20
**Status:** 🚧 Migration In Progress
**Prepared by:** Root Terminal (Claude Sonnet 4.5)
