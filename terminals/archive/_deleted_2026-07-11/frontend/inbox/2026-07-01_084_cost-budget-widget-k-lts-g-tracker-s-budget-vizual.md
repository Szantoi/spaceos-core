---
id: MSG-FRONTEND-084
from: root
to: frontend
type: task
priority: high
status: READ
injected: 2026-07-01
model: sonnet
epic_id: EPIC-DATAHAVEN-UI
created: 2026-07-01
content_hash: 57749a83e0df32d2095b9b32322a767e001fa2d2c25415d89561a6477cfdc3d3
---

# Cost Budget Widget — Költség tracker és budget vizualizáció (EPIC-DATAHAVEN-UI CP-COST)

# Cost Budget Widget Implementation

## Epic
**EPIC-DATAHAVEN-UI** — Checkpoint: CP-COST

## Összefoglaló

Implementáld a Cost Budget Widget-et, ami real-time költség- és budget adatokat jelenít meg a Dashboard-on.

## Feladat

### Widget Features
1. **Költség összesítő** — napi/heti/havi költség Haiku/Sonnet/Opus bontásban
2. **Budget bar** — vizuális progress bar a teljes budget-hez képest
3. **Alert thresholds** — piros/sárga/zöld státusz
4. **Trend sparkline** — mini chart az utolsó 24h-ról

### API Endpoint
```
GET /api/dashboard/cost-stats
```

Response:
```json
{
  "daily": { "haiku": 0.45, "sonnet": 1.20, "opus": 2.50 },
  "weekly": { "total": 28.50, "budget": 50.00, "percentage": 57 },
  "monthly": { "total": 85.00, "budget": 200.00, "percentage": 42.5 },
  "trend": [0.8, 1.2, 0.9, 1.5, 1.1, 0.7, 1.3]
}
```

## Technikai Spec

### CostBudgetWidget Component

```typescript
// components/CostBudgetWidget.tsx
interface CostBudgetWidgetProps {
  refreshInterval?: number; // ms, default 30000
  showDetails?: boolean;
}

interface CostData {
  daily: { haiku: number; sonnet: number; opus: number };
  weekly: { total: number; budget: number; percentage: number };
  monthly: { total: number; budget: number; percentage: number };
  trend: number[];
}
```

### Styling (Bento Grid compatible)

```css
.cost-widget {
  grid-column: span 2; /* Szélesebb widget */
  background: var(--bg-secondary);
  border-radius: var(--radius-lg);
  padding: var(--space-lg);
}

.budget-bar {
  height: 8px;
  background: var(--bg-tertiary);
  border-radius: var(--radius-sm);
}

.budget-fill {
  background: linear-gradient(90deg, var(--color-success), var(--color-warning));
  transition: width 0.3s ease;
}
```

## Acceptance Criteria

- [ ] CostBudgetWidget.tsx komponens létrehozva (TypeScript)
- [ ] Költség breakdown (Haiku/Sonnet/Opus) megjelenítve
- [ ] Budget progress bar vizuális feedback-el
- [ ] Alert status (green/yellow/red) threshold-ok alapján
- [ ] Trend sparkline (utolsó 24h)
- [ ] 30 másodperces auto-refresh
- [ ] Dark theme (Bento Grid CSS variables)
- [ ] Responsive (collapse tablet-en)
- [ ] TypeScript build: 0 errors

## Referenciák

- KPI Card komponensek: `datahaven-web/client/src/components/KPICard.tsx`
- Design spec: `docs/design/datahaven-dashboard-bento-grid-spec.md`
- Planning idea: `docs/planning/ideas/2026-06-30_005_cost-budget-tracker-widget.md`

## ADR-053 Protocol

**FONTOS:** Task felvételkor használd az MCP ACK-ot:
```
mcp__spaceos-knowledge__ack_task
  terminal: "frontend"
  message_id: "MSG-FRONTEND-084"
```

## Timeline

**Estimated:** 4-6 óra

## Acceptance Criteria

- [ ] CostBudgetWidget.tsx komponens létrehozva (TypeScript)
- [ ] Költség breakdown (Haiku/Sonnet/Opus) megjelenítve
- [ ] Budget progress bar vizuális feedback-el
- [ ] Alert status (green/yellow/red) threshold-ok alapján
- [ ] Trend sparkline (utolsó 24h)
- [ ] 30 másodperces auto-refresh
- [ ] Dark theme (Bento Grid CSS variables)
- [ ] Responsive layout
- [ ] TypeScript build: 0 errors
