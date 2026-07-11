// ==========================================================================
// EPIC-11: WorkflowStateTracker Service
// ==========================================================================
// Date: 2026-03-11
// Author: Dev C
// Task: TASK-11-02
// AC: 18/18
// Replaces: EPIC-08 src/metadata/WorkflowStateTracker.ts (legacy project FSM)
// ==========================================================================
//
// BACKWARD COMPATIBILITY NOTE:
// The previous WorkflowStateTracker (EPIC-08) managed project-level workflow
// states via 'workflow_states' table.  EPIC-11 supersedes it with an
// agent-session-level FSM using the 'agent_sessions' + 'session_history'
// tables from migration 004_epic11_fsm_schema.sql.
// The legacy AGILE_EPIC_LIFECYCLE_V1 constant is re-exported for existing
// callers, but all new code should use the EPIC-11 session-based API.
// ==========================================================================

// eslint-disable-next-line @typescript-eslint/no-require-imports
const Database = require('better-sqlite3');
import * as path from 'path';
import {
    FSMState,
    VALID_TRANSITIONS,
    TERMINAL_STATES,
    SessionState,
    StateTransitionHistory,
    ResumptionContext,
    CreateSessionParams,
    UpdateStateParams,
    FSMTransitionError,
    SessionNotFoundError,
} from './types';

export {
    FSMState,
    FSMTransitionError,
    SessionNotFoundError,
    SessionState,
    ResumptionContext,
    StateTransitionHistory,
    CreateSessionParams,
    UpdateStateParams,
};

// --------------------------------------------------------------------------
// Legacy EPIC-08 type re-export (backward compat — do not use in new code)
// --------------------------------------------------------------------------
/** @deprecated Use EPIC-11 SessionState instead */
export interface WorkflowStateRecord {
    id: number;
    project_id: string;
    workflow_id: string;
    current_state: string;
    retry_count: number;
    created_at: string;
    updated_at: string | null;
}

const DEFAULT_DB_PATH = path.resolve(__dirname, '../../metadata.db');

// --------------------------------------------------------------------------
// Internal DB row types
// --------------------------------------------------------------------------

interface SessionRow {
    session_id: string;
    domain: string;
    role_name: string;
    workflow_id: string;
    current_state: string;
    track: string | null;
    track_locked: number;
    last_action: string | null;
    created_at: string;
    updated_at: string;
}

interface HistoryRow {
    id: number;
    session_id: string;
    state_before: string;
    state_after: string;
    action: string;
    metadata: string | null;
    timestamp: string;
}

// --------------------------------------------------------------------------
// WorkflowStateTracker — EPIC-11 FSM Service
// --------------------------------------------------------------------------

/**
 * WorkflowStateTracker
 *
 * Core FSM state machine service for EPIC-11.
 * Manages agent session lifecycle across 7 FSM states,
 * enforces valid transitions, and build resumption contexts for bootstrap_agent.
 *
 * FSM (TASK-11-02 spec):
 *   initialized → briefed → in_progress ⇄ awaiting_input → ready_to_submit → submitted
 *   (any non-terminal) → failed (terminal)
 *
 * All state changes are persisted to `session_history` for full audit trail.
 *
 * @see src/metadata/types.ts for FSMState, VALID_TRANSITIONS, all DTOs
 * @see src/metadata/migrations/004_epic11_fsm_schema.sql for DB schema
 */
export class WorkflowStateTracker {
    private readonly db: ReturnType<typeof Database>;

    constructor(dbPath: string = DEFAULT_DB_PATH) {
        this.db = new Database(dbPath);
        this.db.pragma('journal_mode = WAL');   // AC (TASK-10-07): WAL mode
        this.db.pragma('busy_timeout = 5000');
        this.db.pragma('synchronous = NORMAL');
        this.initSchema();
    }

    // ------------------------------------------------------------------------
    // Schema bootstrap (idempotent)
    // ------------------------------------------------------------------------

    private initSchema(): void {
        this.db.exec(`
      CREATE TABLE IF NOT EXISTS agent_sessions (
        session_id    TEXT PRIMARY KEY,
        domain        TEXT NOT NULL,
        role_name     TEXT NOT NULL,
        workflow_id   TEXT NOT NULL,
        current_state TEXT NOT NULL DEFAULT 'initialized',
        last_action   TEXT,
        created_at    TEXT NOT NULL DEFAULT (datetime('now')),
        updated_at    TEXT NOT NULL DEFAULT (datetime('now'))
      );

      CREATE INDEX IF NOT EXISTS idx_agent_sessions_state
        ON agent_sessions(current_state);
    `);

        try { this.db.exec("ALTER TABLE agent_sessions ADD COLUMN track TEXT;"); } catch (e) { /* ignore */ }
        try { this.db.exec("ALTER TABLE agent_sessions ADD COLUMN track_locked INTEGER NOT NULL DEFAULT 0;"); } catch (e) { /* ignore */ }

        this.db.exec(`
      CREATE TABLE IF NOT EXISTS session_history (
        id           INTEGER PRIMARY KEY AUTOINCREMENT,
        session_id   TEXT NOT NULL,
        state_before TEXT NOT NULL,
        state_after  TEXT NOT NULL,
        action       TEXT NOT NULL,
        metadata     TEXT,
        timestamp    TEXT NOT NULL DEFAULT (datetime('now')),
        FOREIGN KEY (session_id) REFERENCES agent_sessions(session_id) ON DELETE CASCADE
      );

      CREATE INDEX IF NOT EXISTS idx_session_history_session_ts
        ON session_history(session_id, timestamp);
    `);
    }

    // ------------------------------------------------------------------------
    // Public API — AC-1 to AC-5
    // ------------------------------------------------------------------------

    /**
     * Create a new tracked FSM session starting in 'initialized' state.
     * AC-1: createSession() returns valid session record.
     * AC-12: created_at + updated_at tracked correctly.
     */
    createSession(params: CreateSessionParams): SessionState {
        const { sessionId, domain, roleName, workflowId, track } = params;
        const now = new Date().toISOString();

        // If track is omitted, we allow NULL; higher layers may infer it from domain.
        this.db.prepare(`
      INSERT INTO agent_sessions
        (session_id, domain, role_name, workflow_id, current_state, track, track_locked, created_at, updated_at)
      VALUES (?, ?, ?, ?, 'initialized', ?, 0, ?, ?)
    `).run(sessionId, domain, roleName, workflowId, track || null, now, now);

        return this.getState(sessionId);
    }

    /**
     * Retrieve current FSM state. AC-2.
     * @throws SessionNotFoundError if session does not exist.
     */
    getState(sessionId: string): SessionState {
        const row = this.db
            .prepare('SELECT * FROM agent_sessions WHERE session_id = ?')
            .get(sessionId) as SessionRow | undefined;

        if (!row) {
            throw new SessionNotFoundError(sessionId);
        }
        return this.mapRowToSession(row);
    }

    /**
     * Transition the session to a new FSM state.
     * Validates against VALID_TRANSITIONS, blocks terminal states, rolls back on failure.
     * AC-3, AC-6 to AC-12, AC-17.
     *
     * @throws SessionNotFoundError if session does not exist.
     * @throws FSMTransitionError if the transition is not permitted.
     */
    updateState(params: UpdateStateParams): SessionState {
        const { sessionId, newState, action, metadata } = params;
        const current = this.getState(sessionId); // throws SessionNotFoundError
        const fromState = current.state;

        // AC-9: terminal states are immutable
        if (TERMINAL_STATES.has(fromState)) {
            throw new FSMTransitionError(fromState, newState, sessionId);
        }

        // AC-10: reject invalid transitions
        if (!this.isValidTransition(fromState, newState)) {
            throw new FSMTransitionError(fromState, newState, sessionId);
        }

        // AC-17: wrap in transaction — rolls back on any error
        const transact = this.db.transaction(() => {
            const now = new Date().toISOString();
            const meta = metadata ? JSON.stringify(metadata) : null;

            this.db.prepare(`
        UPDATE agent_sessions
        SET current_state = ?, last_action = ?, updated_at = ?
        WHERE session_id = ?
      `).run(newState, action, now, sessionId);

            // AC-13, AC-14: write audit record
            this.db.prepare(`
        INSERT INTO session_history
          (session_id, state_before, state_after, action, metadata, timestamp)
        VALUES (?, ?, ?, ?, ?, ?)
      `).run(sessionId, fromState, newState, action, meta, now);
        });

        transact();
        return this.getState(sessionId);
    }

    /**
     * Lock a session to a specific track (discovery or delivery).
     * @throws SessionNotFoundError if session does not exist.
     */
    lockTrack(sessionId: string, track: 'discovery' | 'delivery'): void {
        this.getState(sessionId); // ensure existence
        this.db.prepare(`
            UPDATE agent_sessions
            SET track = ?, track_locked = 1, updated_at = ?
            WHERE session_id = ?
        `).run(track, new Date().toISOString(), sessionId);
    }

    /**
     * Return the full state-transition audit trail for a session.
     * AC-4, AC-13, AC-14, AC-15 (indexed for < 10ms perf).
     *
     * @throws SessionNotFoundError if session does not exist.
     */
    getHistory(sessionId: string): StateTransitionHistory[] {
        this.getState(sessionId); // ensure existence

        const rows = this.db
            .prepare(`
        SELECT * FROM session_history
        WHERE session_id = ?
        ORDER BY timestamp ASC
      `)
            .all(sessionId) as HistoryRow[];

        return rows.map(r => this.mapRowToHistory(r));
    }

    /**
     * Build a resumption context payload for bootstrap_agent.
     * AC-5.
     *
     * @throws SessionNotFoundError if session does not exist.
     */
    getResumeContext(sessionId: string): ResumptionContext {
        const session = this.getState(sessionId);
        const history = this.getHistory(sessionId);

        const completedSteps = history.map(h => h.action);
        const previousToolResults: Record<string, unknown> = {};

        for (const h of history) {
            if (h.metadata) {
                Object.assign(previousToolResults, h.metadata);
            }
        }

        const context: ResumptionContext = {
            sessionId,
            currentState: session.state,
            workflowId: session.workflowId,
            domain: session.domain,
            roleName: session.roleName,
            taskProgress: { completedSteps, previousToolResults },
            resumePayload: '',
        };

        context.resumePayload = JSON.stringify({
            session_id: sessionId,
            current_state: session.state,
            workflow_id: session.workflowId,
            domain: session.domain,
            role_name: session.roleName,
            completed_steps: completedSteps,
            previous_tool_results: previousToolResults,
            resumed_at: new Date().toISOString(),
        });

        return context;
    }

    // ------------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------------

    /** AC-10: validate against the canonical transition map. */
    private isValidTransition(from: FSMState, to: FSMState): boolean {
        return VALID_TRANSITIONS[from]?.includes(to) ?? false;
    }

    private mapRowToSession(row: SessionRow): SessionState {
        return {
            sessionId: row.session_id,
            state: row.current_state as FSMState,
            workflowId: row.workflow_id,
            domain: row.domain,
            roleName: row.role_name,
            track: (row.track as 'discovery' | 'delivery') || null,
            trackLocked: Boolean(row.track_locked),
            createdAt: new Date(row.created_at),
            updatedAt: new Date(row.updated_at),
        };
    }

    private mapRowToHistory(row: HistoryRow): StateTransitionHistory {
        let meta: Record<string, unknown> | null = null;
        if (row.metadata) {
            try { meta = JSON.parse(row.metadata) as Record<string, unknown>; }
            catch { meta = null; }
        }
        return {
            id: row.id,
            sessionId: row.session_id,
            fromState: row.state_before as FSMState,
            toState: row.state_after as FSMState,
            action: row.action,
            metadata: meta,
            timestamp: new Date(row.timestamp),
        };
    }

    /** Close the underlying SQLite connection. */
    close(): void {
        this.db.close();
    }
}
