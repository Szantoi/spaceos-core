---
id: IDEA-2026-06-30-003
title: "Dark-First Bento Grid Layout — Data-Dense Dashboard"
category: ux
priority: high
effort: medium
domain: industrial
source: "SaaS Dashboard Design 2026, Dark Mode UI Trends, Bento Grid Layout Patterns, Data-Dense Layout Best Practices"
created: 2026-06-30
status: idea
---

# Dark-First Bento Grid Layout — Data-Dense Dashboard

## Összefoglaló

A 2026-os UI trendek szerint a **dark-first design** (dark theme first, light theme adapted after) a standard. Erre építve a **Bento grid** (aszimmetrikus card grid) a legjobb megoldás **data-dense** dashboard-ök számára, ahol az analitikusok/inženörok 4+ órányi munkaidőt töltenek nézéssel.

**Adatahaven Dashboard-ön kritikus:** Terminál státusz, task metrikák, pipeline visualization — mind igényli az optimális kontrasztot és információ-szervezést.

## Pattern Leírás

### Bento Grid Layout
```
┌──────────────────────────────────┐
│   KPI Strip (6 cards, 1/6 tall)  │  ← 4-6 KPI (Sticky top)
├──────────────┬──────────────────┤
│ Kanban Board │  Timeline Graph  │  ← 2 col, 1/2 height
│  (Discovery) │  (Project Gantt)  │
├──────────────┼──────────┬───────┤
│              │  Task    │ Alert │  ← Asymmetric
│ Details      │  List    │ Panel │
│ Sidebar      │ (tall)   │       │
└──────────────┴──────────┴───────┘
```

**Jellemzők:**
- Aszimmetrikus card méretek (1/2, 2/3, 1/3 width)
- CSS Grid auto-placement
- Responsive: grid reorganizes at breakpoints
- Dark background (minimal eye strain)
- Translucent card backgrounds (visual hierarchy)

### Dark-First Design Principles
1. **Contrast First:** WCAG AA minimum (4.5:1 text/background)
2. **Opaque Backgrounds:** Numbers need clean, opaque cards (no translucency for text)
3. **Color Palette:**
   - Primary dark: `#1a1d23` (near-black)
   - Card bg: `#242931` (slightly lighter)
   - Text: `#e5e7eb` (light gray, not white)
   - Accent: Neon colors (blues, greens) for data highlights

4. **Reduced Eye Strain:**
   - Minimal animation (only on user interaction)
   - Soft shadows (not harsh blacks)
   - Consistent spacing (16px base unit)

## Technikai Implementáció

### CSS Grid + Bento Layout
```css
.dashboard-grid {
  display: grid;
  grid-template-columns: repeat(12, 1fr);
  grid-auto-rows: minmax(120px, auto);
  gap: 16px;
  padding: 16px;
  background: #1a1d23;
}

/* KPI Strip: full width, 1 row */
.kpi-strip {
  grid-column: 1 / -1;
  grid-row: auto;
}

/* Kanban: 2 columns, tall */
.kanban-board {
  grid-column: 1 / 7;   /* 50% */
  grid-row: auto / span 4;
}

/* Timeline/Gantt: 2 columns, tall */
.timeline-graph {
  grid-column: 7 / 13;  /* 50% */
  grid-row: auto / span 4;
}

/* Sidebar details: 1 column, tall */
.details-sidebar {
  grid-column: 1 / 4;   /* 33% */
  grid-row: auto / span 4;
}

/* Task list: 2 columns, tall */
.task-list {
  grid-column: 4 / 10;  /* 50% */
  grid-row: auto / span 4;
}

/* Alerts: 1 column */
.alert-panel {
  grid-column: 10 / 13; /* 25% */
  grid-row: auto / span 2;
}

/* Responsive: Tablet breakpoint */
@media (max-width: 1200px) {
  .kanban-board { grid-column: 1 / -1; }
  .timeline-graph { grid-column: 1 / -1; grid-row: auto / span 3; }
  .details-sidebar { grid-column: 1 / -1; }
  .task-list { grid-column: 1 / -1; }
  .alert-panel { grid-column: 1 / -1; }
}

/* Mobile breakpoint */
@media (max-width: 480px) {
  .dashboard-grid {
    grid-template-columns: 1fr;
    gap: 12px;
  }
  * {
    grid-column: 1 / -1;
  }
}
```

### React Component Structure
```typescript
// DashboardLayout.tsx — Dark-first theme
const DarkTheme = {
  bg: { primary: '#1a1d23', card: '#242931' },
  text: { primary: '#e5e7eb', secondary: '#9ca3af' },
  accent: { success: '#10b981', warning: '#f59e0b', error: '#ef4444' },
  border: '#3f444f'
};

export const Dashboard = () => (
  <div className="dashboard-grid" style={{ background: DarkTheme.bg.primary }}>
    <KPIStrip />
    <KanbanBoard />
    <TimelineGraph />
    <DetailsSidebar />
    <TaskList />
    <AlertPanel />
  </div>
);

// Card wrapper with dark theme
const DarkCard = ({ children }) => (
  <div
    style={{
      background: DarkTheme.bg.card,
      border: `1px solid ${DarkTheme.border}`,
      borderRadius: '8px',
      padding: '16px'
    }}
  >
    {children}
  </div>
);
```

### Progressive Disclosure (Data-Dense Pattern)
```typescript
// Show summary, reveal details on demand
<div className="data-dense-table">
  <tr>
    <td>{task.title}</td>
    <td>{task.status}</td>
    <td onClick={() => toggleDetail(task.id)}>
      ▼ Details
    </td>
  </tr>
  {isDetailOpen && (
    <ExpandableRow>
      {/* Full task metadata, timeline, dependencies */}
    </ExpandableRow>
  )}
</div>
```

## Color System (Dark-First)

```json
{
  "dark": {
    "bg": {
      "primary": "#1a1d23",
      "secondary": "#242931",
      "tertiary": "#2f3440"
    },
    "text": {
      "primary": "#e5e7eb",
      "secondary": "#9ca3af",
      "muted": "#6b7280"
    },
    "status": {
      "healthy": "#10b981",
      "warning": "#f59e0b",
      "critical": "#ef4444",
      "info": "#3b82f6"
    }
  },
  "light": {
    "bg": {
      "primary": "#ffffff",
      "secondary": "#f9fafb",
      "tertiary": "#f3f4f6"
    },
    "text": {
      "primary": "#1f2937",
      "secondary": "#6b7280",
      "muted": "#9ca3af"
    },
    "status": {
      "healthy": "#059669",
      "warning": "#d97706",
      "critical": "#dc2626",
      "info": "#2563eb"
    }
  }
}
```

## Datahaven Dashboard Redesign

### Layout Structure
1. **KPI Strip (Sticky)** — Terminal status, queue size, pipeline health
2. **Kanban Board + Timeline** — Discovery & Delivery tracks side-by-side
3. **Task List + Alert Panel** — Data-dense table + critical alerts
4. **Sidebar** — Terminal details, quick actions

### Data-Dense Patterns
- **Tables:** Compact rows (32px height), sortable columns, progressive disclosure
- **Aggregation:** Max 20 rows visible, scroll or pagination
- **Icons:** Status badges (✅/⚠️/❌) instead of text
- **Contrast:** Strong (WCAG AAA where possible)

## Acceptance Criteria

- [ ] CSS Grid Bento layout (12 column, responsive)
- [ ] Dark theme colors defined (primary, secondary, accents)
- [ ] Light theme adapted from dark (not inverse)
- [ ] WCAG AA contrast ratio validation (4.5:1 minimum)
- [ ] Progressive disclosure (expandable rows/modals)
- [ ] Responsive breakpoints: 1200px, 768px, 480px
- [ ] Data-dense table layout (max compact rendering)
- [ ] 80+ FPS performance (scroll, sort, filter)
- [ ] Dark mode as default, light mode as alternate
- [ ] Accessibility: Keyboard nav + screen reader tested

## Hivatkozások

- [SaaS Dashboard Design Examples 2026](https://www.925studios.co/blog/saas-dashboard-design-examples-2026)
- [Dark Mode UI Design Trends 2026](https://www.asappstudio.com/admin-dashboard-designs-2026/)
- [Dark Admin Dashboard Templates](https://wrappixel.com/blog/best-dark-mode-dashboard-designs-and-templates)
- [Data-Dense Dashboard Accessibility](https://www.designsystemproblems.com/accessibility-compliance/lighthouse-accessibility/)
- [Bento Grid Layout Pattern](https://www.925studios.co/blog/saas-dashboard-design-examples-2026#bento-grid)
