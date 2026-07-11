---
id: MSG-FRONTEND-064
from: conductor
to: frontend
type: task
priority: high
status: DONE
read: 2026-06-30
completed: 2026-06-30
model: sonnet
ref: MSG-DESIGNER-020
blocked_by: MSG-DESIGNER-020
created: 2026-06-30
content_hash: 05bd14c87c050e2b69971da8dce5150d71ab763ec95408211768ee33f26e4b2f
---

# Datahaven Dashboard — Bento Grid Layout Implementation

## Kontextus

A Discovery ciklus 2. prioritása: **Dark-First Bento Grid Layout** redesign.

**Előfeltétel:** Designer MSG-DESIGNER-020 DONE (design spec + CSS variables)

## Feladat

Implementáld a Datahaven Dashboard teljes layout redesign-ját Bento grid alapokon, dark-first theme-mel, és data-dense pattern-t alkalmazva.

## Design Spec

**Forrás:**
- Designer DONE outbox: `terminals/designer/outbox/2026-06-30_020_...-done.md`
- Design doc: `docs/design/datahaven-dashboard-bento-grid-spec.md`
- CSS variables: `datahaven-web/client/src/styles/theme-dark-bento.css`

## Implementációs Lépések

### 1. CSS Grid Layout Setup

**File:** `datahaven-web/client/src/styles/dashboard-bento.css`

**Grid container:**
```css
.dashboard-bento-grid {
  display: grid;
  grid-template-columns: repeat(12, 1fr);
  grid-auto-rows: minmax(120px, auto);
  gap: var(--space-md); /* 16px */
  padding: var(--space-md);
  background: var(--bg-primary);
}

/* KPI Strip */
.kpi-strip {
  grid-column: 1 / -1;
  grid-row: auto;
}

/* Kanban Board */
.kanban-board {
  grid-column: 1 / 7;   /* 50% */
  grid-row: auto / span 4;
}

/* Timeline/Gantt */
.timeline-graph {
  grid-column: 7 / 13;  /* 50% */
  grid-row: auto / span 4;
}

/* Sidebar */
.details-sidebar {
  grid-column: 1 / 4;   /* 25% */
  grid-row: auto / span 4;
}

/* Task List */
.task-list {
  grid-column: 4 / 10;  /* 50% */
  grid-row: auto / span 4;
}

/* Alert Panel */
.alert-panel {
  grid-column: 10 / 13; /* 25% */
  grid-row: auto / span 2;
}

/* Responsive breakpoints */
@media (max-width: 1200px) {
  .kanban-board { grid-column: 1 / -1; }
  .timeline-graph { grid-column: 1 / -1; }
  .details-sidebar { grid-column: 1 / -1; }
  .task-list { grid-column: 1 / -1; }
  .alert-panel { grid-column: 1 / -1; }
}

@media (max-width: 768px) {
  .dashboard-bento-grid {
    grid-template-columns: 1fr;
    gap: var(--space-sm);
  }
}
```

### 2. Theme Integration

**Import CSS variables:**
```typescript
// DashboardPage.tsx
import '../styles/theme-dark-bento.css';
import '../styles/dashboard-bento.css';
```

**Apply theme classes:**
```tsx
<div className="dashboard-bento-grid" data-theme="dark">
  <KPIStrip className="kpi-strip" />
  <KanbanBoard className="kanban-board" />
  <TimelineGraph className="timeline-graph" />
  <DetailsSidebar className="details-sidebar" />
  <TaskList className="task-list" />
  <AlertPanel className="alert-panel" />
</div>
```

### 3. Dark Card Component

**File:** `datahaven-web/client/src/components/DarkCard.tsx`

```typescript
interface DarkCardProps {
  children: React.ReactNode;
  className?: string;
  onClick?: () => void;
  status?: 'healthy' | 'warning' | 'critical' | 'info';
}

export const DarkCard: React.FC<DarkCardProps> = ({
  children,
  className,
  onClick,
  status
}) => {
  const statusBorderColor = {
    healthy: 'var(--status-healthy)',
    warning: 'var(--status-warning)',
    critical: 'var(--status-critical)',
    info: 'var(--status-info)'
  }[status];

  return (
    <div
      className={`dark-card ${className || ''}`}
      onClick={onClick}
      style={{
        background: 'var(--bg-card)',
        border: `1px solid var(--border-color)`,
        borderLeft: status ? `3px solid ${statusBorderColor}` : undefined,
        borderRadius: '8px',
        padding: 'var(--space-md)',
        transition: 'all 200ms ease'
      }}
    >
      {children}
    </div>
  );
};
```

### 4. Data-Dense Table Component

**File:** `datahaven-web/client/src/components/DataDenseTable.tsx`

```typescript
interface TableRow {
  id: string;
  columns: string[];
  details?: React.ReactNode;
  status?: 'healthy' | 'warning' | 'critical';
}

export const DataDenseTable: React.FC<{ rows: TableRow[] }> = ({ rows }) => {
  const [expandedRow, setExpandedRow] = useState<string | null>(null);

  return (
    <table className="data-dense-table">
      <thead>
        <tr>
          <th>ID</th>
          <th>Status</th>
          <th>Details</th>
        </tr>
      </thead>
      <tbody>
        {rows.map(row => (
          <>
            <tr key={row.id} onClick={() => setExpandedRow(row.id)}>
              <td>{row.id}</td>
              <td>{row.status === 'healthy' ? '✅' : '⚠️'}</td>
              <td>▼</td>
            </tr>
            {expandedRow === row.id && (
              <tr className="expanded-details">
                <td colSpan={3}>{row.details}</td>
              </tr>
            )}
          </>
        ))}
      </tbody>
    </table>
  );
};
```

### 5. Responsive Layout Testing

**Breakpoints to test:**
- Desktop: 1920x1080 (12 col grid, asymmetric)
- Tablet: 1024x768 (6 col grid, stacked)
- Mobile: 480x800 (1 col grid, vertical stack)

**Chrome DevTools:**
```bash
# Toggle device toolbar (Cmd+Shift+M)
# Test: iPhone 12 Pro, iPad Air, Desktop
```

## Acceptance Criteria

- [ ] `dashboard-bento.css` CSS Grid layout created (12 col, responsive)
- [ ] `theme-dark-bento.css` imported and applied
- [ ] `DarkCard.tsx` component created (status border, hover state)
- [ ] `DataDenseTable.tsx` component with progressive disclosure
- [ ] DashboardPage.tsx refactored to use Bento grid
- [ ] Responsive testing: Desktop, Tablet, Mobile ✅
- [ ] Accessibility: Keyboard nav, ARIA labels, contrast validation
- [ ] TypeScript compilation: 0 errors
- [ ] Build successful: `npm run build`
- [ ] Dark theme as default (no white flash)

## Testing Checklist

- [ ] Desktop (≥1200px): 12 col grid, asymmetric layout
- [ ] Tablet (768-1200px): 6 col grid, Kanban/Timeline full width
- [ ] Mobile (≤768px): 1 col grid, stacked vertically
- [ ] Contrast ratio: 4.5:1 minimum (WebAIM checker)
- [ ] Keyboard nav: Tab through cards, focus-visible outline
- [ ] Hover states: Background, border, shadow transitions
- [ ] Progressive disclosure: Click row → expand details

## Timeline

**Estimated:** 4-6 hours (layout + components + testing)

## References

- Design spec: `docs/design/datahaven-dashboard-bento-grid-spec.md`
- Designer outbox: `terminals/designer/outbox/2026-06-30_020_...-done.md`
- Explorer UX research: `docs/planning/ideas/2026-06-30_003_dark-first-bento-layout.md`

## Next Steps

**After DONE:**
- Conductor dispatches MSG-FRONTEND-065 (Kanban Real-Time Feedback)
- Continue Discovery cycle implementation (8 ideas → 5 remaining)

---

**Priority:** HIGH — 2. ötlet a Discovery ciklus 8-ból
**Blocked by:** MSG-DESIGNER-020 (design spec completion)
**Model:** Sonnet (layout restructuring + component creation)
