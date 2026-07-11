---
id: MSG-BACKEND-080
from: root
to: backend
type: task
priority: critical
status: READ
model: sonnet
ref: ADR-049
epic_id: INFRA-DUAL-SESSION
created: 2026-06-29
processed: 2026-06-29 22:15 UTC
content_hash: 4c2cfdf2de201baf1259027ce542dd13c26d1f06c6cadd0e3ad665db9abea219
---

# ADR-049 Phase 3: Parallel Workers

## Kontextus

Phase 1 (SQLite WAL MemoryStore + Chat Session) és Phase 2 (Work Spawning) kész.
Most jön a Phase 3: Párhuzamos work session-ök támogatása.

**ADR:** `/opt/spaceos/docs/architecture/decisions/ADR-049-dual-session-chat-work-architecture.md`

## Feladatok

### 3.1 Parallel Work Session Support

**File:** `spaceos-nexus/knowledge-service/src/sessionStarter.ts`

Jelenleg: egy terminálnak egy work session (`spaceos-backend-work`)
Új: több párhuzamos work session (`spaceos-backend-work-001`, `-002`, stb.)

```typescript
interface WorkSessionConfig {
  terminal: string;
  taskId: string;
  model: 'haiku' | 'sonnet' | 'opus';
  depends_on?: string[];  // Másik work session ID-k
}

export async function startParallelWorkSession(config: WorkSessionConfig): Promise<string> {
  const workerId = generateWorkerId(config.terminal);  // work-001, work-002...
  const sessionName = `spaceos-${config.terminal}-${workerId}`;

  // Check parallel limit
  const activeWorkers = await getActiveWorkers(config.terminal);
  const maxParallel = calculateMaxParallel(config.terminal);

  if (activeWorkers.length >= maxParallel) {
    throw new Error(`Max parallel limit reached (${maxParallel})`);
  }

  // Check dependencies
  if (config.depends_on?.length) {
    const unfinished = await checkDependencies(config.depends_on);
    if (unfinished.length > 0) {
      return queueForLater(config, unfinished);
    }
  }

  // Start session
  await createTmuxSession(sessionName, getTerminalWorkdir(config.terminal));
  await registerWorker(workerId, config);

  return workerId;
}

function generateWorkerId(terminal: string): string {
  const existing = getActiveWorkerIds(terminal);
  let num = 1;
  while (existing.includes(`work-${String(num).padStart(3, '0')}`)) {
    num++;
  }
  return `work-${String(num).padStart(3, '0')}`;
}
```

**Worker registry:**

```typescript
// File: src/pipeline/workerRegistry.ts

interface WorkerState {
  id: string;
  terminal: string;
  taskId: string;
  status: 'running' | 'done' | 'failed' | 'queued';
  startedAt: string;
  depends_on: string[];
  model: string;
}

const workers: Map<string, WorkerState> = new Map();

export function registerWorker(id: string, config: WorkSessionConfig): void {
  workers.set(id, {
    id,
    terminal: config.terminal,
    taskId: config.taskId,
    status: 'running',
    startedAt: new Date().toISOString(),
    depends_on: config.depends_on || [],
    model: config.model
  });
}

export function getActiveWorkers(terminal: string): WorkerState[] {
  return Array.from(workers.values())
    .filter(w => w.terminal === terminal && w.status === 'running');
}

export function markWorkerDone(id: string): void {
  const worker = workers.get(id);
  if (worker) {
    worker.status = 'done';
    // Trigger dependent workers
    triggerDependentWorkers(id);
  }
}
```

### 3.2 DAG Validation (Kahn's Algorithm)

**File:** `spaceos-nexus/knowledge-service/src/pipeline/dagValidator.ts`

```typescript
interface WorkTask {
  id: string;
  depends_on: string[];
}

interface ValidationResult {
  valid: boolean;
  error?: string;
  executionOrder?: string[];
}

export function validateDependencies(tasks: WorkTask[]): ValidationResult {
  const graph = new Map<string, string[]>();
  const inDegree = new Map<string, number>();

  // Build graph
  for (const task of tasks) {
    graph.set(task.id, task.depends_on || []);
    if (!inDegree.has(task.id)) {
      inDegree.set(task.id, 0);
    }
    for (const dep of task.depends_on || []) {
      inDegree.set(dep, (inDegree.get(dep) || 0));
    }
  }

  // Calculate in-degrees
  for (const [taskId, deps] of graph) {
    inDegree.set(taskId, deps.length);
  }

  // Kahn's algorithm
  const queue: string[] = [];
  const executionOrder: string[] = [];

  for (const [taskId, degree] of inDegree) {
    if (degree === 0) {
      queue.push(taskId);
    }
  }

  while (queue.length > 0) {
    const current = queue.shift()!;
    executionOrder.push(current);

    // Find tasks that depend on current
    for (const [taskId, deps] of graph) {
      if (deps.includes(current)) {
        const newDegree = inDegree.get(taskId)! - 1;
        inDegree.set(taskId, newDegree);
        if (newDegree === 0) {
          queue.push(taskId);
        }
      }
    }
  }

  // Cycle detection
  if (executionOrder.length !== tasks.length) {
    const cycleNodes = tasks
      .filter(t => !executionOrder.includes(t.id))
      .map(t => t.id);
    return {
      valid: false,
      error: `Circular dependency detected: ${cycleNodes.join(' -> ')}`
    };
  }

  return { valid: true, executionOrder };
}

// Identify tasks that can run in parallel (same "level" in DAG)
export function getParallelBatches(tasks: WorkTask[]): string[][] {
  const result = validateDependencies(tasks);
  if (!result.valid) {
    throw new Error(result.error);
  }

  const levels = new Map<string, number>();

  for (const task of tasks) {
    const depLevels = (task.depends_on || [])
      .map(dep => levels.get(dep) || 0);
    const level = depLevels.length > 0 ? Math.max(...depLevels) + 1 : 0;
    levels.set(task.id, level);
  }

  // Group by level
  const batches: Map<number, string[]> = new Map();
  for (const [taskId, level] of levels) {
    if (!batches.has(level)) {
      batches.set(level, []);
    }
    batches.get(level)!.push(taskId);
  }

  return Array.from(batches.entries())
    .sort((a, b) => a[0] - b[0])
    .map(([_, tasks]) => tasks);
}
```

### 3.3 Cost-Based Dynamic Limits

**File:** `spaceos-nexus/knowledge-service/src/pipeline/costLimiter.ts`

```typescript
// Model costs per minute (approximate)
const MODEL_COSTS: Record<string, number> = {
  'haiku': 0.002,   // $0.12/hour
  'sonnet': 0.02,   // $1.20/hour
  'opus': 0.10      // $6.00/hour
};

// Budget limits
const SOFT_LIMIT_PER_HOUR = 3;   // Log warning
const HARD_LIMIT_PER_HOUR = 5;   // Telegram alert
const CRITICAL_LIMIT_PER_HOUR = 10; // Auto-kill + escalate

const HARD_MAX_PARALLEL = 5;  // Absolute maximum

interface CostState {
  terminal: string;
  currentHourlyCost: number;
  activeWorkers: { id: string; model: string; startedAt: Date }[];
}

export function calculateMaxParallel(terminal: string): number {
  const state = getCostState(terminal);

  // Calculate projected cost if we add another worker
  const avgModelCost = 0.02; // Assume Sonnet default
  const projectedHourlyCost = state.currentHourlyCost + (avgModelCost * 60);

  if (projectedHourlyCost >= CRITICAL_LIMIT_PER_HOUR) {
    return 0; // No more workers allowed
  }

  if (projectedHourlyCost >= HARD_LIMIT_PER_HOUR) {
    return Math.min(1, HARD_MAX_PARALLEL - state.activeWorkers.length);
  }

  // Dynamic calculation based on budget headroom
  const headroom = HARD_LIMIT_PER_HOUR - state.currentHourlyCost;
  const maxByBudget = Math.floor(headroom / (avgModelCost * 60));

  return Math.min(maxByBudget, HARD_MAX_PARALLEL);
}

export function getCurrentHourlyCost(terminal: string): number {
  const state = getCostState(terminal);
  let cost = 0;

  for (const worker of state.activeWorkers) {
    const minutesRunning = (Date.now() - worker.startedAt.getTime()) / 60000;
    const costPerMinute = MODEL_COSTS[worker.model] || MODEL_COSTS['sonnet'];
    cost += minutesRunning * costPerMinute;
  }

  return cost;
}

export function checkCostAlerts(terminal: string): 'ok' | 'soft' | 'hard' | 'critical' {
  const hourlyCost = getCurrentHourlyCost(terminal);

  if (hourlyCost >= CRITICAL_LIMIT_PER_HOUR) return 'critical';
  if (hourlyCost >= HARD_LIMIT_PER_HOUR) return 'hard';
  if (hourlyCost >= SOFT_LIMIT_PER_HOUR) return 'soft';
  return 'ok';
}
```

### 3.4 Raw Worker Support

**File:** `spaceos-nexus/knowledge-service/src/sessionStarter.ts` (extend)

Raw worker = gyors prototípus készítés, minimális context betöltéssel.

```typescript
interface RawWorkerConfig {
  terminal: string;
  task: string;
  model: 'haiku' | 'sonnet';
  count: number;  // How many parallel raw workers
  selectBest: boolean;  // Chat selects best result
}

export async function spawnRawWorkers(config: RawWorkerConfig): Promise<string[]> {
  const workerIds: string[] = [];

  for (let i = 0; i < config.count; i++) {
    const workerId = `raw-${String(i + 1).padStart(3, '0')}`;
    const sessionName = `spaceos-${config.terminal}-${workerId}`;

    // Minimal context - just the task
    const prompt = `
Te egy raw worker vagy. Feladatod:

${config.task}

Csak a kódot/megoldást add vissza, minimális magyarázattal.
Ha kész vagy, írd: [RAW-DONE]
`;

    await createTmuxSession(sessionName, getTerminalWorkdir(config.terminal));
    await injectPrompt(sessionName, `claude --model ${config.model}`, prompt);

    workerIds.push(workerId);
  }

  return workerIds;
}

export async function collectRawResults(terminal: string, workerIds: string[]): Promise<RawResult[]> {
  const results: RawResult[] = [];

  for (const workerId of workerIds) {
    const sessionName = `spaceos-${terminal}-${workerId}`;
    const output = await captureTmuxPane(sessionName);

    // Check if done
    if (output.includes('[RAW-DONE]')) {
      results.push({
        workerId,
        output: extractCodeFromOutput(output),
        status: 'done'
      });
      await killTmuxSession(sessionName);
    }
  }

  return results;
}
```

### 3.5 Best-of-N Selection

**File:** `spaceos-nexus/knowledge-service/src/pipeline/bestOfN.ts`

```typescript
interface RawResult {
  workerId: string;
  output: string;
  status: 'done' | 'running' | 'failed';
}

interface SelectionResult {
  bestWorkerId: string;
  reason: string;
  allResults: RawResult[];
}

// Chat session calls this to select best result
export async function selectBestResult(
  terminal: string,
  results: RawResult[],
  criteria: string
): Promise<SelectionResult> {
  // Format results for chat evaluation
  const prompt = `
${results.length} raw worker eredményt kaptam. Válaszd ki a legjobbat.

Kritérium: ${criteria}

${results.map((r, i) => `
### Worker ${i + 1} (${r.workerId})
\`\`\`
${r.output.slice(0, 2000)}
\`\`\`
`).join('\n')}

Válaszolj JSON formátumban:
{
  "bestWorkerId": "raw-001",
  "reason": "Miért ez a legjobb"
}
`;

  const chatSession = `spaceos-${terminal}-chat`;
  const response = await injectAndWaitForResponse(chatSession, prompt);

  const selection = JSON.parse(extractJson(response));

  return {
    bestWorkerId: selection.bestWorkerId,
    reason: selection.reason,
    allResults: results
  };
}
```

### 3.6 MCP Tool Extensions

**File:** `spaceos-nexus/knowledge-service/src/mcp.ts` (extend)

```typescript
// Add new tools for parallel workers

{
  name: 'spawn_parallel_workers',
  description: 'Spawn multiple parallel work sessions for independent tasks',
  inputSchema: {
    type: 'object',
    properties: {
      terminal: { type: 'string' },
      tasks: {
        type: 'array',
        items: {
          type: 'object',
          properties: {
            id: { type: 'string' },
            prompt: { type: 'string' },
            model: { type: 'string', enum: ['haiku', 'sonnet', 'opus'] },
            depends_on: { type: 'array', items: { type: 'string' } }
          },
          required: ['id', 'prompt']
        }
      }
    },
    required: ['terminal', 'tasks']
  },
  handler: async (params) => {
    // Validate DAG
    const validation = validateDependencies(params.tasks);
    if (!validation.valid) {
      return { error: validation.error };
    }

    // Get parallel batches
    const batches = getParallelBatches(params.tasks);

    // Start first batch
    const firstBatch = batches[0];
    const workerIds = await Promise.all(
      firstBatch.map(taskId => {
        const task = params.tasks.find(t => t.id === taskId)!;
        return startParallelWorkSession({
          terminal: params.terminal,
          taskId: task.id,
          model: task.model || 'sonnet',
          depends_on: task.depends_on
        });
      })
    );

    return {
      started: workerIds,
      queued: batches.slice(1).flat(),
      totalBatches: batches.length
    };
  }
},

{
  name: 'spawn_raw_workers',
  description: 'Spawn multiple raw workers for quick prototyping, returns best result',
  inputSchema: {
    type: 'object',
    properties: {
      terminal: { type: 'string' },
      task: { type: 'string' },
      count: { type: 'number', minimum: 2, maximum: 5 },
      model: { type: 'string', enum: ['haiku', 'sonnet'] },
      criteria: { type: 'string', description: 'Selection criteria for best result' }
    },
    required: ['terminal', 'task', 'count', 'criteria']
  },
  handler: async (params) => {
    const workerIds = await spawnRawWorkers({
      terminal: params.terminal,
      task: params.task,
      model: params.model || 'haiku',
      count: params.count,
      selectBest: true
    });

    // Wait for all to complete (with timeout)
    const results = await waitForRawWorkers(params.terminal, workerIds, 5 * 60 * 1000);

    // Select best
    const selection = await selectBestResult(params.terminal, results, params.criteria);

    return selection;
  }
},

{
  name: 'get_worker_status',
  description: 'Get status of all workers for a terminal',
  inputSchema: {
    type: 'object',
    properties: {
      terminal: { type: 'string' }
    },
    required: ['terminal']
  },
  handler: async (params) => {
    const workers = getActiveWorkers(params.terminal);
    const cost = getCurrentHourlyCost(params.terminal);
    const maxParallel = calculateMaxParallel(params.terminal);

    return {
      workers,
      currentHourlyCost: cost,
      maxParallel,
      costAlert: checkCostAlerts(params.terminal)
    };
  }
}
```

## Acceptance Criteria

- [ ] Parallel work session support (`work-001`, `work-002`, stb.)
- [ ] Worker registry (status tracking)
- [ ] DAG validation (cycle detection)
- [ ] `getParallelBatches()` works correctly
- [ ] Cost-based dynamic limits
- [ ] Cost alerts (soft/hard/critical)
- [ ] Raw worker support
- [ ] Best-of-N selection
- [ ] `spawn_parallel_workers` MCP tool
- [ ] `spawn_raw_workers` MCP tool
- [ ] `get_worker_status` MCP tool
- [ ] Unit tests for DAG validator
- [ ] Unit tests for cost limiter
- [ ] Integration test: 3 parallel workers complete independently
- [ ] Dashboard shows multiple workers per terminal

## Tesztelés

```bash
# Unit tests
npm test -- dagValidator
npm test -- costLimiter
npm test -- workerRegistry

# Integration test - parallel workers
curl -X POST localhost:3456/api/mcp/spawn_parallel_workers \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "backend",
    "tasks": [
      {"id": "task-1", "prompt": "Create file A"},
      {"id": "task-2", "prompt": "Create file B"},
      {"id": "task-3", "prompt": "Merge A and B", "depends_on": ["task-1", "task-2"]}
    ]
  }'

# Raw workers test
curl -X POST localhost:3456/api/mcp/spawn_raw_workers \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "backend",
    "task": "Implementálj egy simple cache osztályt TypeScript-ben",
    "count": 3,
    "model": "haiku",
    "criteria": "Legolvashatóbb, legjobban dokumentált kód"
  }'
```

## Fájlok

### Új fájlok (5 db)
1. `src/pipeline/workerRegistry.ts`
2. `src/pipeline/dagValidator.ts`
3. `src/pipeline/costLimiter.ts`
4. `src/pipeline/bestOfN.ts`
5. `src/__tests__/dagValidator.test.ts`

### Módosítandó fájlok (3 db)
1. `src/sessionStarter.ts` — parallel + raw worker support
2. `src/mcp.ts` — 3 új MCP tool
3. `src/server.ts` — cost alert integration

## Határidő

Phase 3 target: 3-4 nap

## Prioritás

**CRITICAL** — Ez teszi lehetővé a párhuzamos fejlesztést és a költségoptimalizálást.
