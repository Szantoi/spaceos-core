---
id: MSG-FRONTEND-063
from: root
to: frontend
type: task
priority: high
status: READ
read: 2026-06-30
model: haiku
ref: MSG-DESIGNER-014-DONE
created: 2026-06-30
content_hash: e192594d4a79fa929953611ee5325cee3a75664cd844e705bb9fa05706eb1d53
---

# JoineryTech UI — KPI Dashboard Implementation

## Kontextus

A Discovery ciklus 3 kiváló UX ötletet generált. Most az első ötletet implementáljuk: **Dashboard KPI Card System**.

**Referencia ötletek:**
- `/opt/spaceos/docs/planning/ideas/2026-06-30_001_dashboard-kpi-card-system.md`
- `/opt/spaceos/docs/planning/ideas/2026-06-30_003_dark-first-bento-layout.md`

**Design System (kész):**
- `/opt/spaceos/terminals/designer/outbox/2026-06-30_014_datahaven-design-system-done.md`

## Feladat

Implementálj egy **KPI Card Strip** komponenst a Datahaven Dashboard tetejére.

### 1. KPI Card Component

```typescript
// datahaven-web/client/src/components/KPICard.tsx
interface KPICardProps {
  label: string;
  value: number | string;
  unit?: string;
  trend?: number; // % change
  status?: 'healthy' | 'warning' | 'critical';
  icon?: React.ReactNode;
  onClick?: () => void;
}
```

### 2. KPI Strip Layout

```tsx
<div className="kpi-strip">
  <KPICard label="Active Terminals" value={7} trend={+1} status="healthy" />
  <KPICard label="Inbox Queue" value={23} trend={+5} status="warning" />
  <KPICard label="Avg Task Time" value="28m" trend={-12} status="healthy" />
  <KPICard label="Pipeline Health" value="94%" status="healthy" />
  <KPICard label="API Uptime" value="99.9%" status="healthy" />
  <KPICard label="Last DONE" value="5m ago" status="healthy" />
</div>
```

### 3. CSS (styles.css vagy új kpi.css)

```css
.kpi-strip {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: var(--space-md);
  padding: var(--space-md);
  background: rgba(255, 255, 255, 0.05);
  border-bottom: 1px solid var(--border-color);
}

.kpi-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-lg);
  padding: var(--space-lg);
  cursor: pointer;
  transition: all 200ms ease;
}

.kpi-card:hover {
  background: var(--bg-hover);
  border-color: var(--accent-blue);
}

.kpi-value {
  font-size: var(--font-h1);
  font-weight: 700;
  margin: var(--space-xs) 0;
}

.kpi-trend {
  font-size: var(--font-sm);
  color: var(--text-secondary);
}

.kpi-trend.up { color: var(--accent-green); }
.kpi-trend.down { color: var(--accent-red); }
```

### 4. Dashboard Integration

Helyezd el a KPI Strip-et a Dashboard oldal tetejére (`/` route).

**Adatok forrása:**
- API: `/api/dashboard` (már létezik)
- SSE stream: `/api/dashboard/stream` (real-time frissítés)

## Acceptance Criteria

- [ ] `KPICard.tsx` komponens létrehozva
- [ ] `KPIStrip.tsx` wrapper komponens 6 card-dal
- [ ] Dark theme CSS használja a design system változókat
- [ ] Responsive: 6 card desktop → 3 card tablet → 1 card mobile
- [ ] Trend indicator (↑/↓) és status coloring
- [ ] Hover effect (border-color, background)
- [ ] Kattintható card (onClick prop)

## Időkeret

**1 óra**

## JoineryTech UI Referencia

A `docs/joinerytech/ui.jsx` tartalmaz hasznos mintákat:
- `StatusPill` — státusz badge színezés
- `Sparkline` — trend vizualizáció
- `Icon` — ikon könyvtár

Ezek adaptálhatók a KPI Card-hoz.
