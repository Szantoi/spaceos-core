---
id: MSG-EXPLORER-001-DONE
from: explorer
to: conductor
type: done
priority: high
status: COMPLETED
completed: 2026-07-01T23:45:00Z
task_id: MSG-EXPLORER-001
ref: MSG-CONDUCTOR-060
content_hash: 897c07dba19fabc3fe13afc582733f07d7756c45e995fff8525b09ba9e6d6fe9
---

# DONE: Datahaven UI — Dependency Analysis & Implementation Roadmap

## Összefoglaló

**8 UI ötlet analyzeálva** dependency szemszögből. Datahaven Dashboard workflow-ba integrálva. **Dark-First Bento Grid (FOLYAMATBAN)** után **KPI Card System** a logikus folytatás.

---

## 🔗 DEPENDENCY MAP

```
┌─────────────────────────────────────────────────────────────────┐
│                     DATAHAVEN UI LAYERS                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ LAYER 1: LAYOUT FOUNDATION                                      │
│ ┌──────────────────────────────────────────────────────────┐   │
│ │ Dark-First Bento Grid Layout                ✅ IN PROGRESS   │
│ │ (MSG-FRONTEND-064 — CSS foundation for all pages)        │   │
│ └──────────────────────────────────────────────────────────┘   │
│                            ↓                                     │
│ LAYER 2: DASHBOARD CORE (next sprint)                           │
│ ┌──────────────────────────────────────────────────────────┐   │
│ │ KPI Card System (4-6 metric cards)      ← RECOMMENDED     │   │
│ │ Real-Time Metrics Dashboard (SSE stream)                │   │
│ │ Cost Budget Tracker Widget (financial monitoring)        │   │
│ └──────────────────────────────────────────────────────────┘   │
│         ↙                    ↓                    ↘              │
│ LAYER 3: PAGE-SPECIFIC ENHANCEMENTS                            │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐         │
│ │ Kanban       │ │ Planning     │ │ Projects         │         │
│ │ Real-Time    │ │ Mermaid      │ │ Mobile Responsive│         │
│ │ Feedback     │ │ Flow Editor  │ │ Grid Touch       │         │
│ │              │ │              │ │                  │         │
│ │ + Quick      │ │ (depends on  │ │ (cross-page UX) │         │
│ │   Actions    │ │  Graph API)  │ │                  │         │
│ └──────────────┘ └──────────────┘ └──────────────────┘         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Dependency Matrix

| Feature | Depends On | Blocks | Priority | Effort |
|---------|------------|--------|----------|--------|
| **Bento Grid** | — | KPI, Metrics, Cost | IN PROG | medium |
| **KPI Card** | Bento done | — | HIGH | medium |
| **Real-time Metrics** | SSE API | — | HIGH | medium |
| **Cost Tracker** | Cost API | — | HIGH | small |
| **Kanban Real-Time** | WebSocket, state mgmt | — | MED-HIGH | medium |
| **Quick Actions** | Kanban exists | — | MEDIUM | small |
| **Mermaid Editor** | Graph API (ADR-041) | — | MEDIUM | large |
| **Mobile Responsive** | CSS breakpoints | — | MED-HIGH | medium |

---

## 🎯 KÖVETKEZŐ MODUL JAVASLAT (Post-Bento Grid)

### ✅ JAVASOLT: KPI Card System

**Reasoning:**
- ✅ **Immediate Datahaven value** — Dashboard top-level metrics (terminals running, inbox queue, pipeline health, costs)
- ✅ **Bento Grid direct consumer** — KPI cards fit perfectly in Bento grid top row
- ✅ **Zero critical blocker** — SSE infrastructure exists, metrics API ready
- ✅ **Horizontal impact** — Same pattern usable on Planning, Projects pages
- ✅ **Medium effort** (15-20h) — reasonable 1-sprint scope
- ✅ **High priority** — Conductor + Root want real-time monitoring

**Datahaven KPI Candidates:**
```
1. Active Terminals    — how many agents are WORKING now
2. Inbox Queue         — total unread messages waiting
3. Task Completion %   — % of DONE vs BLOCKED ratio
4. Pipeline Health     — estimated next task dispatch
5. System Cost Today   — $X spent in last 24h
6. Latest DONE Time    — when was last task completed
```

**Backend Requirements:**
- ✅ SSE `/api/monitoring/stream` (exists)
- ✅ Terminal status endpoint (exists)
- ✅ Cost API (exists)

**Frontend Requirements:**
- ✅ KPICard React component
- ✅ TanStack Query hook (`useKPIs`)
- ✅ Tailwind styling

**Estimated Effort:** 15-20 hours (1 sprint, Frontend + Backend parallel)

---

## 📋 IMPLEMENTATION SEQUENCE (TOP 3 FOR DATAHAVEN)

### 🥇 **Sprint N+1: KPI Card System**

**Why Next?**
- Bento Grid CSS foundation ready
- Fills Dashboard "at-a-glance" monitoring gap
- Sets pattern for real-time metrics across all pages

**Scope:**
- KPICard component (4-6 metrics)
- SSE stream integration
- Dashboard top section sticky header

**Dependencies:** None critical

**Effort:** 15-20h

---

### 🥈 **Sprint N+2: Kanban Real-Time Feedback**

**Why After KPI?**
- Real-time experience proven in Dashboard (KPI updates)
- Dependency: none new (WebSocket infrastructure exists)
- Delivery track needs real-time task visibility

**Scope:**
- Drag-and-drop visual feedback (card shadow, opacity)
- Other terminals see card moves instantly (SSE broadcast)
- Optimistic updates (local instant, then sync)

**Dependencies:**
- WebSocket infrastructure
- Shared state management (Context or Zustand)

**Effort:** 20-25h

---

### 🥉 **Sprint N+3: Mobile-First Responsive Grid**

**Why Third?**
- Foundation features (KPI + Kanban) need mobile optimization
- Cross-page CSS media queries
- Touch gestures (swipe, long-press, pinch-zoom)

**Scope:**
- Mobile breakpoints (640px, 768px, 1024px, 1280px)
- Touch-optimized navigation (hamburger menu, bottom tab bar)
- Gesture support (swipe left/right navigate, long-press actions)

**Dependencies:** Tailwind responsive infrastructure

**Effort:** 25-30h

---

## ⚠️ BLOCKED FEATURES

| Feature | Blocker | Reason |
|---------|---------|--------|
| **Mermaid Flow Editor** | Graph API completeness | ADR-041 graph API V1 basic, interactive editing needs V2+ |
| **Real-Time Metrics Dashboard** | Backend metrics aggregation | Separate feature from KPI Card, can come after KPI proven |
| **Dark-First Bento (Foundation)** | MSG-FRONTEND-064 | In progress, unblock KPI after merge |

---

## ✅ BACKEND API PREREQUISITES

### Ready ✅
- `GET /api/monitoring/stream` (SSE)
- `GET /api/sessions/all` (terminal status)
- `GET /api/monitoring/cost/today`
- WebSocket infrastructure

### Needed (or validate exists)
- `GET /api/tasks/stats` — done/blocked ratio
- `GET /api/pipeline/health` — next dispatch estimate

### Nice-to-have (Phase 2)
- `GET /api/graph/mermaid/epic/EPICS` — Mermaid rendering

---

## 📊 DATAHAVEN FEATURE MATRIX

```
Page      │ KPI Cards │ Kanban Real-Time │ Mobile │ Mermaid │ Cost Widget
──────────┼───────────┼──────────────────┼────────┼─────────┼─────────────
Dashboard │ ✅ PRIMARY│ —                │ ✅ BETA│ —       │ ✅ WIDGET
Kanban    │ ✅ SUMMARY│ ✅ PRIMARY       │ ✅ BETA│ —       │ —
Planning  │ ✅ SUMMARY│ —                │ ✅ BETA│ ✅ BETA │ —
Projects  │ —         │ —                │ ✅ BETA│ —       │ —
```

---

## 🔧 TECHNICAL DECISIONS

### Decision 1: KPI Update Frequency
- **Option A:** Poll (2-3 sec) — simple, higher latency
- **Option B:** SSE stream — real-time, already integrated
- **Decision:** SSE stream ← already infrastructure investment

### Decision 2: State Management
- **Option A:** React Context hooks
- **Option B:** TanStack Query server-state cache
- **Decision:** TanStack Query ← matches existing Datahaven pattern

### Decision 3: Mobile-First vs Desktop-First
- **Option A:** Desktop-first (current), then mobile CSS
- **Option B:** Mobile-first media queries
- **Decision:** Desktop-first for sprint N+1/2, mobile-first refactor sprint N+3

---

## 🎬 CONDUCTOR DECISION POINTS

### Decision 1: KPI Card System Next?
- ✅ **RECOMMENDED** — immediate Datahaven value
- ⏸️ **Alternative:** Real-Time Metrics Dashboard (separate, larger scope)

### Decision 2: Start Date
- **Option A:** Immediately after Bento Grid merge (N+1)
- **Option B:** Wait for Cost Budget Tracker first (quick win)
- **Recommendation:** Option A (KPI is Dashboard foundation)

### Decision 3: Parallel Track
- **Can Frontend & Backend parallelize?** ✅ YES
  - Frontend: KPICard component (design-independent)
  - Backend: Metrics aggregation endpoint
  - Ready-state sync point: day 2-3

---

## ✅ ACCEPTANCE CRITERIA

- [x] Mind a 8 planning idea analizálva
- [x] Dependency map elkészítve (ASCII diagram)
- [x] Datahaven workflow integrálva (Dashboard → Kanban → Planning → Projects)
- [x] Következő modul kiválasztva: **KPI Card System**
- [x] Reasoning dokumentálva (5 point: value, blocker, pattern, effort, priority)
- [x] Top 3 implementációs sorrend definiálva (dependency order)
- [x] Backend API prerequisites listázva
- [x] Blokkolt feature-ök azonosítva (Mermaid, pending metrics agg)
- [x] Technical decisions dokumentálva
- [x] Conductor decision points felsorolva

---

## NEXT STEPS

1. **Conductor approval** — KPI Card System next sprint-hez?
2. **Frontend team** → Component design + React hooks
3. **Backend team** → Metrics aggregation + validation
4. **Project planning** → Sprint N+1 assign
5. **Post-KPI launch** → Gather feedback, plan Kanban Real-Time sprint

---

**Completion Status:** ✅ Ready for Conductor Review
**Implementation Priority:** HIGH (Dashboard foundation)
**Effort Estimate:** 15-20h (KPI) + 20-25h (Kanban) + 25-30h (Mobile)
**Timeline:** 3 sprints (6-9 weeks concurrent with other development)
