# Datahaven Dashboard — React Client

> Real-time visualization dashboard for SpaceOS agent infrastructure

**Tech Stack:**
- React 19
- TypeScript 6
- Vite 8
- Tailwind CSS 4
- Server-Sent Events (SSE)

---

## Features

### 📊 Dashboard
- Real-time metrics for all 17 SpaceOS terminals
- Global inbox/outbox/unread counts
- Terminal status tracking (WORKING/IDLE)
- Per-terminal health indicators
- Auto-refresh every 60 seconds

### 📋 Kanban
- **Dual-track workflow visualization:**
  - **Discovery Track:** Ideas → Selected → Debate → Consensus → Queue
  - **Delivery Track:** Inbox → Working → Review → Done (per terminal)
- WIP limits and metrics
- Real-time updates via SSE
- Click-through to view items/messages

### 🗂️ Planning Pipeline
- 5-stage workflow: Idea → Selected → Debate → Consensus → Queue
- Filtering by status and priority
- Confidence scores
- Automated 30-minute scan cycles

### 📅 Projects (Gantt Chart)
- Interactive timeline view (8-month window: -2 to +6 months)
- "Today" marker on timeline
- List view alternative
- Status-based color coding
- Progress tracking
- Epic and terminal assignment

---

## Quick Start

### Development

```bash
# Install dependencies
npm install

# Start dev server (http://localhost:5173)
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

### Environment Variables

Create `.env` file:

```bash
# Backend API URL (defaults to same origin)
VITE_API_URL=http://localhost:3100

# Auth token (optional, for development)
VITE_DEV_AUTH_TOKEN=your-dev-token-here
```

### Production Build

```bash
npm run build
# Output: dist/
```

Serve the `dist/` directory with any static file server (nginx, express static, etc.)

---

## Architecture

### Pages

| Page | Route | Description |
|------|-------|-------------|
| `DashboardPage` | `/` | Global metrics and terminal grid |
| `KanbanPage` | `/kanban` | Dual-track kanban board |
| `PlanningPage` | `/planning` | Planning pipeline visualization |
| `ProjectsPage` | `/projects` | Gantt chart and project list |

### Components

**Layout:**
- `Layout` — Main app shell with navigation
- `Header` — Top navigation bar
- `AuthOverlay` — Authentication modal

**Dashboard:**
- `StatsOverview` — Global metrics cards
- `TerminalGrid` — Terminal status grid
- `TerminalCard` — Individual terminal card

**Kanban:**
- `KanbanBoard` — Dual-track board container
- `KanbanColumn` — Column component
- `KanbanCard` — Card component
- `MetricsBar` — WIP metrics bar
- `CardModal` — Item detail modal

### Hooks

| Hook | Purpose |
|------|---------|
| `useAuth` | Authentication state and token management |
| `useDashboard` | Dashboard data fetching with auto-refresh |
| `useKanban` | Kanban board data and operations |
| `useSSE` | Server-Sent Events connection |

### Types

- `dashboard.ts` — Dashboard and terminal types
- `kanban.ts` — Kanban board and card types

---

## API Integration

The React client consumes the following backend APIs:

### Dashboard API
```
GET /api/dashboard
Response: { timestamp, metrics, terminals[] }
```

### Kanban API
```
GET /api/kanban/snapshot
Response: { discovery, delivery }

GET /api/kanban/metrics
Response: { discoveryWip, deliveryWip, activeSessions }
```

### Planning API
```
GET /api/planning/items
Response: { items[], metrics }
```

### Projects API
```
GET /api/projects
Response: { projects[], milestones[] }
```

### SSE Events
```
GET /api/events
Events: terminal_wake, planning_update, task_update
```

---

## Development Notes

### Auto-refresh
All pages refresh every 60 seconds by default. Can be configured in each page component.

### Error Handling
- Network errors show retry UI
- Auth errors redirect to login
- Graceful degradation for missing data

### Build Output
```
dist/
├── index.html
├── assets/
│   ├── index-[hash].css  (~7.79 kB, 2.17 kB gzip)
│   └── index-[hash].js   (~278 kB, 83 kB gzip)
```

### CSS Variables
Theme uses CSS custom properties for easy theming:
```css
--bg-primary: #0a0e17
--bg-secondary: #141821
--text-primary: #e2e8f0
--text-secondary: #94a3b8
--accent: #3b82f6
--border: #1e293b
```

---

## Backend Setup

The backend server runs on port 3100 and provides:
- Express.js REST API
- SSE event stream
- File system scanning for real-time data

**Backend location:** `/opt/spaceos/spaceos-nexus/knowledge-service`

**Start backend:**
```bash
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm run dev
# Or production:
npm start
```

---

## Deployment

### Static Hosting (Recommended)

Build and serve with nginx:

```nginx
server {
    listen 80;
    server_name datahaven.example.com;

    root /opt/spaceos/datahaven-web/client/dist;
    index index.html;

    # SPA fallback
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Proxy API requests to backend
    location /api/ {
        proxy_pass http://localhost:3100;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### Express Static (Alternative)

The backend server can also serve the React build:

```typescript
// server.ts
import express from 'express';
import path from 'path';

const app = express();

// Serve React app
app.use(express.static(path.join(__dirname, '../../datahaven-web/client/dist')));

// API routes
app.use('/api', apiRouter);

// SPA fallback
app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, '../../datahaven-web/client/dist/index.html'));
});
```

---

## Testing

```bash
# Type checking
npm run tsc

# Linting (if configured)
npm run lint

# Build verification
npm run build && npm run preview
```

---

## Project Structure

```
client/
├── src/
│   ├── components/
│   │   ├── Auth/           # Auth components
│   │   ├── Dashboard/      # Dashboard components
│   │   ├── Kanban/         # Kanban components
│   │   └── Layout/         # Layout components
│   ├── hooks/              # Custom React hooks
│   ├── pages/              # Page components
│   ├── types/              # TypeScript type definitions
│   ├── App.tsx             # Root component
│   ├── main.tsx            # Entry point
│   └── index.css           # Global styles
├── public/                 # Static assets
├── dist/                   # Production build output
├── package.json
├── tsconfig.json
├── vite.config.ts
└── README.md               # This file
```

---

## Phase 6 + 6.5 Complete ✅

**React Frontend:**
- 4 complete pages (Dashboard, Kanban, Planning, Projects)
- 23 React components
- 4 custom hooks
- Full TypeScript type safety
- 1,784 lines of React/TypeScript code

**Backend APIs:**
- 5 new REST endpoints
- Real-time file system scanning
- SSE event stream
- ~400 lines of Express.js code

**Build Stats:**
- Bundle: 278.25 kB (83.25 kB gzip)
- CSS: 7.79 kB (2.17 kB gzip)
- Build time: <1 second

---

## Next Steps

**Phase 7 (Planned):**
- Marvin integration for advanced planning
- Guardrail service integration
- Advanced metrics (throughput, cycle time)
- React component tests (Vitest)
- Terminal detail modal
- Drag-and-drop kanban cards

---

## License

Part of SpaceOS agent infrastructure. Internal use only.
