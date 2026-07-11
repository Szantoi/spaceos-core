// ===========================================================================
// DiscoveryPhaseTracker
// ===========================================================================
// Maintains phase progression for discovery-track sessions (ideation →
// validation → iteration). Provides helpers for tools to enforce ordering
// and mark completion. Stored in SQLite alongside WorkflowStateTracker via
// new `discovery_phases` table.
//
// See TASK-13-05 assignment for AC details.
// ===========================================================================

// eslint-disable-next-line @typescript-eslint/no-require-imports
const Database = require('better-sqlite3');
import * as path from 'path';

const DEFAULT_DB_PATH = path.resolve(__dirname, '../../metadata.db');

type Phase = 'ideation' | 'validation' | 'iteration';
const PHASE_ORDER: Phase[] = ['ideation', 'validation', 'iteration'];

interface PhaseRow {
    session_id: string;
    current_phase: Phase;
    ideation_complete: number;
    validation_complete: number;
    iteration_complete: number;
    updated_at: string;
}

export class DiscoveryPhaseTracker {
    private readonly db: ReturnType<typeof Database>;

    constructor(dbPath: string = DEFAULT_DB_PATH) {
        this.db = new Database(dbPath);
        this.db.pragma('journal_mode = WAL');
        this.db.pragma('busy_timeout = 5000');
        this.db.pragma('synchronous = NORMAL');
        this.initSchema();
    }

    private initSchema(): void {
        this.db.exec(`
            CREATE TABLE IF NOT EXISTS agent_sessions (
                session_id TEXT PRIMARY KEY
            );

      CREATE TABLE IF NOT EXISTS discovery_phases (
        session_id TEXT PRIMARY KEY,
        current_phase TEXT NOT NULL DEFAULT 'ideation',
        ideation_complete INTEGER NOT NULL DEFAULT 0,
        validation_complete INTEGER NOT NULL DEFAULT 0,
        iteration_complete INTEGER NOT NULL DEFAULT 0,
        updated_at TEXT NOT NULL DEFAULT (datetime('now')),
        FOREIGN KEY(session_id) REFERENCES agent_sessions(session_id) ON DELETE CASCADE
      );
    `);
    }

    /** Ensure a row exists for the given session (idempotent). */
    ensureSession(sessionId: string): void {
        const exists = this.db.prepare(`
            SELECT 1 FROM discovery_phases WHERE session_id = ?
        `).get(sessionId);
        if (!exists) {
            this.db.prepare(`
                INSERT INTO discovery_phases(session_id) VALUES (?)
            `).run(sessionId);
        }
    }

    private mapRow(row: PhaseRow): {
        sessionId: string;
        currentPhase: Phase;
        completed: Record<Phase, boolean>;
    } {
        return {
            sessionId: row.session_id,
            currentPhase: row.current_phase as Phase,
            completed: {
                ideation: Boolean(row.ideation_complete),
                validation: Boolean(row.validation_complete),
                iteration: Boolean(row.iteration_complete)
            }
        };
    }

    getCurrentPhase(sessionId: string): Phase {
        this.ensureSession(sessionId);
        const row = this.db.prepare(
            'SELECT current_phase FROM discovery_phases WHERE session_id = ?'
        ).get(sessionId) as PhaseRow | undefined;
        return (row?.current_phase as Phase) || 'ideation';
    }

    /** Mark a phase as complete; advance current_phase if matching. */
    markPhaseComplete(sessionId: string, phase: Phase): void {
        this.ensureSession(sessionId);
        const now = new Date().toISOString();
        const col = `${phase}_complete`;
        this.db.prepare(`
            UPDATE discovery_phases
            SET ${col} = 1,
                updated_at = ?,
                current_phase = CASE
                    WHEN current_phase = ? THEN ?
                    ELSE current_phase
                END
            WHERE session_id = ?
        `).run(now, phase, this.nextPhase(phase) || phase, sessionId);
    }

    isPhaseComplete(sessionId: string, phase: Phase): boolean {
        this.ensureSession(sessionId);
        const row = this.db.prepare(
            `SELECT ${phase}_complete as c FROM discovery_phases WHERE session_id = ?`
        ).get(sessionId) as any;
        return Boolean(row?.c);
    }

    canSubmitForPhase(sessionId: string, requested: Phase): boolean {
        // ideation always allowed
        if (requested === 'ideation') return true;
        if (requested === 'validation') {
            return this.isPhaseComplete(sessionId, 'ideation');
        }
        if (requested === 'iteration') {
            return this.isPhaseComplete(sessionId, 'validation');
        }
        return false;
    }

    private nextPhase(phase: Phase): Phase | null {
        const idx = PHASE_ORDER.indexOf(phase);
        if (idx >= 0 && idx < PHASE_ORDER.length - 1) {
            return PHASE_ORDER[idx + 1];
        }
        return null;
    }

    close(): void {
        this.db.close();
    }
}
