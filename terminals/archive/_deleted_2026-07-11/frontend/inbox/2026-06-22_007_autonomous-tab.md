---
id: MSG-FRONTEND-007
from: conductor
to: frontend
type: task
priority: medium
status: DONE
model: sonnet
ref: MSG-CONDUCTOR-007
created: 2026-06-22
completed: 2026-06-22
content_hash: ecfda163dd60a1d8ee349d37358893a2f0333c23f20a717a64f6c6114bdc40cd
---

# Dashboard Autonomous Tab Implementation

## Kontextus

Az autonóm fejlesztési folyamatok vizuális monitorozása érdekében új tab-ot kell hozzáadni a Datahaven dashboardhoz.

## Feladat

Implementálj egy "Autonomous" oldalt a Datahaven dashboardon, amely megjeleníti az autonomous dev history-t és real-time státuszt.

## Specifikáció

### URL struktúra

- **Route:** `/autonomous`
- **Navigation:** Dashboard header-ben új tab (Dashboard | **Autonomous** | Kanban | Planning | Projects)

### UI elemek

#### 1. Státusz kártyák (felső sor)

```
┌────────────────────┬────────────────────┬────────────────────┐
│ Current Cycle      │ Next Scheduled     │ Skip Rate          │
│ #42                │ 10:30 (2 min)      │ 15% (3/20)         │
└────────────────────┴────────────────────┴────────────────────┘
```

#### 2. Cycle history táblázat

| ID | Timestamp | Conductor Status | Dispatched Task | Result | Duration |
|----|-----------|------------------|-----------------|--------|----------|
| 42 | 10:28 | ✅ Working | MSG-BACKEND-016 | ✅ Dispatched | 12s |
| 41 | 10:26 | ⏭️ Skipped | - | ⚠️ Busy (backend) | 2s |
| 40 | 10:24 | ✅ Working | MSG-FRONTEND-006 | ✅ Dispatched | 15s |

#### 3. Cycles/hour grafikon

- Chart library: Recharts (már telepítve)
- X tengely: utolsó 24 óra
- Y tengely: cycles count
- Bar chart: zöld = dispatched, sárga = skipped

### Implementáció részletek

#### Frontend komponensek

**Új fájl:** `datahaven-web/client/src/pages/AutonomousPage.tsx`

```tsx
export default function AutonomousPage() {
  const { data, loading } = useAutonomous();

  return (
    <div className="p-6">
      <h1>Autonomous Development</h1>

      {/* Státusz kártyák */}
      <div className="grid grid-cols-3 gap-4 mb-6">
        <StatusCard title="Current Cycle" value={data.currentCycle} />
        <StatusCard title="Next Scheduled" value={data.nextScheduled} />
        <StatusCard title="Skip Rate" value={data.skipRate} />
      </div>

      {/* History táblázat */}
      <HistoryTable cycles={data.history} />

      {/* Grafikon */}
      <CyclesChart data={data.chartData} />
    </div>
  );
}
```

**Hook:** `datahaven-web/client/src/hooks/useAutonomous.ts`

```tsx
export function useAutonomous() {
  const [data, setData] = useState<AutonomousData | null>(null);

  useEffect(() => {
    // Initial fetch
    fetch('/api/autonomous/history').then(/* ... */);

    // SSE real-time updates
    const eventSource = new EventSource('/api/stream');
    eventSource.addEventListener('autonomous', (e) => {
      setData(JSON.parse(e.data));
    });

    return () => eventSource.close();
  }, []);

  return { data, loading: !data };
}
```

**Components:**
- `StatusCard.tsx` — kártya komponens
- `HistoryTable.tsx` — táblázat komponens
- `CyclesChart.tsx` — Recharts bar chart

#### Backend API

**Új endpoint:** `datahaven-web/src/routes/autonomous.ts`

```typescript
// GET /api/autonomous/history
router.get('/history', async (req, res) => {
  const history = await db.query(`
    SELECT id, timestamp, conductor_status, dispatched_task, result, duration
    FROM autonomous_cycles
    ORDER BY timestamp DESC
    LIMIT 50
  `);

  const chartData = await db.query(`
    SELECT DATE_TRUNC('hour', timestamp) as hour, COUNT(*) as count
    FROM autonomous_cycles
    WHERE timestamp > NOW() - INTERVAL '24 hours'
    GROUP BY hour
    ORDER BY hour
  `);

  res.json({
    currentCycle: /* ... */,
    nextScheduled: /* ... */,
    skipRate: /* ... */,
    history,
    chartData
  });
});
```

**Database:**
- Tábla: `autonomous_cycles` (már létezik a knowledge-service-ben)
- Ha nincs, migráció szükséges:

```sql
CREATE TABLE IF NOT EXISTS autonomous_cycles (
  id SERIAL PRIMARY KEY,
  timestamp TIMESTAMPTZ NOT NULL,
  conductor_status VARCHAR(50),
  dispatched_task VARCHAR(100),
  result VARCHAR(50),
  duration_ms INT
);
```

#### SSE integráció

- Existing SSE endpoint: `/api/stream`
- Új event type: `autonomous`
- Trigger: minden autonomous cycle után

```typescript
// datahaven-web/src/server.js
function sendAutonomousUpdate(cycleData) {
  clients.forEach(client => {
    client.write(`event: autonomous\ndata: ${JSON.stringify(cycleData)}\n\n`);
  });
}
```

### Routing integráció

**Frissítés:** `datahaven-web/client/src/App.tsx`

```tsx
import AutonomousPage from './pages/AutonomousPage';

// Routes
<Route path="/autonomous" element={<AutonomousPage />} />
```

**Navigation:** `datahaven-web/client/src/components/Header.tsx`

```tsx
<nav>
  <NavLink to="/">Dashboard</NavLink>
  <NavLink to="/autonomous">Autonomous</NavLink>
  <NavLink to="/kanban">Kanban</NavLink>
  <NavLink to="/planning">Planning</NavLink>
  <NavLink to="/projects">Projects</NavLink>
</nav>
```

## Acceptance Criteria

- [ ] `/autonomous` oldal elérhető
- [ ] 3 státusz kártya megjelenik helyes adatokkal
- [ ] History táblázat renderel (utolsó 50 cycle)
- [ ] Cycles/hour chart működik (utolsó 24h)
- [ ] Real-time frissül (SSE integration)
- [ ] Navigation tab hozzáadva a header-ben
- [ ] Responsive design (mobile friendly)

## Megjegyzések

- Design: kövesse a meglévő Dashboard/Kanban/Planning oldal stílusát
- Tailwind CSS: használd a már létező utility class-okat
- Error handling: loading state, error boundary
- Ha `autonomous_cycles` tábla nincs, koordinálj backend terminállal

## Definition of Done

1. Frontend komponensek implementálva
2. Backend API endpoint kész
3. SSE integration működik
4. Navigáció hozzáadva
5. Manual teszt: oldal betölt, adatok megjelennek, real-time frissül
6. DONE outbox üzenet küldése screenshot-tal
