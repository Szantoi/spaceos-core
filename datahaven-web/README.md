# Datahaven Web Dashboard

Web-based dashboard for monitoring and managing the Datahaven message queue system.

## Features

- **Real-time Stats** — Live message counts with SSE updates
- **Daemon Monitoring** — Online/offline status, pending messages per daemon
- **Message Browser** — Filter, search, and inspect messages
- **Knowledge Search** — RAG-based search across the knowledge base
- **Authentication** — Token-based API protection
- **Responsive UI** — Dark theme, mobile-friendly design

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     datahaven-web                           │
├─────────────────────────────────────────────────────────────┤
│  public/                 │  Frontend (HTML/CSS/JS)          │
│    ├── index.html        │  Single Page Application         │
│    ├── css/styles.css    │  Dark theme dashboard            │
│    └── js/app.js         │  API client + SSE                │
├─────────────────────────────────────────────────────────────┤
│  src/                    │  Backend (3-layer architecture)  │
│    ├── routes/           │  HTTP endpoint handlers          │
│    ├── services/         │  Business logic                  │
│    ├── data/             │  Database access                 │
│    └── middleware/       │  Auth, rate limiting             │
└─────────────────────────────────────────────────────────────┘
           │                              │
           ▼                              ▼
┌──────────────────┐           ┌──────────────────┐
│   messages.db    │           │ knowledge-service│
│   (SQLite)       │           │   (port 3456)    │
└──────────────────┘           └──────────────────┘
```

## Quick Start

```bash
# Install dependencies
cd /opt/spaceos/datahaven-web
npm install

# Configure
cp .env.example .env
nano .env

# Run (development)
npm run dev

# Run (production)
npm start
```

## Configuration

Edit `.env` file:

```bash
# Server
PORT=3457
HOST=0.0.0.0

# Database
MESSAGES_DB=/opt/spaceos/datahaven/messages.db

# Knowledge Service
KNOWLEDGE_URL=http://localhost:3456

# Authentication (optional)
AUTH_ENABLED=true
AUTH_TOKEN=your-secret-token

# Rate Limiting
RATE_LIMIT_WINDOW_MS=60000
RATE_LIMIT_MAX=100
```

## API Endpoints

### Stats
- `GET /api/stats` — Dashboard statistics
- `GET /api/stats/daemon/:id` — Stats for specific daemon

### Daemons
- `GET /api/daemons` — List all daemons
- `GET /api/daemons/summary` — Online/offline summary
- `GET /api/daemons/:id` — Daemon details

### Messages
- `GET /api/messages` — List messages (with filters)
- `GET /api/messages/:id` — Message details
- `GET /api/messages/inbox/:daemon` — Pending messages for daemon
- `GET /api/messages/pending` — Pending counts by daemon

### Knowledge
- `GET /api/knowledge/search?q=query` — Search knowledge base
- `GET /api/knowledge/health` — Knowledge service status

### Events
- `GET /api/events` — SSE stream for real-time updates

### Health
- `GET /health` — Server health check

## Authentication

When `AUTH_ENABLED=true`, API requests require authentication:

```bash
# Header
Authorization: Bearer your-token

# Or query parameter
GET /api/stats?token=your-token
```

The dashboard UI will prompt for the token on first load.

## Systemd Service

Install as a system service:

```bash
# Install
sudo bash scripts/install-service.sh

# Start
sudo systemctl start datahaven-web
sudo systemctl enable datahaven-web

# Check status
sudo systemctl status datahaven-web

# View logs
journalctl -u datahaven-web -f
```

## Development

```bash
# Run with auto-reload
npm run dev

# Project structure
datahaven-web/
├── package.json
├── .env.example
├── README.md
├── public/
│   ├── index.html
│   ├── css/
│   │   └── styles.css
│   └── js/
│       └── app.js
├── src/
│   ├── server.js          # Main entry point
│   ├── data/
│   │   ├── database.js    # SQLite connection
│   │   ├── messageRepository.js
│   │   └── daemonRepository.js
│   ├── services/
│   │   ├── statsService.js
│   │   ├── messageService.js
│   │   ├── daemonService.js
│   │   └── knowledgeService.js
│   ├── routes/
│   │   ├── statsRoutes.js
│   │   ├── daemonRoutes.js
│   │   ├── messageRoutes.js
│   │   ├── knowledgeRoutes.js
│   │   └── sseRoutes.js
│   └── middleware/
│       └── auth.js
└── scripts/
    └── install-service.sh
```

## Requirements

- Node.js 18+
- Access to `messages.db` (read-only)
- Knowledge service running (optional, for search)

## License

MIT
