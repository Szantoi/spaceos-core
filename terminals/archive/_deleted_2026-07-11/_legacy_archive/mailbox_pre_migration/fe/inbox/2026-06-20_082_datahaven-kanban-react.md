---
id: MSG-FE-082
from: nexus
to: fe
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-20
---

# Datahaven Kanban UI — React Migráció

Te vagy a SpaceOS Frontend fejlesztő. A feladatod a Datahaven web dashboard kanban felületének React-ra történő migrálása és továbbfejlesztése.

## Kontextus

A Datahaven egy belső agent koordinációs dashboard, amit a SpaceOS terminálok állapotának monitorozására használunk. Jelenleg vanilla HTML/CSS/JS stack fut:

```
/opt/spaceos/datahaven-web/
├── public/
│   ├── kanban.html       ← meglévő kanban UI (vanilla JS)
│   ├── planning.html     ← planning pipeline view
│   ├── projects.html     ← project tracking
│   ├── css/
│   │   ├── styles.css
│   │   └── kanban.css
│   └── js/
│       └── kanban.js     ← meglévő kanban logika
└── src/
    └── server.js         ← Express API (port 3457)
```

## Feladat

### 1. React projekt inicializálás

```bash
# Vite + React + TypeScript setup
cd /opt/spaceos/datahaven-web
npm create vite@latest client -- --template react-ts
cd client
npm install
```

### 2. Kanban komponensek

A meglévő `kanban.html` struktúráját React komponensekre bontva:

```
src/
├── components/
│   ├── Layout/
│   │   ├── Header.tsx         ← nav: Dashboard, Kanban, Planning, Projects
│   │   └── Layout.tsx
│   ├── Kanban/
│   │   ├── KanbanBoard.tsx    ← fő board komponens
│   │   ├── KanbanColumn.tsx   ← Ideas, Selected, Debate, Consensus, Queue
│   │   ├── KanbanCard.tsx     ← egyedi task kártya
│   │   ├── CardModal.tsx      ← task részletek modal
│   │   └── MetricsBar.tsx     ← WIP, Throughput, Active Sessions
│   └── Auth/
│       └── AuthOverlay.tsx
├── hooks/
│   ├── useAuth.ts
│   ├── useKanban.ts           ← API hívások, állapot
│   └── useSSE.ts              ← real-time updates
├── types/
│   └── kanban.ts              ← TypeScript típusok
└── App.tsx
```

### 3. Meglévő API-k

A datahaven-web Express server már ezeket biztosítja:

- `GET /api/stats` — terminál statisztikák
- `GET /api/kanban` — kanban board adatok (ideas, queue, active stb.)
- `GET /api/messages/:terminal/inbox` — inbox üzenetek
- `GET /sse` — Server-Sent Events real-time frissítésekhez

### 4. Dual-Track Kanban layout

**Discovery Track (horizontal):**
- Ideas → Selected → Debate → Consensus → Queue

**Delivery Track (vertical swimlanes):**
- Minden terminálnak saját swimlane
- Oszlopok: Inbox | Working | Review | Done

### 5. Drag-and-drop (opcionális)

Ha marad idő: `react-beautiful-dnd` vagy `@dnd-kit/core` a kártyák mozgatásához.

## Elvárások

1. **TypeScript** kötelező
2. **Vite dev server** működjön (HMR)
3. **API proxy** — Vite config legyen beállítva a `/api` és `/sse` útvonalakhoz
4. **CSS** — vagy TailwindCSS, vagy a meglévő styles.css átvétele
5. **Komponens tesztek** nem szükségesek most (walking skeleton first)

## Teljesítési kritérium

A `npm run dev` indítson működő kanban dashboardot, ami:
- Megjeleníti a discovery és delivery track-eket
- Kártyákat API-ból tölti
- SSE-vel frissül real-time

## Referencia

- Meglévő kanban.html: `/opt/spaceos/datahaven-web/public/kanban.html`
- Meglévő JS logika: `/opt/spaceos/datahaven-web/public/js/kanban.js`
- API server: `/opt/spaceos/datahaven-web/src/server.js`

## DONE kritérium

- React client mappa létrejött
- KanbanBoard komponens renderel
- API-ból jön az adat
- SSE működik (connection status zöld)
