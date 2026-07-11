---
id: MSG-FRONTEND-105
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-DATAHAVEN-UI
ref: 2026-06-30_001_dashboard-kpi-card-system.md, 2026-06-30_005_cost-budget-tracker-widget.md
created: 2026-07-04
completed: 2026-07-04
estimated_nwt: 60
content_hash: f78a75fd6ba3c2b41d9cae1558263b443dd74378463d2dfe8f6379cbe0c177c2
---

# Datahaven Dashboard Widgets — KPI Cards + Cost Budget Tracker

**Epic:** EPIC-DATAHAVEN-UI (Dashboard UI Redesign)
**Priority:** HIGH (JoineryTech top focus)
**Estimated:** 60 NWT (~2 hours)

---

## Context

Explorer validated both widgets as implementation-ready:
- **KPI Card System** — Validated infrastructure (SSE real-time, localStorage persistence)
- **Cost Budget Tracker** — Backend API ready (MSG-BACKEND-138 cost monitoring endpoints)

Both widgets directly support JoineryTech project monitoring and coordination.

---

## Task 1: KPI Card System

**Planning Reference:** `docs/planning/ideas/2026-06-30_001_dashboard-kpi-card-system.md`

### Requirements

**Component: `KPICard.tsx`**
- Props: `{ label, value, unit?, trend?, status?, realtime? }`
- Status variants: `success | warning | danger | neutral`
- Trend indicator: ↑ ↓ with % change
- Dark-first design (CSS variables from existing system)

**Component: `KPIStrip.tsx`**
- Horizontal strip layout (3-5 cards)
- Responsive: Desktop 5-column, Tablet 3-column, Mobile 1-column
- Auto-refresh via SSE (if `realtime: true`)

### Data Flow

```typescript
// SSE connection (existing useSSE hook)
const { data } = useSSE('/api/metrics/realtime');

// LocalStorage fallback (offline-first)
const cachedMetrics = localStorage.getItem('datahaven_kpi_cache');
```

### Acceptance Criteria

- [ ] `KPICard.tsx` component with 4 status variants
- [ ] `KPIStrip.tsx` responsive layout (3 breakpoints)
- [ ] SSE real-time updates working
- [ ] localStorage cache persistence (offline fallback)
- [ ] Dark mode CSS variables (no hard-coded colors!)
- [ ] Module CSS files (KPICard.module.css, KPIStrip.module.css)

---

## Task 2: Cost Budget Tracker Widget

**Planning Reference:** `docs/planning/ideas/2026-06-30_005_cost-budget-tracker-widget.md`
**Backend API:** MSG-BACKEND-138 (Cost Monitoring API)

### Requirements

**Component: `CostBudgetWidget.tsx`**
- Display: Current spend / Budget limit
- Visual: Progress bar (green <70%, yellow 70-90%, red >90%)
- Real-time: SSE updates every 5 minutes
- Drill-down: Click → detailed cost breakdown modal

**API Integration:**

```typescript
// Existing Backend API (MSG-BACKEND-138)
GET /api/cost/current        → { spend, budget, percentage }
GET /api/cost/breakdown      → { byTerminal, byModel, byProject }
```

### Data Flow

1. Fetch initial data: `useCost()` hook (TanStack Query)
2. SSE updates: Subscribe to `/api/cost/updates`
3. Cache: localStorage fallback for offline mode

### Acceptance Criteria

- [ ] `CostBudgetWidget.tsx` component with progress bar
- [ ] Color thresholds: <70% green, 70-90% yellow, >90% red
- [ ] SSE real-time updates (5-minute interval)
- [ ] Drill-down modal: `CostBreakdownModal.tsx`
- [ ] TanStack Query integration (`useCost` hook)
- [ ] localStorage cache persistence
- [ ] Dark mode CSS variables

---

## Technical Constraints

### Must Use Existing Patterns

1. **CSS Variables** (NO hard-coded hex colors!)
   ```css
   /* Use existing datahaven-web/client/src/index.css variables */
   var(--bg-primary), var(--text-primary), var(--success), var(--warning), var(--danger)
   ```

2. **TanStack Query** (API data fetching)
   ```typescript
   const { data, isLoading } = useQuery({
     queryKey: ['cost', 'current'],
     queryFn: () => fetch('/api/cost/current').then(r => r.json())
   });
   ```

3. **SSE Hook** (real-time updates)
   ```typescript
   const { data: liveMetrics } = useSSE('/api/metrics/realtime');
   ```

4. **Module CSS** (scoped styles)
   ```typescript
   import styles from './KPICard.module.css';
   <div className={styles.card}>...</div>
   ```

### File Structure

```
datahaven-web/client/src/
  components/
    KPICard.tsx
    KPICard.module.css
    KPIStrip.tsx
    KPIStrip.module.css
    CostBudgetWidget.tsx
    CostBudgetWidget.module.css
    CostBreakdownModal.tsx
    CostBreakdownModal.module.css
  hooks/
    useCost.ts           # NEW: Cost data hook
  types/
    metrics.ts           # Types for KPI/Cost data
```

---

## Integration Points

### Dashboard Page Integration

```typescript
// datahaven-web/client/src/pages/DashboardPage.tsx
import { KPIStrip } from '@/components/KPIStrip';
import { CostBudgetWidget } from '@/components/CostBudgetWidget';

export function DashboardPage() {
  return (
    <div className="dashboard">
      <KPIStrip metrics={terminalMetrics} realtime />
      <CostBudgetWidget />
      {/* existing dashboard content */}
    </div>
  );
}
```

### Backend API Endpoints (Already Exist)

```
GET /api/cost/current         → { spend, budget, percentage }
GET /api/cost/breakdown       → { byTerminal, byModel, byProject }
GET /api/metrics/realtime     → SSE stream (terminals, inbox, sessions)
```

---

## Testing Requirements

### Manual Testing Checklist

- [ ] **KPI Cards:** All 4 status variants render correctly
- [ ] **KPI Strip:** Responsive layout works (Desktop/Tablet/Mobile)
- [ ] **Real-time:** SSE updates reflect in <5 seconds
- [ ] **Cost Widget:** Progress bar colors correct at 60%, 75%, 95%
- [ ] **Drill-down:** Modal opens with breakdown data
- [ ] **Offline:** localStorage cache works without API
- [ ] **Dark Mode:** All colors use CSS variables (no hex codes)

### Build Gate

```bash
cd datahaven-web/client
npm run build        # Must succeed with 0 errors
npm run lint         # Must pass
```

---

## DONE Criteria

1. All 4 components implemented with Module CSS
2. SSE real-time updates working
3. TanStack Query integration complete
4. localStorage caching functional
5. Build succeeds with 0 errors
6. Designer approval (NO hard-coded colors!)

---

## Priority Rationale

**Why HIGH priority:**
- JoineryTech is top project focus (user explicit request)
- Explorer validated infrastructure is ready
- Backend API already exists (MSG-BACKEND-138)
- Frontend is idle (waiting for Backend CRM API)
- Parallel development: Dashboard widgets while Backend works on CRM

**Unblocks:**
- Real-time project monitoring for JoineryTech
- Cost tracking for multi-terminal coordination
- Dashboard UI maturity milestone

---

## References

- Planning Idea: `docs/planning/ideas/2026-06-30_001_dashboard-kpi-card-system.md`
- Planning Idea: `docs/planning/ideas/2026-06-30_005_cost-budget-tracker-widget.md`
- Backend API: `terminals/backend/outbox/2026-07-04_138_msg-130-cost-monitoring-api-done.md`
- Explorer Validation: `terminals/librarian/outbox/2026-07-04_008_explorer-joinerytech-synthesis-done.md`
- Design System: `datahaven-web/client/src/index.css` (CSS variables)

---

**Next After Completion:**
When Frontend completes this, Backend should be finishing MSG-BACKEND-103 (CRM API). Then dispatch Frontend CRM integration task (real API replacing mock).

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
