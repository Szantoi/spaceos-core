# EPIC-MCP-CONNECTOR Implementation Plan

> **Epic ID:** EPIC-MCP-CONNECTOR
> **Status:** PLANNING
> **Priority:** HIGH
> **Owner:** Root → Conductor → Backend
> **Estimated:** 600 NWT (~20 hours)
> **Timeline:** 2 hét
> **ADR:** ADR-061

---

## Executive Summary

**Cél:** Központi MCP Connector Pattern implementálása → 8 terminál × 1 MCP config (instead of 8 × 4 MCP).

**Business Value:**
- **-72% process count** (32 → 9 process)
- **-75% config complexity** (32 sor → 8 sor)
- **Központi audit trail** (4 különböző log → 1 central log)
- **Gyorsabb terminal setup** (5 min → 30 sec)
- **Future-proof** (új MCP backend <10 perc hozzáadás)

**ROI:** Break-even 4 hét, annual savings ~250 hours

---

## Architecture Overview

```
TERMINALS (8) → MCP CONNECTOR (1) → BACKENDS (4)
                   :3457                ├── knowledge-service :3456
                                       ├── ref (npx)
                                       ├── context7 (npx)
                                       └── brave-search (npx)

Before: 8 terminals × 4 MCP = 32 process
After:  8 terminals + 1 connector + 4 backends = 9 process
```

**Key Components:**
1. **MCP Connector** — HTTP szerver (Express + TypeScript)
2. **Backend Manager** — HTTP + STDIO backend lifecycle
3. **Routing Engine** — Tool name → backend mapping
4. **Permission System** — Terminal × tool matrix
5. **Audit Logger** — Központi tool call logging + metrics

---

## Phase Breakdown

### Phase 1: Prototype & MVP (2-3 nap, 180 NWT)

**Goal:** Working prototype 2 terminállal (root, conductor)

#### Tasks

**1.1 Project Setup (30 NWT)**

**Deliverables:**
```
spaceos-nexus/mcp-connector/
├── src/
│   ├── server.ts
│   ├── types.ts
│   └── config/
│       ├── loader.ts
│       └── validator.ts
├── config.yaml
├── package.json
├── tsconfig.json
└── README.md
```

**Implementation:**
```bash
cd /opt/spaceos/spaceos-nexus
mkdir -p mcp-connector/src/config
cd mcp-connector

# package.json
npm init -y
npm install express cors dotenv yaml
npm install -D typescript @types/express @types/node ts-node nodemon

# tsconfig.json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "commonjs",
    "outDir": "./dist",
    "rootDir": "./src",
    "strict": true,
    "esModuleInterop": true
  }
}
```

**Acceptance Criteria:**
- ✅ Project struktura létrehozva
- ✅ TypeScript build működik (`npm run build`)
- ✅ Dev mode működik (`npm run dev`)

---

**1.2 Config Loader (40 NWT)**

**File:** `src/config/loader.ts`

**Implementation:**
```typescript
import fs from 'fs';
import yaml from 'yaml';

interface BackendConfig {
  type: 'http' | 'stdio';
  url?: string;
  command?: string;
  args?: string[];
  headers?: Record<string, string>;
  env?: Record<string, string>;
  health_check?: string;
  timeout?: number;
  auto_restart?: boolean;
}

interface ConnectorConfig {
  backends: Record<string, BackendConfig>;
  routing: Record<string, string>;  // tool → backend
  permissions: Record<string, string[] | { backends?: string[], tools?: string[] }>;
  audit: {
    enabled: boolean;
    log_file: string;
    log_format: 'json' | 'text';
    metrics_enabled: boolean;
  };
}

export function loadConfig(path: string): ConnectorConfig {
  const fileContents = fs.readFileSync(path, 'utf8');
  const config = yaml.parse(fileContents);

  // Validation (basic)
  if (!config.backends) throw new Error('Missing backends config');
  if (!config.routing) throw new Error('Missing routing config');
  if (!config.permissions) throw new Error('Missing permissions config');

  return config;
}

export function validateConfig(config: ConnectorConfig): void {
  // Validate: all routing targets exist in backends
  for (const [tool, backend] of Object.entries(config.routing)) {
    if (!config.backends[backend]) {
      throw new Error(`Routing tool ${tool} targets non-existent backend ${backend}`);
    }
  }

  // Validate: all permission tools exist in routing
  for (const [terminal, perms] of Object.entries(config.permissions)) {
    if (Array.isArray(perms)) continue;  // ['*'] case

    const tools = perms.tools || [];
    for (const tool of tools) {
      if (tool === '*') continue;
      if (!config.routing[tool]) {
        throw new Error(`Permission tool ${tool} (terminal ${terminal}) not in routing`);
      }
    }
  }
}
```

**Acceptance Criteria:**
- ✅ config.yaml betöltés működik
- ✅ Validation detektálja a hibás config-ot
- ✅ Env variable substitution (${VAR}) működik

---

**1.3 HTTP Backend Manager (50 NWT)**

**File:** `src/backends/httpBackend.ts`

**Implementation:**
```typescript
import axios, { AxiosInstance } from 'axios';

export class HttpBackend {
  private client: AxiosInstance;
  private name: string;
  private healthCheckUrl?: string;

  constructor(name: string, url: string, headers?: Record<string, string>, healthCheck?: string) {
    this.name = name;
    this.healthCheckUrl = healthCheck ? `${url}${healthCheck}` : undefined;

    this.client = axios.create({
      baseURL: url,
      headers: headers || {},
      timeout: 30000,
    });
  }

  async call(mcpRequest: any): Promise<any> {
    const response = await this.client.post('', mcpRequest);
    return response.data;
  }

  async healthCheck(): Promise<boolean> {
    if (!this.healthCheckUrl) return true;  // No health check configured

    try {
      const response = await axios.get(this.healthCheckUrl, { timeout: 5000 });
      return response.status === 200;
    } catch (error) {
      console.error(`[${this.name}] Health check failed:`, error.message);
      return false;
    }
  }

  getName(): string {
    return this.name;
  }
}
```

**Acceptance Criteria:**
- ✅ HTTP MCP backend (knowledge-service) hívás működik
- ✅ Health check működik (`/health` endpoint)
- ✅ Timeout handling (30s)

---

**1.4 Routing Engine (40 NWT)**

**File:** `src/routing/router.ts`

**Implementation:**
```typescript
import { ConnectorConfig } from '../config/loader';

export class Router {
  private routing: Record<string, string>;
  private permissions: Record<string, string[] | { backends?: string[], tools?: string[] }>;

  constructor(config: ConnectorConfig) {
    this.routing = config.routing;
    this.permissions = config.permissions;
  }

  getBackend(toolName: string): string {
    const backend = this.routing[toolName];
    if (!backend) {
      throw new Error(`No backend configured for tool: ${toolName}`);
    }
    return backend;
  }

  hasPermission(terminal: string, toolName: string): boolean {
    const terminalPerms = this.permissions[terminal];

    // No permission config = deny
    if (!terminalPerms) {
      console.warn(`Terminal ${terminal} has no permission config`);
      return false;
    }

    // Root: all tools
    if (Array.isArray(terminalPerms) && terminalPerms.includes('*')) {
      return true;
    }

    // Specific tool list
    if (Array.isArray(terminalPerms)) {
      return terminalPerms.includes(toolName);
    }

    // Object format: { tools: [...] }
    const tools = terminalPerms.tools || [];
    return tools.includes('*') || tools.includes(toolName);
  }
}
```

**Acceptance Criteria:**
- ✅ Tool → backend routing működik
- ✅ Permission check működik (terminal × tool)
- ✅ Root terminal `*` permission működik
- ✅ Permission denied error clean message

---

**1.5 Express HTTP Server (20 NWT)**

**File:** `src/server.ts`

**Implementation:**
```typescript
import express from 'express';
import cors from 'cors';
import { loadConfig, validateConfig } from './config/loader';
import { Router } from './routing/router';
import { HttpBackend } from './backends/httpBackend';

const app = express();
app.use(cors());
app.use(express.json({ limit: '50mb' }));

// Load config
const config = loadConfig('./config.yaml');
validateConfig(config);

// Initialize router
const router = new Router(config);

// Initialize backends (HTTP only for MVP)
const backends: Record<string, HttpBackend> = {};
for (const [name, backendConfig] of Object.entries(config.backends)) {
  if (backendConfig.type === 'http') {
    backends[name] = new HttpBackend(
      name,
      backendConfig.url!,
      backendConfig.headers,
      backendConfig.health_check
    );
    console.log(`[Backend] ${name} initialized (HTTP: ${backendConfig.url})`);
  }
}

// MCP endpoint
app.post('/mcp', async (req, res) => {
  const terminal = req.headers['x-terminal'] as string;
  const mcpRequest = req.body;
  const toolName = mcpRequest.params?.name;

  const startTime = Date.now();

  try {
    // Validate terminal header
    if (!terminal) {
      throw new Error('Missing X-Terminal header');
    }

    // Validate tool name
    if (!toolName) {
      throw new Error('Missing tool name in MCP request');
    }

    // Permission check
    if (!router.hasPermission(terminal, toolName)) {
      throw new Error(`Terminal ${terminal} not allowed to use tool ${toolName}`);
    }

    // Route to backend
    const backendName = router.getBackend(toolName);
    const backend = backends[backendName];

    if (!backend) {
      throw new Error(`Backend ${backendName} not initialized`);
    }

    // Call backend
    const result = await backend.call(mcpRequest);

    // Log success
    const latency = Date.now() - startTime;
    console.log(`[Audit] ${terminal} → ${toolName} → ${backendName} (${latency}ms) ✅`);

    res.json(result);
  } catch (error: any) {
    const latency = Date.now() - startTime;
    console.error(`[Audit] ${terminal} → ${toolName} (${latency}ms) ❌`, error.message);

    res.status(500).json({
      jsonrpc: '2.0',
      error: {
        code: -32603,
        message: error.message,
      },
      id: mcpRequest.id,
    });
  }
});

// Health endpoint
app.get('/health', async (req, res) => {
  const backendHealth: Record<string, boolean> = {};

  for (const [name, backend] of Object.entries(backends)) {
    backendHealth[name] = await backend.healthCheck();
  }

  const allHealthy = Object.values(backendHealth).every(h => h);

  res.status(allHealthy ? 200 : 503).json({
    status: allHealthy ? 'healthy' : 'degraded',
    backends: backendHealth,
  });
});

// Start server
const PORT = 3457;
app.listen(PORT, () => {
  console.log(`MCP Connector listening on http://localhost:${PORT}`);
  console.log(`Health check: http://localhost:${PORT}/health`);
});
```

**Acceptance Criteria:**
- ✅ HTTP szerver fut (localhost:3457)
- ✅ `/mcp` endpoint működik
- ✅ `/health` endpoint működik
- ✅ Permission check működik (403 ha nincs jog)
- ✅ Audit log console-on látható

---

**1.6 Config.yaml (MVP) (20 NWT)**

**File:** `config.yaml`

```yaml
version: "1.0"

backends:
  knowledge:
    type: http
    url: "http://localhost:3456/mcp"
    headers:
      Authorization: "Bearer IoUpLUgr4v6Mj5lt4u2XD1JOy5iGmVdxne473srMl2o="
    health_check: "/health"
    timeout: 30000

routing:
  # Knowledge Service tools (top 20 most used)
  search_knowledge: knowledge
  list_inbox: knowledge
  send_message: knowledge
  read_inbox_message: knowledge
  complete_inbox_message: knowledge
  read_memory: knowledge
  write_memory: knowledge
  append_memory: knowledge
  get_terminal_status_aggregate: knowledge
  register_working: knowledge
  register_idle: knowledge
  build_session_start_context: knowledge
  get_context_saturation: knowledge
  write_session_state: knowledge
  read_session_state: knowledge
  write_terminal_status_md: knowledge
  increment_turn_count: knowledge
  reset_turn_count: knowledge
  resolve_dependencies: knowledge
  create_task: knowledge

permissions:
  root:
    - "*"  # All tools

  conductor:
    tools:
      - search_knowledge
      - list_inbox
      - send_message
      - create_task
      - get_terminal_status_aggregate
      - resolve_dependencies
      - register_working
      - register_idle

audit:
  enabled: true
  log_file: "/opt/spaceos/logs/mcp-connector-audit.log"
  log_format: json
  metrics_enabled: false  # Phase 2
```

**Acceptance Criteria:**
- ✅ Config betöltés működik
- ✅ Knowledge service backend konfigurálva
- ✅ Top 20 tool routing konfigurálva
- ✅ Root + Conductor permissions konfigurálva

---

### Phase 1 Testing & Validation

**Test Plan:**

**1. Root Terminal Test:**
```bash
# Terminal: root
# Tool: search_knowledge (knowledge backend)

# Expected result: 200 OK, search results returned
# Expected log: [Audit] root → search_knowledge → knowledge (45ms) ✅
```

**2. Conductor Terminal Test:**
```bash
# Terminal: conductor
# Tool: get_terminal_status_aggregate (knowledge backend)

# Expected result: 200 OK, terminal status returned
# Expected log: [Audit] conductor → get_terminal_status_aggregate → knowledge (67ms) ✅
```

**3. Permission Denial Test:**
```bash
# Terminal: conductor
# Tool: write_memory (NOT in conductor permissions)

# Expected result: 500 error, "Terminal conductor not allowed to use tool write_memory"
# Expected log: [Audit] conductor → write_memory (3ms) ❌
```

**4. Health Check Test:**
```bash
curl http://localhost:3457/health

# Expected result:
# {
#   "status": "healthy",
#   "backends": {
#     "knowledge": true
#   }
# }
```

**5. Latency Benchmark:**
```bash
# 10 consecutive tool calls
# Expected: p95 latency <50ms (connector overhead <20ms)
```

**Phase 1 Success Criteria:**
- ✅ 2 terminál (root, conductor) működik connector-on keresztül
- ✅ Permission check működik (conductor write_memory denied)
- ✅ Latency overhead <50ms
- ✅ Health check működik
- ✅ Audit log clean (console output)

---

### Phase 2: Production Hardening (3-4 nap, 240 NWT)

**Goal:** Production-ready connector minden terminálra

#### Tasks

**2.1 STDIO Backend Support (60 NWT)**

**File:** `src/backends/stdioBackend.ts`

**Implementation:**
```typescript
import { spawn, ChildProcess } from 'child_process';

export class StdioBackend {
  private process?: ChildProcess;
  private name: string;
  private command: string;
  private args: string[];
  private env?: Record<string, string>;
  private autoRestart: boolean;
  private requestId = 0;
  private pendingRequests: Map<number, { resolve: Function, reject: Function }> = new Map();

  constructor(name: string, command: string, args: string[], env?: Record<string, string>, autoRestart = true) {
    this.name = name;
    this.command = command;
    this.args = args;
    this.env = env;
    this.autoRestart = autoRestart;
  }

  async start(): Promise<void> {
    this.process = spawn(this.command, this.args, {
      env: { ...process.env, ...this.env },
      stdio: ['pipe', 'pipe', 'pipe'],
    });

    this.process.stdout!.on('data', (data) => {
      // Parse JSON-RPC response
      try {
        const response = JSON.parse(data.toString());
        const pending = this.pendingRequests.get(response.id);
        if (pending) {
          pending.resolve(response);
          this.pendingRequests.delete(response.id);
        }
      } catch (error) {
        console.error(`[${this.name}] Failed to parse response:`, error);
      }
    });

    this.process.on('exit', (code) => {
      console.warn(`[${this.name}] Process exited with code ${code}`);
      if (this.autoRestart) {
        console.log(`[${this.name}] Auto-restarting...`);
        setTimeout(() => this.start(), 5000);
      }
    });

    console.log(`[Backend] ${this.name} started (STDIO: ${this.command})`);
  }

  async call(mcpRequest: any): Promise<any> {
    if (!this.process) {
      throw new Error(`Backend ${this.name} not started`);
    }

    const requestId = ++this.requestId;
    mcpRequest.id = requestId;

    return new Promise((resolve, reject) => {
      this.pendingRequests.set(requestId, { resolve, reject });

      this.process!.stdin!.write(JSON.stringify(mcpRequest) + '\n');

      // Timeout after 30s
      setTimeout(() => {
        if (this.pendingRequests.has(requestId)) {
          this.pendingRequests.delete(requestId);
          reject(new Error(`Timeout waiting for ${this.name} response`));
        }
      }, 30000);
    });
  }

  async healthCheck(): Promise<boolean> {
    return this.process !== undefined && !this.process.killed;
  }

  getName(): string {
    return this.name;
  }
}
```

**Config Extension:**
```yaml
backends:
  ref:
    type: stdio
    command: "npx"
    args: ["-y", "@joshuarileydev/ref-mcp-server"]
    auto_restart: true

  context7:
    type: stdio
    command: "npx"
    args: ["-y", "@context7/mcp-server"]
    auto_restart: true

  brave-search:
    type: stdio
    command: "npx"
    args: ["-y", "@modelcontextprotocol/server-brave-search"]
    env:
      BRAVE_API_KEY: "${BRAVE_API_KEY}"
    auto_restart: true

routing:
  ref_search_documentation: ref
  ref_read_url: ref
  context7_resolve_library_id: context7
  context7_query_docs: context7
  brave_web_search: brave-search
```

**Acceptance Criteria:**
- ✅ NPX STDIO backend spawning működik
- ✅ JSON-RPC request/response működik
- ✅ Auto-restart működik (process crash esetén)
- ✅ Env variable injection működik (BRAVE_API_KEY)

---

**2.2 Structured Audit Logger (50 NWT)**

**File:** `src/audit/logger.ts`

**Implementation:**
```typescript
import fs from 'fs';

interface AuditEntry {
  timestamp: string;
  terminal: string;
  tool: string;
  backend: string;
  latency: number;
  status: 'success' | 'error';
  error?: string;
}

export class AuditLogger {
  private logFile: string;
  private logFormat: 'json' | 'text';

  constructor(logFile: string, logFormat: 'json' | 'text' = 'json') {
    this.logFile = logFile;
    this.logFormat = logFormat;
  }

  log(entry: AuditEntry): void {
    const timestamp = new Date().toISOString();
    const logEntry = { ...entry, timestamp };

    let logLine: string;
    if (this.logFormat === 'json') {
      logLine = JSON.stringify(logEntry) + '\n';
    } else {
      const status = entry.status === 'success' ? '✅' : '❌';
      logLine = `[${timestamp}] ${entry.terminal} → ${entry.tool} → ${entry.backend} (${entry.latency}ms) ${status}${entry.error ? ' ' + entry.error : ''}\n`;
    }

    fs.appendFileSync(this.logFile, logLine);

    // Console log (colored)
    const statusIcon = entry.status === 'success' ? '✅' : '❌';
    console.log(`[Audit] ${entry.terminal} → ${entry.tool} → ${entry.backend} (${entry.latency}ms) ${statusIcon}`);
  }
}
```

**Acceptance Criteria:**
- ✅ JSON format logging működik
- ✅ Log fájl írás működik (`/opt/spaceos/logs/mcp-connector-audit.log`)
- ✅ Console output működik (színes emoji)

---

**2.3 Prometheus Metrics (60 NWT)**

**File:** `src/audit/metrics.ts`

**Implementation:**
```typescript
import client from 'prom-client';

export class MetricsCollector {
  private register: client.Registry;
  private toolCallLatency: client.Histogram;
  private toolCallCount: client.Counter;
  private backendHealthGauge: client.Gauge;
  private permissionDeniedCount: client.Counter;

  constructor() {
    this.register = new client.Registry();

    this.toolCallLatency = new client.Histogram({
      name: 'mcp_connector_tool_latency_seconds',
      help: 'Tool call latency in seconds',
      labelNames: ['terminal', 'tool', 'backend'],
      buckets: [0.01, 0.05, 0.1, 0.5, 1, 5],
      registers: [this.register],
    });

    this.toolCallCount = new client.Counter({
      name: 'mcp_connector_tool_calls_total',
      help: 'Total tool calls',
      labelNames: ['terminal', 'tool', 'backend', 'status'],
      registers: [this.register],
    });

    this.backendHealthGauge = new client.Gauge({
      name: 'mcp_connector_backend_healthy',
      help: 'Backend health status (1=healthy, 0=unhealthy)',
      labelNames: ['backend'],
      registers: [this.register],
    });

    this.permissionDeniedCount = new client.Counter({
      name: 'mcp_connector_permission_denied_total',
      help: 'Permission denied count',
      labelNames: ['terminal', 'tool'],
      registers: [this.register],
    });
  }

  recordToolCall(terminal: string, tool: string, backend: string, latency: number, status: 'success' | 'error'): void {
    this.toolCallLatency.labels(terminal, tool, backend).observe(latency / 1000);
    this.toolCallCount.labels(terminal, tool, backend, status).inc();
  }

  recordPermissionDenied(terminal: string, tool: string): void {
    this.permissionDeniedCount.labels(terminal, tool).inc();
  }

  setBackendHealth(backend: string, healthy: boolean): void {
    this.backendHealthGauge.labels(backend).set(healthy ? 1 : 0);
  }

  getMetrics(): string {
    return this.register.metrics();
  }
}
```

**Server Integration:**
```typescript
// src/server.ts
const metrics = new MetricsCollector();

app.get('/metrics', (req, res) => {
  res.set('Content-Type', 'text/plain');
  res.send(metrics.getMetrics());
});
```

**Acceptance Criteria:**
- ✅ Prometheus metrics endpoint működik (`/metrics`)
- ✅ Tool call latency histogram
- ✅ Tool call counter
- ✅ Backend health gauge
- ✅ Permission denied counter

---

**2.4 Circuit Breaker Pattern (40 NWT)**

**File:** `src/health/circuitBreaker.ts`

**Implementation:**
```typescript
export class CircuitBreaker {
  private name: string;
  private failureThreshold: number;
  private timeout: number;
  private halfOpenAfter: number;
  private failureCount = 0;
  private state: 'closed' | 'open' | 'half-open' = 'closed';
  private nextAttempt?: number;

  constructor(name: string, failureThreshold = 5, timeout = 60000, halfOpenAfter = 30000) {
    this.name = name;
    this.failureThreshold = failureThreshold;
    this.timeout = timeout;
    this.halfOpenAfter = halfOpenAfter;
  }

  async execute<T>(fn: () => Promise<T>): Promise<T> {
    // Check state
    if (this.state === 'open') {
      if (Date.now() < this.nextAttempt!) {
        throw new Error(`Circuit breaker open for ${this.name}`);
      }
      // Try half-open
      this.state = 'half-open';
      console.log(`[CircuitBreaker] ${this.name} half-open (trying recovery)`);
    }

    try {
      const result = await fn();

      // Success: reset
      if (this.state === 'half-open') {
        this.state = 'closed';
        this.failureCount = 0;
        console.log(`[CircuitBreaker] ${this.name} closed (recovered)`);
      } else if (this.state === 'closed') {
        this.failureCount = 0;
      }

      return result;
    } catch (error) {
      this.failureCount++;

      if (this.failureCount >= this.failureThreshold) {
        this.state = 'open';
        this.nextAttempt = Date.now() + this.halfOpenAfter;
        console.error(`[CircuitBreaker] ${this.name} OPEN (${this.failureCount} failures)`);
      }

      throw error;
    }
  }

  getState(): 'closed' | 'open' | 'half-open' {
    return this.state;
  }
}
```

**Acceptance Criteria:**
- ✅ Circuit breaker OPEN efter 5 failure
- ✅ Half-open recovery próbálkozás 30s után
- ✅ Circuit breaker CLOSED efter successful call in half-open

---

**2.5 Config Hot-Reload (30 NWT)**

**File:** `src/config/watcher.ts`

**Implementation:**
```typescript
import fs from 'fs';

export class ConfigWatcher {
  private configPath: string;
  private onChange: () => void;

  constructor(configPath: string, onChange: () => void) {
    this.configPath = configPath;
    this.onChange = onChange;
  }

  start(): void {
    fs.watch(this.configPath, (eventType) => {
      if (eventType === 'change') {
        console.log('[ConfigWatcher] config.yaml changed, reloading...');
        setTimeout(() => this.onChange(), 1000);  // Debounce 1s
      }
    });
    console.log('[ConfigWatcher] Watching config.yaml for changes');
  }
}
```

**Server Integration:**
```typescript
// src/server.ts
const configWatcher = new ConfigWatcher('./config.yaml', () => {
  const newConfig = loadConfig('./config.yaml');
  validateConfig(newConfig);

  // Reinitialize router
  router = new Router(newConfig);

  console.log('[Config] Hot-reload complete');
});

configWatcher.start();
```

**Acceptance Criteria:**
- ✅ config.yaml változás detektálása
- ✅ Router újrainicializálás (routing + permissions frissítés)
- ✅ Restart nélküli config reload

---

### Phase 2 Testing & Validation

**Test Plan:**

**1. Full Backend Integration Test:**
```bash
# Test all 4 backends
# - knowledge (HTTP)
# - ref (STDIO)
# - context7 (STDIO)
# - brave-search (STDIO)

# Expected: All backends operational
```

**2. Load Test:**
```bash
# 100 concurrent requests (ab tool)
ab -n 1000 -c 100 -p request.json http://localhost:3457/mcp

# Expected: p95 latency <100ms
```

**3. Circuit Breaker Test:**
```bash
# Stop knowledge-service
systemctl stop knowledge-service

# Make 5 tool calls → circuit breaker should OPEN
# Wait 30s → circuit breaker should go HALF-OPEN
# Start knowledge-service → circuit breaker should CLOSE

# Expected: Auto-recovery working
```

**4. Config Hot-Reload Test:**
```bash
# Edit config.yaml (add new routing)
# Expected: Router reloaded within 2s, no restart needed
```

**Phase 2 Success Criteria:**
- ✅ All 4 backends operational (HTTP + STDIO)
- ✅ p95 latency <100ms (load test 100 concurrent)
- ✅ Circuit breaker auto-recovery <30s
- ✅ Config hot-reload működik
- ✅ Prometheus metrics endpoint működik

---

### Phase 3: Rollout (1 hét, 120 NWT)

**Goal:** Minden terminál (8 darab) átállítása connector-ra

#### Rollout Schedule

| Nap | Terminálok | Validáció | NWT |
|-----|-----------|-----------|-----|
| **Day 1** | root, conductor | Coordinator terminálok | 20 |
| **Day 2** | backend | Worker terminál (leggyakrabban használt) | 20 |
| **Day 3** | frontend | Worker terminál | 20 |
| **Day 4** | architect, librarian | Support terminálok | 20 |
| **Day 5** | explorer, designer | Support terminálok | 20 |
| **Day 6** | monitor | Monitoring terminál | 10 |
| **Day 7** | Stabilizáció | 24h monitoring, rollback test | 10 |

#### Per-Terminal Rollout Protocol

**Pre-Rollout (5 perc):**

1. **Backup settings.json:**
```bash
cp terminals/<terminal>/.claude/settings.json \
   terminals/<terminal>/.claude/settings.json.backup
```

2. **Connector health check:**
```bash
curl http://localhost:3457/health
# Expected: status=healthy
```

3. **Terminal session backup:**
```bash
tmux capture-pane -t spaceos-<terminal> -p > /tmp/<terminal>-session-backup.log
```

**Rollout (10 perc):**

1. **Update settings.json:**
```json
{
  "mcpServers": {
    "spaceos-connector": {
      "type": "http",
      "url": "http://localhost:3457/mcp",
      "headers": {
        "X-Terminal": "<terminal>",
        "Authorization": "Bearer <terminal-token>"
      }
    }
  }
}
```

2. **Restart terminal session:**
```bash
tmux send-keys -t spaceos-<terminal> C-c
tmux send-keys -t spaceos-<terminal> "claude --profile <terminal>" Enter
```

3. **Smoke test (5 tool calls):**
```bash
# Terminal: <terminal>
# Tool calls:
# - search_knowledge
# - read_memory
# - list_inbox
# - register_working
# - register_idle

# Expected: All 5 tool calls successful
```

**Post-Rollout Validation (30 perc):**

1. **Audit log check:**
```bash
tail -100 /opt/spaceos/logs/mcp-connector-audit.log | grep <terminal>
# Expected: All tool calls success, no permission denied
```

2. **Latency check:**
```bash
grep <terminal> /opt/spaceos/logs/mcp-connector-audit.log | jq '.latency' | sort -n | tail -10
# Expected: p95 latency <100ms
```

3. **Terminal stability:**
```bash
tmux capture-pane -t spaceos-<terminal> -p | tail -20
# Expected: No crashes, no error loops
```

**Rollback Trigger:**
- Tool call failure rate >10%
- Latency >500ms (p95)
- Terminal session crash

**Rollback Procedure (10 perc):**
```bash
# 1. Restore settings.json
cp terminals/<terminal>/.claude/settings.json.backup \
   terminals/<terminal>/.claude/settings.json

# 2. Restart session
tmux send-keys -t spaceos-<terminal> C-c
tmux send-keys -t spaceos-<terminal> "claude --profile <terminal>" Enter

# 3. Smoke test
# Expected: All 5 tool calls successful (multi-MCP mode)
```

**Phase 3 Success Criteria:**
- ✅ 8/8 terminál átállítva connector-ra
- ✅ 0 rollback (stable átállás)
- ✅ Tool call success rate >99%
- ✅ p95 latency <100ms (production load)

---

### Phase 4: Documentation & Optimization (60 NWT)

#### Documentation

**4.1 README.md (20 NWT)**

**File:** `spaceos-nexus/mcp-connector/README.md`

**Content:**
- Architecture overview
- Installation & setup
- Configuration guide
- Running the connector
- Health monitoring
- Troubleshooting
- Adding new MCP backends

**4.2 User Guide (20 NWT)**

**File:** `docs/knowledge/patterns/MCP_CONNECTOR_GUIDE.md`

**Content:**
- Terminal setup guide
- Permission configuration
- Tool catalog
- Common issues
- Performance tuning

**4.3 Operations Runbook (20 NWT)**

**File:** `docs/knowledge/deployment/MCP_CONNECTOR_OPERATIONS.md`

**Content:**
- Start/stop procedures
- Health monitoring
- Log analysis
- Rollback procedures
- Incident response

---

## Task Breakdown Summary

| Phase | Tasks | NWT | Duration |
|-------|-------|-----|----------|
| **Phase 1: Prototype** | 6 tasks | 180 | 2-3 nap |
| **Phase 2: Production** | 5 tasks | 240 | 3-4 nap |
| **Phase 3: Rollout** | 8 terminals | 120 | 1 hét |
| **Phase 4: Documentation** | 3 docs | 60 | 1 nap |
| **TOTAL** | **22 tasks** | **600 NWT** | **~2 hét** |

---

## Dependencies

**External Dependencies:**
- ✅ Knowledge Service (localhost:3456) — already running
- ✅ NPM packages (@context7, @joshuarileydev/ref, @modelcontextprotocol/server-brave-search)
- ✅ Terminal tokens (authentication)

**Internal Dependencies:**
- ✅ Terminal structure (terminals/<name>/.claude/settings.json)
- ✅ CLAUDE.md per terminal
- ✅ Tmux sessions (spaceos-<terminal>)

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Connector crash** | High (all terminals blind) | Circuit breaker + auto-restart + rollback plan |
| **Performance degradation** | Medium (latency >100ms) | Load testing + optimization + monitoring |
| **Permission bypass** | High (security) | Runtime permission check + audit logging + testing |
| **Backend failure** | Medium (tool unavailable) | Circuit breaker + health monitoring + auto-reconnect |
| **Config corruption** | Medium (connector won't start) | Config validation + hot-reload testing |

---

## Success Metrics

### Technical Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Process count** | 9 (vs. 32 before) | `ps aux \| grep -E 'mcp\|npx' \| wc -l` |
| **Latency overhead** | <20ms | Prometheus `mcp_connector_tool_latency_seconds` |
| **Tool call success rate** | >99% | Audit log success/failure ratio |
| **Backend uptime** | >99.9% | Health check monitoring |
| **Config reload time** | <5s | ConfigWatcher logs |

### Operational Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Terminal setup time** | <30sec (vs. 5 min) | Time to add new terminal |
| **Config lines** | 8 (vs. 32) | `wc -l terminals/*/.claude/settings.json` |
| **Audit log centralization** | 1 file (vs. 4) | Log file count |
| **New MCP backend add time** | <10 min | Time to add backend to config.yaml |

### Business Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **ROI break-even** | 4 hét | Development time / weekly savings |
| **Annual savings** | ~250 hours | 5 hours/week × 50 weeks |
| **Developer satisfaction** | High | Survey: setup ease, debugging experience |

---

## Deliverables Checklist

### Phase 1
- [ ] Project setup (package.json, tsconfig.json)
- [ ] Config loader + validator
- [ ] HTTP backend manager
- [ ] Routing engine
- [ ] Express HTTP server
- [ ] config.yaml (MVP)
- [ ] README.md (basic)
- [ ] 2 terminal test (root, conductor)

### Phase 2
- [ ] STDIO backend support
- [ ] Structured audit logger
- [ ] Prometheus metrics
- [ ] Circuit breaker pattern
- [ ] Config hot-reload
- [ ] Load test (100 concurrent)
- [ ] Circuit breaker test
- [ ] Config hot-reload test

### Phase 3
- [ ] Root terminal rollout
- [ ] Conductor terminal rollout
- [ ] Backend terminal rollout
- [ ] Frontend terminal rollout
- [ ] Architect terminal rollout
- [ ] Librarian terminal rollout
- [ ] Explorer terminal rollout
- [ ] Designer terminal rollout
- [ ] Monitor terminal rollout
- [ ] 24h stability monitoring
- [ ] Rollback test

### Phase 4
- [ ] README.md (complete)
- [ ] MCP_CONNECTOR_GUIDE.md
- [ ] MCP_CONNECTOR_OPERATIONS.md

---

## Next Steps

1. **✅ ROOT APPROVAL** — ADR-061 approved (2026-07-08)
2. **⏭️ Conductor Task** — MSG-CONDUCTOR-XXX: MCP Connector Prototype (Phase 1)
3. **⏭️ Backend Delegation** — Connector implementation (TypeScript/Express)
4. **⏭️ Pilot Test** — Root + Conductor validation (2 terminals)
5. **⏭️ Rollout** — 1 terminal/nap, monitoring + rollback readiness

**Contact:** Root (Sárkány) — Conductor koordinálás
**Timeline:** Start 2026-07-08, Complete ~2026-07-22 (2 hét)
