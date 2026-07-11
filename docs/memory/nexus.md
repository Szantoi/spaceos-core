# NEXUS Memory — Updated 2026-07-07

## 🌐 PRODUCTION STATUS

**Live Services:**
- **MCP Endpoint**: `https://nexus.joinerytech.hu/mcp`
- **Datahaven Dashboard**: `https://datahaven.joinerytech.hu`
- **Health Check**: `https://nexus.joinerytech.hu/health`
- **Systemd Service**: `spaceos-knowledge`

**Deployment:**
- ChromaDB: localhost:8001 (1106 docs indexed)
- Nginx: `/etc/nginx/sites-enabled/nexus-knowledge`
- SSL: `/etc/letsencrypt/live/joinerytech.hu/` (9 domains)
- Logs: `/var/log/spaceos/knowledge-service.log`

---

## 🚀 COMPLETED PHASES (1-6.5)

| Phase | Feature | Status |
|-------|---------|--------|
| **Phase 1** | Knowledge Service (RAG) — ChromaDB + Voyage AI | ✅ |
| **Phase 2** | Mailbox Tools — list_inbox, send_message, submit_done | ✅ |
| **Phase 3** | SSE Live Notifications — subscribe, broadcast, event emitter | ✅ |
| **Phase 4** | MCP Protocol — JSON-RPC 2.0, 23 tools, Claude Code integration | ✅ |
| **Phase 4.5** | HTTPS + Auth — public endpoint, systemd service | ✅ |
| **Phase 5** | TypeScript Nightwatch Scheduler — bash → TS migration | ✅ |
| **Phase 5.5** | TypeScript Reviewer Pipeline — Anthropic SDK dual review | ✅ |
| **Phase 6** | React Datahaven Frontend — Planning + Projects pages | ✅ |
| **Phase 6.5** | Dashboard & Kanban APIs — Real-time file scanning | ✅ |

---

## 🔐 MCP ACCESS

### Claude Code Configuration (Remote)

```json
// ~/.claude/mcp.json or project .mcp.json
{
  "mcpServers": {
    "spaceos-knowledge": {
      "type": "http",
      "url": "https://nexus.joinerytech.hu/mcp",
      "timeout": 60000,
      "headers": {
        "Authorization": "Bearer IoUpLUgr4v6Mj5lt4u2XD1JOy5iGmVdxne473srMl2o="
      }
    }
  }
}
```

### MCP Tools Summary (23 tools)

| Category | Tools | Count |
|----------|-------|-------|
| **Knowledge** | search_knowledge | 1 |
| **Mailbox** | list_inbox, send_message, submit_done | 3 |
| **Tasks** | get_task_status | 1 |
| **Identity** | get_identity, list_terminals, read/write/append_memory | 5 |
| **Skills & Workflow** | list_skills, get_skill, get_workflow, get_terminal_setup, get_project_context | 5 |
| **Terminal Docs** | list_terminal_docs, get_terminal_docs, get_terminals_index | 3 |
| **Terminal Status** | register_working, register_idle, get_terminal_status | 3 |
| **System** | get_capabilities, get_service_status | 2 |

**Full list**: `spaceos-nexus/knowledge-service/src/mcp.ts`

---

## ⚙️ WAKE-ON-INBOX ARCHITECTURE

**Automated session management for cost optimization**

### Key Components

1. **inboxWatcher.ts** — Chokidar v5 recursive file watch (polling mode)
2. **terminalStatus.ts** — WORKING/IDLE tracking (10 min timeout)
3. **sessionStarter.ts** — Tmux session auto-start (ALLOWED_TERMINALS whitelist)
4. **SSE Bridge** — Wake-up events for IDLE terminals only

### Workflow

```
New UNREAD inbox message detected
  ↓
Terminal status check (WORKING/IDLE)
  ↓
If IDLE → SSE wake_up event + tmux session start
  ↓
Claude starts with frontmatter model (haiku/sonnet/opus)
  ↓
15+ min idle + 0 UNREAD → graceful shutdown (/exit)
```

**Result**: ~600MB RAM savings, cost-efficient operation

**Critical**: `PrivateTmp=false` in systemd (tmux socket access)

---

## 📊 DATAHAVEN DASHBOARD (Phase 6+6.5)

**URL**: https://datahaven.joinerytech.hu

### Pages Implemented

1. **Dashboard** — Real-time metrics (17 terminals, inbox/outbox counts)
2. **Kanban** — Dual-track board (Discovery + Delivery swimlanes)
3. **Planning** — 5-stage pipeline (Idea → Queue)
4. **Projects** — Gantt timeline (8 months: -2 → +6)

### Tech Stack

- **Frontend**: React 19 + TypeScript 6 + Vite 8 + Tailwind CSS 4
- **Backend**: Express.js + Node.js 22 + TypeScript
- **Data Source**: File system (docs/planning/, docs/tasks/, docs/mailbox/)
- **Build**: 278.25 kB bundle (83.25 kB gzip)

### Backend API Endpoints (5)

| Endpoint | Function |
|----------|----------|
| `/api/dashboard` | Terminal status + inbox/outbox counts |
| `/api/kanban/snapshot` | Discovery + Delivery tracks WIP |
| `/api/kanban/metrics` | Throughput + cycle time placeholders |
| `/api/planning/items` | 5-stage pipeline items |
| `/api/projects` | Gantt timeline + project list |

**Architecture**: File system = real-time database, markdown frontmatter parsing

---

## 🔧 OPERATIONS

### Systemd Management

```bash
sudo systemctl status spaceos-knowledge
sudo systemctl restart spaceos-knowledge
sudo journalctl -u spaceos-knowledge -f
```

### Health Check

```bash
# No auth required
curl https://nexus.joinerytech.hu/health

# MCP tools list (with auth)
curl -X POST https://nexus.joinerytech.hu/mcp \
  -H 'Authorization: Bearer IoUpLUgr4v6Mj5lt4u2XD1JOy5iGmVdxne473srMl2o=' \
  -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'
```

### Terminal Status API

```bash
# Register working
POST /api/terminal/:terminal/status
  { "state": "working", "taskId": "MSG-XXX" }

# Register idle
POST /api/terminal/:terminal/status
  { "state": "idle" }

# SSE subscription
GET /api/mailbox/:terminal/subscribe
```

---

## 📁 CODE STRUCTURE

**Location**: `/opt/spaceos/spaceos-nexus/knowledge-service/`

**Core Files**:
- `src/server.ts` — Express + SSE + MCP mount + rate limiting
- `src/mcp.ts` — MCP JSON-RPC protocol handler (23 tools)
- `src/mailbox.ts`, `src/vectorStore.ts`, `src/embeddings.ts`
- `src/identity.ts`, `src/skills.ts`, `src/indexer.ts`
- `src/inboxWatcher.ts`, `src/terminalStatus.ts`, `src/sessionStarter.ts`

**Pipeline Directory** (Bash → TypeScript migration):
- `src/pipeline/nightwatch.ts` — Main dispatcher (2 min interval)
- `src/pipeline/watchDone.ts` — DONE → reviewer trigger
- `src/pipeline/reviewer.ts` — Anthropic SDK dual Haiku review
- `src/pipeline/pipeline.ts` — Post-review actions
- `src/pipeline/watchIdle.ts`, `watchStuck.ts`, `watchPriority.ts`

**Full tree**: `spaceos-nexus/knowledge-service/README.md`

---

## 🎯 NEXT STEPS (Phase 7)

1. **Marvin Integration** — Planning pipeline automation
2. **Guardrail Service** — Advanced safety checks
3. **Advanced Metrics** — Real throughput + cycle time (not placeholders)

---

## 🔐 SECURITY (2026-06-20 Audit)

**Completed Security Enhancements:**

| Priority | Fix | Status |
|----------|-----|--------|
| **P0** | `.env` permission 755→600 | ✅ |
| **P0** | `sessionStarter.ts` path traversal protection (ALLOWED_TERMINALS whitelist) | ✅ |
| **P1** | Rate limiting middleware (100 req/min/IP) | ✅ |
| **P1** | `/ready` endpoint (Kubernetes probe compatible) | ✅ |
| **P1** | Input validation with Zod (search, mailbox, terminal params) | ✅ |
| **P2** | Graceful shutdown (SIGTERM/SIGINT, 10s timeout) | ✅ |
| **P2** | Log rotation config (`scripts/logrotate.conf`) | ✅ |
| **P3** | ChromaDB backup script (`scripts/chromadb-backup.sh`) | ✅ |

**Audit Document**: `spaceos-nexus/SECURITY_AUDIT_2026-06-20.md`

---

## 🧠 KEY LEARNINGS

### Architecture Patterns

1. **Event-Driven Session Management** — Chokidar master, nightwatch fallback
2. **File System as Database** — Real-time markdown frontmatter parsing
3. **Cost Optimization** — Wake-on-inbox + 15 min idle shutdown = 600MB RAM savings
4. **Modular TypeScript Pipeline** — Gradual bash → TS migration (Phase 5+5.5)

### Protocol Decisions

1. **HTTP > SSE** for remote MCP servers (no WebSocket handshake)
2. **JSON-RPC 2.0** strict format for MCP protocol
3. **Bearer token** mandatory for public endpoints
4. **EventSource API** native browser support for SSE

### DevOps Lessons

1. **Systemd hardening** — `PrivateTmp=false` for tmux socket access
2. **Nginx auth forwarding** — `proxy_set_header Authorization` required
3. **certbot --expand** — Add new subdomain without recreating cert
4. **Rate limiting** — Skip `/health` and `/ready` for monitoring
5. **DNS propagation** — Rackforest fast (~1 min)

### Best Practices (2026-06-20)

1. **Sub-Agent Orchestration** — Complex tasks → Explore agents, Code review → Security + Architect parallel
2. **Model Selection** — Haiku (search, summary), Sonnet (code), Opus (architecture)
3. **Multi-Agent Audit** — 4 agents (DevOps, Security, Architect, Devil's Advocate) for comprehensive review

---

## 📝 SOLVED ISSUES

**HTTPS + Systemd:**
- DNS record setup + certbot SSL expansion
- systemd `ProtectHome` conflict with node_modules → relaxed hardening

**MCP TypeScript Types:**
- `sendMessage` params type assertion: `as 'task' | 'question' | 'done' | 'blocked'`

**watch-stuck.sh Spam:**
- Removed "input field" and "empty prompt" detection
- Kept only "queued-messages" and "model-selector"
- Result: false positive alerts eliminated

---

**Last Updated:** 2026-07-07
**Status:** 🟢 PRODUCTION READY
**Memory Tier:** Warm (14-day, critical infrastructure knowledge)

---

_This memory is compressed from 17KB to ~6KB by removing redundant session narratives and detailed documentation (available in codebase). Preserved: URLs, auth, service info, architecture patterns, and key learnings._
