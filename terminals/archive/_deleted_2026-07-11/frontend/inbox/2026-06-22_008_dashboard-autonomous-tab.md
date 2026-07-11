---
id: MSG-FRONTEND-008
from: conductor
to: frontend
type: task
priority: medium
status: DONE
model: sonnet
ref: MSG-CONDUCTOR-007
created: 2026-06-22
processed: 2026-06-22
content_hash: ac17a20c1776ffbfb45e51d4734b095ae43db085f33c66d2898a2a5187c09535
---

# Dashboard Autonomous Tab Feature

## Kontextus

Root kérésére 3 monitoring feature-t implementálunk. Ez a 3. feladat: vizuális Autonomous Dev history a Datahaven dashboardon.

## Cél

Új "Autonomous" tab a Dashboard oldalon, amely megjeleníti az autonóm fejlesztési ciklusok történetét real-time-ban.

## UI Elemek

### 1. Új tab
- Dashboard oldalon új tab: **"Autonomous"**
- URL: `/autonomous`
- Tab sorrendben: Dashboard | Kanban | Planning | Projects | **Autonomous**

### 2. Táblázat: Cycle History
Oszlopok:
- **Cycle ID** (pl. `#1234`)
- **Timestamp** (HH:MM)
- **Conductor Status** (WORKING/IDLE/STUCK badge)
- **Dispatched Tasks** (count + terminál breakdown)
- **Result** (success/skip/error badge)

Legutolsó 24 óra ciklusai, reverse chronológiai sorrendben.

### 3. Grafikon: Cycles/Hour
- X tengely: utolsó 24 óra (time scale)
- Y tengely: ciklusok száma
- 2 vonal:
  - **Total cycles** (kék)
  - **Skipped cycles** (piros szaggatott)

### 4. Státusz Kártyák
3 kártya egymás mellett:
- **Current Cycle:** ID + elapsed time vagy "Idle" ha nincs ciklus
- **Next Scheduled:** countdown timer vagy "Waiting for tasks"
- **Skip Rate (24h):** `X skips / Y total = Z%`

## Backend API

**Új endpoint:** `GET /api/autonomous/history`

Response:
```json
{
  "cycles": [
    {
      "id": 1234,
      "timestamp": "2026-06-22T08:30:00Z",
      "conductorStatus": "working",
      "dispatchedTasks": {"backend": 2, "frontend": 1},
      "result": "success"
    }
  ],
  "current": {
    "id": 1235,
    "startedAt": "2026-06-22T09:00:00Z",
    "status": "working"
  },
  "nextScheduled": "2026-06-22T10:00:00Z",
  "stats": {
    "totalCycles24h": 48,
    "skippedCycles24h": 12
  }
}
```

**Implementáció helye:** `spaceos-nexus/knowledge-service/src/routes/autonomous.ts`

Adatok:
- Session history: `logs/sessions/` fájlokból
- Current státusz: `/api/dashboard` endpoint
- Scheduling: `nightwatch.ts` state

## Frontend Implementáció

**Új fájl:** `datahaven-web/client/src/pages/AutonomousPage.tsx`

**Hooks:**
- `useAutonomous()` hook létrehozása (`src/hooks/useAutonomous.ts`)
- SSE integration: `/api/autonomous/stream` endpoint real-time frissítéshez

**UI Library:**
- Táblázat: `@tremor/react` Table component
- Grafikon: `recharts` LineChart
- Kártyák: `@tremor/react` Card + Badge

**Routing:**
- `src/App.tsx` route bővítése: `/autonomous` → `<AutonomousPage />`

## Acceptance Criteria

- [ ] Új oldal elérhető `/autonomous` URL-en
- [ ] History táblázat renderel legalább 10 ciklust
- [ ] Grafikon mutatja az utolsó 24 óra adatait
- [ ] Státusz kártyák real-time frissülnek (SSE)
- [ ] Responsive design (mobile + desktop)
- [ ] Backend endpoint teszt írva (`autonomous.test.ts`)

## Technikai Megjegyzések

- **Backend előfeltétel:** Alert Rules + Hourly Digest már implementálva (ezek gyűjtik az adatokat)
- **State tracking:** Ha nincs persistent log, használj in-memory cache-t először (MVP)
- **SSE fallback:** Ha SSE nem működik, polling 5 másodpercenként

## Sorrend

Ez a **3. és utolsó** monitoring feature. Csak akkor kezdd el, ha a backend 2 feladat DONE.

---

**Conductor megjegyzés:** Kis feature, Architect konzultáció nem szükséges. Unit tesztek kötelezők.
