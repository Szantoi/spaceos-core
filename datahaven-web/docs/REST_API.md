# Datahaven Web Dashboard — REST API

A dashboard által biztosított REST API a frontend és külső integrációk számára.

---

## Base URL

```
http://localhost:3457
```

---

## Autentikáció

Ha `AUTH_ENABLED=true`:

```http
# Header
Authorization: Bearer <token>

# Vagy query parameter
GET /api/stats?token=<token>
```

### Error Responses

**401 Unauthorized:**
```json
{
  "error": "Authentication required",
  "message": "Provide token via Authorization header or ?token= query parameter"
}
```

**403 Forbidden:**
```json
{
  "error": "Invalid token",
  "message": "The provided authentication token is invalid"
}
```

---

## Endpoints

### Health

#### `GET /health`

Server health check (no auth required).

**Response:**
```json
{
  "status": "ok",
  "timestamp": "2026-06-19T12:00:00.000Z"
}
```

---

### Stats

#### `GET /api/stats`

Dashboard statistics.

**Response:**
```json
{
  "total": 150,
  "pending": 23,
  "acked": 127,
  "daemons": 5
}
```

---

#### `GET /api/stats/daemon/:id`

Statistics for specific daemon.

**Response:**
```json
{
  "daemon": {
    "id": "kernel",
    "description": "Backend kernel daemon",
    "last_heartbeat": "2026-06-19T12:00:00Z",
    "online": true,
    "pending_count": 3
  },
  "pendingMessages": 3,
  "criticalCount": 1,
  "highCount": 2
}
```

**404 Response:**
```json
{
  "error": "Daemon not found"
}
```

---

### Daemons

#### `GET /api/daemons`

List all registered daemons.

**Response:**
```json
[
  {
    "id": "kernel",
    "description": "Backend kernel daemon",
    "last_heartbeat": "2026-06-19T12:00:00Z",
    "online": true,
    "pending_count": 3
  },
  {
    "id": "conductor",
    "description": "Orchestration daemon",
    "last_heartbeat": "2026-06-19T11:58:00Z",
    "online": true,
    "pending_count": 0
  }
]
```

---

#### `GET /api/daemons/summary`

Daemon summary with online/offline counts.

**Response:**
```json
{
  "total": 5,
  "online": 3,
  "offline": 2,
  "daemons": [...]
}
```

---

#### `GET /api/daemons/:id`

Get specific daemon details.

**Response:**
```json
{
  "id": "kernel",
  "description": "Backend kernel daemon",
  "last_heartbeat": "2026-06-19T12:00:00Z",
  "online": true,
  "pending_count": 3,
  "inbox": {
    "total": 3,
    "critical": 1,
    "high": 2
  }
}
```

**404 Response:**
```json
{
  "error": "Daemon not found"
}
```

---

### Messages

#### `GET /api/messages`

List messages with optional filters.

**Query Parameters:**

| Param | Type | Description |
|-------|------|-------------|
| `status` | string | Filter: `pending` or `acked` |
| `daemon` | string | Filter: from or to daemon |
| `type` | string | Filter: msg_type |
| `limit` | number | Max results (default: 50) |
| `offset` | number | Pagination offset (default: 0) |

**Examples:**
```http
GET /api/messages
GET /api/messages?status=pending
GET /api/messages?daemon=kernel
GET /api/messages?type=task&limit=20
```

**Response:**
```json
{
  "messages": [
    {
      "id": 42,
      "from_daemon": "conductor",
      "to_daemon": "kernel",
      "msg_type": "task",
      "subject": "Build project",
      "payload": "{\"action\":\"build\"}",
      "priority": "high",
      "status": "pending",
      "created_at": "2026-06-19T12:00:00Z",
      "correlation_id": null
    }
  ],
  "pagination": {
    "limit": 50,
    "offset": 0,
    "count": 1
  }
}
```

---

#### `GET /api/messages/:id`

Get single message details.

**Response:**
```json
{
  "id": 42,
  "from_daemon": "conductor",
  "to_daemon": "kernel",
  "msg_type": "task",
  "subject": "Build project",
  "payload": "{\"action\":\"build\"}",
  "priority": "high",
  "status": "pending",
  "created_at": "2026-06-19T12:00:00Z",
  "correlation_id": null
}
```

**404 Response:**
```json
{
  "error": "Message not found"
}
```

---

#### `GET /api/messages/pending`

Pending message counts grouped by daemon.

**Response:**
```json
{
  "kernel": 3,
  "conductor": 1,
  "telegram-bot": 5
}
```

---

#### `GET /api/messages/inbox/:daemon`

Get pending messages for specific daemon, sorted by priority.

**Response:**
```json
{
  "messages": [
    {
      "id": 45,
      "from_daemon": "conductor",
      "to_daemon": "kernel",
      "msg_type": "task",
      "subject": "Critical fix",
      "priority": "critical",
      "status": "pending",
      ...
    }
  ],
  "total": 3,
  "byPriority": {
    "critical": [...],
    "high": [...],
    "medium": [...],
    "low": [...]
  }
}
```

---

### Knowledge

#### `GET /api/knowledge/search`

Search knowledge base (proxy to knowledge-service).

**Query Parameters:**

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `q` | string | yes | Search query |
| `limit` | number | no | Max results (default: 5) |

**Example:**
```http
GET /api/knowledge/search?q=JWT+authentication&limit=5
```

**Response:**
```json
{
  "results": [
    {
      "source": "docs/security/JWT_GUIDE.md",
      "content": "JWT (JSON Web Token) is...",
      "score": 0.892
    }
  ],
  "query": "JWT authentication",
  "count": 1
}
```

**400 Response:**
```json
{
  "error": "Query parameter \"q\" required"
}
```

**503 Response:**
```json
{
  "error": "Knowledge service unavailable: Connection refused"
}
```

---

#### `GET /api/knowledge/health`

Check knowledge service health.

**Response:**
```json
{
  "status": "ok",
  "doc_count": 441,
  "chroma_status": "connected"
}
```

**Offline Response:**
```json
{
  "status": "offline",
  "error": "Connection refused"
}
```

---

### Server-Sent Events

#### `GET /api/events`

Real-time updates via SSE.

**Headers:**
```http
Content-Type: text/event-stream
Cache-Control: no-cache
Connection: keep-alive
```

**Event Stream:**
```
data: {"type":"stats","total":150,"pending":23}

data: {"type":"update","total":151}
```

**Event Types:**

| Type | Description |
|------|-------------|
| `stats` | Initial statistics (on connect) |
| `update` | Data changed, refresh needed |

---

## Error Responses

All endpoints return consistent error format:

```json
{
  "error": "Error message here"
}
```

### HTTP Status Codes

| Status | Meaning |
|--------|---------|
| 200 | Success |
| 400 | Bad Request — Missing/invalid parameter |
| 401 | Unauthorized — Auth required |
| 403 | Forbidden — Invalid token |
| 404 | Not Found — Resource not found |
| 429 | Too Many Requests — Rate limited |
| 500 | Internal Server Error |
| 503 | Service Unavailable — External service down |

---

## Rate Limiting

When enabled, requests are limited per IP:

```bash
RATE_LIMIT_WINDOW_MS=60000   # 1 minute window
RATE_LIMIT_MAX=100           # Max 100 requests/minute/IP
```

**429 Response:**
```json
{
  "error": "Too many requests",
  "message": "Rate limit exceeded. Try again in 45 seconds"
}
```

---

## Data Types

### Message Object

```typescript
{
  id: number;
  from_daemon: string;
  to_daemon: string;
  msg_type: "task" | "question" | "done" | "blocked";
  subject: string | null;
  payload: string | null;  // JSON string
  priority: "critical" | "high" | "medium" | "low";
  status: "pending" | "acked";
  created_at: string;      // ISO 8601
  correlation_id: string | null;
}
```

### Daemon Object

```typescript
{
  id: string;
  description: string | null;
  last_heartbeat: string | null;  // ISO 8601
  online: boolean;                 // Computed field
  pending_count: number;           // Computed field
}
```

### Knowledge Result

```typescript
{
  source: string;    // File path
  content: string;   // Relevant text
  score: number;     // 0.0 - 1.0
}
```
