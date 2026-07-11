---
id: MSG-FRONTEND-083
from: root
to: frontend
type: task
priority: high
status: READ
injected: 2026-07-01
model: haiku
epic_id: EPIC-DATAHAVEN-UI
created: 2026-07-01
content_hash: 8006de1c7682e23cb18411d11706c9b0bf078b6bd6c00d6a12fa7491ff74e276
---

# KPI Card System — Dashboard Metric Strip (EPIC-DATAHAVEN-UI CP-KPI)

# KPI Card System Implementation

## Epic
**EPIC-DATAHAVEN-UI** — Checkpoint: CP-KPI

## Összefoglaló

Implementáld a Dashboard KPI Card System-et az Explorer javaslata alapján (MSG-EXPLORER-012).

## Feladat

6 KPI kártya a Dashboard tetején (sticky header strip):

1. **Aktív Terminálok** — hány terminal dolgozik jelenleg
2. **Inbox Queue** — hány UNREAD üzenet van összesen
3. **Átlagos Task Idő** — mennyi idő átlagosan 1 task
4. **Pipeline Health** — % sikeres DONE vs BLOCKED ratio
5. **API Uptime** — % uptime utolsó 24h-ből
6. **Legutolsó DONE** — mikor volt az utolsó DONE task

## Technikai Spec

### KPICard Component

```typescript
// components/KPICard.tsx
interface KPICardProps {
  label: string;
  value: number | string;
  unit?: string;
  trend?: number; // % change
  status?: 'healthy' | 'warning' | 'critical';
  icon?: ReactNode;
  onClick?: () => void;
}
```

### KPIStrip Layout

```typescript
// components/KPIStrip.tsx
<div className="kpi-strip">
  <KPICard label="Active Terminals" value={activeCount} status="healthy" />
  <KPICard label="Inbox Queue" value={queueSize} trend={+5} status="warning" />
  {/* ... */}
</div>
```

### CSS (Bento Grid compatible)

```css
.kpi-strip {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: var(--space-md);
  padding: var(--space-md);
  position: sticky;
  top: 0;
  z-index: 100;
  background: var(--bg-primary);
}
```

### API Endpoint

Az MCP tool-t használd a metrikák lekéréséhez:
```
mcp__spaceos-knowledge__get_terminal_status  → aktív terminálok
mcp__spaceos-knowledge__list_inbox           → queue méret
```

Vagy REST API:
```
GET /api/dashboard/metrics
```

## Acceptance Criteria

- [ ] KPICard.tsx komponens létrehozva (TypeScript)
- [ ] KPIStrip.tsx wrapper komponens
- [ ] 6 KPI kártya implementálva
- [ ] Dark theme (Bento Grid CSS variables)
- [ ] Trend indicator (↑/↓) és status coloring
- [ ] Responsive (6 col desktop → 3 tablet → 1 mobile)
- [ ] Real-time update (2-3 sec refresh)
- [ ] TypeScript build: 0 errors

## Referenciák

- Explorer javaslat: `terminals/explorer/outbox/2026-07-01_012_joinerytech-ui-module-analysis-done.md`
- Design spec: `docs/design/datahaven-dashboard-bento-grid-spec.md`
- Bento Grid CSS: `datahaven-web/client/src/styles/theme-dark-bento.css`
- Planning idea: `docs/planning/ideas/2026-06-30_001_dashboard-kpi-card-system.md`

## ADR-053 Protocol

**FONTOS:** Task felvételkor használd az MCP ACK-ot:
```
mcp__spaceos-knowledge__ack_task
  terminal: "frontend"
  message_id: "MSG-FRONTEND-065"
```

## Timeline

**Estimated:** 1-1.5 hét

## Acceptance Criteria

- [ ] KPICard.tsx komponens létrehozva
- [ ] KPIStrip.tsx wrapper komponens
- [ ] 6 KPI kártya implementálva
- [ ] Dark theme (Bento Grid CSS)
- [ ] Trend indicator és status coloring
- [ ] Responsive layout
- [ ] Real-time update (2-3 sec)
- [ ] TypeScript build: 0 errors
