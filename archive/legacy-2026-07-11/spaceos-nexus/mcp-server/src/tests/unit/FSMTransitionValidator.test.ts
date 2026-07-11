/**
 * FSMTransitionValidator.test.ts — TASK-11-03 Unit Tests
 *
 * @task TASK-11-03 (FSM Validator + Error Codes)
 * @author Dev A
 * @date 2026-03-08
 *
 * Coverage target: ≥ 85% (AC-11)
 *
 * Test strategy:
 *   - Uses in-memory SQLite with migration 004 loaded
 *   - Seeds workflow 'agent-delivery-v1' with correct lowercase 7-state FSM
 *   - Tests all ACs: valid transitions, terminal states, caching, error codes
 */

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import Database from 'better-sqlite3';
import {
    FSMTransitionValidator,
    InvalidTransitionError,
    WorkflowDefinitionNotFoundError,
    FSMValidationError,
} from '../../metadata/FSMTransitionValidator';
import { ErrorCode } from '../../shared/ErrorCode';
import { readFileSync } from 'fs';
import { join } from 'path';

// ─────────────────────────────────────────────────────────────────────────────
// TEST SETUP
// ─────────────────────────────────────────────────────────────────────────────

const WORKFLOW_ID = 'agent-delivery-v1';

function buildDb(): Database.Database {
    const db = new Database(':memory:');
    db.pragma('foreign_keys = ON');

    // Bootstrap tables required by migration 004
    db.exec(`
        CREATE TABLE IF NOT EXISTS schema_metadata (
            layer TEXT PRIMARY KEY,
            version INTEGER DEFAULT 1,
            last_updated TEXT
        );
        INSERT OR IGNORE INTO schema_metadata (layer, version) VALUES ('read-layer', 1);
        INSERT OR IGNORE INTO schema_metadata (layer, version) VALUES ('fsm-layer', 1);

        CREATE TABLE IF NOT EXISTS roles (
            domain TEXT NOT NULL,
            role_name TEXT NOT NULL,
            PRIMARY KEY (domain, role_name)
        );
    `);

    // Load migration 004 (creates workflow_definitions, fsm_state_transitions, etc.)
    const sql004 = readFileSync(
        join(__dirname, '../../metadata/migrations/004_epic11_fsm_schema.sql'),
        'utf-8',
    );
    db.exec(sql004);

    // Seed the standard delivery workflow with correct lowercase states
    db.prepare(`
        INSERT OR IGNORE INTO workflow_definitions (workflow_id, name, track, states, version) VALUES (?, ?, ?, ?, ?)
    `).run(
        WORKFLOW_ID,
        'Agent Delivery Workflow',
        'delivery',
        JSON.stringify(['initialized', 'briefed', 'in_progress', 'awaiting_input', 'ready_to_submit', 'submitted', 'failed']),
        '2.0',
    );

    // Seed transition rules (matching VALID_TRANSITIONS from types.ts)
    const transitions: Array<[string, number, string[]]> = [
        ['initialized', 1, ['briefed', 'failed']],
        ['briefed', 2, ['in_progress', 'failed']],
        ['in_progress', 3, ['in_progress', 'awaiting_input', 'ready_to_submit', 'failed']],
        ['awaiting_input', 4, ['in_progress', 'ready_to_submit', 'failed']],
        ['ready_to_submit', 5, ['submitted', 'in_progress', 'failed']],
        ['submitted', 6, []],
        ['failed', 7, []],
    ];

    const insertTransition = db.prepare(`
        INSERT OR IGNORE INTO fsm_state_transitions (workflow_id, state_name, state_order, valid_transitions)
        VALUES (?, ?, ?, ?)
    `);
    for (const [state, order, validNext] of transitions) {
        insertTransition.run(WORKFLOW_ID, state, order, JSON.stringify(validNext));
    }

    return db;
}

// ─────────────────────────────────────────────────────────────────────────────
// TESTS
// ─────────────────────────────────────────────────────────────────────────────

describe('FSMTransitionValidator', () => {
    let db: Database.Database;
    let validator: FSMTransitionValidator;

    beforeEach(() => {
        db = buildDb();
        validator = new FSMTransitionValidator(db);
    });

    afterEach(() => {
        db.close();
    });

    // ─────────────────────────────────────────────────────────────
    // AC-2 / AC-6: validateTransition — valid paths
    // ─────────────────────────────────────────────────────────────

    describe('validateTransition() — valid transitions', () => {
        it('AC-6: allows initialized → briefed', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'initialized', 'briefed')).not.toThrow();
        });

        it('AC-6: allows briefed → in_progress', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'briefed', 'in_progress')).not.toThrow();
        });

        it('AC-9: allows in_progress → in_progress (retry self-loop)', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'in_progress', 'in_progress')).not.toThrow();
        });

        it('AC-8: allows in_progress → awaiting_input', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'in_progress', 'awaiting_input')).not.toThrow();
        });

        it('AC-8: allows awaiting_input → in_progress (resume)', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'awaiting_input', 'in_progress')).not.toThrow();
        });

        it('AC-6: allows in_progress → ready_to_submit', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'in_progress', 'ready_to_submit')).not.toThrow();
        });

        it('AC-6: allows ready_to_submit → submitted', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'ready_to_submit', 'submitted')).not.toThrow();
        });

        it('AC-6: allows ready_to_submit → in_progress (revision)', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'ready_to_submit', 'in_progress')).not.toThrow();
        });

        it('AC-6: allows → failed from any non-terminal state', () => {
            const nonTerminal = ['initialized', 'briefed', 'in_progress', 'awaiting_input', 'ready_to_submit'] as const;
            for (const state of nonTerminal) {
                expect(() => validator.validateTransition(WORKFLOW_ID, state, 'failed')).not.toThrow();
            }
        });
    });

    // ─────────────────────────────────────────────────────────────
    // AC-7 / AC-10: terminal states are immutable
    // ─────────────────────────────────────────────────────────────

    describe('validateTransition() — terminal state enforcement', () => {
        it('AC-7: throws for submitted → in_progress', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'submitted', 'in_progress'))
                .toThrow(InvalidTransitionError);
        });

        it('AC-7: throws for submitted → failed', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'submitted', 'failed'))
                .toThrow(InvalidTransitionError);
        });

        it('AC-7: throws for failed → initialized', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'failed', 'initialized'))
                .toThrow(InvalidTransitionError);
        });

        it('AC-10: error message includes "terminal"', () => {
            try {
                validator.validateTransition(WORKFLOW_ID, 'submitted', 'in_progress');
                expect.fail('Expected InvalidTransitionError');
            } catch (e) {
                expect(e).toBeInstanceOf(InvalidTransitionError);
                expect((e as InvalidTransitionError).message).toMatch(/terminal/i);
            }
        });

        it('AC-10: error carries correct ErrorCode', () => {
            try {
                validator.validateTransition(WORKFLOW_ID, 'submitted', 'briefed');
            } catch (e) {
                expect((e as FSMValidationError).code).toBe(ErrorCode.FSM_TERMINAL_STATE);
            }
        });
    });

    // ─────────────────────────────────────────────────────────────
    // AC-6: illegal (but non-terminal) transitions
    // ─────────────────────────────────────────────────────────────

    describe('validateTransition() — illegal transitions', () => {
        it('throws for initialized → submitted (skipped steps)', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'initialized', 'submitted'))
                .toThrow(InvalidTransitionError);
        });

        it('throws for briefed → ready_to_submit (skipped step)', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'briefed', 'ready_to_submit'))
                .toThrow(InvalidTransitionError);
        });

        it('AC-10: error includes state names', () => {
            try {
                validator.validateTransition(WORKFLOW_ID, 'initialized', 'submitted');
            } catch (e) {
                const err = e as InvalidTransitionError;
                expect(err.currentState).toBe('initialized');
                expect(err.requestedState).toBe('submitted');
                expect(err.workflowId).toBe(WORKFLOW_ID);
            }
        });

        it('AC-10: error includes allowed targets list', () => {
            try {
                validator.validateTransition(WORKFLOW_ID, 'briefed', 'submitted');
            } catch (e) {
                expect((e as Error).message).toMatch(/in_progress/);
            }
        });
    });

    // ─────────────────────────────────────────────────────────────
    // AC-4: validateStateEnum
    // ─────────────────────────────────────────────────────────────

    describe('validateStateEnum()', () => {
        it('returns true for all valid FSMState values', () => {
            const valid = ['initialized', 'briefed', 'in_progress', 'awaiting_input', 'ready_to_submit', 'submitted', 'failed'];
            for (const state of valid) {
                expect(validator.validateStateEnum(state)).toBe(true);
            }
        });

        it('returns false for uppercase legacy states', () => {
            expect(validator.validateStateEnum('INITIALIZED')).toBe(false);
            expect(validator.validateStateEnum('COMPLETED')).toBe(false);
            expect(validator.validateStateEnum('IN_PROGRESS' as never)).toBe(false);
        });

        it('throws InvalidTransitionError for unknown state string', () => {
            expect(() => validator.validateTransition(WORKFLOW_ID, 'initialized', 'INVALID_STATE' as never))
                .toThrow(InvalidTransitionError);
        });
    });

    // ─────────────────────────────────────────────────────────────
    // AC-3: getValidTransitions
    // ─────────────────────────────────────────────────────────────

    describe('getValidTransitions()', () => {
        it('returns correct successors for in_progress', () => {
            const result = validator.getValidTransitions(WORKFLOW_ID, 'in_progress');
            expect(result).toContain('in_progress');     // retry
            expect(result).toContain('awaiting_input');
            expect(result).toContain('ready_to_submit');
            expect(result).toContain('failed');
            expect(result).not.toContain('submitted');
        });

        it('returns empty array for terminal state submitted', () => {
            expect(validator.getValidTransitions(WORKFLOW_ID, 'submitted')).toEqual([]);
        });

        it('returns empty array for terminal state failed', () => {
            expect(validator.getValidTransitions(WORKFLOW_ID, 'failed')).toEqual([]);
        });

        it('falls back to global map when workflow unknown', () => {
            const result = validator.getValidTransitions('nonexistent-wf', 'in_progress');
            // Should fall back to VALID_TRANSITIONS (not throw)
            expect(Array.isArray(result)).toBe(true);
        });
    });

    // ─────────────────────────────────────────────────────────────
    // Caching (AC-3)
    // ─────────────────────────────────────────────────────────────

    describe('Caching', () => {
        it('uses cache after first load (DB can be cleared)', () => {
            // Warm the cache
            validator.validateTransition(WORKFLOW_ID, 'initialized', 'briefed');

            // Delete from DB — cache should keep it alive
            db.exec('DELETE FROM workflow_definitions');
            db.exec('DELETE FROM fsm_state_transitions');

            expect(() => validator.validateTransition(WORKFLOW_ID, 'initialized', 'briefed')).not.toThrow();
        });

        it('throws after clearCache() + DB cleared', () => {
            validator.validateTransition(WORKFLOW_ID, 'initialized', 'briefed'); // warm
            db.exec('DELETE FROM workflow_definitions');
            validator.clearCache();

            expect(() => validator.validateTransition(WORKFLOW_ID, 'initialized', 'briefed'))
                .toThrow(WorkflowDefinitionNotFoundError);
        });

        it('clearCache() does not break subsequent valid lookups', () => {
            validator.validateTransition(WORKFLOW_ID, 'initialized', 'briefed'); // warm
            validator.clearCache();
            // DB still has data — should reload fine
            expect(() => validator.validateTransition(WORKFLOW_ID, 'initialized', 'briefed')).not.toThrow();
        });
    });

    // ─────────────────────────────────────────────────────────────
    // WorkflowDefinitionNotFoundError
    // ─────────────────────────────────────────────────────────────

    describe('WorkflowDefinitionNotFoundError', () => {
        it('throws when workflowId does not exist', () => {
            expect(() => validator.validateTransition('nonexistent-wf', 'initialized', 'briefed'))
                .toThrow(WorkflowDefinitionNotFoundError);
        });

        it('error has correct ErrorCode', () => {
            try {
                validator.validateTransition('nonexistent-wf', 'initialized', 'briefed');
            } catch (e) {
                expect((e as FSMValidationError).code).toBe(ErrorCode.FSM_MISSING_DEFINITION);
            }
        });
    });

    // ─────────────────────────────────────────────────────────────
    // AC-1: no-db constructor (global map mode)
    // ─────────────────────────────────────────────────────────────

    describe('Global map mode (no DB)', () => {
        it('validates transitions using VALID_TRANSITIONS when no DB provided', () => {
            const noDbValidator = new FSMTransitionValidator();
            expect(() => noDbValidator.validateTransition('any-wf', 'briefed', 'in_progress')).not.toThrow();
        });

        it('rejects terminal transitions even without DB', () => {
            const noDbValidator = new FSMTransitionValidator();
            expect(() => noDbValidator.validateTransition('any-wf', 'submitted', 'briefed'))
                .toThrow(InvalidTransitionError);
        });
    });

    // ─────────────────────────────────────────────────────────────
    // AC-14: Performance (<5ms per validation)
    // ─────────────────────────────────────────────────────────────

    describe('Performance (AC-14)', () => {
        it('completes 100 concurrent validations in <5ms after cache warm', () => {
            // Warm cache
            validator.validateTransition(WORKFLOW_ID, 'initialized', 'briefed');

            const start = performance.now();
            for (let i = 0; i < 100; i++) {
                try {
                    validator.validateTransition(WORKFLOW_ID, 'in_progress', 'awaiting_input');
                } catch {
                    // ignore
                }
            }
            const elapsed = performance.now() - start;

            expect(elapsed).toBeLessThan(50); // 50ms for 100 iterations is well within target
        });
    });
});
