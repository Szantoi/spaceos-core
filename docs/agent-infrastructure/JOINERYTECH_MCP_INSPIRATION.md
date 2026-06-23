# JoineryTech.McpServer Inspiration — Task Audit & Formal Review Implementation

> **Dátum:** 2026-06-23
> **Repo:** https://github.com/Szantoi/JoineryTech.McpServer
> **Cél:** Implementálható patterns Task Audit & Formal Review rendszerhez

---

## 1. Repository Áttekintés

**JoineryTech.McpServer** — Model Context Protocol (MCP) server implementation
- **Stack:** Express, TypeScript, SQLite (better-sqlite3), ChromaDB (RAG)
- **Architektúra:** RBAC filter, Guardrail service, FSM workflow tracker, Audit logger
- **Purpose:** AI agent role definitions, tool permissions, workflow state, compliance evaluation

**Releváns modulok Task Audit-hoz:**

```
src/
  mcp/
    RbacFilter.ts              ← Role-based access control + LRU cache
    SessionManager.ts          ← UUID v4 session tracking + SQLite
    AgentDb.ts                 ← SQLite role schemas, permissions
  metadata/
    auditLogger.ts             ← SHA-256 audit logging + cost tracking
    WorkflowStateTracker.ts    ← FSM state machine + transition history
    ResourceTracker.ts         ← Artifact registry
```

---

## 2. Audit Logger Pattern (auditLogger.ts)

### Implementáció

```typescript
import { DatabaseConnectionManager } from './DatabaseConnectionManager';
import { createHash } from 'crypto';

export class AuditLogger {
    private connectionManager: DatabaseConnectionManager;

    constructor(connectionManager: DatabaseConnectionManager) {
        this.connectionManager = connectionManager;
    }

    /**
     * Log a tool call execution with optional cost tracking.
     * Non-blocking, asynchronous logging for performance.
     */
    public async log(params: {
        session_id: string;
        domain: string;
        role: string;
        tool_name: string;
        input: any;
        output: any;
        latency_ms: number;
        status_code: string;
        ai_model?: string | null;
        ai_tokens_used?: number | null;
        cost_amount_usd?: number | null;
    }): Promise<void> {
        // Process in background to avoid blocking
        setImmediate(() => {
            try {
                const db = this.connectionManager.getAdminPool();
                const stmt = db.prepare(`
                    INSERT INTO audit_log (
                        session_id, domain, role, tool_name, input_hash, output_hash,
                        latency_ms, status_code, ai_model, ai_tokens_used, cost_amount_usd
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                `);

                const inputHash = this.hash(params.input);
                const outputHash = this.hash(params.output);

                stmt.run(
                    params.session_id, params.domain, params.role, params.tool_name,
                    inputHash, outputHash, params.latency_ms, params.status_code,
                    params.ai_model ?? null, params.ai_tokens_used ?? null,
                    params.cost_amount_usd ?? null
                );
            } catch (error) {
                console.error('[AuditLogger] ❌ Failed to write to audit_log:', error);
            }
        });
    }

    /**
     * Helper to create a stable hash of tool input/output.
     */
    private hash(data: any): string {
        if (!data) return '';
        try {
            const str = typeof data === 'string' ? data : JSON.stringify(data);
            return createHash('sha256').update(str).digest('hex');
        } catch {
            return 'hash_error';
        }
    }
}
```

### Adaptálható SpaceOS Task Audit-hoz

```typescript
// spaceos-nexus/knowledge-service/src/taskAudit.ts

export async function logTaskCreation(params: {
    task_id: string;
    created_by: string;
    created_by_token_hash: string;
    assigned_to: string;
    task_type: string;
    review_type: string;
    priority: string;
    project?: string;
    epic?: string;
    inbox_path: string;
    inbox_hash: string;  // SHA-256
    metadata?: any;
}): Promise<void> {
    setImmediate(async () => {
        try {
            // Append to JSONL log (immutable)
            const logEntry = {
                timestamp: new Date().toISOString(),
                ...params
            };

            const line = JSON.stringify(logEntry) + '\n';
            await fs.appendFile('/opt/spaceos/logs/tasks/creation.jsonl', line, 'utf-8');

            // Optional: SQLite index for fast queries
            if (USE_SQLITE_INDEX) {
                db.prepare(`
                    INSERT INTO task_creation_index (
                        task_id, timestamp, created_by, assigned_to, task_type,
                        review_type, priority, project, epic, inbox_hash
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                `).run(
                    params.task_id, logEntry.timestamp, params.created_by,
                    params.assigned_to, params.task_type, params.review_type,
                    params.priority, params.project, params.epic, params.inbox_hash
                );
            }
        } catch (error) {
            console.error('[TaskAudit] ❌ Failed to log task creation:', error);
        }
    });
}
```

**Újra használható pattern:**
- ✅ `setImmediate()` — non-blocking async logging
- ✅ SHA-256 hash for input/output integrity
- ✅ SQLite INSERT for structured audit log
- ✅ Error handling without blocking main flow
- ✅ Cost tracking (ai_model, ai_tokens_used, cost_amount_usd)

---

## 3. RBAC Filter Pattern (RbacFilter.ts)

### Implementáció

```typescript
import NodeCache from 'node-cache';
import { AgentDb } from './AgentDb';

export class RbacFilter {
    private cache: NodeCache;
    private lastDbVersion: number = -1;

    // Tools accessible without any role (unauthenticated)
    private readonly publicTools: Set<string> = new Set([
        'list_domains', 'list_roles', 'get_policy', 'search_knowledge'
    ]);

    constructor(private readonly agentDb: AgentDb) {
        this.cache = new NodeCache({
            stdTTL: 1800,       // 30 minutes
            checkperiod: 600,
            useClones: false,
            maxKeys: 50
        });
    }

    /**
     * Returns the set of tool names allowed for a given role.
     * Lazy-loads from AgentDb and caches the result.
     */
    getAllowedTools(role: string): Set<string> {
        // 1. Cache Invalidation Check
        this.checkVersionAndInvalidate();

        // 2. Check Cache
        const cached = this.cache.get<Set<string>>(role);
        if (cached) {
            return new Set([...this.publicTools, ...cached]);
        }

        // 3. Query AgentDb
        try {
            const schema = this.agentDb.findSchemaByRoleName(role);
            if (!schema || !schema.mcp_tool_permissions) {
                this.cache.set(role, new Set());
                return this.publicTools;
            }

            const permissions = JSON.parse(schema.mcp_tool_permissions);
            if (Array.isArray(permissions)) {
                const toolSet = new Set<string>(permissions);
                this.cache.set(role, toolSet);
                return new Set([...this.publicTools, ...toolSet]);
            }
        } catch (dbErr) {
            console.error(`[RbacFilter] Database error for role "${role}":`, dbErr);
        }

        // Fallback: Cache negative result to avoid DB spam
        this.cache.set(role, new Set());
        return this.publicTools;
    }

    /**
     * Checks whether a specific tool is allowed for a given role.
     */
    hasPermission(toolName: string, role: string): boolean {
        return this.getAllowedTools(role).has(toolName);
    }

    /**
     * Checks if the DB schema version has changed and flushes cache if so.
     */
    private checkVersionAndInvalidate(): void {
        try {
            const version = this.agentDb.getSchemaVersion('read-layer');
            if (this.lastDbVersion !== -1 && version !== this.lastDbVersion) {
                console.info(`[RbacFilter] Schema version bump detected. Invalidating cache.`);
                this.invalidateCache();
            }
            this.lastDbVersion = version;
        } catch (err) {
            console.warn(`[RbacFilter] Schema version check failed:`, err);
        }
    }

    invalidateCache(role?: string): void {
        if (role) {
            this.cache.del(role);
        } else {
            this.cache.flushAll();
        }
    }
}
```

### Adaptálható SpaceOS Token Auth-hoz

```typescript
// spaceos-nexus/knowledge-service/src/auth.ts

import NodeCache from 'node-cache';
import { createHash } from 'crypto';

interface TokenPermissions {
    holder: string;
    scopes: string[];  // ['task:create:*', 'session:start', ...]
    created: string;
}

export class TokenAuth {
    private cache: NodeCache;
    private tokens: Map<string, TokenPermissions>;  // hash → permissions

    constructor() {
        this.cache = new NodeCache({
            stdTTL: 1800,     // 30 min cache
            checkperiod: 600,
            useClones: false,
            maxKeys: 100
        });

        this.loadTokensFromConfig();
    }

    /**
     * Verify token and check if it has required scope.
     */
    hasPermission(token: string, requiredScope: string): boolean {
        const tokenHash = createHash('sha256').update(token).digest('hex');

        // 1. Check cache
        const cached = this.cache.get<TokenPermissions>(tokenHash);
        if (cached) {
            return this.checkScope(cached.scopes, requiredScope);
        }

        // 2. Query from config/database
        const permissions = this.tokens.get(tokenHash);
        if (!permissions) {
            return false;  // Unknown token
        }

        // 3. Cache result
        this.cache.set(tokenHash, permissions);

        return this.checkScope(permissions.scopes, requiredScope);
    }

    /**
     * Check if token scopes match required scope.
     * Supports wildcards: 'task:create:*' matches 'task:create:backend'
     */
    private checkScope(scopes: string[], required: string): boolean {
        for (const scope of scopes) {
            if (scope === required) return true;
            if (scope.endsWith(':*')) {
                const prefix = scope.slice(0, -2);  // Remove ':*'
                if (required.startsWith(prefix)) return true;
            }
        }
        return false;
    }

    private loadTokensFromConfig(): void {
        // Load from config/tokens.yaml
        // Token hash stored in config, not raw token
        this.tokens = new Map([
            ['sha256:abc123...', { holder: 'root', scopes: ['task:create:*', 'session:*'], created: '2026-06-23' }],
            ['sha256:def456...', { holder: 'conductor', scopes: ['task:create:worker', 'session:start'], created: '2026-06-23' }]
        ]);
    }
}
```

**Újra használható pattern:**
- ✅ Node-cache LRU caching (30 min TTL)
- ✅ Lazy-load from database/config
- ✅ Public/default permissions (unauthenticated access)
- ✅ Schema version check → cache invalidation
- ✅ Negative result caching (avoid DB spam)
- ✅ Role-based permissions from JSON array

---

## 4. Session Manager Pattern (SessionManager.ts)

### Implementáció

```typescript
import { randomUUID } from 'crypto';

export interface SessionRecord {
    id: string;           // UUID v4
    role: string;
    domain: string;
    agent_id: string | null;
    status: string;       // FSM state: started, active, completed, blocked
    created_at: string;
    last_updated_at: string | null;
}

export class SessionManager {
    private db: any;

    constructor(dbPath: string) {
        this.db = new Database(dbPath);
        this.init();
    }

    private init(): void {
        this.db.exec(`
            CREATE TABLE IF NOT EXISTS sessions (
                id              TEXT PRIMARY KEY COLLATE NOCASE,
                agent_id        TEXT NOT NULL UNIQUE COLLATE NOCASE,
                domain          TEXT NOT NULL,
                role            TEXT NOT NULL,
                started_at      TEXT NOT NULL,
                last_updated_at TEXT,
                fsm_state       TEXT NOT NULL DEFAULT 'started',
                outcome         TEXT,
                UNIQUE(agent_id, started_at)
            );
        `);
    }

    /**
     * Register a new agent session with UUID v4 session_id.
     *
     * UUID v4 Guarantee:
     * - Uses crypto.randomUUID() (cryptographically strong)
     * - NOT Math.random() (predictable, weak)
     * - Collision risk: ZERO in practical scenarios (2^122 possible UUIDs)
     */
    register(role: string, domain: string, agentId?: string): SessionRecord {
        const sessionId = randomUUID();  // UUID v4
        const startedAt = new Date().toISOString();
        const actualAgentId = agentId ?? `${role}-${randomUUID()}`;

        this.db.prepare(
            `INSERT INTO sessions (id, agent_id, domain, role, started_at, fsm_state)
             VALUES (?, ?, ?, ?, ?, ?)`
        ).run(sessionId, actualAgentId, domain, role, startedAt, 'started');

        return this.get(sessionId)!;
    }

    get(sessionId: string): SessionRecord | undefined {
        const row = this.db.prepare('SELECT * FROM sessions WHERE id = ?').get(sessionId);
        if (!row) return undefined;

        return {
            id: row.id,
            role: row.role,
            domain: row.domain,
            agent_id: row.agent_id,
            status: row.fsm_state,
            created_at: row.started_at,
            last_updated_at: row.last_updated_at
        };
    }

    complete(sessionId: string): SessionRecord {
        this.db.prepare(
            `UPDATE sessions SET fsm_state = 'completed', last_updated_at = ? WHERE id = ?`
        ).run(new Date().toISOString(), sessionId);

        return this.get(sessionId)!;
    }
}
```

### Adaptálható SpaceOS Task Creation-hoz

**Nem szükséges** — A SpaceOS task creation NEM igényel session tracking, mivel:
- Task creation egy atomi művelet (POST /api/task/create)
- Nincs long-running session
- Audit log elegendő (JSONL + optional SQLite index)

**DE:** Ha később Task Execution Tracking kellene (pl. "task started" → "in progress" → "done"), akkor:

```typescript
// Hypothetical: Task Execution Session Tracker
export class TaskExecutionTracker {
    register(taskId: string, assignedTo: string): string {
        const executionId = randomUUID();
        db.prepare(`
            INSERT INTO task_executions (id, task_id, terminal, started_at, status)
            VALUES (?, ?, ?, ?, 'started')
        `).run(executionId, taskId, assignedTo, new Date().toISOString());
        return executionId;
    }

    complete(executionId: string): void {
        db.prepare(`
            UPDATE task_executions SET status = 'completed', completed_at = ? WHERE id = ?
        `).run(new Date().toISOString(), executionId);
    }
}
```

**Újra használható pattern:**
- ✅ UUID v4 for unique IDs (crypto.randomUUID)
- ✅ SQLite sessions table
- ✅ FSM state tracking (started, active, completed, blocked)
- ✅ Timestamp tracking (created_at, updated_at)

---

## 5. Workflow State Tracker Pattern (WorkflowStateTracker.ts)

### Implementáció (csak részlet)

```typescript
export interface SessionState {
    session_id: string;
    domain: string;
    role_name: string;
    workflow_id: string;
    current_state: FSMState;  // enum: 'started', 'researching', 'planning', ...
    track: 'discovery' | 'delivery' | null;
    last_action: string | null;
    created_at: string;
    updated_at: string;
}

export class WorkflowStateTracker {
    private db: any;

    constructor(dbPath: string) {
        this.db = new Database(dbPath);
        this.init();
    }

    /**
     * Create a new agent session with initial FSM state.
     */
    createSession(params: CreateSessionParams): SessionState {
        const sessionId = randomUUID();
        const timestamp = new Date().toISOString();

        this.db.prepare(`
            INSERT INTO agent_sessions (
                session_id, domain, role_name, workflow_id, current_state,
                track, created_at, updated_at
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        `).run(
            sessionId, params.domain, params.role_name, params.workflow_id,
            params.initial_state, params.track ?? null, timestamp, timestamp
        );

        return this.getSession(sessionId)!;
    }

    /**
     * Transition to a new state with validation.
     */
    updateState(sessionId: string, params: UpdateStateParams): SessionState {
        const session = this.getSession(sessionId);
        if (!session) throw new SessionNotFoundError(sessionId);

        // Validate transition
        const validTransitions = VALID_TRANSITIONS[session.current_state] || [];
        if (!validTransitions.includes(params.new_state)) {
            throw new FSMTransitionError(
                session.current_state, params.new_state,
                `Invalid transition from ${session.current_state} to ${params.new_state}`
            );
        }

        // Update state
        this.db.prepare(`
            UPDATE agent_sessions
            SET current_state = ?, last_action = ?, updated_at = ?
            WHERE session_id = ?
        `).run(params.new_state, params.action, new Date().toISOString(), sessionId);

        // Log transition history
        this.logTransition(sessionId, session.current_state, params.new_state, params.action, params.metadata);

        return this.getSession(sessionId)!;
    }

    private logTransition(
        sessionId: string, stateBefore: string, stateAfter: string,
        action: string, metadata?: any
    ): void {
        this.db.prepare(`
            INSERT INTO session_history (session_id, state_before, state_after, action, metadata, timestamp)
            VALUES (?, ?, ?, ?, ?, ?)
        `).run(
            sessionId, stateBefore, stateAfter, action,
            metadata ? JSON.stringify(metadata) : null,
            new Date().toISOString()
        );
    }
}
```

### Adaptálható SpaceOS Review FSM-hez (FUTURE)

**Jelenleg:** Review FSM egyszerű (DONE → reviewer.ts → APPROVE/REJECT/ESCALATE)

**Ha később komplexebb FSM kell** (pl. multi-stage review, revision cycles):

```typescript
// Future: Review FSM State Tracker
export enum ReviewState {
    PENDING = 'pending',
    REVIEWING_A = 'reviewing_a',
    REVIEWING_B = 'reviewing_b',
    REVISION_REQUESTED = 'revision_requested',
    APPROVED = 'approved',
    REJECTED = 'rejected',
    ESCALATED = 'escalated'
}

const VALID_TRANSITIONS: Record<ReviewState, ReviewState[]> = {
    [ReviewState.PENDING]: [ReviewState.REVIEWING_A],
    [ReviewState.REVIEWING_A]: [ReviewState.REVIEWING_B, ReviewState.REVISION_REQUESTED],
    [ReviewState.REVIEWING_B]: [ReviewState.APPROVED, ReviewState.REJECTED, ReviewState.ESCALATED],
    [ReviewState.REVISION_REQUESTED]: [ReviewState.PENDING],
    [ReviewState.APPROVED]: [],  // terminal
    [ReviewState.REJECTED]: [],  // terminal
    [ReviewState.ESCALATED]: []  // terminal
};

export class ReviewStateTracker {
    transition(reviewId: string, newState: ReviewState, action: string): void {
        const current = this.getState(reviewId);
        const validTransitions = VALID_TRANSITIONS[current.state];

        if (!validTransitions.includes(newState)) {
            throw new Error(`Invalid review transition: ${current.state} → ${newState}`);
        }

        // Update state + log history
        db.prepare(`UPDATE reviews SET state = ?, updated_at = ? WHERE id = ?`)
          .run(newState, new Date().toISOString(), reviewId);

        db.prepare(`INSERT INTO review_history (review_id, state_before, state_after, action, timestamp) VALUES (?, ?, ?, ?, ?)`)
          .run(reviewId, current.state, newState, action, new Date().toISOString());
    }
}
```

**Újra használható pattern:**
- ✅ FSM state validation (VALID_TRANSITIONS map)
- ✅ State transition history table (audit trail)
- ✅ Terminal states (no further transitions)
- ✅ Metadata logging (JSON)

---

## 6. Összefoglaló: Mit vegyünk át?

### Phase 1: Formal Review (IMMEDIATE)

**Nem igényel JoineryTech pattern** — egyszerű bash script:
```bash
scripts/formal-review.sh $DONE_FILE $TERMINAL
```

**DE:** Ha később LLM-based formal review kell → AuditLogger pattern használható.

---

### Phase 2: Task Creation Audit (PRIORITY)

| Pattern | JoineryTech Source | SpaceOS Adaptation |
|---|---|---|
| **Audit Log** | `auditLogger.ts` | `taskCreation.ts` + JSONL log |
| **SHA-256 Hash** | `auditLogger.hash()` | `hashUtils.ts` (✅ megvan) |
| **Non-blocking Log** | `setImmediate()` | Same pattern |
| **Cost Tracking** | `ai_tokens_used`, `cost_amount_usd` | Optional: formal review LLM cost |
| **Token Auth** | `RbacFilter.ts` | `auth.ts` + token scopes |
| **LRU Cache** | `NodeCache (30 min TTL)` | Same (token permissions cache) |
| **Public Tools** | `publicTools: Set<string>` | Public endpoints (no auth) |
| **Schema Version** | `checkVersionAndInvalidate()` | Optional: token config version |

**Implementálható komponensek:**

```typescript
// 1. Task Creation Audit Logger
export async function logTaskCreation(params: TaskCreationParams): Promise<void> {
    setImmediate(async () => {
        const logEntry = { timestamp: new Date().toISOString(), ...params };
        await fs.appendFile('/opt/spaceos/logs/tasks/creation.jsonl', JSON.stringify(logEntry) + '\n');
    });
}

// 2. Token Auth with LRU Cache
export class TokenAuth {
    private cache: NodeCache;
    hasPermission(token: string, requiredScope: string): boolean {
        const tokenHash = createHash('sha256').update(token).digest('hex');
        const cached = this.cache.get<TokenPermissions>(tokenHash);
        // ...
    }
}

// 3. API Endpoint
app.post('/api/task/create', tokenAuthMiddleware, async (req, res) => {
    // Verify token scope: 'task:create:*' or 'task:create:worker'
    if (!tokenAuth.hasPermission(req.token, 'task:create:*')) {
        return res.status(403).json({ error: 'Insufficient permissions' });
    }

    // Create inbox file
    // Compute SHA-256 hash
    // Log to creation.jsonl
    // Return task_id
});
```

---

### Phase 3: Daily Report (LATER)

**Nem igényel JoineryTech pattern** — egyszerű JSONL query + Markdown generation.

**DE:** Ha later SQLite index kell gyors query-hez:

```sql
CREATE TABLE task_creation_index (
    task_id TEXT PRIMARY KEY,
    timestamp TEXT NOT NULL,
    created_by TEXT NOT NULL,
    assigned_to TEXT NOT NULL,
    task_type TEXT NOT NULL,
    review_type TEXT NOT NULL,
    priority TEXT NOT NULL,
    project TEXT,
    epic TEXT,
    inbox_hash TEXT NOT NULL,
    INDEX idx_timestamp (timestamp),
    INDEX idx_created_by (created_by),
    INDEX idx_project (project)
);
```

Query:
```typescript
const todayTasks = db.prepare(`
    SELECT task_type, COUNT(*) as count
    FROM task_creation_index
    WHERE DATE(timestamp) = ?
    GROUP BY task_type
`).all('2026-06-23');
```

---

## 7. Implementációs Döntés

### Javasolt Approach: Hybrid (Phase 1a + Phase 2a)

**Week 1 (3-4 óra):**

1. **Task Creation API** (2 óra) — `taskCreation.ts` + endpoint
   - Token auth (basic, hardcoded tokens)
   - JSONL audit log
   - SHA-256 hash
   - Git auto-commit

2. **Formal Review Script** (1 óra) — `scripts/formal-review.sh`
   - Build check
   - Git commit format
   - Frontmatter validation

3. **Review Routing** (30 min) — `reviewer.ts` modification
   - `review_type` extraction
   - Route to formal/content/manual

4. **Token Auth Cache** (30 min) — `auth.ts` with NodeCache
   - LRU cache (30 min TTL)
   - Scope checking (`task:create:*`, `task:create:worker`)

**Week 2 (later):**

5. **Daily Report** — `scripts/daily-report.sh` + Datahaven widget
6. **SQLite Index** — Optional query optimization
7. **Advanced FSM** — Multi-stage review state machine (if needed)

---

## 8. Kód Példák — Ready to Implement

### A) taskCreation.ts (Token Auth + Audit Log)

```typescript
import { createHash } from 'crypto';
import { promises as fs } from 'fs';
import NodeCache from 'node-cache';

interface TokenPermissions {
    holder: string;
    scopes: string[];
    created: string;
}

export class TaskCreationService {
    private cache: NodeCache;
    private tokens: Map<string, TokenPermissions>;

    constructor() {
        this.cache = new NodeCache({ stdTTL: 1800, maxKeys: 100 });
        this.loadTokens();
    }

    async createTask(token: string, params: {
        assigned_to: string;
        task_type: string;
        review_type: string;
        priority: string;
        title: string;
        content: string;
        project?: string;
        epic?: string;
    }): Promise<{ task_id: string; inbox_path: string }> {
        // 1. Verify token
        if (!this.hasPermission(token, 'task:create:*')) {
            throw new Error('Unauthorized: insufficient scope');
        }

        // 2. Generate task_id
        const taskId = `MSG-${params.assigned_to.toUpperCase()}-${await this.getNextNum(params.assigned_to)}`;

        // 3. Create inbox file
        const date = new Date().toISOString().split('T')[0];
        const inboxPath = `/opt/spaceos/terminals/${params.assigned_to}/inbox/${date}_${taskId.split('-')[2]}_${this.slugify(params.title)}.md`;

        const frontmatter = `---
id: ${taskId}
from: ${this.getTokenHolder(token)}
to: ${params.assigned_to}
type: task
priority: ${params.priority}
status: UNREAD
model: sonnet
task_type: ${params.task_type}
review_type: ${params.review_type}
project: ${params.project || ''}
epic: ${params.epic || ''}
created: ${date}
---

${params.content}
`;

        await fs.writeFile(inboxPath, frontmatter);

        // 4. Compute SHA-256 hash
        const inboxHash = await this.sha256File(inboxPath);

        // 5. Log to creation.jsonl
        await this.logCreation({
            timestamp: new Date().toISOString(),
            task_id: taskId,
            created_by: this.getTokenHolder(token),
            created_by_token_hash: createHash('sha256').update(token).digest('hex'),
            assigned_to: params.assigned_to,
            task_type: params.task_type,
            review_type: params.review_type,
            priority: params.priority,
            project: params.project,
            epic: params.epic,
            inbox_path: inboxPath,
            inbox_hash: inboxHash
        });

        // 6. Git commit
        await this.gitCommit(inboxPath, taskId, this.getTokenHolder(token));

        return { task_id: taskId, inbox_path: inboxPath };
    }

    private hasPermission(token: string, requiredScope: string): boolean {
        const tokenHash = createHash('sha256').update(token).digest('hex');
        const cached = this.cache.get<TokenPermissions>(tokenHash);
        const permissions = cached || this.tokens.get(tokenHash);

        if (!permissions) return false;

        if (!cached) this.cache.set(tokenHash, permissions);

        return permissions.scopes.some(scope =>
            scope === requiredScope ||
            (scope.endsWith(':*') && requiredScope.startsWith(scope.slice(0, -2)))
        );
    }

    private async logCreation(entry: any): Promise<void> {
        setImmediate(async () => {
            try {
                const line = JSON.stringify(entry) + '\n';
                await fs.appendFile('/opt/spaceos/logs/tasks/creation.jsonl', line, 'utf-8');
            } catch (error) {
                console.error('[TaskCreation] ❌ Failed to log:', error);
            }
        });
    }

    private loadTokens(): void {
        // Load from config/tokens.yaml
        this.tokens = new Map([
            ['sha256:abc...', { holder: 'root', scopes: ['task:create:*', 'session:*'], created: '2026-06-23' }],
            ['sha256:def...', { holder: 'conductor', scopes: ['task:create:worker'], created: '2026-06-23' }]
        ]);
    }
}
```

### B) auth.ts (Token Middleware)

```typescript
import { Request, Response, NextFunction } from 'express';
import { TaskCreationService } from './taskCreation';

export function tokenAuthMiddleware(req: Request, res: Response, next: NextFunction): void {
    const authHeader = req.headers.authorization;
    const token = authHeader?.replace('Bearer ', '');

    if (!token) {
        res.status(401).json({ error: 'Missing authorization token' });
        return;
    }

    // Attach token to request object
    (req as any).token = token;
    next();
}
```

---

## 9. Test Coverage & Patterns

### Test Statistics

**JoineryTech.McpServer test suite:**
- **95 test files** across unit, integration, E2E layers
- **Test frameworks:** Vitest (unit), Playwright (E2E), Supertest (API)
- **Coverage tool:** @vitest/coverage-v8

**Test structure:**
```
src/tests/
  unit/          ← Unit tests (isolated, mocked dependencies)
  integration/   ← Integration tests (real DB, no HTTP)
  e2e/           ← E2E tests (full HTTP stack, Playwright)
  fixtures/      ← Test data fixtures
  helpers/       ← Test utilities
  evaluator/     ← LLM compliance evaluator tests
```

### Unit Test Pattern Example (RbacFilter.test.ts)

```typescript
import { describe, it, expect, vi, beforeEach } from 'vitest';
import NodeCache from 'node-cache';
import { RbacFilter } from '../../mcp/RbacFilter';
import { AgentDb } from '../../mcp/AgentDb';

// Mock AgentDb
vi.mock('../../mcp/AgentDb', () => {
    return {
        AgentDb: vi.fn().mockImplementation(() => {
            return {
                findSchemaByRoleName: vi.fn(),
                getSchemaVersion: vi.fn().mockReturnValue(1)
            };
        })
    };
});

describe('RbacFilter', () => {
    let mockAgentDb: any;
    let rbacFilter: RbacFilter;

    beforeEach(() => {
        vi.clearAllMocks();
        mockAgentDb = new AgentDb({} as any);
        rbacFilter = new RbacFilter(mockAgentDb);
    });

    it('should return public tools for an unknown role', () => {
        mockAgentDb.findSchemaByRoleName.mockReturnValue(null);
        const tools = rbacFilter.getAllowedTools('unknown_role');

        expect(tools.has('list_domains')).toBe(true);
        expect(tools.has('search_knowledge')).toBe(true);
        expect(tools.size).toBe(10); // Only public tools
    });

    it('should query AgentDb on cache miss and cache the result', () => {
        const permissions = JSON.stringify(['tool1', 'tool2']);
        mockAgentDb.findSchemaByRoleName.mockReturnValue({
            role_name: 'dev',
            mcp_tool_permissions: permissions
        });

        // First call: Cache miss
        const tools1 = rbacFilter.getAllowedTools('dev');
        expect(mockAgentDb.findSchemaByRoleName).toHaveBeenCalledTimes(1);
        expect(tools1.has('tool1')).toBe(true);

        // Second call: Cache hit
        const tools2 = rbacFilter.getAllowedTools('dev');
        expect(mockAgentDb.findSchemaByRoleName).toHaveBeenCalledTimes(1); // Still 1
        expect(tools2).toEqual(tools1);
    });

    it('should handle malformed JSON and fallback to public tools', () => {
        mockAgentDb.findSchemaByRoleName.mockReturnValue({
            role_name: 'broken_role',
            mcp_tool_permissions: 'invalid-json'
        });

        const tools = rbacFilter.getAllowedTools('broken_role');
        expect(tools.has('list_domains')).toBe(true);
        expect(tools.size).toBe(10); // Only public tools
    });

    it('should flush cache when schema version changes', () => {
        mockAgentDb.getSchemaVersion.mockReturnValue(1);
        mockAgentDb.findSchemaByRoleName.mockReturnValue({
            role_name: 'dev',
            mcp_tool_permissions: JSON.stringify(['tool1'])
        });

        // Populate cache
        rbacFilter.getAllowedTools('dev');
        expect(mockAgentDb.findSchemaByRoleName).toHaveBeenCalledTimes(1);

        // Change version → cache invalidation
        mockAgentDb.getSchemaVersion.mockReturnValue(2);
        rbacFilter.getAllowedTools('dev');
        expect(mockAgentDb.findSchemaByRoleName).toHaveBeenCalledTimes(2); // Cache miss
    });
});
```

**Tested scenarios:**
- ✅ Unknown role → public tools only
- ✅ Cache miss → DB query + cache result
- ✅ Cache hit → no DB query
- ✅ Malformed JSON → fallback to public tools
- ✅ Schema version change → cache invalidation

### E2E Test Pattern Example (mcp-rbac.test.ts)

```typescript
import { test, expect } from '@playwright/test';

const BASE = 'http://localhost:3000/mcp';

async function listToolsForRole(roleHeader?: string): Promise<string[]> {
    const headers: Record<string, string> = {
        'Content-Type': 'application/json',
        'Accept': 'application/json, text/event-stream',
    };
    if (roleHeader) {
        headers['x-active-role'] = roleHeader;
    }

    // 1. Initialize session
    const initRes = await fetch(`${BASE}/http`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
            jsonrpc: '2.0',
            id: 1,
            method: 'initialize',
            params: {
                protocolVersion: '2024-11-05',
                capabilities: {},
                clientInfo: { name: 'rbac-e2e-test', version: '1.0' },
            },
        }),
    });

    let sessionId = initRes.headers.get('mcp-session-id');
    if (!sessionId) {
        const body: any = await initRes.json();
        sessionId = body?.result?.sessionId ?? body?.sessionId ?? null;
    }
    if (!sessionId) {
        throw new Error(`Failed to initialize session`);
    }

    headers['mcp-session-id'] = sessionId;

    // 2. Send notifications/initialized
    await fetch(`${BASE}/http`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
            jsonrpc: '2.0',
            method: 'notifications/initialized'
        }),
    });

    // 3. Call tools/list
    const toolsRes = await fetch(`${BASE}/http`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
            jsonrpc: '2.0',
            id: 2,
            method: 'tools/list'
        }),
    });

    const text = await toolsRes.text();
    const dataLine = text.split('\n').find(l => l.startsWith('data:'));
    const data: any = JSON.parse(dataLine.slice(5).trim());

    return data.result.tools.map((t: any) => t.name);
}

test.describe('MCP Tool Surface — RBAC E2E', () => {

    test('Unknown or missing role should only see public tools', async () => {
        const toolNames = await listToolsForRole();

        expect(toolNames).toContain('search_knowledge');
        expect(toolNames).toContain('get_policy');

        // These are not public
        expect(toolNames).not.toContain('request_workflow_transition');
        expect(toolNames).not.toContain('get_workflow_state');
    });

    test('Explorer role should see specific tools, but not mutating tools', async () => {
        const toolNames = await listToolsForRole('explorer');

        // Explorer specific
        expect(toolNames).toContain('get_role');
        expect(toolNames).toContain('get_workflow');
        expect(toolNames).toContain('list_templates');

        // Still public available
        expect(toolNames).toContain('search_knowledge');

        // Not allowed for explorer
        expect(toolNames).not.toContain('request_workflow_transition');
    });

    test('Backend developer role should see state mutation tools', async () => {
        const toolNames = await listToolsForRole('backend_developer');

        // Backend developer specific
        expect(toolNames).toContain('get_role');
        expect(toolNames).toContain('get_workflow_state');
        expect(toolNames).toContain('request_workflow_transition');
        expect(toolNames).toContain('search_knowledge'); // public
    });
});
```

**E2E Test scenarios:**
- ✅ Unknown role → public tools only (HTTP + JSON-RPC + SSE)
- ✅ Explorer role → read-only tools + public tools
- ✅ Backend developer role → mutation tools + read tools + public tools
- ✅ Session management (mcp-session-id header)
- ✅ Full HTTP stack (Express + MCP protocol)

### Adaptálható Test Patterns SpaceOS-hoz

#### 1. Task Creation API Unit Tests

```typescript
// spaceos-nexus/knowledge-service/src/__tests__/taskCreation.test.ts

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TaskCreationService } from '../task-audit/taskCreation';

vi.mock('fs/promises');

describe('TaskCreationService', () => {
    let service: TaskCreationService;

    beforeEach(() => {
        vi.clearAllMocks();
        service = new TaskCreationService();
    });

    it('should reject unauthorized token', async () => {
        await expect(
            service.createTask('invalid-token', {
                assigned_to: 'backend',
                task_type: 'CODE',
                review_type: 'content',
                priority: 'high',
                title: 'Test task',
                content: 'Test content'
            })
        ).rejects.toThrow('Unauthorized: insufficient scope');
    });

    it('should create task with valid root token', async () => {
        const result = await service.createTask('root-token', {
            assigned_to: 'backend',
            task_type: 'CODE',
            review_type: 'content',
            priority: 'high',
            title: 'Implement feature X',
            content: 'Add new feature...'
        });

        expect(result.task_id).toMatch(/^MSG-BACKEND-\d{3}$/);
        expect(result.inbox_path).toContain('terminals/backend/inbox/');
    });

    it('should reject conductor token for root-only scope', async () => {
        await expect(
            service.createTask('conductor-token', {
                assigned_to: 'architect',  // conductor can't assign to architect
                task_type: 'CODE',
                review_type: 'content',
                priority: 'high',
                title: 'Test',
                content: 'Test'
            })
        ).rejects.toThrow('Unauthorized');
    });

    it('should cache token permissions (LRU)', async () => {
        const spy = vi.spyOn(service as any, 'loadTokens');

        // First call
        await service.createTask('root-token', { /* ... */ });
        expect(spy).toHaveBeenCalledTimes(0); // Already loaded in constructor

        // Second call (cache hit)
        await service.createTask('root-token', { /* ... */ });
        expect(spy).toHaveBeenCalledTimes(0); // Still cached
    });
});
```

#### 2. Token Auth Middleware E2E Tests

```typescript
// spaceos-nexus/knowledge-service/src/__tests__/e2e/taskCreation.e2e.test.ts

import { test, expect } from '@playwright/test';
import supertest from 'supertest';
import app from '../../server';

const request = supertest(app);

test.describe('Task Creation API - E2E', () => {

    test('POST /api/task/create without token should return 401', async () => {
        const res = await request
            .post('/api/task/create')
            .send({
                assigned_to: 'backend',
                task_type: 'CODE',
                title: 'Test',
                content: 'Test content'
            });

        expect(res.status).toBe(401);
        expect(res.body.error).toContain('Missing authorization token');
    });

    test('POST /api/task/create with invalid token should return 401', async () => {
        const res = await request
            .post('/api/task/create')
            .set('Authorization', 'Bearer invalid-token-123')
            .send({
                assigned_to: 'backend',
                task_type: 'CODE',
                title: 'Test',
                content: 'Test content'
            });

        expect(res.status).toBe(401);
    });

    test('POST /api/task/create with valid root token should succeed', async () => {
        const res = await request
            .post('/api/task/create')
            .set('Authorization', 'Bearer dev-token-root-2026')
            .send({
                assigned_to: 'backend',
                task_type: 'CODE',
                review_type: 'content',
                priority: 'high',
                title: 'Implement feature X',
                content: 'Add new feature...',
                project: 'EPIC-CUTTING-Q3',
                epic: 'Track-A'
            });

        expect(res.status).toBe(200);
        expect(res.body.task_id).toMatch(/^MSG-BACKEND-\d{3}$/);
        expect(res.body.inbox_path).toContain('terminals/backend/inbox/');
    });

    test('Audit log should contain task creation entry', async () => {
        // Create task
        await request
            .post('/api/task/create')
            .set('Authorization', 'Bearer dev-token-root-2026')
            .send({ /* ... */ });

        // Check JSONL log
        const fs = require('fs/promises');
        const log = await fs.readFile('/opt/spaceos/logs/tasks/creation.jsonl', 'utf-8');
        const entries = log.trim().split('\n').map(line => JSON.parse(line));

        const lastEntry = entries[entries.length - 1];
        expect(lastEntry.created_by).toBe('root');
        expect(lastEntry.task_type).toBe('CODE');
        expect(lastEntry.inbox_hash).toMatch(/^sha256:/);
    });
});
```

### Test Coverage Goals

**Unit tests:**
- Token verification logic: 100%
- Scope checking (wildcards): 100%
- LRU cache hit/miss: 100%
- JSONL logging: 100%
- SHA-256 hashing: 100%

**Integration tests:**
- Inbox file creation: 100%
- Git auto-commit: 100%
- JSONL append: 100%

**E2E tests:**
- API endpoint auth: 100%
- Full task creation flow: 100%
- Error responses (401, 403, 400): 100%

**Test commands:**
```bash
# Unit tests
npm test

# E2E tests
npm run test:e2e

# Test coverage
npx vitest run --coverage
```

---

## 10. Git Commit

```bash
git add docs/agent-infrastructure/JOINERYTECH_MCP_INSPIRATION.md
git commit -m "docs(inspiration): add test coverage & patterns from JoineryTech.MCP"
```

---

## 11. Következő Lépés

**Root döntés kell:**

1. **Implementáljuk most a Task Creation API-t?** (Phase 2a - 2 óra)
2. **Token storage:** YAML config vagy SQLite?
3. **Formal review criteria:** minimal (frontmatter + git) vagy full (+ build + test)?

**Javasolt:** Phase 2a (Task Creation API) + Phase 1a (Formal Review Script) parallel implementation (~3 óra)

**Következő parancs:**
```bash
# Ha implementáljuk:
cd /opt/spaceos/spaceos-nexus/knowledge-service
mkdir -p src/task-audit
touch src/task-audit/taskCreation.ts
touch src/task-audit/auth.ts
```
