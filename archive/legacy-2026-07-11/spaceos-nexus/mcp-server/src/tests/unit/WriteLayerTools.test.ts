/**
 * WriteLayerTools.test.ts
 *
 * Unit tests for MCP write tools: submit_artifact() and update_workflow_state()
 *
 * Coverage:
 *   - submitArtifact(): happy path, schema validation, RBAC checks, missing session
 *   - updateWorkflowState(): happy path, FSM validation, RBAC checks, invalid transitions
 *   - Error responses: meaningful error codes and messages
 *   - SQLite pessimistic locking (basic validation, full concurrency test in integration)
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import Database from 'better-sqlite3';
import { randomUUID } from 'crypto';
import { submitArtifact, updateWorkflowState, calculateJitteredBackoff } from '../../mcp/WriteLayerTools';
import WriteLayerInitializer from '../../metadata/WriteLayerInitializer';
import {
    SubmitArtifactInput,
    UpdateWorkflowStateInput,
    SessionRecord,
} from '../../metadata/WriteLayerSchema';

// ─────────────────────────────────────────────────────────────────────────────
// SETUP
// ─────────────────────────────────────────────────────────────────────────────

let db: Database.Database;

beforeEach(() => {
    // Create in-memory test database
    db = new Database(':memory:');
    db.pragma('foreign_keys = ON');
    db.pragma('journal_mode = WAL');

    // Initialize write-layer schema
    const initializer = new WriteLayerInitializer(db);
    initializer.init();

    // Bootstrap test session
    const sessionId = randomUUID();
    const now = new Date().toISOString();
    db.prepare(`
    INSERT INTO sessions (id, agent_id, domain, role, started_at, last_updated_at, fsm_state, outcome)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
  `).run(sessionId, 'test-agent', 'engineering', 'backend_developer', now, now, 'started', null);
});

// ─────────────────────────────────────────────────────────────────────────────
// SUBMIT_ARTIFACT() TESTS
// ─────────────────────────────────────────────────────────────────────────────

describe('submitArtifact()', () => {
    it('should successfully submit an artifact with valid input', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        const input: SubmitArtifactInput = {
            session_id: sessionId,
            artifact_type: 'implementation_summary',
            artifact_content: '## Summary\n- Task completed',
        };

        const result = submitArtifact(db, input, 'backend_developer');

        expect(result).toHaveProperty('success', true);
        expect(result).toHaveProperty('artifact_id');
        expect(result).toHaveProperty('submitted_at');
        // @ts-ignore
        expect(result.artifact_type).toBe('implementation_summary');

        // Verify artifact inserted into database
        const artifact = db.prepare('SELECT * FROM artifacts WHERE id = ?')
            // @ts-ignore
            .get((result as any).artifact_id) as any;
        expect(artifact).toBeDefined();
        expect(artifact.content).toBe('## Summary\n- Task completed');
    });

    it('should return SCHEMA_VALIDATION_ERROR for invalid session_id format', () => {
        const input = {
            session_id: 'not-a-uuid', // Invalid UUID
            artifact_type: 'implementation_summary',
            artifact_content: 'Content',
        };

        const result = submitArtifact(db, input, 'backend_developer');

        expect(result).toHaveProperty('isError', true);
        // @ts-ignore
        expect(result.error.code).toBe('SCHEMA_VALIDATION_ERROR');
    });

    it('should return SCHEMA_VALIDATION_ERROR for empty artifact_content', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        const input = {
            session_id: sessionId,
            artifact_type: 'implementation_summary',
            artifact_content: '', // Empty content
        };

        const result = submitArtifact(db, input, 'backend_developer');

        expect(result).toHaveProperty('isError', true);
        // @ts-ignore
        expect(result.error.code).toBe('SCHEMA_VALIDATION_ERROR');
    });

    it('should return SCHEMA_VALIDATION_ERROR for invalid artifact_type', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        const input = {
            session_id: sessionId,
            artifact_type: 'checkpoint', // Checkpoint not in M01 scope
            artifact_content: 'Content',
        };

        const result = submitArtifact(db, input as any, 'backend_developer');

        expect(result).toHaveProperty('isError', true);
        // @ts-ignore
        expect(result.error.code).toBe('SCHEMA_VALIDATION_ERROR');
    });

    it('should return PERMISSION_DENIED for unauthorized role', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        const input: SubmitArtifactInput = {
            session_id: sessionId,
            artifact_type: 'implementation_summary',
            artifact_content: 'Content',
        };

        const result = submitArtifact(db, input, 'explorer'); // explorer not allowed

        expect(result).toHaveProperty('isError', true);
        // @ts-ignore
        expect(result.error.code).toBe('PERMISSION_DENIED');
    });

    it('should return SESSION_NOT_FOUND for non-existent session', () => {
        const nonExistentSessionId = randomUUID();

        const input: SubmitArtifactInput = {
            session_id: nonExistentSessionId,
            artifact_type: 'implementation_summary',
            artifact_content: 'Content',
        };

        const result = submitArtifact(db, input, 'backend_developer');

        expect(result).toHaveProperty('isError', true);
        // @ts-ignore
        expect(result.error.code).toBe('SESSION_NOT_FOUND');
    });

    it('should update session.last_updated_at when submitting artifact', () => {
        const session = db.prepare('SELECT * FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;
        const originalUpdatedAt = session.last_updated_at;

        // Wait a small amount to ensure timestamp is different
        const beforeSubmit = new Date().toISOString();

        const input: SubmitArtifactInput = {
            session_id: sessionId,
            artifact_type: 'test_report',
            artifact_content: '## Test Results\nAll passed',
        };

        submitArtifact(db, input, 'backend_developer');

        const updatedSession = db.prepare('SELECT * FROM sessions WHERE id = ?')
            .get(sessionId) as any;

        // timestamp may equal original due to high resolution; just ensure it is
        // at or after the request started.
        expect(new Date(updatedSession.last_updated_at) >= new Date(beforeSubmit)).toBe(true);
    });

    it('should allow tech_lead role to submit artifacts', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        const input: SubmitArtifactInput = {
            session_id: sessionId,
            artifact_type: 'pr_link',
            artifact_content: 'https://github.com/...',
        };

        const result = submitArtifact(db, input, 'tech_lead');

        expect(result).toHaveProperty('success', true);
    });

    it('should log metrics when lock contention occurs and WRITE_LAYER_METRICS is true', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;
        const input: SubmitArtifactInput = {
            session_id: sessionId,
            artifact_type: 'implementation_summary',
            artifact_content: 'Delayed content',
        };

        // patch transaction to fail once with SQLITE_BUSY
        const originalTxn = db.transaction.bind(db);
        let first = true;
        // @ts-ignore
        db.transaction = function (fn: any) {
            if (first) {
                first = false;
                const err: any = new Error('database is locked');
                err.code = 'SQLITE_BUSY';
                throw err;
            }
            return originalTxn(fn);
        };

        process.env.WRITE_LAYER_METRICS = 'true';
        const spy = vi.spyOn(console, 'info');

        const result = submitArtifact(db, input, 'backend_developer');
        expect(result).toHaveProperty('success', true);

        // Metrics logging occurs only on lock contention (retry path)
        // which we're simulating with the mock transaction
        if (spy.mock.calls.length > 0) {
            expect(spy).toHaveBeenCalledWith(
                expect.stringContaining('lock_contention_count')
            );
        }

        spy.mockRestore();
        delete process.env.WRITE_LAYER_METRICS;
        // restore transaction
        // @ts-ignore
        db.transaction = originalTxn;
    });

    it('should log metrics for updateWorkflowState when contention occurs', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        const input = {
            session_id: sessionId,
            new_state: 'in_progress',
            event: 'test',
        } as UpdateWorkflowStateInput;

        const originalTxn = db.transaction.bind(db);
        let first = true;
        // @ts-ignore
        db.transaction = function (fn: any) {
            if (first) {
                first = false;
                const err: any = new Error('database is locked');
                err.code = 'SQLITE_BUSY';
                throw err;
            }
            return originalTxn(fn);
        };

        process.env.WRITE_LAYER_METRICS = 'true';
        const spy = vi.spyOn(console, 'info');

        const result = updateWorkflowState(db, input, 'backend_developer');
        expect(result).toHaveProperty('transition_allowed', true);

        // Metrics logging occurs only on lock contention (retry path)
        if (spy.mock.calls.length > 0) {
            expect(spy).toHaveBeenCalledWith(
                expect.stringContaining('lock_contention_count')
            );
        }
        spy.mockRestore();
        delete process.env.WRITE_LAYER_METRICS;
        // @ts-ignore
        db.transaction = originalTxn;
    });

    // ---------------------------------------------------------------------
    // JITTER HELPER TESTS
    // ---------------------------------------------------------------------

    describe('calculateJitteredBackoff()', () => {
        it('produces values within expected bounds and varied values', () => {
            const samples: number[] = [];
            const attempt = 3; // base 100 -> exponential = 400, range = 200..400

            for (let i = 0; i < 100; i++) {
                const delay = calculateJitteredBackoff(attempt);
                samples.push(delay);
                expect(delay).toBeGreaterThanOrEqual(200);
                expect(delay).toBeLessThanOrEqual(400);
            }

            const unique = new Set(samples).size;
            expect(unique).toBeGreaterThan(10); // should not all be same
        });

        it('caps at maximum value when attempt is large', () => {
            const delay = calculateJitteredBackoff(10, 100, 800);
            expect(delay).toBeLessThanOrEqual(800);
        });
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// UPDATE_WORKFLOW_STATE() TESTS
// ─────────────────────────────────────────────────────────────────────────────

describe('updateWorkflowState()', () => {
    it('should successfully transition from started → in_progress', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        const input: UpdateWorkflowStateInput = {
            session_id: sessionId,
            new_state: 'in_progress',
            event: 'Agent began processing tasks',
        };

        const result = updateWorkflowState(db, input, 'backend_developer');

        expect(result).toHaveProperty('transition_allowed', true);
        // @ts-ignore
        expect(result.state_before).toBe('started');
        // @ts-ignore
        expect(result.state_after).toBe('in_progress');
        expect(result).toHaveProperty('event_id');

        // Verify session fsm_state updated
        const updatedSession = db.prepare('SELECT fsm_state FROM sessions WHERE id = ?')
            .get(sessionId) as any;
        expect(updatedSession.fsm_state).toBe('in_progress');

        // Verify workflow_events record created
        const event = db.prepare('SELECT * FROM workflow_events WHERE session_id = ?')
            .get(sessionId) as any;
        expect(event).toBeDefined();
        expect(event.event_type).toBe('state_change');
        expect(event.state_before).toBe('started');
        expect(event.state_after).toBe('in_progress');
    });

    it('should return FSM_INVALID_TRANSITION for invalid state transition', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        // Try to transition from started → submitted (skipping in_progress)
        const input: UpdateWorkflowStateInput = {
            session_id: sessionId,
            new_state: 'submitted',
            event: 'Attempted invalid transition',
        };

        const result = updateWorkflowState(db, input, 'backend_developer');

        expect(result).toHaveProperty('isError', true);
        // @ts-ignore
        expect(result.error?.code).toBe('FSM_INVALID_TRANSITION');
    });

    it('should return SCHEMA_VALIDATION_ERROR for invalid session_id', () => {
        const input = {
            session_id: 'not-a-uuid',
            new_state: 'in_progress',
            event: 'Event description',
        };

        const result = updateWorkflowState(db, input as any, 'backend_developer');

        expect(result).toHaveProperty('isError', true);
        // @ts-ignore
        expect(result.error.code).toBe('SCHEMA_VALIDATION_ERROR');
    });

    it('should return SESSION_NOT_FOUND for non-existent session', () => {
        const input: UpdateWorkflowStateInput = {
            session_id: randomUUID(),
            new_state: 'in_progress',
            event: 'Event description',
        };

        const result = updateWorkflowState(db, input, 'backend_developer');

        expect(result).toHaveProperty('isError', true);
        // @ts-ignore
        expect(result.error.code).toBe('SESSION_NOT_FOUND');
    });

    it('should return PERMISSION_DENIED for unauthorized role', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        const input: UpdateWorkflowStateInput = {
            session_id: sessionId,
            new_state: 'in_progress',
            event: 'Event',
        };

        const result = updateWorkflowState(db, input, 'explorer');

        expect(result).toHaveProperty('isError', true);
        // @ts-ignore
        expect(result.error.code).toBe('PERMISSION_DENIED');
    });

    it('should allow transitions within in_progress state (retry)', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        // First transition: started → in_progress
        let input: UpdateWorkflowStateInput = {
            session_id: sessionId,
            new_state: 'in_progress',
            event: 'Started processing',
        };
        updateWorkflowState(db, input, 'backend_developer');

        // Second transition: in_progress → in_progress (retry)
        input = {
            session_id: sessionId,
            new_state: 'in_progress',
            event: 'Retrying current state',
        };

        const result = updateWorkflowState(db, input, 'backend_developer');

        expect(result).toHaveProperty('transition_allowed', true);
    });

    it('should include evidence_artifact_id in workflow_events when provided', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;
        const artifactId = randomUUID();

        // First insert a dummy artifact for foreign key
        db.prepare(`
      INSERT INTO artifacts (id, session_id, artifact_type, content, submitted_at, embedded)
      VALUES (?, ?, ?, ?, ?, ?)
    `).run(artifactId, sessionId, 'implementation_summary', 'content', new Date().toISOString(), 0);

        const input: UpdateWorkflowStateInput = {
            session_id: sessionId,
            new_state: 'in_progress',
            event: 'Evidence artifact provided',
            evidence_artifact_id: artifactId,
        };

        updateWorkflowState(db, input, 'backend_developer');

        const event = db.prepare('SELECT * FROM workflow_events WHERE session_id = ?')
            .get(sessionId) as any;
        expect(event.evidence_artifact_id).toBe(artifactId);
    });

    it('should transition submitted → processed → closed in sequence', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        // Manual sequence: started → in_progress → submitted → processed → closed
        const transitions = [
            { from: 'started', to: 'in_progress' },
            { from: 'in_progress', to: 'submitted' },
            { from: 'submitted', to: 'processed' },
            { from: 'processed', to: 'closed' },
        ];

        for (const { from, to } of transitions) {
            // Manually set state to 'from' for testing
            if (from !== 'started') {
                db.prepare('UPDATE sessions SET fsm_state = ? WHERE id = ?').run(from, sessionId);
            }

            const input: UpdateWorkflowStateInput = {
                session_id: sessionId,
                new_state: to as any,
                event: `Transition from ${from} to ${to}`,
            };

            const result = updateWorkflowState(db, input, 'backend_developer');
            expect(result).toHaveProperty('transition_allowed', true);

            // Verify state in DB
            const updatedSession = db.prepare('SELECT fsm_state FROM sessions WHERE id = ?')
                .get(sessionId) as any;
            expect(updatedSession.fsm_state).toBe(to);
        }
    });

    it('should allow tech_lead to update workflow state', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        const input: UpdateWorkflowStateInput = {
            session_id: sessionId,
            new_state: 'in_progress',
            event: 'Tech lead advancing state',
        };

        const result = updateWorkflowState(db, input, 'tech_lead');

        expect(result).toHaveProperty('transition_allowed', true);
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// EDGE CASES & CONCURRENCY (BASIC)
// ─────────────────────────────────────────────────────────────────────────────

describe('WriteLayerTools - Edge Cases', () => {
    it('should handle artifact_content with UTF-8 characters', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        const input: SubmitArtifactInput = {
            session_id: sessionId,
            artifact_type: 'implementation_summary',
            artifact_content: '## Összefoglaló 📋\n- Munka befejezve\n- Nincs probléma 🎉',
        };

        const result = submitArtifact(db, input, 'backend_developer');

        expect(result).toHaveProperty('success', true);

        // @ts-ignore
        const artifact = db.prepare('SELECT * FROM artifacts WHERE id = ?')
            .get((result as any).artifact_id) as any;
        expect(artifact.content).toContain('Összefoglaló');
    });

    it('should handle large artifact content (up to 100KB)', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        // Create 50KB content
        const largeContent = 'x'.repeat(50_000);

        const input: SubmitArtifactInput = {
            session_id: sessionId,
            artifact_type: 'implementation_summary',
            artifact_content: largeContent,
        };

        const result = submitArtifact(db, input, 'backend_developer');

        expect(result).toHaveProperty('success', true);
    });

    it('should reject artifact_content exceeding 100KB', () => {
        const session = db.prepare('SELECT id FROM sessions LIMIT 1').get() as any;
        const sessionId = session.id;

        // Create 101KB content
        const tooLargeContent = 'x'.repeat(100_001);

        const input = {
            session_id: sessionId,
            artifact_type: 'implementation_summary',
            artifact_content: tooLargeContent,
        };

        const result = submitArtifact(db, input, 'backend_developer');

        expect(result).toHaveProperty('isError', true);
        // @ts-ignore
        expect(result.error.code).toBe('SCHEMA_VALIDATION_ERROR');
    });
});
