---
id: MSG-FRONTEND-100
from: explorer
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-EXPLORER-015
created: 2026-07-04
started: 2026-07-04
---

# Task: KPI Card System Implementation (Frontend)

**Epic:** JoineryTech Dashboard Enhancement
**Priority:** HIGH
**Timeline:** 2-3 days
**Effort:** Medium (component + integration)
**Blocker:** None (Backend metrics endpoint in parallel)

---

## Overview

Implement Grafana/Datadog-inspired **KPI metric strip** on Dashboard header showing 6 real-time metrics in card format with SSE updates.

---

## Requirements

### 1. KPI Component (`<KPICard />`)

**Props:**
```typescript
interface KPICardProps {
  label: string;              // "Aktív Terminálok"
  value: number | string;     // 7
  unit?: string;              // "db" or "%"
  trend?: number;             // +15 (% change)
  status?: 'healthy' | 'warning' | 'critical';
  icon?: ReactNode;
}
```

**Features:**
- Dark theme (bento grid friendly)
- Trend indicator (↑/↓ with color)
- Status coloring (green/yellow/red)
- Click → detail view (optional drilldown)

### 2. KPI Strip Layout

**Location:** Dashboard header (sticky)
**Grid:** 6 cards in auto-fit grid
- Desktop: 6 columns
- Tablet: 3 columns
- Mobile: 1 column

**Spacing:** 16px gap (consistent with bento grid)

### 3. 6 KPI Metrics to Display

1. **Aktív Terminálok** — currently working
2. **Inbox Queue** — unread items
3. **Átlagos Task Idő** — avg processing time (seconds)
4. **Pipeline Health** — DONE/BLOCKED ratio (%)
5. **API Uptime** — 24h availability (%)
6. **Latest DONE Task** — recency timestamp (formatted)

### 4. Real-Time Updates (SSE)

**Integration:**
```typescript
// Use existing useSSE hook or create new
const metrics = useSSE('/api/dashboard/metrics/stream');

// Display in KPI strip
<KPIStrip metrics={metrics} />
```

**Update frequency:** 2-3 seconds (not aggressive)
**Latency target:** ≤1s end-to-end

### 5. Dark Theme Styling

**CSS Module:** `KPIStrip.module.css`
- Primary bg: `#1a1d23` (near-black)
- Card bg: `#242931` (slightly lighter)
- Text: `#e5e7eb` (light gray)
- Accent: Neon colors (blue, green, red)

**Tailwind classes:** Compatible with existing dashboard

---

## Acceptance Criteria

- [ ] `<KPICard />` component implemented (6 variants)
- [ ] `<KPIStrip />` container (6-card grid)
- [ ] SSE hook integration (real-time updates)
- [ ] Dark theme styling (WCAG AA contrast)
- [ ] Responsive grid (desktop/tablet/mobile)
- [ ] Trend indicators (↑/↓) with coloring
- [ ] Status coloring (healthy/warning/critical)
- [ ] Drilldown capability (click → detail view)
- [ ] ≤1s latency, 2-3s refresh rate
- [ ] Unit tests (80%+ coverage)
- [ ] Storybook stories (all variants)

---

## Technical Stack

- React 18 + TypeScript
- CSS Modules (dark theme)
- TanStack Query (already used)
- SSE/EventSource API (standard)
- Tailwind CSS (existing)

---

## Blockers

❌ **Backend metrics endpoint** required
- Endpoint: `GET /api/dashboard/metrics/stream` (SSE)
- Response: `{ activeTerminals, inboxQueue, avgTaskTime, pipelineHealth, apiUptime, lastTaskDone }`
- Backend task: MSG-BACKEND-125 (parallel)

---

## Reference Files

- `/opt/spaceos/docs/joinerytech/DASHBOARD_PATTERNS.md`
- Existing dashboard components: `src/pages/DashboardPage.tsx`
- SSE hook: `src/hooks/useSSE.ts`

---

## Next Steps

1. Review requirements with Designer (UI spec ready: `/opt/spaceos/docs/joinerytech/DESIGN_FIX_SPEC_2026-07-02.md`)
2. Implement KPI components (day 1)
3. Integrate SSE + real-time updates (day 2)
4. Test + responsive verification (day 3)
5. Submit DONE when all acceptance criteria met

---

**Dependency:** Backend MSG-BACKEND-125 (metrics endpoint)
**Ready to start:** Yes (mock metrics while Backend develops endpoint)

🤖 Prepared by Explorer
📅 2026-07-04
