/**
 * Test Helper Functions for Write Layer Integration Tests
 *
 * Provides reusable test utilities for:
 *   - Bootstrap mocked sessions
 *   - Submit artifacts safely
 *   - Update workflow state
 *   - Clean up test data
 *
 * Used by:
 *   - src/tests/integration/WriteLayerTools.test.ts
 *   - src/tests/e2e/write-layer.e2e.test.ts
 */

import Database from 'better-sqlite3';
import { randomUUID } from 'crypto';
import {
  SessionRecord,
  ArtifactRecord,
  WorkflowEventRecord,
} from '../../mcp/schemas';

// ─────────────────────────────────────────────────────────────────────────────
// TEST DATABASE SETUP
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Creates an in-memory test database with the write-layer schema initialized.
 *
 * @returns SQLite database instance
 *
 * @example
 *   const db = createTestDb();
 *   const session = await bootstrapSession(db, 'backend_developer');
 */
export function createTestDb(): Database.Database {
  const db = new Database(':memory:');

  // Initialize with proper pragmas
  db.pragma('journal_mode = WAL');
  db.pragma('synchronous = NORMAL');
  db.pragma('foreign_keys = ON');
  db.pragma('locking_mode = NORMAL');
  db.pragma('busy_timeout = 5000');

  // Create schema (simplified for testing)
  db.exec(`
    CREATE TABLE IF NOT EXISTS sessions (
      id             INTEGER PRIMARY KEY AUTOINCREMENT,
      session_id     TEXT NOT NULL UNIQUE,
      role           TEXT NOT NULL,
      domain         TEXT NOT NULL,
      agent_name     TEXT,
      fsm_state      TEXT NOT NULL DEFAULT 'started',
      created_at     TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
      last_updated_at TIMESTAMP
    );

    CREATE TABLE IF NOT EXISTS artifacts (
      id              INTEGER PRIMARY KEY AUTOINCREMENT,
      session_id      TEXT NOT NULL,
      artifact_id     TEXT NOT NULL UNIQUE,
      artifact_type   TEXT NOT NULL,
      content         TEXT NOT NULL,
      submitted_at    TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
      embedded        INTEGER DEFAULT 0,
      FOREIGN KEY (session_id) REFERENCES sessions(session_id)
    );

    CREATE TABLE IF NOT EXISTS workflow_events (
      id                    INTEGER PRIMARY KEY AUTOINCREMENT,
      session_id            TEXT NOT NULL,
      event_id              TEXT NOT NULL UNIQUE,
      event_type            TEXT NOT NULL,
      state_before          TEXT NOT NULL,
      state_after           TEXT NOT NULL,
      evidence_artifact_id  TEXT,
      timestamp             TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
      FOREIGN KEY (session_id) REFERENCES sessions(session_id)
    );

    CREATE TABLE IF NOT EXISTS checkpoints (
      id                INTEGER PRIMARY KEY AUTOINCREMENT,
      session_id        TEXT NOT NULL,
      checkpoint_id     TEXT NOT NULL UNIQUE,
      checkpoint_data   TEXT NOT NULL,
      checkpoint_label  TEXT,
      created_at        TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
      FOREIGN KEY (session_id) REFERENCES sessions(session_id)
    );

    CREATE INDEX IF NOT EXISTS idx_sessions_session_id ON sessions(session_id);
    CREATE INDEX IF NOT EXISTS idx_artifacts_session_id ON artifacts(session_id);
    CREATE INDEX IF NOT EXISTS idx_workflow_events_session_id ON workflow_events(session_id);
    CREATE INDEX IF NOT EXISTS idx_checkpoints_session_id ON checkpoints(session_id);
  `);

  return db;
}

// ─────────────────────────────────────────────────────────────────────────────
// SESSION BOOTSTRAP
// ─────────────────────────────────────────────────────────────────────────────

export interface MockSessionOptions {
  role?: string;
  domain?: string;
  agentName?: string;
  fsmState?: 'started' | 'in_progress' | 'submitted' | 'processed' | 'closed';
}

/**
 * Bootstrap a test session in the database.
 *
 * @param db Database instance
 * @param options Session configuration (role, domain, etc.)
 * @returns Created session record
 *
 * @example
 *   const db = createTestDb();
 *   const session = bootstrapSession(db, {
 *     role: 'backend_developer',
 *     domain: 'engineering',
 *     agentName: 'test-agent-001',
 *   });
 *   console.log(session.session_id); // UUID
 */
export function bootstrapSession(
  db: Database.Database,
  options: MockSessionOptions = {}
): SessionRecord {
  const {
    role = 'backend_developer',
    domain = 'engineering',
    agentName = 'test-agent-001',
    fsmState = 'started',
  } = options;

  const sessionId = randomUUID();
  const now = new Date().toISOString();

  db.prepare(`
    INSERT INTO sessions (session_id, role, domain, agent_name, fsm_state, created_at, last_updated_at)
    VALUES (?, ?, ?, ?, ?, ?, ?)
  `).run(sessionId, role, domain, agentName, fsmState, now, now);

  return getSession(db, sessionId)!;
}

/**
 * Retrieve a session from the database.
 *
 * @param db Database instance
 * @param sessionId Session UUID
 * @returns Session record or undefined
 */
export function getSession(db: Database.Database, sessionId: string): SessionRecord | undefined {
  const stmt = db.prepare('SELECT * FROM sessions WHERE session_id = ?');
  const row = stmt.get(sessionId) as any;
  return row
    ? {
        id: row.id,
        session_id: row.session_id,
        role: row.role,
        domain: row.domain,
        agent_name: row.agent_name,
        fsm_state: row.fsm_state,
        created_at: row.created_at,
        last_updated_at: row.last_updated_at,
      }
    : undefined;
}

// ─────────────────────────────────────────────────────────────────────────────
// ARTIFACT SUBMISSION
// ─────────────────────────────────────────────────────────────────────────────

export interface MockArtifactOptions {
  content?: string;
  type?: 'implementation_summary' | 'test_report' | 'pr_link' | 'checkpoint';
}

/**
 * Submit an artifact to a session.
 *
 * @param db Database instance
 * @param sessionId Session UUID
 * @param options Artifact configuration (content, type)
 * @returns Created artifact record
 *
 * @example
 *   const db = createTestDb();
 *   const session = bootstrapSession(db);
 *   const artifact = submitArtifact(db, session.session_id, {
 *     type: 'implementation_summary',
 *     content: '## Summary\n- Task completed\n- No issues',
 *   });
 */
export function submitArtifact(
  db: Database.Database,
  sessionId: string,
  options: MockArtifactOptions = {}
): ArtifactRecord {
  const {
    content = '## Test Artifact\nGenerated for testing purposes',
    type = 'implementation_summary',
  } = options;

  const artifactId = randomUUID();
  const now = new Date().toISOString();

  db.prepare(`
    INSERT INTO artifacts (session_id, artifact_id, artifact_type, content, submitted_at, embedded)
    VALUES (?, ?, ?, ?, ?, ?)
  `).run(sessionId, artifactId, type, content, now, 0);

  return getArtifact(db, artifactId)!;
}

/**
 * Retrieve an artifact from the database.
 */
export function getArtifact(db: Database.Database, artifactId: string): ArtifactRecord | undefined {
  const stmt = db.prepare('SELECT * FROM artifacts WHERE artifact_id = ?');
  const row = stmt.get(artifactId) as any;
  return row
    ? {
        id: row.id,
        session_id: row.session_id,
        artifact_id: row.artifact_id,
        artifact_type: row.artifact_type,
        content: row.content,
        submitted_at: row.submitted_at,
        embedded: row.embedded,
      }
    : undefined;
}

/**
 * List all artifacts for a session.
 */
export function listArtifactsBySession(
  db: Database.Database,
  sessionId: string
): ArtifactRecord[] {
  const stmt = db.prepare('SELECT * FROM artifacts WHERE session_id = ? ORDER BY submitted_at ASC');
  const rows = stmt.all(sessionId) as any[];
  return rows.map((row) => ({
    id: row.id,
    session_id: row.session_id,
    artifact_id: row.artifact_id,
    artifact_type: row.artifact_type,
    content: row.content,
    submitted_at: row.submitted_at,
    embedded: row.embedded,
  }));
}

// ─────────────────────────────────────────────────────────────────────────────
// WORKFLOW STATE UPDATES
// ─────────────────────────────────────────────────────────────────────────────

export interface MockStateChangeOptions {
  event?: string;
  evidenceArtifactId?: string;
}

/**
 * Update workflow state for a session.
 *
 * @param db Database instance
 * @param sessionId Session UUID
 * @param newState Target FSM state
 * @param options Event description and optional evidence artifact
 * @returns Created workflow event record
 *
 * @example
 *   const db = createTestDb();
 *   const session = bootstrapSession(db);
 *   const event = updateWorkflowState(db, session.session_id, 'in_progress', {
 *     event: 'Agent started implementation',
 *   });
 */
export function updateWorkflowState(
  db: Database.Database,
  sessionId: string,
  newState: 'started' | 'in_progress' | 'submitted' | 'processed' | 'closed',
  options: MockStateChangeOptions = {}
): WorkflowEventRecord {
  const { event = 'State update', evidenceArtifactId } = options;

  // Get current state
  const session = getSession(db, sessionId);
  if (!session) {
    throw new Error(`Session ${sessionId} not found`);
  }

  const currentState = session.fsm_state;
  const eventId = randomUUID();
  const now = new Date().toISOString();

  // Insert event
  db.prepare(`
    INSERT INTO workflow_events (session_id, event_id, event_type, state_before, state_after, evidence_artifact_id, timestamp)
    VALUES (?, ?, ?, ?, ?, ?, ?)
  `).run(sessionId, eventId, event, currentState, newState, evidenceArtifactId || null, now);

  // Update session state
  db.prepare('UPDATE sessions SET fsm_state = ?, last_updated_at = ? WHERE session_id = ?').run(
    newState,
    now,
    sessionId
  );

  return getWorkflowEvent(db, eventId)!;
}

/**
 * Retrieve a workflow event from the database.
 */
export function getWorkflowEvent(
  db: Database.Database,
  eventId: string
): WorkflowEventRecord | undefined {
  const stmt = db.prepare('SELECT * FROM workflow_events WHERE event_id = ?');
  const row = stmt.get(eventId) as any;
  return row
    ? {
        id: row.id,
        session_id: row.session_id,
        event_id: row.event_id,
        event_type: row.event_type,
        state_before: row.state_before,
        state_after: row.state_after,
        evidence_artifact_id: row.evidence_artifact_id,
        timestamp: row.timestamp,
      }
    : undefined;
}

/**
 * List all workflow events for a session.
 */
export function listWorkflowEventsBySession(
  db: Database.Database,
  sessionId: string
): WorkflowEventRecord[] {
  const stmt = db.prepare('SELECT * FROM workflow_events WHERE session_id = ? ORDER BY timestamp ASC');
  const rows = stmt.all(sessionId) as any[];
  return rows.map((row) => ({
    id: row.id,
    session_id: row.session_id,
    event_id: row.event_id,
    event_type: row.event_type,
    state_before: row.state_before,
    state_after: row.state_after,
    evidence_artifact_id: row.evidence_artifact_id,
    timestamp: row.timestamp,
  }));
}

// ─────────────────────────────────────────────────────────────────────────────
// CHECKPOINT STORAGE
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Store a checkpoint for session recovery (M02).
 */
export function storeCheckpoint(
  db: Database.Database,
  sessionId: string,
  data: Record<string, any>,
  label?: string
): { checkpoint_id: string; created_at: string } {
  const checkpointId = randomUUID();
  const now = new Date().toISOString();
  const checkpointJson = JSON.stringify(data);

  db.prepare(`
    INSERT INTO checkpoints (session_id, checkpoint_id, checkpoint_data, checkpoint_label, created_at)
    VALUES (?, ?, ?, ?, ?)
  `).run(sessionId, checkpointId, checkpointJson, label || null, now);

  return { checkpoint_id: checkpointId, created_at: now };
}

// ─────────────────────────────────────────────────────────────────────────────
// CLEANUP
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Clear all test data from the database (for between-test cleanup).
 *
 * @param db Database instance
 */
export function cleanupTestDb(db: Database.Database): void {
  db.exec(`
    DELETE FROM workflow_events;
    DELETE FROM artifacts;
    DELETE FROM checkpoints;
    DELETE FROM sessions;
  `);
}
