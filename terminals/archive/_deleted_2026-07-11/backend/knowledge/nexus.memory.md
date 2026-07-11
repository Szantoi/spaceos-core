# Nexus Domain Memory

> Automatikusan betöltődik ha a feladat Knowledge Service / Agent Infra modulhoz kapcsolódik.

## Domain Scope

- **Modul:** `spaceos-nexus/knowledge-service`
- **Felelősség:** MCP server, Terminal coordination, Mailbox, Memory management
- **Tech stack:** Node.js 22, TypeScript, SQLite, Express

## Aktív Patterns

### 1. Parallel Workers (ADR-049 Phase 3)
```typescript
// Worker registry
interface WorkerState {
  id: string;           // work-001, work-002
  terminal: string;     // backend, frontend
  taskId: string;       // MSG-BACKEND-080
  status: 'running' | 'done' | 'failed' | 'queued';
  model: string;        // haiku, sonnet, opus
  startedAt: string;
  depends_on: string[];
}

// Parallel spawning
const workers = await spawnParallelWorkers({
  terminal: 'backend',
  tasks: [
    { id: 'api', prompt: 'Implement API endpoint' },
    { id: 'tests', prompt: 'Write unit tests', depends_on: ['api'] }
  ]
});
```

### 2. DAG Validation (Kahn's Algorithm)
```typescript
function validateDependencies(tasks: WorkTask[]): ValidationResult {
  // Cycle detection
  // Topological sort
  // Parallel batch generation
}

function getParallelBatches(tasks: WorkTask[]): string[][] {
  // Returns tasks grouped by execution level
  // Level 0: no dependencies (can run immediately)
  // Level 1: depends on level 0
  // etc.
}
```

### 3. Cost-Based Limits
```typescript
const MODEL_COSTS = {
  'haiku': 0.002,   // $0.12/hour
  'sonnet': 0.02,   // $1.20/hour
  'opus': 0.10      // $6.00/hour
};

const LIMITS = {
  SOFT: 3,      // $3/hour - log warning
  HARD: 5,      // $5/hour - Telegram alert
  CRITICAL: 10  // $10/hour - auto-kill + escalate
};
```

### 4. Memory Store (SQLite WAL)
```typescript
class MemoryStore {
  // Sections: chat, work, shared
  append(section: MemorySection, content: string, author: string): number;
  read(section: MemorySection, limit: number): MemoryEntry[];
  exportToMarkdown(): string;
}
```

## MCP Tools

| Tool | Purpose |
|------|---------|
| `spawn_parallel_workers` | DAG-validated parallel worker spawning |
| `spawn_raw_workers` | Best-of-N prototyping |
| `get_worker_status` | Worker status + cost monitoring |
| `create_task` | Task creation with acceptance criteria |
| `complete_task` | Task completion + memory save |
| `telegram_reply` | Telegram message response |

## Session Types

| Session | Model | Mode | Purpose |
|---------|-------|------|---------|
| `{terminal}-chat` | Haiku | Continuous | Telegram, quick responses |
| `{terminal}-work` | Sonnet | Cold | Main inbox tasks |
| `{terminal}-work-NNN` | Sonnet/Opus | Cold | Parallel workers |
| `{terminal}-raw-NNN` | Haiku | Cold | Quick prototypes |

## Legutóbbi Tanulságok

- **SQLite WAL mode** concurrent read/write support
- **tmux session naming** fontos az egyediség
- **MCP tool validation** inputSchema mindig kell
- **Heartbeat** stalled worker detection

## Kapcsolódó Fájlok

- `src/sessionStarter.ts` - Session management
- `src/mcp.ts` - MCP tool definitions
- `src/pipeline/workerRegistry.ts` - Worker tracking
- `src/pipeline/dagValidator.ts` - DAG validation
- `src/pipeline/costLimiter.ts` - Cost controls
- `src/memoryStore.ts` - SQLite memory
