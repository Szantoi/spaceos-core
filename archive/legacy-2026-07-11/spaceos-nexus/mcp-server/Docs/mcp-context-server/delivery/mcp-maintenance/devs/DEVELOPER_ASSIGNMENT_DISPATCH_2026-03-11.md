---
title: "🚀 Developer Task Dispatch — M02 (2026-03-11)"
type: "tech-lead-dispatch"
created: 2026-03-11
status: "🟢 READY FOR EXECUTION"
mission: "All developers start coding TODAY — Zero waiting"
---

# 🚀 Developer Task Dispatch — M02 Full Execution

**Release Date:** 2026-03-11
**Status:** ✅ **ALL DEVELOPERS UNBLOCKED — GO**
**Target Completion:** 2026-03-24 (M02 Close)
**Team Size:** 5 developers (Dev A, B, C, D, E)

---

## 📊 EXECUTIVE SUMMARY

| Developer | Epic | Track | Tasks | Hours | Start Condition | Status |
|:----------|:----:|:-----:|:-----:|:-----:|:-----------------|:------:|
| **Dev A** | 14 | Transport | 3 | 36h | No blocker → **START IMMEDIATELY** | 🟢 GO |
| **Dev B** | 14 | Transport | 2 | 43h | When TASK-14-01 complete (Dev A) | 🟢 GO |
| **Dev C** | 14 | Plugins | 4 | 54h | No blocker → **START IMMEDIATELY** | 🟢 GO |
| **Dev D** | 12 | Memory | 4 | 40h | No blocker → **START NOW (TODAY)** | **🚨 GO** |
| **Dev E** | 13 | Discovery | 7 | 100h | No blocker → **START IMMEDIATELY** | 🟢 GO |
| **TOTAL** | — | — | **20** | **273h** | Dependency-driven | **5 devs active** |

**Critical Path:** Dev D (EPIC-12) starts TODAY → Dev A (EPIC-14-01) starts 3-18 → Dev B/C parallel

---

## 🚨 DEV D — START NOW (No Blocker)

### **EPIC-12: Episodic Memory Layer (Episode Storage + Search)**

**Your Mission:** Build the memory backbone — store, search, and retrieve discovery episodes

**Location:** `Docs/mcp-context-server/delivery/mcp-maintenance/devs/dev-d/`

**Start Condition:** ✅ **No dependencies** → **Kick off TODAY**

**Effort:** 40 hours total (sequential, no blockers)

---

### 📋 Task Sequence (4 Tasks, Sequential Blocking)

#### **TASK-12-01: Episode Schema & Storage (8h) — START NOW (No Blocker)**

**Location:** `dev-d/TASK-12-01/`

**Acceptance Criteria (4 total):**
1. `episodes` table created (SQLite) with columns: id, session_id, timestamp, content, metadata, embedding_vector_null
2. Two indexes: `(session_id, timestamp)` for retrieval, `content` for FTS5 prep
3. Size validation: reject episodes > 5 MB
4. Unit tests: ≥10 tests cover schema, validation, insertion, performance

**Deliverables:**
- ✅ [`src/episodic/EpisodeStore.ts`](../../../src/episodic/EpisodeStore.ts) (main service)
- ✅ `src/episodic/types.ts` (Episode, EpisodeQuery interfaces)
- ✅ Database migration: `migrations/003_episodes_table.sql`
- ✅ `tests/episodic/EpisodeStore.test.ts` (10+ tests)
- ✅ Performance baseline: store 1000 episodes in < 500ms

**Files Affected:**
```
src/episodic/
├─ EpisodeStore.ts              [NEW]
├─ types.ts                     [NEW]
tests/episodic/
├─ EpisodeStore.test.ts         [NEW]
database/
├─ migrations/003_episodes_table.sql [NEW]
```

**Implementation Guide:**
1. Study `src/metadata/WorkflowStateTracker.ts` — your pattern for SQLite service
2. Create EpisodeStore class with methods: `store(episode)`, `validateSize()`, `getRecent(sessionId, limit)`
3. Add migration SQL file with both indexes
4. Write unit tests: happy path, size validation, query performance
5. Commit with message: `feat(episodic): Episode storage schema + SQLite integration`

**Success Metric:** All tests green, episodes table correctly indexed, performance < 500ms for 1000 inserts

---

#### **TASK-12-02: FTS5 Full-Text Search (8-9h) — START when TASK-12-01 Complete**

**Location:** `dev-d/TASK-12-02/`

**Blocker:** TASK-12-01 (Episode Schema)

**Accepts Criteria (5 total):**
1. FTS5 virtual table created (`episodes_fts`) on `episodes.content`
2. `searchEpisodes(query)` returns top 10 results in < 50ms
3. Synonym expansion (chromadb + embedding fallback)
4. Blacklist: exclude stop words (the, a, an)
5. Performance: < 50ms for 1000-episode corpus

**Deliverables:**
- ✅ [`src/episodic/FtsSearch.ts`](../../../src/episodic/FtsSearch.ts)
- ✅ Migration: `004_episodes_fts_table.sql`
- ✅ `tests/episodic/FtsSearch.test.ts` (15+ tests)
- ✅ SOTA documentation: `docs/FTS5-SEARCH-SOTA.md` (why FTS5, alternatives, tuning)

**Key Insight:** FTS5 is a SQLite extension for full-text search. Use it for keyword matching; later (TASK-12-03) we add semantic search via ChromaDB.

**Implementation Guide:**
1. Create FTS5 virtual table via migration
2. Implement `searchEpisodes(query)` with MATCH operator
3. Add stop-word filtering in client code (before query)
4. Write 15+ tests: single word, multi-word, edge cases, performance
5. Document why FTS5 (vs alternatives): fast, embedded, no extra dependencies

---

#### **TASK-12-03: ChromaDB Semantic Search (10-11h) — START when TASK-12-02 Complete**

**Location:** `dev-d/TASK-12-03/`

**Blocker:** TASK-12-02 (FTS5 Search)

**Acceptance Criteria (6 total):**
1. ChromaDB integration: connect to embeddings collection (episodes)
2. `similaritySearch(query, topK=10)` returns < 100ms
3. Similarity threshold: configurable (default 0.7) to tune precision/recall
4. Hybrid search: FTS5 + semantic, merge results with scoring
5. Config: threshold (0.7), embedding model (default: OpenAI), tuning guide
6. Tests: 20+ covering all modes, threshold tuning, performance

**Deliverables:**
- ✅ [`src/episodic/ChromaSearch.ts`](../../../src/episodic/ChromaSearch.ts)
- ✅ `src/episodic/HybridSearch.ts` (combines FTS5 + ChromaDB)
- ✅ `src/episodic/config.ts` (threshold, model settings)
- ✅ `tests/episodic/ChromaSearch.test.ts` (20+ tests)
- ✅ Tuning guide: `docs/CHROMADB-THRESHOLD-TUNING.md`

**Key Insight:** Semantic search (embeddings) catches meaning similarity; FTS5 catches keyword matches. Together they're powerful.

---

#### **TASK-12-04: E2E Integration + QA Rubric (18-20h) — START when TASK-12-03 Complete**

**Location:** `dev-d/TASK-12-04/`

**Blocker:** TASK-12-03 (ChromaDB Semantic)

**Acceptance Criteria (4 total):**
1. End-to-end: store episode → FTS5 search + semantic search → retrieve works
2. Caching layer: frequently searched queries cached (no re-embed)
3. Quality rubric: define "good episode" (length, relevance, metadata)
4. Reliability: tests for failures (ChromaDB down, embedding fails), fallback behavior

**Deliverables:**
- ✅ E2E test suite: `tests/episodic/E2E-Integration.test.ts` (50+ tests)
- ✅ Caching service: `src/episodic/SearchCache.ts`
- ✅ Quality rubric: `docs/EPISODE-QUALITY-RUBRIC.md`
- ✅ Reliability tests: failure modes + fallback behavior

**Success Metric:** All 4 AC passed, E2E tests green, < 50ms search latency maintained under load

---

### 📝 Daily Standup & Completion

**Template Location:** `coordinator/feedback/STANDUP-TEMPLATE.md`

**Your Standup Process:**

Each day (9:00 UTC, 12:00 UTC, 18:00 UTC), create:
```
coordinator/feedback/dev-d/DEV-D-[DATE]-STANDUP-MORNING.md
coordinator/feedback/dev-d/DEV-D-[DATE]-STANDUP-MIDDAY.md
coordinator/feedback/dev-d/DEV-D-[DATE]-STANDUP-EVENING.md
```

**Format:**
```markdown
# Dev D Standup — [DATE]

## Status
- [ ] TASK-12-0X progress: X% (Step Y of Z)
- Blocker: [None | Describe]
- Confidence: [Green | Yellow | Red]

## Completed Today
- [Item A]
- [Item B]

## Next Session
- [Plan for next 3-4h]
```

**On Completion:** Create `DEV-D-[DATE]-COMPLETION.md` with final report link to GitHub PR

---

---

## 🚀 DEV A — EPIC-14 TRANSPORT LEAD (No Blocker → Start Immediately)

### **EPIC-14 Phase 1: Modern MCP Transports**

**Your Role:** Transport abstraction architect + critical path lead

**Location:** `Docs/mcp-context-server/delivery/mcp-maintenance/devs/dev-a/`

**Start Condition:** ✅ **No dependencies** → **Start immediately** (no blocker)

**Effort:** 36 hours total (TASK-14-01 → 14-02 → 14-05)

---

### 📋 Task Sequence (3 Tasks, Sequential Critical Path)

#### **TASK-14-01: Transport Abstraction & Factory (12h) — START Now (No Blocker)**

**Location:** `dev-a/TASK-14-01/`

**Acceptance Criteria (15 total):**
1. `ITransport` interface defined (initialize, shutdown, isHealthy, getTransportInfo, sendMessage)
2. `TransportFactory` reads `MCP_TRANSPORT` env var (stdio | http)
3. Factory creates correct transport instance with validation checks
4. HTTP transport: listen on port (env: MCP_HTTP_PORT=3000)
5. Stdio transport: JSON-RPC 2.0 RFC 8174-compliant
6. Graceful shutdown: ≤200ms request draining before close
7. Health check: `getTransportInfo()` returns { type, uptime, requestCount, avgLatency }
8. Error handling: transport errors → standardized ErrorResponse (from EPIC-11)
9. Logging: structured logs (transport init, shutdown, errors)
10. Monitoring: tracks request count, latency percentiles (50th, 95th, 99th)
11. Connection pooling: HTTP transport supports max 100 concurrent connections
12. Message validation: reject malformed JSON-RPC 2.0 messages
13. Mode detection: warn if stdio used in terminal (not piped)
14. Tests: 20+ unit tests covering factory, interfaces, error paths
15. Documentation: 1000+ chars explaining design, rationale, future extensibility

**Deliverables:**
- ✅ `src/mcp/transport/ITransport.ts` (interface definition)
- ✅ `src/mcp/transport/TransportFactory.ts` (factory)
- ✅ `src/mcp/transport/HttpTransport.ts` (existing PoC → production)
- ✅ `src/mcp/transport/StdioTransport.ts` (extended)
- ✅ `src/mcp/transport/types.ts` (TransportConfig, TransportInfo)
- ✅ `tests/mcp/transport/TransportFactory.test.ts` (20+ tests)
- ✅ `docs/TRANSPORT-ARCHITECTURE.md` (design document)

**Files Affected:**
```
src/mcp/transport/
├─ index.ts                     [EDIT — add exports]
├─ ITransport.ts               [NEW]
├─ TransportFactory.ts         [NEW]
├─ types.ts                    [NEW]
├─ HttpTransport.ts            [EDIT — from PoC]
├─ StdioTransport.ts           [EDIT — extend]
tests/mcp/transport/
├─ TransportFactory.test.ts    [NEW]
docs/
├─ TRANSPORT-ARCHITECTURE.md   [NEW]
```

**Implementation Guide:**

**Step 1: Design ITransport interface**
```typescript
interface ITransport {
  initialize(): Promise<void>;
  shutdown(): Promise<void>;
  isHealthy(): Promise<boolean>;
  getTransportInfo(): TransportInfo;
  sendMessage(msg: JsonRpcMessage): Promise<void>;
  onMessage(handler: (msg: JsonRpcMessage) => void): void;
}
```

**Step 2: Create TransportFactory**
```typescript
export class TransportFactory {
  static create(config: TransportConfig): ITransport {
    const type = process.env.MCP_TRANSPORT || 'stdio';
    if (type === 'http') return new HttpTransport(config);
    return new StdioTransport(config);
  }
}
```

**Step 3: Implement HttpTransport** (extend existing PoC)
- Express server on port 3000
- CORS enabled
- Health check: `GET /health` → { status: "ok", uptime: ... }
- Message handling: `POST /messages` accepts JSON-RPC 2.0
- Connection limits: max 100 concurrent

**Step 4: Implement StdioTransport** (extend existing)
- JSON-RPC 2.0 parsing from stdin
- Graceful SIGTERM/SIGINT handling
- Terminal detection warning

**Step 5: Write 20+ unit tests**
- Factory: creates correct transport by env var
- HttpTransport: server starts/stops, health check works
- StdioTransport: message parsing, error handling
- Error paths: invalid config, port in use, etc.

**Step 6: Document architecture**
- Why two transports? (embedded vs remote)
- How to add a new transport? (implement ITransport)
- Performance expectations (latency, throughput)

---

#### **TASK-14-02: HTTP Transport Production (12h) — START when TASK-14-01 Complete**

**Blocker:** TASK-14-01 (Transport Abstraction)

**Acceptance Criteria (18 total):**
[See `dev-a/TASK-14-02/` for full assignment]

Key highlights:
- Express server + CORS + rate limiting
- Graceful shutdown (200ms drain)
- Connection pooling (max 100)
- Health check < 100ms response
- 80%+ test coverage

---

#### **TASK-14-05: Stdio Transport Production (12h) — START when TASK-14-02 Complete**

**Blocker:** TASK-14-02 (HTTP Transport)

**Acceptance Criteria (15 total):**
[See `dev-a/TASK-14-05/` for full assignment]

Key highlights:
- JSON-RPC 2.0 RFC 8174 compliance
- Terminal mode detection + warning
- Graceful shutdown (SIGTERM, SIGINT)
- Zero regression: EPIC-11 agents work unchanged
- 80%+ test coverage

---

---

## 🚀 DEV B — HTTP TRANSPORT SPECIALIST (Starts when TASK-14-01 Complete)

### **EPIC-14 Phase 1: HTTP Transport Implementation**

**Your Role:** HTTP transport production lead

**Location:** `Docs/mcp-context-server/delivery/mcp-maintenance/devs/dev-b/`

**Start Condition:** When TASK-14-01 complete (Dev A) → HTTP transport unblocked

**Effort:** 43 hours total (TASK-14-02 primary + TASK-14-05 support)

---

### 📋 Task Sequence

#### **TASK-14-02: HTTP Transport Implementation (21h)**

**Start:** When TASK-14-01 complete (Dev A blockage lifted)

**Acceptance Criteria (18 total):**

1. Express server listening on port 3000 (configurable via MCP_HTTP_PORT)
2. CORS headers: Access-Control-Allow-Origin: * (configurable)
3. Rate limiting: 100 req/min per IP
4. JSON-RPC 2.0 message validation + error responses
5. Graceful shutdown: < 200ms request drain time before exit
6. Health check endpoint: `GET /health` → { status, uptime, requestCount }
7. Request/response logging: structured (timestamp, method, latency, status)
8. Connection pooling: max 100 concurrent connections
9. Timeout handling: 30s request timeout, graceful error response
10. Error responses: use EPIC-11 ErrorResponse format
11. Performance: < 100ms response time for health check, < 500ms for tool invocation
12. Load test: handle 50 concurrent requests without dropping
13. Memory management: no memory leaks under sustained load
14. TLS support (optional, configurable): HTTPS if cert + key provided
15. Environment variables: MCP_HTTP_PORT, MCP_HTTP_CORS_ORIGIN, MCP_HTTP_SSL_CERT, MCP_HTTP_SSL_KEY
16. Documentation: deployment guide (local + Docker)
17. E2E tests: 25+ tests covering all AC
18. Integration: works with Tool plugin system (TASK-14-03)

**Deliverables:**
- ✅ `src/mcp/transport/HttpTransport.ts` (production impl)
- ✅ `src/mcp/transport/HttpServer.ts` (Express setup)
- ✅ `src/mcp/middleware/HttpRateLimiter.ts` (rate limiting)
- ✅ `src/mcp/middleware/HttpLogger.ts` (structured logging)
- ✅ `tests/mcp/transport/HttpTransport.e2e.test.ts` (25+ tests)
- ✅ `docs/HTTP-TRANSPORT-DEPLOYMENT.md` (setup guide)
- ✅ Load test results: `load-test-results-http-transport.json`

**Files Affected:**
```
src/mcp/transport/
├─ HttpTransport.ts                [CREATE/EDIT]
├─ HttpServer.ts                   [NEW]
src/mcp/middleware/
├─ HttpRateLimiter.ts             [NEW]
├─ HttpLogger.ts                  [NEW]
tests/mcp/transport/
├─ HttpTransport.e2e.test.ts      [NEW]
docs/
├─ HTTP-TRANSPORT-DEPLOYMENT.md   [NEW]
```

**Implementation Guide:**

1. **Express Server Setup**
   ```typescript
   const app = express();
   app.use(express.json());
   app.use(rateLimit({ windowMs: 60000, max: 100 }));
   app.post('/messages', handler);
   app.get('/health', healthCheck);
   ```

2. **Rate Limiting Middleware** (100 req/min per IP)

3. **Graceful Shutdown** (200ms drain)

4. **Health Check Endpoint** (< 100ms response)

5. **Load Testing** (50 concurrent requests)

---

#### **TASK-14-05: Stdio Transport Support (22h)**

**Start:** When TASK-14-02 complete (HTTP Transport ready)

**You support:** Ensure Stdio transport integrates with HTTP transport layer

**Acceptance Criteria:** (See `dev-b/TASK-14-05/`)

---

---

## 🚀 DEV C — PLUGIN SYSTEM LEAD (No Blocker → Start Immediately)

### **EPIC-14 Phase 1: Tool Plugin Architecture**

**Your Role:** Plugin system architect, tool module lead

**Location:** `Docs/mcp-context-server/delivery/mcp-maintenance/devs/dev-c/`

**Start Condition:** ✅ **No dependencies** → **Start immediately** (independent of Dev A TASK-14-01)

**Effort:** 54 hours total (TASK-14-03 primary, 14-04/05/06)

---

### 📋 Task Sequence

#### **TASK-14-03: Plugin System Architecture (26h)**

**Start:** Immediately (no blocker, parallel with Dev A TASK-14-01)

**Acceptance Criteria (24 total):**

1. Plugin interface: `IToolPlugin` (name, version, tools[], initialize, shutdown)
2. Plugin registry: centralized lookup (tool name → plugin instance)
3. Plugin loader: discovers .ts files in `src/mcp/tools/*.ts`
4. Tool export: each plugin exports `getTool()` function
5. Bootstrap plugin: moves `bootstrap_agent` tool to plugin
6. Context plugin: moves `get_context` tool to plugin
7. Discovery plugin: moves `get_discovery_state` tool to plugin
8. Error handling: missing plugin → warning + fallback
9. Versioning: plugins have version, compatibility check
10. Dependency resolution: plugins can depend on other plugins
11. Hot reload (optional): plugin reload without server restart
12. Documentation: add plugin tutorial (create new tool plugin in 10 min)
13. Resource templates: plugin can define resource URIs (e.g., resource://task/{id})
14. Tool schemas: consistent zod validation per plugin
15. Initialization order: ensure tool dependencies resolved in order
16. Shutdown cleanup: plugins clean up on server shutdown
17. Logging: structured logs per plugin (init, shutdown, errors)
18. Performance: plugin load time < 100ms per plugin
19. Tests: 25+ unit tests covering registry, loader, lifecycle
20. E2E: all plugins load and work correctly
21. Sampling support: plugins can declare sampling support (LLM-assisted args)
22. Notification system: plugins can emit notifications (debounced)
23. Configuration: plugins read from environment/config
24. Monitoring: tracks plugin health, error rates per plugin

**Deliverables:**
- ✅ `src/mcp/plugins/IToolPlugin.ts` (interface)
- ✅ `src/mcp/plugins/PluginRegistry.ts` (registry + loader)
- ✅ `src/mcp/tools/bootstrap.ts` → plugin (move from monolith)
- ✅ `src/mcp/tools/context.ts` → plugin (move from monolith)
- ✅ `src/mcp/tools/discovery.ts` → plugin (move from monolith)
- ✅ `tests/mcp/plugins/PluginRegistry.test.ts` (25+ tests)
- ✅ `docs/PLUGIN-SYSTEM-TUTORIAL.md` (create new tool plugin)
- ✅ `docs/RESOURCE-TEMPLATES.md` (resource URI patterns)

**Files Affected:**
```
src/mcp/plugins/
├─ index.ts                      [NEW — export registry]
├─ IToolPlugin.ts               [NEW]
├─ PluginRegistry.ts            [NEW]
├─ PluginLoader.ts              [NEW]
src/mcp/tools/                  [REFACTOR from monolith]
├─ bootstrap.ts                 [NEW — move bootstrap_agent]
├─ context.ts                   [NEW — move get_context]
├─ discovery.ts                 [NEW — move get_discovery_state]
tests/mcp/plugins/
├─ PluginRegistry.test.ts       [NEW]
├─ PluginLoader.test.ts         [NEW]
docs/
├─ PLUGIN-SYSTEM-TUTORIAL.md    [NEW]
├─ RESOURCE-TEMPLATES.md        [NEW]
src/index.ts                    [EDIT — use plugin registry]
```

**Implementation Guide:**

**Step 1: Define IToolPlugin interface**
```typescript
interface IToolPlugin {
  name: string;
  version: string;
  tools: Tool[];
  initialize(): Promise<void>;
  shutdown(): Promise<void>;
  dependencies?: string[];
}

function getTool(): IToolPlugin { ... }
```

**Step 2: Create PluginRegistry**
```typescript
class PluginRegistry {
  register(plugin: IToolPlugin): void;
  getPlugin(name: string): IToolPlugin;
  getAllTools(): Tool[];
  initialize(): Promise<void>;
  shutdown(): Promise<void>;
}
```

**Step 3: Create PluginLoader**
```typescript
class PluginLoader {
  loadFrom(dir: string): IToolPlugin[];
  resolveOrder(plugins: IToolPlugin[]): IToolPlugin[];
}
```

**Step 4: Refactor existing tools into plugins**
- Move `bootstrap_agent` → `src/mcp/tools/bootstrap.ts`
- Move `get_context` → `src/mcp/tools/context.ts`
- Move `get_discovery_state` → `src/mcp/tools/discovery.ts`

**Step 5: Update server initialization**
```typescript
const registry = new PluginRegistry();
const loader = new PluginLoader();
const plugins = loader.loadFrom('src/mcp/tools');
for (const plugin of plugins) {
  registry.register(plugin);
}
await registry.initialize();
```

---

#### **TASK-14-04: Bootstrap + Context Plugins (6h)**

**Start:** When TASK-14-03 complete + Transport abstraction stable (TASK-14-01)

**Move plugins:**
- `bootstrap_agent` tool → plugin
- `get_context` tool → plugin
- Include resource templates for each

---

#### **TASK-14-05: Discovery Plugin (10h)**

**Start:** When TASK-14-03 complete + Transport abstraction stable

**Move + enhance:**
- `get_discovery_state` → plugin
- Add resource templates: `resource://discovery/{phase}`

---

#### **TASK-14-06: Advanced Patterns (Optional, 12h)**

**Start:** When TASK-14-05 complete & if Phase 1 confidence high (optional for Phase 2)

**Sampling delegation + notification debouncing**

---

---

## 🚀 DEV E — DISCOVERY TRACK LEAD (No Blocker → Start Immediately)

### **EPIC-13: Discovery Track Tools — Full Implementation**

**Your Mission:** Build first-class discovery track support (DWI workflow, tools, RBAC)

**Location:** `Docs/mcp-context-server/delivery/mcp-maintenance/devs/dev-e/`

**Start Condition:** ✅ **No dependencies** → **Start immediately**

**Effort:** 100 hours total (7 sequential tasks)

---

### 📋 Task Sequence (7 Tasks, Sequential Blocking)

#### **TASK-13-01: Discovery Roles & DWI Workflow (17h) — START Immediately (No Blocker)**

**Acceptance Criteria:**
1. Roles created: `architect` (discovery lead), `researcher` (discovery implementer)
2. DWI workflow template: phases = [ideation, validation, iteration, delivery_handoff]
3. FSM schema: discovery_workflow state machine (FSM_ADR § 4)
4. SQL schema: workflows table schema supports discovery + delivery
5-17. [See full assignment]

**Key Deliverables:**
- `database/roles/discovery/architect/` (YAML role def)
- `database/roles/discovery/researcher/` (YAML role def)
- `database/workflows/dwiworkflow.json` (template)
- `src/metadata/DiscoveryFSM.ts` (FSM implementation)

---

#### **TASK-13-02: RBAC Discovery Filter (14h) — START when TASK-13-01 Complete**

**Blocker:** TASK-13-01 (Discovery Roles)

Track-aware RBAC: filter tools by `session.track` field

---

#### **TASK-13-03: Discovery Tools (16h) — START when TASK-13-02 Complete**

**Blocker:** TASK-13-02 (RBAC Filter)

Core tools:
- `request_discovery_context()`
- `reference_prior_discovery()`
- `submit_discovery_proposal()`

---

#### **TASK-13-04 through 13-07:**

[Sequential blocking chain — each starts when prior complete]

---

---

## 🎯 SUCCESS CRITERIA (ALL DEVELOPERS)

### ✅ Acceptance Criteria Met
- [x] All AC passed per task
- [x] Tests green: ≥80% coverage per epic
- [x] Code review: Architect + Tech Lead approved
- [x] Documentation: Design rationale + deployment guide

### ✅ Definition of Done
- [x] Code committed to feature branch
- [x] PR created with clear description
- [x] All tests passing in CI/CD
- [x] Security review passed (if applicable)
- [x] Performance baseline met (latency, memory, throughput)
- [x] Standup reports filed daily
- [x] Merge approved by Architect + Tech Lead

---

## � EXECUTION MODEL: Dependency-Driven (NOT Calendar-Driven)

**Key Principle:** Each task starts **when its blocker is done**, not by calendar date.

### Start Triggers (Dependency-Based)

| Developer | Task | Blocker | Start When |
|:----------|:----:|:-------:|:-----------|
| **Dev D** | TASK-12-01 | None | **🚨 NOW (no blocker)** |
| Dev D | TASK-12-02 | 12-01 | When 12-01 ✅ |
| Dev D | TASK-12-03 | 12-02 | When 12-02 ✅ |
| Dev D | TASK-12-04 | 12-03 | When 12-03 ✅ |
| — | — | — | — |
| **Dev A** | TASK-14-01 | None | **NOW (no blocker)** |
| **Dev B** | TASK-14-02 | 14-01 | When 14-01 ✅ |
| Dev A | TASK-14-02 | 14-01 | When 14-01 ✅ |
| **Dev C** | TASK-14-03 | None | **NOW (no blocker, parallel)** |
| Dev C | TASK-14-04 | 14-03 | When 14-03 ✅ |
| **Dev E** | TASK-13-01 | None | **NOW (no blocker)** |
| Dev E | TASK-13-02 | 13-01 | When 13-01 ✅ |
| Dev E | TASK-13-03 | 13-02 | When 13-02 ✅ |

---

## 📅 ESTIMATED MILESTONES (For Reference Only)

**These are targets based on effort estimates, not hard deadlines.**

| Milestone | Owners | Target (est.) | Trigger |
|:----------|:--------|:------:|:---------:
| **Dev D starts** | EPIC-12 ready | Dev D | 🚨 **Starting NOW** |
| **Dev A/C/E start** | EPICs 14/13 ready | A, C, E | ~2-3 days after D starts (est.) |
| **Dev B starts** | After TASK-14-01 done | B | ~2-3 days after A starts |
| **Dev A TASK-14-02 starts** | When 14-01 done | A | ~3-4 days |
| **M02 Phase 1 END** | All P0 tasks done | All | ~10-14 days (depends on velocity) |
| **EPIC-13 END** | Dev E completes all 7 tasks | E | ~3-4 weeks |

---

## 🛠️ DAILY OPERATIONS

### Standup Process (All Devs)
- **09:00 UTC:** Post morning standup to `coordinator/feedback/dev-[a/b/c/d/e]/`
- **12:00 UTC:** Post midday update
- **18:00 UTC:** Post evening update + next session plan
- **On Completion:** File completion report with GitHub PR link

### Blockers & Escalation
- 🟡 **Minor blocker (< 1h wait):** Continue next task, note in standup
- 🔴 **Major blocker (> 1h wait):** Escalate to Tech Lead immediately (Slack + email)
- ✅ **Design question:** Post in #architecture, tag Architect

### Code Review & Merge
- PR review: Architect review **required**, Tech Lead review **required**
- Merge criteria: All tests green + perf baseline met
- Merge owner: Tech Lead (final approval)

---

## 🚀 GO FORWARD

**Status:** ✅ All developers unblocked, all tasks ready

**Your Next Step:**
1. Read your epic goal.md + state.md (linked above)
2. Read your first task assignment
3. Create your standup template
4. **START CODING**

**Questions?** Tag Tech Lead in #architecture or email directly.

---

**Happy coding! 🚀**

