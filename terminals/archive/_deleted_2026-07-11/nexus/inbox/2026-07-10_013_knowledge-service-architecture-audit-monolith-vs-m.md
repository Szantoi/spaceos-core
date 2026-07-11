---
id: MSG-NEXUS-013
from: root
to: nexus
type: task
priority: high
status: UNREAD
model: opus
created: 2026-07-10
content_hash: 69aef8cc8106888b4fa47ea1faa1ad94304e27e80a0c9cb058bd5cc1304a7b04
---

# Knowledge-Service Architecture Audit — Monolith vs Microservices Analysis

## Kontextus

A knowledge-service jelenleg **99 MCP tool-t** és több pipeline-t szolgál ki egyetlen Node.js processzből. Vizsgáld meg, hogy:

1. Érdemes-e darabolni felelősség szerint?
2. Van-e teljesítményvesztés a sok tool miatt?
3. Mi a költség/haszon a microservices vs monolith között?

## Jelenlegi Architektúra

```
knowledge-service (port 3456) — SINGLE PROCESS
├── MCP Tools (99 db)
│   ├── Mailbox tools (15)
│   ├── Identity tools (8)
│   ├── Task tools (12)
│   ├── Memory tools (10)
│   ├── Context persistence (12)
│   ├── Pipeline tools (8)
│   ├── Codegen tools (6)
│   ├── Graph tools (5)
│   ├── Telegram tools (4)
│   ├── Project tools (6)
│   ├── Subscription tools (5)
│   └── Misc tools (8)
├── HTTP API Routes
│   ├── /api/terminal/*
│   ├── /api/mailbox/*
│   ├── /api/graph/*
│   ├── /api/session/*
│   └── /api/planning/*
├── Pipeline Watchers (background)
│   ├── watchInbox.ts
│   ├── watchDone.ts
│   ├── watchStuck.ts
│   ├── watchMonitor.ts
│   ├── watchQueue.ts
│   └── nightwatch.ts
├── Data Stores
│   ├── SQLite (memories, sessions)
│   └── ChromaDB (vector search)
└── External Integrations
    ├── Telegram Bot API
    ├── Claude API (via claude CLI)
    └── tmux session management
```

## Vizsgálandó Kérdések

### 1. Teljesítmény Audit

Mérj és dokumentálj:

```bash
# Memory usage
ps aux | grep knowledge-service

# CPU usage under load
top -p $(pgrep -f knowledge-service)

# Response time per tool category
curl -w "%{time_total}" localhost:3456/api/health
```

**Kérdések:**
- [ ] Átlagos response time tool kategóriánként?
- [ ] Memory footprint idle vs loaded?
- [ ] CPU spike nagy tool híváskor?
- [ ] Event loop blocking van-e?

### 2. Darabolási Opciók Elemzése

**Opció A: Marad Monolith (jelenlegi)**
```
Előnyök:
+ Egyszerű deployment
+ Nincs network overhead
+ Shared state (SQLite, cache)
+ Egyszerű debugging

Hátrányok:
- Single point of failure
- Nem skálázható horizontálisan
- Nagy memory footprint
- Egy bug az egészet leállíthatja
```

**Opció B: Domain-Based Split (3 service)**
```
core-service (3456):
├── Mailbox, Identity, Terminal
├── ~35 tools
└── SQLite master

pipeline-service (3457):
├── Watchers, Nightwatch, Cron
├── ~15 tools
└── SQLite replica (read-only)

integration-service (3458):
├── Telegram, Codegen, Graph
├── ~20 tools
└── ChromaDB, external APIs
```

**Opció C: Functional Split (2 service)**
```
api-service (3456):
├── Minden MCP tool (99)
├── HTTP routes
└── Stateless

worker-service (3457):
├── Pipeline watchers
├── Background jobs
└── Stateful (SQLite, cron)
```

### 3. Benchmark Terv

Készíts benchmark-ot:

```typescript
// benchmark.ts
import { performance } from 'perf_hooks';

const TOOLS_TO_TEST = [
  'list_inbox',
  'create_task',
  'get_terminal_status_aggregate',
  'search_knowledge',
  'scaffold_component',
];

async function benchmark() {
  for (const tool of TOOLS_TO_TEST) {
    const start = performance.now();
    // Call tool 100 times
    for (let i = 0; i < 100; i++) {
      await callMcpTool(tool, {});
    }
    const duration = performance.now() - start;
    console.log(`${tool}: ${duration/100}ms avg`);
  }
}
```

### 4. Döntési Mátrix

| Kritérium | Monolith | 2-Split | 3-Split |
|-----------|----------|---------|---------|
| Deployment complexity | ✅ Low | ⚠️ Medium | ❌ High |
| Network latency | ✅ None | ⚠️ Some | ❌ More |
| Fault isolation | ❌ None | ⚠️ Partial | ✅ Good |
| Horizontal scaling | ❌ Hard | ⚠️ Partial | ✅ Easy |
| Memory efficiency | ⚠️ High | ✅ Better | ✅ Best |
| Development speed | ✅ Fast | ⚠️ Medium | ❌ Slow |
| Debugging | ✅ Easy | ⚠️ Medium | ❌ Hard |

## Deliverables

1. **Performance Report** (`docs/architecture/KNOWLEDGE_SERVICE_PERFORMANCE_AUDIT.md`)
   - Memory usage baseline
   - Response time by tool category
   - Bottleneck identification

2. **Architecture Decision Record** (`docs/architecture/decisions/ADR-062-knowledge-service-architecture.md`)
   - Current state analysis
   - Options comparison
   - Recommendation with rationale

3. **Benchmark Results** (`spaceos-nexus/knowledge-service/__benchmarks__/`)
   - Raw benchmark data
   - Visualization (chart)

## Acceptance Criteria

- [ ] Performance baseline documented
- [ ] 3 architecture options analyzed
- [ ] Benchmark results (100+ calls per tool)
- [ ] ADR with clear recommendation
- [ ] Cost/benefit analysis
- [ ] Migration plan (if split recommended)

## Acceptance Criteria

- [ ] Performance baseline documented
- [ ] 3 architecture options analyzed
- [ ] Benchmark results (100+ calls per tool)
- [ ] ADR with clear recommendation
- [ ] Cost/benefit analysis
- [ ] Migration plan (if split recommended)
