# SpaceOS Knowledge Service

RAG (Retrieval-Augmented Generation) + Mailbox service for SpaceOS agent infrastructure.

## Features

### Phase 1: Knowledge Service ✅
- ✅ Indexes `/opt/spaceos/docs/knowledge/**/*.md` recursively
- ✅ ChromaDB vector store (persistent, Docker-based)
- ✅ Three embedding backends (priority order):
  1. **Voyage AI** (`voyage-3-lite`, 1024 dims) — production recommended
  2. **Google Gemini** (`text-embedding-004`, 768 dims) — requires model name fix
  3. **Local** (`all-MiniLM-L6-v2`) — CPU-dependent, may fail on QEMU/old arch
- ✅ Express REST API (`GET/POST /api/knowledge/search`)
- ✅ Auto-indexing on startup (if store empty)

### Phase 2: Mailbox Tools ✅
- ✅ `list_inbox` — List inbox messages for a terminal
- ✅ `send_message` — Create new inbox message for a terminal
- ✅ `submit_done` — Create DONE outbox message
- ✅ `get_task_status` — Query task status from `docs/tasks/`

### Phase 3: SSE Live Notifications ✅
- ✅ `subscribe` — SSE endpoint for real-time inbox notifications
- ✅ `broadcast` — Send message to all connected clients
- ✅ Automatic event emission on `send_message` and `submit_done`
- ✅ 30-second heartbeat to keep connections alive

## Setup

### 1. ChromaDB

```bash
cd /opt/spaceos/spaceos-nexus
docker compose up -d
```

### 2. Embedding API Key

**Option A: Voyage AI (recommended)**

1. Get API key: https://dash.voyageai.com/
2. Add to `.env`:
   ```bash
   VOYAGE_API_KEY=your_key_here
   ```

**Option B: Google Gemini (requires fix)**

1. Fix model name in `src/embeddings.ts:66`:
   ```ts
   modelName: 'text-embedding-004',  // was: gemini-embedding-001
   ```
2. Ensure `GOOGLE_API_KEY` is set in `.env`

### 3. Install & Run

```bash
npm install
npm run dev
```

## Usage

### Index knowledge base

```bash
npm run index
```

### Start server

```bash
npm run dev
# Or production:
npm run build && npm start
```

### Test

**Knowledge Service**
```bash
../scripts/test-rag.sh
```

**Mailbox Tools**
```bash
../scripts/test-mailbox.sh
```

### API Endpoints

#### Health & Status

**Health**
```bash
curl http://localhost:3456/health
```

#### Knowledge Service

**Search (GET)**
```bash
curl "http://localhost:3456/api/knowledge/search?q=EF+Core+migration&topK=5"
```

**Search (POST)**
```bash
curl -X POST http://localhost:3456/api/knowledge/search \
  -H 'Content-Type: application/json' \
  -d '{"q": "React testing patterns", "topK": 3}'
```

**Re-index**
```bash
curl -X POST http://localhost:3456/api/knowledge/index
```

#### Mailbox Tools

**List Inbox**
```bash
# All messages
curl http://localhost:3456/api/mailbox/nexus/inbox

# UNREAD only
curl http://localhost:3456/api/mailbox/nexus/inbox?status=UNREAD

# READ only
curl http://localhost:3456/api/mailbox/nexus/inbox?status=READ
```

**Send Message**
```bash
curl -X POST http://localhost:3456/api/mailbox/kernel/inbox \
  -H 'Content-Type: application/json' \
  -d '{
    "type": "task",
    "content": "# Task Title\n\nTask description here...",
    "priority": "high",
    "model": "sonnet",
    "ref": "MSG-ROOT-001"
  }'
```

**Submit DONE**
```bash
curl -X POST http://localhost:3456/api/mailbox/nexus/outbox \
  -H 'Content-Type: application/json' \
  -d '{
    "task_id": "MSG-NEXUS-016",
    "summary": "Task completed successfully",
    "files_changed": [
      "file1.ts",
      "file2.ts"
    ]
  }'
```

**Get Task Status**
```bash
# All tasks
curl http://localhost:3456/api/tasks/status

# Specific task
curl http://localhost:3456/api/tasks/status?task_id=JOINERY-V2
```

#### SSE Live Notifications (Phase 3)

**Subscribe to Terminal**
```bash
# Subscribe to live updates for a terminal
# Keep-alive SSE connection (use curl -N for unbuffered output)
curl -N http://localhost:3456/api/mailbox/nexus/subscribe

# Example output:
# event: connected
# data: {"terminal":"nexus","timestamp":"2026-06-20T06:56:48.254Z"}
#
# event: new_message
# data: {"terminal":"nexus","type":"new_message","messageId":"MSG-NEXUS-019",...}
```

**Broadcast to All**
```bash
curl -X POST http://localhost:3456/api/mailbox/broadcast \
  -H 'Content-Type: application/json' \
  -d '{"message": "System maintenance in 5 minutes", "priority": "high"}'
```

**JavaScript/Browser Usage**
```javascript
const es = new EventSource('http://localhost:3456/api/mailbox/nexus/subscribe');

es.addEventListener('connected', (e) => {
  console.log('Connected:', JSON.parse(e.data));
});

es.addEventListener('new_message', (e) => {
  const msg = JSON.parse(e.data);
  console.log('New message:', msg.messageId);
});

es.addEventListener('done_submitted', (e) => {
  const msg = JSON.parse(e.data);
  console.log('Task completed:', msg.details.task_id);
});
```

#### MCP Protocol (Phase 4)

**Server Info**
```bash
curl http://localhost:3456/mcp
```

**Initialize Session**
```bash
curl -X POST http://localhost:3456/mcp \
  -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","method":"initialize","id":1}'
```

**List Available Tools**
```bash
curl -X POST http://localhost:3456/mcp \
  -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":2}'
```

**Call Tool**
```bash
# search_knowledge
curl -X POST http://localhost:3456/mcp \
  -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"search_knowledge","arguments":{"query":"EF Core migration","limit":5}},"id":3}'

# list_inbox
curl -X POST http://localhost:3456/mcp \
  -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"list_inbox","arguments":{"terminal":"nexus","status":"UNREAD"}},"id":4}'
```

**Claude Code Configuration (`.mcp.json`)**
```json
{
  "mcpServers": {
    "spaceos-knowledge": {
      "type": "http",
      "url": "http://localhost:3456/mcp",
      "timeout": 60000
    }
  }
}
```

## Architecture

```
knowledge-service/
├── src/
│   ├── server.ts        — Express API (knowledge + mailbox + SSE routes)
│   ├── mcp.ts           — MCP JSON-RPC protocol handler (Phase 4)
│   ├── indexer.ts       — Markdown indexing pipeline
│   ├── vectorStore.ts   — ChromaDB client
│   ├── embeddings.ts    — Multi-backend embedding provider
│   └── mailbox.ts       — Mailbox tools (Phase 2)
├── package.json
├── tsconfig.json
└── .env
```

## Status

### Phase 1: Knowledge Service ✅
- ✅ Code complete
- ⚠️  Google Gemini model name needs fix (`text-embedding-004`)
- ⚠️  Voyage AI key needed for production (free tier available)
- ✅ ChromaDB running and accessible
- ✅ 21 knowledge docs ready (`/opt/spaceos/docs/knowledge/`)

### Phase 2: Mailbox Tools ✅
- ✅ `list_inbox` implemented
- ✅ `send_message` implemented
- ✅ `submit_done` implemented
- ✅ `get_task_status` implemented
- ✅ Test script created (`scripts/test-mailbox.sh`)
- ✅ Documentation updated

### Phase 3: SSE Live Notifications ✅
- ✅ `subscribe` endpoint implemented (SSE)
- ✅ `broadcast` endpoint implemented
- ✅ Automatic event emission on mailbox operations
- ✅ Heartbeat every 30 seconds
- ✅ Client cleanup on disconnect
- ✅ Documentation updated

### Phase 4: MCP Protocol ✅
- ✅ MCP JSON-RPC endpoint (`/mcp`)
- ✅ 6 tools: `search_knowledge`, `list_inbox`, `send_message`, `submit_done`, `get_task_status`, `get_service_status`
- ✅ Bearer token authentication (optional, via `MCP_AUTH_TOKEN` env)
- ✅ Project `.mcp.json` configuration
- ✅ Claude Code integration ready

## Next Steps (Phase 5)

1. Marvin integration for planning pipeline
2. Guardrail service integration
3. HTTPS/WSS for production
