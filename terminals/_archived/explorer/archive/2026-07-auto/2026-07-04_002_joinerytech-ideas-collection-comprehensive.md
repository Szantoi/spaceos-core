---
id: MSG-EXPLORER-IDEAS-COLLECTION-001
from: explorer
to: librarian
type: research
priority: high
status: READ
created: 2026-07-04
content_hash: 0c1c78c777dd82d42863b0930a32c8dfebb4abc9f411a4b5bfc7fe3be3ef6202
---

# JoineryTech & Datahaven — Comprehensive Ideas Collection

**Compiled:** 2026-07-04 (Explorer Research)
**Scope:** 8 UI/UX ideas from discovery research (2026-06-30)
**Target:** Dashboard modernization + JoineryTech development

---

## Executive Summary

8 validated UI/UX ideas spanning **Dashboard infrastructure**, **JoineryTech dashboard widgets**, and **manufacturing-domain patterns**. Prioritized by:
- **Business value** (JoineryTech relevance)
- **Implementation effort** (time-to-market)
- **Technical feasibility** (existing patterns)

### Priority Matrix

```
HIGH EFFORT / HIGH VALUE              MEDIUM EFFORT / HIGH VALUE
├─ Mermaid Flow Editor (P3)           ├─ KPI Card System (P1) ⭐
│                                     ├─ Kanban Real-Time (P2)
                                      ├─ Cost Budget Tracker (P1) ⭐
                                      ├─ Mobile Responsive (P2)

MEDIUM EFFORT / MEDIUM VALUE          MEDIUM EFFORT / LOW VALUE
├─ Dark-First Bento (P1, DONE)        ├─ Real-Time Metrics (P3)
└─ Kanban Quick Actions (P2)          └─ (Reserved)
```

---

## IDEA #1: KPI Card System ⭐⭐⭐ P1 RECOMMENDED

**Status:** IDEA (Ready for Sprint 1)
**Priority:** P1 (High business value, LOW effort)
**Effort:** 2-3 days (sonnet)
**Domain:** Dashboard foundation

### Overview

Grafana/Datadog-inspired **metric strip** at dashboard header showing 4-6 critical KPIs in card format. Real-time updates via SSE or WebSocket.

### KPI Candidates for Datahaven
1. **Aktív Terminálok** — currently working
2. **Inbox Queue** — unread items
3. **Átlagos Task Idő** — avg processing time
4. **Pipeline Health** — DONE vs BLOCKED ratio
5. **API Uptime** — 24h availability %
6. **Latest DONE Task** — recency timestamp

### JoineryTech Relevance: ⭐⭐⭐ CRITICAL
- **Use case:** Executive dashboard for FSM status distribution
  - CRM leads: `uj/kapcsolat/minosites/konvertalva` breakdown
  - QA inspections: pass rate visualization
  - EHS incidents: lifecycle status heatmap
- **Reusable:** Every FSM-world can leverage `<FSMStatusCard />`
- **Foundation:** Bento grid layout (already DONE)

### Technical Stack

**Frontend:** React 18 + TypeScript
```typescript
interface KPICard {
  label: string;
  value: number | string;
  unit?: string;
  trend?: number; // % change
  status?: 'healthy' | 'warning' | 'critical';
  icon?: ReactNode;
}
```

**Backend:** Node.js MCP endpoint
```typescript
GET /api/dashboard/metrics/stream (SSE)
Response: {
  activeTerminals: 7,
  inboxQueue: 23,
  avgTaskTime: 1680, // seconds
  pipelineHealth: 0.94,
  apiUptime: 0.999,
  lastTaskDone: { time: "...", taskId: "..." }
}
```

**CSS:** Dark theme, bento grid
- Grid layout: `repeat(auto-fit, minmax(180px, 1fr))`
- Sticky header strip
- 16px spacing
- Responsive: 6 cards (desktop) → 3 (tablet) → 1 (mobile)

### Acceptance Criteria
- [ ] 6 KPI card components
- [ ] Real-time SSE/WebSocket updates
- [ ] Dark theme + Bento grid CSS
- [ ] Trend indicators (↑/↓) + status coloring
- [ ] Responsive design (3 breakpoints)
- [ ] Drilldown capability (click → detail view)
- [ ] ≤1s latency, 2-3s refresh rate
- [ ] Analytics tracking

### Next Steps
1. **Backend:** Create `/api/dashboard/metrics/stream` endpoint (1 day)
2. **Frontend:** Build `<KPICard />` component + grid layout (1 day)
3. **Integration:** Wire SSE updates + test real-time (0.5 day)

---

## IDEA #2: Cost Budget Tracker Widget ⭐⭐ P1 RECOMMENDED

**Status:** IDEA (Ready for Sprint 1)
**Priority:** P1 (Operational necessity, LOW effort)
**Effort:** 2-3 days (sonnet)
**Domain:** Monitoring & alerts

### Overview

Real-time cost monitoring dashboard widget with daily/weekly budget tracking and **Soft/Hard/Critical alert thresholds**.

### Widget Display
```
┌─────────────────────────────┐
│ 💰 Today's Cost Budget      │
├─────────────────────────────┤
│ $12.34 / $50.00 (24%)      │
│ ████████░░░░░░░░░░░░░░░░    │
│                             │
│ ⚠️  2 soft alerts           │
│ Breakdown:                  │
│ • Backend:  $7.20 (58%)    │
│ • Architect: $3.15 (26%)   │
│ • Frontend: $1.99 (16%)    │
│ [View Details] [Config]    │
└─────────────────────────────┘
```

### Alert Levels
- **Soft limit:** 60% → Toast warning
- **Hard limit:** 80% → Alert badge + Telegram
- **Critical:** 100% → Auto-pause workers + Root escalation

### JoineryTech Relevance: ⭐⭐ IMPORTANT
- Kontrolling module needs cost tracking
- Multi-terminal cost breakdown
- Historical trending (7-day chart)

### API Endpoints
```
GET /api/monitoring/cost/today
GET /api/monitoring/cost/terminal/:terminal
GET /api/monitoring/cost/history?days=7
PUT /api/monitoring/cost/config
```

### Acceptance Criteria
- [ ] Cost widget on Dashboard
- [ ] Daily budget + progress bar
- [ ] Terminal breakdown (pie/bar chart)
- [ ] Alert logic (Soft/Hard/Critical)
- [ ] Toast notifications
- [ ] Telegram integration
- [ ] Config modal (adjust budget limit)
- [ ] 7-day history chart
- [ ] Auto-pause workers at critical

---

## IDEA #3: Kanban Board Real-Time Feedback ⭐⭐ P2

**Status:** IDEA (Sprint 2)
**Priority:** P2 (Enhancement, MEDIUM effort)
**Effort:** 3-5 days (sonnet)
**Domain:** Dual-track board (Discovery + Delivery)

### Overview

Modern Kanban board with **instant drag-and-drop feedback** and **real-time collaboration** via WebSocket. Mobile-first UX (long-press to drag, swipe to navigate).

### Real-Time Features
- **Optimistic updates:** Local change immediately, sync async
- **Conflict resolution:** "X is moving this card" tooltip
- **Animation:** 200ms easing, 60 FPS
- **Broadcast:** All connected clients see updates instantly

### Mobile Enhancements
- **Touch drag:** Long-press (500ms) to initiate
- **Swipe gestures:** Swipe left = move, long-press = detail
- **Responsive:** Desktop (side-by-side) → Tablet (stacked) → Mobile (tabbed)
- **Button sizing:** 44×44px minimum

### JoineryTech Relevance: ⭐ NICE-TO-HAVE
- Not critical for early migration phases
- Improves UX for teams using Brief Q&A

### Technical Stack

**Frontend:** React + dnd-kit + WebSocket
```typescript
const KanbanCard = ({ task }) => {
  const { listeners, attributes } = useDraggable({ id: task.id });
  return (
    <div {...listeners} {...attributes}>
      {task.title}
    </div>
  );
};

// WebSocket
const ws = new WebSocket('ws://localhost:3456/api/kanban/stream');
ws.onmessage = (event) => {
  const { type, task } = JSON.parse(event.data);
  if (type === 'CARD_MOVED') updateTaskList(task);
};
```

**Backend:** Node.js + Socket.io
```typescript
io.on('card:move', async (taskId, targetColumn) => {
  const task = await updateTaskStatus(taskId, targetColumn);
  io.emit('card:moved', { type: 'CARD_MOVED', task });
});
```

### Acceptance Criteria
- [ ] dnd-kit drag-and-drop
- [ ] WebSocket real-time sync
- [ ] Optimistic updates
- [ ] Visual feedback (hover, dragging, drop-over)
- [ ] Mobile touch support
- [ ] Keyboard nav (Tab, arrows)
- [ ] 200ms animations
- [ ] Conflict tooltip
- [ ] 60 FPS performance
- [ ] Responsive (3 breakpoints)

---

## IDEA #4: Dark-First Bento Grid Layout ✅ DONE

**Status:** IMPLEMENTED (MSG-FRONTEND-064)
**Priority:** P1
**Effort:** MEDIUM (completed)
**Domain:** Dashboard foundation

### Overview

**Bento grid** (aszimmetrikus card layout) with **dark-first design** for data-dense dashboards. WCAG AA contrast minimum, reduced eye strain.

### Layout Structure
```
┌────────────────────────────────┐
│ KPI Strip (1 row, full width) │
├──────────────┬────────────────┤
│ Kanban Board │  Timeline      │
│ (50% width)  │  (50% width)   │
├──────────────┼────────┬───────┤
│ Details      │ Task   │ Alert │
│ Sidebar      │ List   │ Panel │
│ (33%)        │ (50%)  │ (17%) │
└──────────────┴────────┴───────┘
```

### Dark Theme Colors
- **Primary dark:** `#1a1d23` (near-black)
- **Card bg:** `#242931`
- **Text:** `#e5e7eb` (light gray)
- **Accent:** Neon colors (blue, green, red)

### CSS Grid
```css
.dashboard-grid {
  display: grid;
  grid-template-columns: repeat(12, 1fr);
  gap: 16px;
}

.kanban-board { grid-column: 1 / 7; grid-row: auto / span 4; }
.timeline-graph { grid-column: 7 / 13; grid-row: auto / span 4; }
/* ... */
```

### Responsive Breakpoints
- **1200px:** Desktop (full grid)
- **768px:** Tablet (stacked columns)
- **480px:** Mobile (single column)

### Acceptance Criteria
- ✅ CSS Grid Bento layout
- ✅ Dark theme colors + light adaptation
- ✅ WCAG AA contrast (4.5:1+)
- ✅ Progressive disclosure (expandable rows)
- ✅ Data-dense table layout
- ✅ 60+ FPS performance
- ✅ Accessibility tested

---

## IDEA #5: Kanban Quick Actions (Inline Operators) ⭐ P2

**Status:** IDEA (Sprint 2)
**Priority:** P2 (UX enhancement)
**Effort:** 1-2 days (haiku)
**Domain:** Brief Q&A workflow

### Overview

Inline action buttons on Kanban cards for **quick status transitions** without modal dialogs. Single-click actions: "Approve", "Reject", "Move to Next Stage".

### Use Case: Brief Q&A
```
Card: "Define window frame thickness"
├─ [✅ Approve]  ← Click to convert to Brief
├─ [❌ Reject]   ← Click to send back
└─ [→ Next]     ← Click to move to technical request
```

### Acceptance Criteria
- [ ] Inline action buttons on cards
- [ ] Quick status transitions (no modal)
- [ ] Keyboard shortcuts (A/R/N)
- [ ] Undo capability (30s timeout)
- [ ] Notification toast
- [ ] Mobile-friendly (tap zones)

---

## IDEA #6: Real-Time Metrics Dashboard ⭐ P3

**Status:** IDEA (Future enhancement)
**Priority:** P3 (Nice-to-have)
**Effort:** MEDIUM
**Domain:** Monitoring

### Overview

**Live metrics** dashboard showing terminal CPU, memory, response time, queue depth with animated trend charts.

### Metrics
- Terminal CPU usage (%)
- Memory consumption (MB)
- Avg API response time (ms)
- Task queue depth
- Error rate (%)

### Acceptance Criteria
- [ ] Real-time metric collection
- [ ] Animated line charts
- [ ] Alert thresholds
- [ ] Historical data (24h)

---

## IDEA #7: Mermaid Flow Editor (Interactive) ⭐ P3

**Status:** IDEA (Future enhancement)
**Priority:** P3 (Visualization)
**Effort:** HIGH (5+ days)
**Domain:** Epic/workflow visualization

### Overview

Interactive **Mermaid diagram editor** in Dashboard for visualizing epic dependencies and FSM flows. Click-to-edit, save to database.

### Use Cases
- **Epic dependency graph:** Visualize critical path
- **FSM flows:** State machine diagrams for each world
- **Architecture:** System boundaries and integrations

### Acceptance Criteria
- [ ] Mermaid editor UI
- [ ] Click-to-edit nodes/edges
- [ ] Save to backend
- [ ] Share diagrams
- [ ] Export (PNG/SVG)

---

## IDEA #8: Mobile Responsive Grid (Touch-First) ⭐ P2

**Status:** IDEA (Sprint 2)
**Priority:** P2 (UX completeness)
**Effort:** 2-3 days (haiku)
**Domain:** Mobile-first workflows

### Overview

Fully optimized **mobile experience** for manufacturing workshop tablets:
- Touch-friendly button sizing (44×44px minimum)
- Swipe gestures for navigation
- Collapsible sections (save vertical space)
- Offline capability (service worker)

### Tablet Use Cases (Manufacturing)
- Production floor status dashboard
- Quality inspection workflow
- Maintenance ticket management
- Time tracking (attendance kiosk)

### Acceptance Criteria
- [ ] Mobile-first layout
- [ ] Touch gestures (swipe, long-press)
- [ ] 44×44px minimum buttons
- [ ] Collapsible sections
- [ ] Offline sync
- [ ] Battery optimization

---

## Prioritization Summary

### Recommended Delivery Order

**Sprint 1 (Q3 2026, Week 1-2):** Foundation
1. ✅ **Dark-First Bento Grid** (already DONE)
2. ⭐ **KPI Card System** (P1, 2-3 days)
3. ⭐ **Cost Budget Tracker** (P1, 2-3 days)

**Sprint 2 (Week 3-4):** Enhancement
4. **Kanban Real-Time Feedback** (P2, 3-5 days)
5. **Mobile Responsive Grid** (P2, 2-3 days)
6. **Kanban Quick Actions** (P2, 1-2 days)

**Sprint 3+ (Q3 later, Future):** Advanced
7. **Real-Time Metrics Dashboard** (P3, medium effort)
8. **Mermaid Flow Editor** (P3, high effort)

---

## Cross-Functional Ownership

| Idea | Frontend | Backend | Designer | Effort |
|------|----------|---------|----------|--------|
| KPI Cards | ✓ | ✓ | ✓ | 2-3d |
| Cost Tracker | ✓ | ✓ | ✓ | 2-3d |
| Kanban Real-Time | ✓ | ✓ | ✓ | 3-5d |
| Mobile Grid | ✓ | - | ✓ | 2-3d |
| Kanban Inline | ✓ | - | ✓ | 1-2d |
| Real-Time Metrics | ✓ | ✓ | - | 3-5d |
| Mermaid Editor | ✓ | ✓ | ✓ | 5+d |

---

## JoineryTech Integration Points

### Which ideas support JoineryTech migration?

| Idea | JoineryTech Use | Benefit |
|------|-----------------|---------|
| **KPI Cards** | FSM status breakdown per module | Executive visibility |
| **Cost Tracker** | Kontrolling EAC widget | Financial intelligence |
| **Kanban Real-Time** | Brief Q&A inline approval | Faster design workflow |
| **Mobile Grid** | Workshop tablet workflows | Field operability |
| **Kanban Inline** | Quick status transitions | Reduced modal dialogs |

### Not directly JoineryTech-specific:
- Real-Time Metrics (system infrastructure)
- Mermaid Editor (epic visualization, useful but not critical)

---

## Technical Dependencies

```
KPI Cards
├─ Requires: SSE endpoint (new)
├─ Depends on: Dashboard page render

Cost Tracker
├─ Requires: Cost aggregation API (new)
├─ Depends on: Terminal-level cost tracking

Kanban Real-Time
├─ Requires: WebSocket server (io/Socket.io)
├─ Depends on: dnd-kit library

Mobile Grid
├─ Requires: Service worker (offline)
├─ Depends on: CSS Grid responsive design

Mermaid Editor
├─ Requires: Mermaid.js + editor UI
├─ Depends on: EPICS.yaml persistence
```

---

## Deliverables Generated

**Source:** 8 idea files from `/opt/spaceos/docs/planning/ideas/2026-06-30/`
**Compiled by:** Explorer (2026-07-04)
**For:** Librarian (synthesis & knowledge docs)
**Next:** Architect review of integration points + Backend feasibility check

---

## References

- **Grafana Dashboard Patterns:** https://grafana.com/grafana/dashboards/
- **Datadog Executive Dashboards:** https://www.datadoghq.com/blog/
- **Jira Kanban UX:** https://titanapps.io/blog/jira-kanban-board
- **SaaS Dashboard Design 2026:** https://www.925studios.co/blog/saas-dashboard-design-examples-2026
- **dnd-kit Documentation:** https://docs.dndkit.com/
- **Socket.io Real-time Guide:** https://socket.io/docs/v4/socket-io-protocol/
- **WCAG Contrast Checker:** https://webaim.org/resources/contrastchecker/

---

**Compilation complete.** Report ready for Librarian synthesis into knowledge base.
