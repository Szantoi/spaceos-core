# ADR-061: MCP Connector Pattern — Központi MCP Aggregation & Routing

> **Státusz:** APPROVED
> **Dátum:** 2026-07-08
> **Döntéshozó:** Root
> **Érintettek:** Minden terminál, Backend, Nexus Infrastructure

---

## Kontextus

### Jelenlegi Probléma

A SpaceOS terminálok jelenleg **4 különböző MCP szervert** használnak egyidejűleg:

```json
// terminals/backend/.claude/settings.json
{
  "mcpServers": {
    "spaceos-knowledge": { "url": "http://localhost:3456/mcp" },
    "context7": { "command": "npx", "args": [...] },
    "ref": { "command": "npx", "args": [...] },
    "brave-search": { "command": "npx", "args": [...] }
  }
}
```

**Problémák:**
1. **32 process** fut (8 terminál × 4 MCP szerver)
2. **32 sor konfiguráció** (8 terminál × 4 MCP config)
3. **Fragmentált audit log** (4 különböző helyen)
4. **Nincs központi jogosultságkezelés** (terminal-based permissions)
5. **Új terminál setup:** 5 perc (4 MCP config másolás + token beállítás)
6. **Maintenance overhead:** 4× upgrade, 4× debugging

### Követelmények

1. **Egyetlen MCP kapcsolat** minden terminálnál
2. **Központi jogosultságkezelés** (terminal-based tool permissions)
3. **Központi audit logging** (minden tool call 1 helyen)
4. **Pluggable backend-ek** (új MCP szerver hozzáadása NEM változtatja a terminal config-okat)
5. **Backwards compatible** (fokozatos rollout, rollback lehetőség)
6. **Future-proof** (könnyen bővíthető új MCP szerverekkel)

---

## Döntés

### MCP Connector Pattern Implementálása

**Központi connector szerver** (`mcp-connector`) amely:
- Aggregálja az összes backend MCP szervert
- Tool routing (tool name → backend MCP)
- Terminal-based permission management
- Központi audit logging + metrics
- Health monitoring + auto-reconnect

### Architektúra

```
┌─────────────────────────────────────────────────────────────┐
│                    TERMINALS (8 darab)                       │
│   root, conductor, backend, frontend, architect, ...        │
│                                                               │
│   .claude/settings.json:                                    │
│   {                                                          │
│     "mcpServers": {                                         │
│       "spaceos-connector": {                                │
│         "type": "http",                                     │
│         "url": "http://localhost:3457/mcp",                 │
│         "headers": {                                        │
│           "X-Terminal": "backend",                          │
│           "Authorization": "Bearer <terminal-token>"        │
│         }                                                   │
│       }                                                     │
│     }                                                       │
│   }                                                         │
└────────────────────────┬────────────────────────────────────┘
                         │ Egyetlen MCP kapcsolat
                         ▼
┌─────────────────────────────────────────────────────────────┐
│            MCP CONNECTOR (localhost:3457)                    │
│          spaceos-nexus/mcp-connector/                        │
│                                                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  REQUEST HANDLER                                     │   │
│  │  • Terminal identity (X-Terminal header)            │   │
│  │  • Tool name parsing                                │   │
│  │  • Permission check (terminal × tool)               │   │
│  └─────────────────┬───────────────────────────────────┘   │
│                    │                                         │
│  ┌─────────────────▼───────────────────────────────────┐   │
│  │  ROUTING ENGINE                                      │   │
│  │  • Tool → Backend mapping                           │   │
│  │  • Load balancing (future)                          │   │
│  │  • Circuit breaker (backend failure handling)       │   │
│  └─────────────────┬───────────────────────────────────┘   │
│                    │                                         │
│  ┌─────────────────▼───────────────────────────────────┐   │
│  │  BACKEND MANAGER                                     │   │
│  │  • HTTP backend (knowledge-service)                 │   │
│  │  • STDIO backend (npx spawned processes)            │   │
│  │  • Health monitoring + auto-reconnect               │   │
│  └─────────────────┬───────────────────────────────────┘   │
│                    │                                         │
│  ┌─────────────────▼───────────────────────────────────┐   │
│  │  AUDIT & METRICS                                     │   │
│  │  • Tool call logging (terminal, tool, latency)      │   │
│  │  • Success/failure tracking                         │   │
│  │  • Performance metrics (Prometheus)                 │   │
│  └─────────────────────────────────────────────────────┘   │
└──────┬──────┬──────┬──────┬──────────────────────────────────┘
       │      │      │      │
       ▼      ▼      ▼      ▼
┌──────────┐ ┌────┐ ┌──────────┐ ┌────────────┐
│Knowledge │ │Ref │ │Context7  │ │Brave       │
│Service   │ │Docs│ │Library   │ │Search      │
│(HTTP)    │ │    │ │Docs      │ │            │
│:3456/mcp │ │    │ │          │ │            │
└──────────┘ └────┘ └──────────┘ └────────────┘
```

---

## Részletes Design

### 1. Connector Configuration

**Fájl:** `spaceos-nexus/mcp-connector/config.yaml`

```yaml
version: "1.0"

# Backend MCP servers
backends:
  knowledge:
    type: http
    url: "http://localhost:3456/mcp"
    headers:
      Authorization: "Bearer ${KNOWLEDGE_TOKEN}"
    health_check: "/health"
    timeout: 30000  # 30s

  ref:
    type: stdio
    command: "npx"
    args: ["-y", "@joshuarileydev/ref-mcp-server"]
    auto_restart: true
    health_check_interval: 60000  # 1 min

  context7:
    type: stdio
    command: "npx"
    args: ["-y", "@context7/mcp-server"]
    auto_restart: true
    health_check_interval: 60000

  brave-search:
    type: stdio
    command: "npx"
    args: ["-y", "@modelcontextprotocol/server-brave-search"]
    env:
      BRAVE_API_KEY: "${BRAVE_API_KEY}"
    auto_restart: true

# Tool routing (tool name → backend)
routing:
  # Knowledge Service (80+ tools)
  search_knowledge: knowledge
  list_inbox: knowledge
  send_message: knowledge
  read_memory: knowledge
  write_memory: knowledge
  get_terminal_status_aggregate: knowledge
  # ... (80+ tools)

  # Ref MCP
  ref_search_documentation: ref
  ref_read_url: ref

  # Context7 MCP
  context7_resolve_library_id: context7
  context7_query_docs: context7

  # Brave Search MCP
  brave_web_search: brave-search

# Terminal permissions (terminal → allowed tools)
permissions:
  root:
    - "*"  # All tools

  conductor:
    backends: [knowledge, ref, brave-search]
    tools:
      - search_knowledge
      - list_inbox
      - send_message
      - get_terminal_status_aggregate
      - resolve_dependencies
      - ref_search_documentation
      - brave_web_search

  backend:
    backends: [knowledge, ref, context7]
    tools:
      - search_knowledge
      - read_memory
      - ref_search_documentation
      - context7_query_docs

  frontend:
    backends: [knowledge, ref]
    tools:
      - search_knowledge
      - read_memory
      - ref_search_documentation

  architect:
    backends: [knowledge, ref, brave-search]
    tools:
      - search_knowledge
      - read_memory
      - ref_search_documentation
      - brave_web_search

  librarian:
    backends: [knowledge, ref]
    tools:
      - search_knowledge
      - read_memory
      - write_memory
      - ref_search_documentation

  explorer:
    backends: [knowledge, ref]
    tools:
      - search_knowledge
      - ref_search_documentation

  designer:
    backends: [knowledge, ref]
    tools:
      - search_knowledge
      - ref_search_documentation

  monitor:
    backends: [knowledge]
    tools:
      - get_terminal_status_aggregate
      - get_context_saturation

# Audit logging
audit:
  enabled: true
  log_file: "/opt/spaceos/logs/mcp-connector-audit.log"
  log_format: json
  log_level: info
  metrics_enabled: true
  metrics_port: 9090  # Prometheus metrics

# Performance tuning
performance:
  request_timeout: 30000  # 30s
  max_concurrent_requests: 100
  circuit_breaker:
    enabled: true
    failure_threshold: 5
    timeout: 60000  # 1 min
    half_open_after: 30000  # 30s
```

---

### 2. Connector Implementation

**Technológia:** TypeScript + Express (konzisztens Nexus stack-kel)

**Fájlstruktúra:**

```
spaceos-nexus/mcp-connector/
├── src/
│   ├── server.ts               # Express HTTP szerver
│   ├── routing/
│   │   ├── router.ts           # Tool routing engine
│   │   ├── permissions.ts      # Permission checker
│   │   └── registry.ts         # Tool registry (tool → backend mapping)
│   ├── backends/
│   │   ├── httpBackend.ts      # HTTP MCP backend (knowledge-service)
│   │   ├── stdioBackend.ts     # STDIO MCP backend (npx spawned)
│   │   └── manager.ts          # Backend lifecycle management
│   ├── audit/
│   │   ├── logger.ts           # Audit logger
│   │   └── metrics.ts          # Prometheus metrics
│   ├── health/
│   │   ├── monitor.ts          # Backend health monitoring
│   │   └── circuitBreaker.ts   # Circuit breaker pattern
│   └── config/
│       ├── loader.ts           # config.yaml loader
│       └── validator.ts        # Config validation
├── config.yaml                 # Connector configuration
├── package.json
├── tsconfig.json
└── README.md
```

**Core Implementation:**

```typescript
// src/server.ts
import express from 'express';
import { Router } from './routing/router';
import { BackendManager } from './backends/manager';
import { AuditLogger } from './audit/logger';
import { loadConfig } from './config/loader';

const app = express();
const config = loadConfig('./config.yaml');

// Initialize backends
const backendManager = new BackendManager(config.backends);
await backendManager.initialize();

// Initialize router
const router = new Router(config.routing, config.permissions);

// Initialize audit logger
const audit = new AuditLogger(config.audit);

// MCP endpoint
app.post('/mcp', async (req, res) => {
  const terminal = req.headers['x-terminal'] as string;
  const mcpRequest = req.body;

  // Audit: request start
  const startTime = Date.now();

  try {
    // Permission check
    if (!router.hasPermission(terminal, mcpRequest.params.name)) {
      throw new Error(`Terminal ${terminal} not allowed to use tool ${mcpRequest.params.name}`);
    }

    // Route to backend
    const backend = router.getBackend(mcpRequest.params.name);
    const result = await backendManager.call(backend, mcpRequest);

    // Audit: success
    audit.log({
      terminal,
      tool: mcpRequest.params.name,
      backend,
      latency: Date.now() - startTime,
      status: 'success'
    });

    res.json(result);
  } catch (error) {
    // Audit: failure
    audit.log({
      terminal,
      tool: mcpRequest.params.name,
      latency: Date.now() - startTime,
      status: 'error',
      error: error.message
    });

    res.status(500).json({ error: error.message });
  }
});

// Health endpoint
app.get('/health', async (req, res) => {
  const backends = await backendManager.healthCheck();
  res.json({
    status: backends.every(b => b.healthy) ? 'healthy' : 'degraded',
    backends
  });
});

// Metrics endpoint (Prometheus)
app.get('/metrics', (req, res) => {
  res.set('Content-Type', 'text/plain');
  res.send(audit.getMetrics());
});

app.listen(3457, () => {
  console.log('MCP Connector listening on http://localhost:3457');
});
```

---

### 3. Terminal Configuration (Simplified)

**Minden terminál settings.json EGYSZERŰSÖDIK:**

```json
{
  "permissions": {
    "allow": ["Bash", "Read", "Write", "Edit", "Glob", "Grep"],
    "defaultMode": "bypassPermissions"
  },
  "mcpServers": {
    "spaceos-connector": {
      "type": "http",
      "url": "http://localhost:3457/mcp",
      "headers": {
        "X-Terminal": "backend",
        "Authorization": "Bearer <terminal-token>"
      }
    }
  }
}
```

**Before (4 MCP servers):**
- 32 sor konfiguráció (8 terminál × 4 MCP)
- 32 process (8 terminál × 4 MCP szerver)

**After (1 MCP connector):**
- 8 sor konfiguráció (8 terminál × 1 MCP)
- 9 process (8 terminál + 1 connector + 4 backend MCP)

**Megtakarítás:** -72% process count, -75% config complexity

---

## Implementációs Fázisok

### Phase 1: Prototype & MVP (2-3 nap)

**Cél:** Working prototype 2 terminállal (root, conductor)

**Deliverables:**
1. ✅ Connector HTTP szerver (Express + TypeScript)
2. ✅ HTTP backend support (knowledge-service integration)
3. ✅ STDIO backend support (ref, context7, brave-search spawning)
4. ✅ Tool routing (tool name → backend mapping)
5. ✅ Permission checker (terminal × tool)
6. ✅ Basic audit logging
7. ✅ Health endpoint
8. ✅ Config.yaml loader

**Testing:**
- Root terminál: 3-5 tool hívás (search_knowledge, read_memory, brave_web_search)
- Conductor terminál: get_terminal_status_aggregate, resolve_dependencies
- Latency benchmark: <50ms overhead target
- Permission test: backend terminál NEM kaphat conductor-only tool-t

**Success Criteria:**
- ✅ 2 terminál átállítva connector-ra
- ✅ Latency overhead <50ms
- ✅ 0 permission bypass (security test)
- ✅ Health monitoring működik

---

### Phase 2: Production Hardening (3-4 nap)

**Cél:** Production-ready connector minden terminálra

**Deliverables:**
1. ✅ Circuit breaker pattern (backend failure handling)
2. ✅ Auto-reconnect (backend restart detection)
3. ✅ Prometheus metrics
4. ✅ Structured logging (JSON format)
5. ✅ Performance tuning (connection pooling, request queuing)
6. ✅ Error handling (graceful degradation)
7. ✅ Config hot-reload (config.yaml változás → restart nélkül)

**Testing:**
- Load test: 100 concurrent requests (target: <100ms p95 latency)
- Failure test: Backend crash → auto-reconnect within 30s
- Permission matrix test: 8 terminál × 20 tool = 160 test case
- Config hot-reload test: új MCP backend hozzáadása restart nélkül

**Success Criteria:**
- ✅ p95 latency <100ms
- ✅ Backend failure auto-recovery <30s
- ✅ 0 permission leak
- ✅ Config hot-reload működik

---

### Phase 3: Rollout (1 hét)

**Cél:** Minden terminál (8 darab) átállítása connector-ra

**Rollout Strategy:**

| Nap | Terminálok | Validáció |
|-----|-----------|-----------|
| **Day 1** | root, conductor | Coordinator terminálok (legkritikusabb) |
| **Day 2** | backend | Worker terminál (leggyakrabban használt tool-ok) |
| **Day 3** | frontend | Worker terminál (frontend-specifikus tool-ok) |
| **Day 4** | architect, librarian | Support terminálok |
| **Day 5** | explorer, designer | Support terminálok |
| **Day 6** | monitor | Monitoring terminál (utolsó, legkevésbé kritikus) |
| **Day 7** | Stabilizáció | 24 órás monitoring, rollback plan tesztelés |

**Rollout Protocol (terminálonként):**

1. **Pre-rollout Check:**
   - Connector health check
   - Terminal session backup
   - settings.json backup

2. **Rollout:**
   - settings.json módosítás (multi-MCP → connector)
   - Terminal session restart
   - 5 tool hívás teszt (smoke test)

3. **Post-rollout Validation:**
   - 30 perc monitoring (audit log check)
   - Terminal session stability check
   - Rollback plan ready (settings.json restore)

4. **Rollback Trigger:**
   - Tool call failure rate >10%
   - Latency >500ms (p95)
   - Terminal session crash

**Success Criteria:**
- ✅ 8/8 terminál átállítva connector-ra
- ✅ 0 rollback (stable átállás)
- ✅ Tool call success rate >99%
- ✅ p95 latency <100ms

---

### Phase 4: Optimization & Expansion (folyamatos)

**Cél:** Performance tuning + új MCP backend-ek hozzáadása

**Optimization:**
1. ✅ Connection pooling (HTTP backend-ekhez)
2. ✅ Request caching (gyakori tool hívások)
3. ✅ Load balancing (multi-instance backend support)
4. ✅ Rate limiting (terminal quota management)

**Expansion (új MCP backend-ek):**

| MCP Backend | Használat | Prioritás |
|-------------|-----------|-----------|
| **GitHub MCP** | PR/issue management | High |
| **Playwright MCP** | E2E testing automation | High |
| **Docker MCP** | Container management | Medium |
| **Postgres MCP** | Database query tool | Medium |
| **Slack MCP** | Team notifications | Low |

**Új MCP Backend Hozzáadás Workflow:**

1. **config.yaml módosítás:**
```yaml
backends:
  github:
    type: stdio
    command: "npx"
    args: ["-y", "@modelcontextprotocol/server-github"]
    env:
      GITHUB_TOKEN: "${GITHUB_TOKEN}"

routing:
  github_create_pr: github
  github_list_issues: github

permissions:
  root: ["*"]
  conductor: [github_create_pr, github_list_issues]
```

2. **Config reload (restart nélkül)**
3. **Validation:** 1-2 tool hívás teszt
4. **Rollout:** Fokozatos engedélyezés termináloknak

**Success Criteria:**
- ✅ Új MCP backend hozzáadás idő: <10 perc
- ✅ 0 terminal restart (config hot-reload)
- ✅ Tool availability <1 perc

---

## Migration Path

### Backwards Compatibility

**Legacy Multi-MCP Support (átmeneti):**

```json
// Terminal settings.json (átmeneti konfiguráció)
{
  "mcpServers": {
    // NEW: Connector (prioritás)
    "spaceos-connector": {
      "type": "http",
      "url": "http://localhost:3457/mcp"
    },

    // LEGACY: Direct MCP-k (fallback)
    "spaceos-knowledge": {
      "type": "http",
      "url": "http://localhost:3456/mcp"
    }
  }
}
```

**Claude Code behavior:**
- Először connector-t próbálja
- Ha connector leállt → fallback direct MCP-re
- Audit log warning: "Fallback to direct MCP (connector unavailable)"

**Deprecation Timeline:**

| Dátum | Esemény |
|-------|---------|
| **2026-07-08** | Connector prototype (Phase 1) |
| **2026-07-15** | Connector production (Phase 2) |
| **2026-07-22** | Rollout complete (Phase 3) |
| **2026-08-01** | Legacy multi-MCP deprecation warning |
| **2026-09-01** | Legacy multi-MCP removal (csak connector) |

---

## Performance Metrics

### Latency Targets

| Metrika | Target | Kritikus Küszöb |
|---------|--------|-----------------|
| Connector overhead | <20ms | <50ms |
| End-to-end latency (tool call) | <100ms (p95) | <200ms (p95) |
| Backend health check | <1s | <5s |
| Config reload | <5s | <10s |

### Resource Usage

| Resource | Before (Multi-MCP) | After (Connector) | Megtakarítás |
|----------|-------------------|-------------------|--------------|
| **Process count** | 32 | 9 | **-72%** |
| **Memory (RSS)** | ~800 MB | ~300 MB | **-62%** |
| **CPU (idle)** | ~5% | ~2% | **-60%** |

### Availability

| SLA Metrika | Target | Mérés |
|-------------|--------|-------|
| **Uptime** | >99.9% | Prometheus uptime metric |
| **Tool call success rate** | >99% | Audit log success/failure ratio |
| **Backend auto-recovery** | <30s | Health monitor restart time |

---

## Security Considerations

### Terminal Authentication

**Bearer token:** Minden terminál egyedi token-t kap (X-Terminal header mellett)

```yaml
# config.yaml
authentication:
  enabled: true
  tokens:
    root: "IoUpLUgr4v6Mj5lt4u2XD1JOy5iGmVdxne473srMl2o="
    conductor: "pWz7Y3kFvN2rL9hQ8xT4jU6mS1cV5bE0aR9gD3yH7iO="
    backend: "tN5jK2wP8vL1qR9xM4cF7bS0gH3yU6aE9zV2nD8oT1r="
    # ... (minden terminál egyedi token)
```

**Validation:**
```typescript
async function validateTerminal(terminal: string, token: string): Promise<boolean> {
  const expectedToken = config.authentication.tokens[terminal];
  return token === `Bearer ${expectedToken}`;
}
```

### Permission Matrix

**Runtime permission check:**

```typescript
function hasPermission(terminal: string, tool: string): boolean {
  const terminalPerms = config.permissions[terminal];

  // Root: all tools
  if (terminalPerms.includes('*')) return true;

  // Specific tool list
  return terminalPerms.includes(tool);
}
```

**Audit Trail:**
```json
{
  "timestamp": "2026-07-08T10:30:00Z",
  "terminal": "backend",
  "tool": "search_knowledge",
  "backend": "knowledge",
  "latency": 45,
  "status": "success",
  "permission_granted": true
}
```

### Attack Vectors & Mitigations

| Attack | Mitigation |
|--------|------------|
| **Token leak** | Token rotation (3 hónaponként) |
| **Permission bypass** | Runtime permission check + audit log |
| **DoS (request flood)** | Rate limiting (100 req/min/terminal) |
| **Backend injection** | Input validation + sanitization |
| **MITM** | Localhost-only (http://localhost:3457), later HTTPS |

---

## Monitoring & Observability

### Prometheus Metrics

**Endpoint:** `http://localhost:3457/metrics`

**Metrics:**

```
# Tool call latency
mcp_connector_tool_latency_seconds{terminal="backend",tool="search_knowledge",backend="knowledge"}

# Tool call count
mcp_connector_tool_calls_total{terminal="backend",tool="search_knowledge",backend="knowledge",status="success"}

# Backend health
mcp_connector_backend_healthy{backend="knowledge"} 1

# Permission denials
mcp_connector_permission_denied_total{terminal="backend",tool="write_memory"}

# Circuit breaker state
mcp_connector_circuit_breaker_state{backend="ref"} 0  # 0=closed, 1=open, 2=half-open
```

### Grafana Dashboard

**Panels:**
1. **Tool Call Rate** (calls/sec, per terminal)
2. **Latency Heatmap** (p50, p95, p99)
3. **Error Rate** (errors/sec, per backend)
4. **Backend Health** (healthy/unhealthy status)
5. **Permission Denials** (denials/hour)
6. **Circuit Breaker Events** (open/close events)

### Alerts

| Alert | Condition | Action |
|-------|-----------|--------|
| **Connector Down** | Uptime <99.9% for 5 min | Page on-call (Root) |
| **High Latency** | p95 >200ms for 10 min | Investigate performance |
| **Backend Unhealthy** | Backend down >5 min | Auto-restart + alert |
| **Permission Spike** | Denial rate >10/min | Security review |
| **Circuit Breaker Open** | Backend failing >5 times | Escalate to backend team |

---

## Rollback Plan

### Rollback Trigger Conditions

1. **Critical Failure:**
   - Connector crash loop (restart >5× within 10 min)
   - Tool call success rate <90%
   - p95 latency >500ms

2. **Security Issue:**
   - Permission bypass detected
   - Token leak suspected
   - Unauthorized access attempt

3. **Operational Issue:**
   - Backend connectivity lost (knowledge-service unreachable)
   - Config corruption
   - Deployment error

### Rollback Procedure

**Per-Terminal Rollback (10 perc):**

1. **Backup restore:**
```bash
# Restore settings.json backup
cp terminals/backend/.claude/settings.json.backup \
   terminals/backend/.claude/settings.json
```

2. **Session restart:**
```bash
tmux send-keys -t spaceos-backend C-c
tmux send-keys -t spaceos-backend "claude --profile backend" Enter
```

3. **Validation:**
   - 5 tool hívás teszt (smoke test)
   - 5 perc monitoring (audit log check)

**Full System Rollback (1 óra):**

1. **Connector shutdown:**
```bash
systemctl stop mcp-connector
systemctl disable mcp-connector
```

2. **All terminals rollback:**
```bash
for terminal in root conductor backend frontend architect librarian explorer designer monitor; do
  cp terminals/$terminal/.claude/settings.json.backup \
     terminals/$terminal/.claude/settings.json
  tmux send-keys -t spaceos-$terminal C-c
  tmux send-keys -t spaceos-$terminal "claude --profile $terminal" Enter
done
```

3. **Validation:**
   - Every terminal: 5 tool hívás teszt
   - 30 perc monitoring (stability check)

**Success Criteria:**
- ✅ All terminals operational (multi-MCP mode)
- ✅ Tool call success rate >99%
- ✅ 0 terminal crash

---

## Cost-Benefit Analysis

### Development Cost

| Fázis | Estimated NWT | Calendar Time |
|-------|---------------|---------------|
| Phase 1: Prototype | 180 NWT (~6h) | 1-2 nap |
| Phase 2: Production | 240 NWT (~8h) | 3-4 nap |
| Phase 3: Rollout | 120 NWT (~4h) | 1 hét |
| Phase 4: Documentation | 60 NWT (~2h) | 1 nap |
| **TOTAL** | **600 NWT (~20h)** | **~2 hét** |

### Operational Savings

| Metrika | Before | After | Savings |
|---------|--------|-------|---------|
| **Terminal setup time** | 5 min | 30 sec | **-90%** |
| **Config maintenance** | 32 sor | 8 sor | **-75%** |
| **Process count** | 32 | 9 | **-72%** |
| **Memory usage** | 800 MB | 300 MB | **-62%** |
| **Debugging time** | 4 log helyen | 1 central log | **-75%** |

**Estimated ROI:**
- Development: 600 NWT (~20 hours)
- Savings: ~5 hours/week (terminal management, debugging, setup)
- **Break-even:** ~4 hét
- **Annual savings:** ~250 hours/year

---

## Success Criteria

### Phase 1 (Prototype)

- ✅ 2 terminál (root, conductor) connector-on keresztül működik
- ✅ Latency overhead <50ms
- ✅ 0 permission bypass (security test pass)
- ✅ Health monitoring működik

### Phase 2 (Production)

- ✅ p95 latency <100ms (100 concurrent request load test)
- ✅ Backend failure auto-recovery <30s
- ✅ 0 permission leak (permission matrix test 160 case)
- ✅ Config hot-reload működik

### Phase 3 (Rollout)

- ✅ 8/8 terminál átállítva connector-ra
- ✅ 0 rollback (stable átállás)
- ✅ Tool call success rate >99%
- ✅ p95 latency <100ms (production load)

### Phase 4 (Optimization)

- ✅ Új MCP backend hozzáadás <10 perc
- ✅ 0 terminal restart (config hot-reload)
- ✅ Tool availability <1 perc (új backend after config reload)

---

## Alternatives Considered

### Alternative A: Knowledge Service Expansion

**Concept:** Bake all MCP tools into knowledge-service (monolith).

**Rejected Reasons:**
- ❌ Monolith complexity (knowledge-service already 30k+ LOC)
- ❌ External dependencies (NPM packages for ref, context7, brave)
- ❌ Less pluggable (new MCP = code change + restart)
- ❌ Single point of failure (knowledge-service crash = all tools unavailable)

### Alternative B: Multi-MCP Status Quo

**Concept:** Keep current 4 MCP server config per terminal.

**Rejected Reasons:**
- ❌ 32 process overhead
- ❌ 32 sor config maintenance
- ❌ Fragmentált audit log
- ❌ No central permission management
- ❌ High terminal setup time (5 min)

### Alternative C: Client-Side Routing (Claude Code Extension)

**Concept:** Claude Code client-side tool routing (config-based).

**Rejected Reasons:**
- ❌ Claude Code specific (not CLI-agnostic per ADR-060)
- ❌ No central audit logging
- ❌ No server-side permission enforcement
- ❌ Requires Claude Code feature development (external dependency)

---

## References

- **ADR-060:** CLI-Agnosztikus Telegram Architektúra (connector pattern precedens)
- **MCP Protocol Spec:** https://spec.modelcontextprotocol.io/
- **Knowledge Service Implementation:** `spaceos-nexus/knowledge-service/src/mcp.ts`
- **Terminal Config Docs:** `docs/knowledge/deployment/SESSION_REPAIR_GUIDE.md`

---

## Appendix: Tool Catalog

**Knowledge Service Tools (80+):**
- Mailbox: `list_inbox`, `send_message`, `read_inbox_message`, `complete_inbox_message`
- Identity: `get_identity`, `read_memory`, `write_memory`, `append_memory`
- Terminal Status: `get_terminal_status_aggregate`, `register_working`, `register_idle`
- Context Persistence: `build_session_start_context`, `get_context_saturation`, `write_session_state`
- Epic Management: `resolve_dependencies`, `get_epic_status`
- ... (full list: `docs/knowledge/patterns/MCP_TOOLS_CATALOGUE.md`)

**Ref MCP Tools:**
- `ref_search_documentation`: Search developer documentation
- `ref_read_url`: Fetch and parse documentation URL

**Context7 MCP Tools:**
- `context7_resolve_library_id`: Resolve NPM/PyPI library ID
- `context7_query_docs`: Query library documentation

**Brave Search MCP Tools:**
- `brave_web_search`: Web search with context

---

**Implementáció kezdése:** Backend-nek task kiosztás (Conductor koordinálja)
**Estimated Timeline:** 2 hét (prototype → production → rollout)
**ROI:** Break-even 4 hét, annual savings ~250 hours

**Approval:** ✅ ROOT APPROVED (2026-07-08)
