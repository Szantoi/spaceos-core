---
id: IDEA-2026-06-30-001
title: "Dashboard KPI Card System — Grafana-inspired Metric Strip"
category: ux
priority: high
effort: medium
domain: industrial
source: "Grafana Dashboards, Datadog Design Patterns, Production Dashboard Grid Systems"
created: 2026-06-30
status: idea
---

# Dashboard KPI Card System

## Összefoglaló

A Grafana és Datadog dashboard-ök sikere azon alapul, hogy az **4-6 legfontosabb KPI-t** az oldal tetejére helyezik egy **card-alapú metric strip** formájában. Ez a minta hatékonyan kiemeli a kritikus információkat, amik első pillantásra észrevehetőek.

**Datahaven Dashboard-ön adaptálható:** Terminál státuszok, feladat queue méretek, pipeline health metrikák — mind kiváló KPI jelöltek.

## Pattern Leírás

### Layout
- **Sticky header strip** — 4-6 KPI card csoportosítva
- **Card szélességi arány:** egyenlő szélesség vagy aszimmetrikus (nagyobb card = fontosabb metrika)
- **Szín/Ikonok:** Gyors vizuális azonosítás (zöld = healthy, narancssárga = warning, piros = critical)
- **Real-time frissítés:** SSE vagy WebSocket → card értékek 2-3 másodpercenként frissülnek

### Card szerkezete
```
┌─────────────────────┐
│  📊 METRIC LABEL    │
│  ▲ 42               │  ← value (nagy, olvasható)
│  +2.5% (trend)      │  ← trend indicator
└─────────────────────┘
```

### Datahaven alkalmazás
**Javaslat:** Felületi dashboard-ön az alábbi KPI-ek:
1. **Aktív Terminálok** — hány terminal dolgozik jelenleg
2. **Inbox Queue** — hány unread üzenet van összesen
3. **Átlagos Task Idő** — mennyi idő átlagosan 1 task
4. **Pipeline Health** — % sikeres DONE vs BLOCKED ratio
5. **Datahaven API Uptime** — % uptime utolsó 24h-ből
6. **Legutolsó DONE Task** — órája / percé / mostanában

## Technikai Implementáció

### Frontend (React/TypeScript)
```typescript
// KPICard.tsx
interface KPICard {
  label: string;
  value: number | string;
  unit?: string;
  trend?: number; // % change
  status?: 'healthy' | 'warning' | 'critical';
  icon?: ReactNode;
  onClick?: () => void;
}

// Dashboard header
<div className="kpi-strip">
  <KPICard label="Active Terminals" value={7} trend={+1} status="healthy" />
  <KPICard label="Inbox Queue" value={23} trend={+5} status="warning" />
  <KPICard label="Avg Task Time" value="28m" trend={-12} status="healthy" />
  {/* ... */}
</div>
```

### Backend (Node.js/Knowledge Service)
```typescript
// MCP tool: get_dashboard_metrics
{
  "activeTerminals": 7,
  "inboxQueue": 23,
  "avgTaskTime": 1680, // seconds
  "pipelineHealth": 0.94, // 94% success rate
  "apiUptime": 0.999,
  "lastTaskDone": {
    "time": "2026-06-30T12:45:00Z",
    "taskId": "MSG-BACKEND-042"
  }
}
```

### Real-time Updates
- SSE endpoint: `GET /api/dashboard/metrics/stream`
- Refetch interval: 2-3 secondenként (configurable)
- Caching: Dashboard state memorization (React Query)

## CSS/Design System

**Datahaven theme-ben:**
- **Dark mode:** Bento grid layout (aszmimetrikus, card-based)
- **Color system:** Status-based (✅ green, ⚠️ orange, ❌ red)
- **Typography:** KPI value = 2.5rem (nagy, olvasható)
- **Spacing:** 16px gap between cards
- **Responsive:** 6 cards desktop → 3 cards tablet → 1 card mobile

```css
.kpi-strip {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 16px;
  padding: 16px;
  background: rgba(255, 255, 255, 0.05);
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.kpi-card {
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 8px;
  padding: 16px;
  cursor: pointer;
  transition: all 200ms ease;
}

.kpi-card:hover {
  background: rgba(255, 255, 255, 0.12);
  border-color: rgba(255, 255, 255, 0.2);
}

.kpi-value {
  font-size: 2.5rem;
  font-weight: 700;
  margin: 8px 0;
}

.kpi-trend {
  font-size: 0.875rem;
  color: var(--trend-color); /* green/orange/red */
}
```

## Acceptance Criteria

- [ ] 6 KPI card component létrehozva
- [ ] Real-time update MCP endpoint implementálva
- [ ] Dark theme CSS/Bento grid
- [ ] Trend indicator (↑/↓) és status coloring
- [ ] Responsive design (desktop/tablet/mobile)
- [ ] Click → detail view (drilldown capability)
- [ ] 2 másodperc refresh Rate ≤ 1s latency
- [ ] Analytics tracking (which KPI users click)

## Hivatkozások

- [Grafana Dashboard Best Practices](https://grafana.com/grafana/dashboards/)
- [Datadog Executive Dashboards](https://www.datadoghq.com/blog/datadog-executive-dashboards/)
- [Production Dashboard Grid Systems](https://fullstackinfra.substack.com/p/day-64-building-a-production-grade)
