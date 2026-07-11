---
id: MSG-FRONTEND-101
from: explorer
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-EXPLORER-015
created: 2026-07-04
started: 2026-07-04
content_hash: 490ef4b7e040cd7dd615c842224b52a3812a3ccba9918c59ac2371e9a6893f28
---

# Task: Cost Budget Tracker Widget Implementation (Frontend)

**Epic:** JoineryTech Dashboard Enhancement
**Priority:** HIGH
**Timeline:** 2-3 days
**Effort:** Medium (widget + real-time updates + alerts)
**Blocker:** None (Backend cost API in parallel)

---

## Overview

Implement **real-time cost monitoring widget** on Datahaven Dashboard showing daily budget tracking, terminal cost breakdown, and soft/hard/critical alerts with auto-pause coordination.

---

## Requirements

### 1. Cost Budget Widget Component (`<CostBudgetWidget />`)

**Props:**
```typescript
interface CostBudgetWidgetProps {
  dailyBudget: number;              // $50.00
  currentCost: number;              // $34.27
  currencySymbol?: string;           // "$" (default)
  showTerminalBreakdown?: boolean;   // true (default)
  autoRefreshMs?: number;            // 2000 (default 2 seconds)
}
```

**Features:**
- Budget progress bar with color coding (green → yellow → red)
- Soft/Hard/Critical thresholds clearly marked
- Current spend vs budget display
- Time-to-threshold countdown (e.g., "at current rate, critical in 45 min")
- Terminal cost breakdown (collapsible)
- Alert notifications (toast/modal)

### 2. Cost Status Visualization

**Budget Bar Segments:**
```
0%        60%        80%        100%
|---------|----------|-----------|
✅ HEALTHY  ⚠️ CAUTION  🔴 CRITICAL
green      yellow      red
```

**Color Scheme:**
- **HEALTHY (0-60%):** `#10b981` (green) — Safe operating zone
- **CAUTION (60-80%):** `#f59e0b` (amber) — Soft alert, warn operators
- **CRITICAL (80-100%):** `#ef4444` (red) — Hard alert, auto-pause workers
- **EXCEEDED (>100%):** `#7c3aed` (purple) — Over budget

### 3. Terminal Cost Breakdown

**Expandable Section:** Click to see per-terminal costs

```typescript
interface TerminalCost {
  terminal: string;           // "backend", "frontend", "architect"
  cost: number;              // $8.50
  percentage: number;        // 24.8%
  status: 'healthy' | 'caution' | 'critical';
  thresholdMinutes?: number; // "at current rate, critical in 30 min"
}
```

**Display Format:**
```
Terminal Breakdown (4 costs)
│
├─ Backend:    $12.40  (36%) 🟢
├─ Architect:   $8.90  (26%) 🟢
├─ Frontend:    $7.80  (23%) 🟡
└─ Designer:    $5.17  (15%) 🟢
```

### 4. Alert System

**Toast Notifications** (Soft Alert at 60%):
```
⚠️  Budget Alert
Daily budget at 60% ($30.00 spent of $50.00)
[Dismiss] [View Details]
```

**Modal Dialog** (Hard Alert at 80%):
```
🔴 Critical Budget Alert
Daily budget at 80% ($40.00 spent of $50.00)

Auto-pausing new workers to prevent overspend.
Current workers will continue to completion.

Recent costs:
└─ Worker-12 (backend): +$2.10 (30s ago)
└─ Worker-08 (architect): +$1.50 (45s ago)

[Continue Anyway] [View Dashboard]
```

**Critical Exceeded (>100%)**:
```
🔴 Budget Exceeded
Daily budget at 125% ($62.50 spent of $50.00)

All workers PAUSED.
Contact Conductor for budget override.

[Request Override] [View Dashboard]
```

### 5. Real-Time Updates (SSE)

**Integration:**
```typescript
const costData = useSSE('/api/monitoring/cost/stream');

// Display widget with live updates
<CostBudgetWidget
  dailyBudget={50}
  currentCost={costData.current}
  terminalBreakdown={costData.terminals}
/>
```

**Update frequency:** 1-2 seconds (aggressive for cost alerts)
**Latency target:** ≤500ms end-to-end

### 6. 7-Day History Chart

**Optional expandable section** showing daily cost trend:
```
Daily Cost History (7 days)
│  $60
│     ╱╲
│    ╱  ╲___
│   ╱
│__╱
 Mon Tue Wed Thu Fri Sat Sun
```

**Data points:**
- Daily total cost
- Daily budget threshold (horizontal line)
- Highlight days that exceeded budget

### 7. Auto-Pause Coordination

**Backend Integration:**
```typescript
// When critical threshold is reached
if (currentCost > dailyBudget * 0.8) {
  // Notify Conductor to pause new worker spawns
  await fetch('/api/monitoring/cost/pause-threshold-reached', {
    method: 'POST',
    body: JSON.stringify({
      currentCost,
      dailyBudget,
      terminalCosts
    })
  });
}
```

**User can manually override** (requires Conductor authorization)

---

## Acceptance Criteria

- [ ] `<CostBudgetWidget />` component implemented
- [ ] Progress bar with 4 color zones (green/amber/red/purple)
- [ ] Real-time SSE updates (1-2 sec refresh)
- [ ] Soft alert toast at 60% threshold
- [ ] Hard alert modal at 80% threshold
- [ ] Critical exceeded modal at >100%
- [ ] Terminal breakdown collapsible section (4 terminals min)
- [ ] Time-to-threshold countdown calculation
- [ ] 7-day history chart (optional but recommended)
- [ ] Auto-pause coordination trigger
- [ ] Dark theme styling (consistent with KPI cards)
- [ ] Responsive design (desktop/tablet/mobile)
- [ ] Unit tests (80%+ coverage)
- [ ] Integration tests (real SSE endpoint)
- [ ] Storybook stories (all alert states)
- [ ] Performance: <500ms latency, smooth animations

---

## Technical Stack

- React 18 + TypeScript
- CSS Modules (dark theme, matching KPI cards)
- TanStack Query + SSE hook
- Chart library: Recharts or Chart.js for 7-day history
- Toast notifications: existing toast library in Portal
- Modal: existing modal library in Portal

---

## Blockers

❌ **Backend cost monitoring API** required
- Endpoints: `GET /api/monitoring/cost/stream` (SSE)
- Endpoints: `GET /api/monitoring/cost/today`, `GET /api/monitoring/cost/terminal/:terminal`
- Response: `{ current, dailyBudget, terminals: [{terminal, cost, percentage, status}], history: [{date, cost}] }`
- Backend task: MSG-BACKEND-126 (parallel)

---

## Reference Files

- `/opt/spaceos/docs/joinerytech/DASHBOARD_PATTERNS.md`
- Existing dashboard: `src/pages/DashboardPage.tsx`
- KPI Strip component (MSG-FRONTEND-100): similar layout pattern
- SSE hook: `src/hooks/useSSE.ts`

---

## Next Steps

1. Review cost alert requirements with Conductor (validation on alert thresholds)
2. Implement `<CostBudgetWidget />` component (day 1)
3. Integrate SSE + real-time cost updates (day 1-2)
4. Test alert transitions (soft → hard → critical) (day 2)
5. Add 7-day history chart (day 2)
6. Submit DONE when all acceptance criteria met

---

**Dependency:** Backend MSG-BACKEND-126 (cost monitoring API)
**Ready to start:** Yes (mock cost data while Backend develops endpoint)

🤖 Prepared by Explorer
📅 2026-07-04
