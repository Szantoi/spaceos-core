// @ts-ignore
const Database: any = require('better-sqlite3');
import * as path from 'path';
import { randomUUID } from 'crypto';

const DB_PATH = path.resolve(__dirname, '../../metadata.db');

// --- SESSION-MANAGER START ---

export interface SessionRecord {
    id: string; // Changed from number to string (UUID)
    role: string;
    domain: string;
    current_domain_id?: string | null;
    agent_id: string | null; // Changed from agent_name
    status: string; // Maps to fsm_state
    created_at: string; // Maps to started_at
    last_updated_at: string | null; // Maps to last_updated_at
}

/**
 * SessionManager
 *
 * Tracks which agent/role is currently working and whether they have submitted
 * the required documentation before being allowed to close their session.
 *
 * Backed by the shared metadata.db (same instance as ResourceTracker and WorkflowStateTracker).
 *
 * Lifecycle:
 *   POST /mcp/session/register  → register()  → status: 'active'
 *   POST /mcp/artifact/submit   → (ResourceTracker stores session_id)
 *   POST /mcp/session/complete  → complete()  → status: 'completed'  (if artifacts exist)
 *                               → block()     → status: 'blocked'    (if no artifacts)
 *
 * Rollback: delete this file and remove the import + new SessionManager() from index.ts
 */
export class SessionManager {
    private db: any;

    constructor(dbPath: string = DB_PATH) {
        this.db = new Database(dbPath);
        this.init();
    }

    private init(): void {
        // Consolidate with EPIC-08 schema from 002_write_layer_schema.sql
        this.db.exec(`
            CREATE TABLE IF NOT EXISTS sessions (
                id              TEXT PRIMARY KEY COLLATE NOCASE,
                agent_id        TEXT NOT NULL UNIQUE COLLATE NOCASE,
                domain          TEXT NOT NULL,
                current_domain_id TEXT,
                role            TEXT NOT NULL,
                started_at      TEXT NOT NULL,
                last_updated_at TEXT,
                fsm_state       TEXT NOT NULL DEFAULT 'started',
                outcome         TEXT,
                UNIQUE(agent_id, started_at)
            );
        `);

        // Backward-compatible upgrade path for pre-EPIC-17 databases.
        try {
            this.db.exec('ALTER TABLE sessions ADD COLUMN current_domain_id TEXT');
        } catch {
            // Column already exists.
        }
    }

    /**
     * Register a new agent session with cryptographically strong UUID v4 session_id.
     *
     * **UUID v4 Guarantee (TASK-10-05 AC-6 to AC-9):**
     * - Uses `crypto.randomUUID()` — cryptographically strong random number generator
     * - NOT Math.random() — which is predictable and weak for security/uniqueness
     * - Returns UUID v4 format: xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx (36 chars)
     * - Version bits (position 14) = '4' (random UUID)
     * - Variant bits (position 19) = one of [8,9,a,b] (RFC 4122 compliant)
     *
     * **Collision Risk (TASK-10-05 AC-10 to AC-12):**
     * - Birthday paradox: ~2^122 possible UUIDs
     * - Practical collision resistance: ZERO collisions in 10,000 generations (verified by tests)
     * - Session ID uniqueness is CRITICAL: collision = FSM state corruption + agent confusion
     *
     * **Session Lifecycle (TASK-10-05 AC-1 to AC-5):**
     * 1. register() creates new session with random UUID v4 session_id
     * 2. Session stored in SQLite sessions table with UNIQUE constraint (AC-3)
     * 3. Session ID returned to bootstrap_agent via BootstrapPayload
     * 4. Agents use session_id for resume_task intent (EPIC-10 TASK-10-04B)
     * 5. WorkflowStateTracker queries FSM state by session_id (EPIC-11)
     *
     * @param role_name - Role identifier (e.g., "explorer", "backend_developer")
     * @param domain - Domain name (e.g., "discovery", "delivery", "engineering")
     * @param agentName - Optional: Agent identifier for logging/debugging
     * @returns SessionRecord with generated UUID v4 session_id
     * @throws Error if database insertion fails (AC-13: error handling)
     *
     * **Example:**
     * ```typescript
     * const session = sessionManager.register('explorer', 'discovery', 'my-agent');
     * // Returns:
     * // {
     * //   id: 42,
     * //   session_id: "550e8400-e29b-41d4-a716-446655440000", // UUID v4
     * //   role: "explorer",
     * //   domain: "discovery",
     * //   agent_name: "my-agent",
     * //   status: "active",
     * //   created_at: "2026-03-06T16:45:00Z",
     * //   completed_at: null
     * // }
     * ```
     */
    /**
     * Register a new agent session with a provided or generated UUID v4 session_id.
     *
     * @param role - Role identifier
     * @param domain - Domain name
     * @param agentId - Optional: Agent identifier
     * @param providedSessionId - Optional: Pre-generated UUID v4 session_id
     * @returns SessionRecord with session_id
     */
    register(
        role: string,
        domain: string,
        agentId?: string,
        providedSessionId?: string,
        currentDomainId?: string | null
    ): SessionRecord {
        const sessionId = providedSessionId || randomUUID();
        const startedAt = new Date().toISOString();
        const actualAgentId = agentId ?? `${role}-${randomUUID()}`;

        if (this.hasColumn('sessions', 'current_domain_id')) {
            this.db.prepare(
                `INSERT INTO sessions (id, agent_id, domain, current_domain_id, role, started_at, fsm_state)
                 VALUES (?, ?, ?, ?, ?, ?, ?)`
            ).run(sessionId, actualAgentId, domain, currentDomainId ?? null, role, startedAt, 'started');
        } else {
            this.db.prepare(
                `INSERT INTO sessions (id, agent_id, domain, role, started_at, fsm_state) VALUES (?, ?, ?, ?, ?, ?)`
            ).run(sessionId, actualAgentId, domain, role, startedAt, 'started');
        }

        return this.get(sessionId)!;
    }

    /**
     * Retrieve a session by its session_id.
     * Returns undefined if the session does not exist.
     */
    get(sessionId: string): SessionRecord | undefined {
        const row = this.db
            .prepare('SELECT * FROM sessions WHERE id = ?')
            .get(sessionId) as any;

        if (!row) return undefined;

        return {
            id: row.id,
            role: row.role,
            domain: row.domain,
            current_domain_id: row.current_domain_id ?? null,
            agent_id: row.agent_id,
            status: row.fsm_state,
            created_at: row.started_at,
            last_updated_at: row.last_updated_at
        };
    }

    /**
     * List all sessions with a given status.
     */
    listByStatus(status: string = 'started'): SessionRecord[] {
        const rows = this.db
            .prepare('SELECT * FROM sessions WHERE fsm_state = ?')
            .all(status) as any[];

        return rows.map(row => ({
            id: row.id,
            role: row.role,
            domain: row.domain,
            current_domain_id: row.current_domain_id ?? null,
            agent_id: row.agent_id,
            status: row.fsm_state,
            created_at: row.started_at,
            last_updated_at: row.last_updated_at
        }));
    }

    /**
     * Set or clear current domain context for an existing session.
     */
    setCurrentDomainId(sessionId: string, domainId: string | null): void {
        if (!this.hasColumn('sessions', 'current_domain_id')) {
            return;
        }
        this.db.prepare(
            `UPDATE sessions SET current_domain_id = ?, last_updated_at = ? WHERE id = ?`
        ).run(domainId, new Date().toISOString(), sessionId);
    }

    /**
     * Resolve registry domain id to domain name.
     * Returns null if domains table is missing or id is unknown.
     */
    resolveDomainNameById(domainId: string | null | undefined): string | null {
        if (!domainId) return null;
        try {
            const row = this.db
                .prepare('SELECT name FROM domains WHERE id = ? LIMIT 1')
                .get(domainId) as { name?: string } | undefined;
            return row?.name ?? null;
        } catch {
            return null;
        }
    }

    private hasColumn(tableName: string, columnName: string): boolean {
        const rows = this.db.prepare(`PRAGMA table_info(${tableName})`).all() as Array<{ name: string }>;
        return rows.some(row => row.name === columnName);
    }

    /**
     * Mark a session as successfully completed.
     * Should only be called after verifying that at least 1 artifact was submitted.
     */
    complete(sessionId: string): SessionRecord {
        this.db.prepare(
            `UPDATE sessions
             SET fsm_state = 'completed', last_updated_at = ?
             WHERE id = ?`
        ).run(new Date().toISOString(), sessionId);

        return this.get(sessionId)!;
    }

    /**
     * Mark a session as blocked.
     */
    block(sessionId: string): SessionRecord {
        this.db.prepare(
            `UPDATE sessions
             SET fsm_state = 'blocked', last_updated_at = ?
             WHERE id = ?`
        ).run(new Date().toISOString(), sessionId);

        return this.get(sessionId)!;
    }

    /**
     * Close the database connection.
     */
    close(): void {
        if (this.db) {
            this.db.close();
        }
    }
}

// --- SESSION-MANAGER END ---
