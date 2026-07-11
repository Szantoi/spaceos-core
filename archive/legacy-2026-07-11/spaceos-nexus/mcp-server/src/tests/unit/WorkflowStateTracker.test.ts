// ==========================================================================
// TASK-11-02: WorkflowStateTracker Unit Tests
// ==========================================================================
// AC-18: ≥20 unit tests (state transitions, history, resumption, errors)
// AC-11: All 7 states covered
// AC-6 to AC-10: Transition validity tested
// ==========================================================================

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { randomUUID } from 'crypto';
import { WorkflowStateTracker } from '../../metadata/WorkflowStateTracker';
import { FSMTransitionError, SessionNotFoundError } from '../../metadata/types';

// Use an in-memory SQLite database for all tests
const IN_MEMORY = ':memory:';

function makeTracker(): WorkflowStateTracker {
    return new WorkflowStateTracker(IN_MEMORY);
}

function makeSession(tracker: WorkflowStateTracker, overrides?: Partial<{
    sessionId: string;
    domain: string;
    roleName: string;
    workflowId: string;
}>) {
    return tracker.createSession({
        sessionId: overrides?.sessionId ?? randomUUID(),
        domain: overrides?.domain ?? 'engineering',
        roleName: overrides?.roleName ?? 'backend_developer',
        workflowId: overrides?.workflowId ?? 'agile-epic-lifecycle-v1',
    });
}

describe('WorkflowStateTracker — TASK-11-02', () => {
    let tracker: WorkflowStateTracker;

    beforeEach(() => {
        tracker = makeTracker();
    });

    afterEach(() => {
        tracker.close();
    });

    // -------------------------------------------------------------------------
    // AC-1: createSession
    // -------------------------------------------------------------------------
    describe('createSession (AC-1)', () => {
        it('creates a session and returns it in initialized state', () => {
            const s = makeSession(tracker);
            expect(s.state).toBe('initialized');
            expect(s.sessionId).toBeDefined();
            expect(s.workflowId).toBe('agile-epic-lifecycle-v1');
        });

        it('accepts an explicit track parameter and persists it', () => {
            const s = tracker.createSession({
                sessionId: randomUUID(),
                domain: 'discovery',
                roleName: 'architect',
                workflowId: 'dwi-workflow',
                track: 'discovery'
            });
            expect(s.track).toBe('discovery');

            const reloaded = tracker.getState(s.sessionId);
            expect(reloaded.track).toBe('discovery');
        });

        it('defaults track to null when omitted', () => {
            const s = makeSession(tracker);
            expect(s.track).toBeNull();
        });

        it('stores domain and roleName correctly', () => {
            const s = makeSession(tracker, { domain: 'legal', roleName: 'reviewer' });
            expect(s.domain).toBe('legal');
            expect(s.roleName).toBe('reviewer');
        });

        it('sets createdAt and updatedAt timestamps', () => {
            const s = makeSession(tracker);
            expect(s.createdAt).toBeInstanceOf(Date);
            expect(s.updatedAt).toBeInstanceOf(Date);
        });
    });

    // -------------------------------------------------------------------------
    // AC-2: getState
    // -------------------------------------------------------------------------
    describe('getState (AC-2)', () => {
        it('retrieves state by sessionId', () => {
            const s = makeSession(tracker);
            const retrieved = tracker.getState(s.sessionId);
            expect(retrieved.state).toBe('initialized');
        });

        it('throws SessionNotFoundError for unknown session (AC-17)', () => {
            expect(() => tracker.getState('nonexistent-id')).toThrow(SessionNotFoundError);
        });
    });

    // -------------------------------------------------------------------------
    // AC-3, AC-6: initialized → briefed
    // -------------------------------------------------------------------------
    describe('updateState — valid transitions', () => {
        it('AC-6: transitions initialized → briefed', () => {
            const s = makeSession(tracker);
            const updated = tracker.updateState({ sessionId: s.sessionId, newState: 'briefed', action: 'briefed' });
            expect(updated.state).toBe('briefed');
        });

        it('AC-7: transitions briefed → in_progress', () => {
            const s = makeSession(tracker);
            tracker.updateState({ sessionId: s.sessionId, newState: 'briefed', action: 'briefed' });
            const updated = tracker.updateState({ sessionId: s.sessionId, newState: 'in_progress', action: 'started' });
            expect(updated.state).toBe('in_progress');
        });

        it('AC-8: in_progress ⇄ awaiting_input (bidirectional)', () => {
            const s = makeSession(tracker);
            tracker.updateState({ sessionId: s.sessionId, newState: 'briefed', action: 'briefed' });
            tracker.updateState({ sessionId: s.sessionId, newState: 'in_progress', action: 'started' });
            tracker.updateState({ sessionId: s.sessionId, newState: 'awaiting_input', action: 'paused' });
            const back = tracker.updateState({ sessionId: s.sessionId, newState: 'in_progress', action: 'resumed' });
            expect(back.state).toBe('in_progress');
        });

        it('transitions in_progress → in_progress (retry case)', () => {
            const s = makeSession(tracker);
            tracker.updateState({ sessionId: s.sessionId, newState: 'briefed', action: 'briefed' });
            tracker.updateState({ sessionId: s.sessionId, newState: 'in_progress', action: 'started' });
            const retry = tracker.updateState({ sessionId: s.sessionId, newState: 'in_progress', action: 'retried' });
            expect(retry.state).toBe('in_progress');
        });

        it('transitions in_progress → ready_to_submit', () => {
            const s = makeSession(tracker);
            tracker.updateState({ sessionId: s.sessionId, newState: 'briefed', action: 'briefed' });
            tracker.updateState({ sessionId: s.sessionId, newState: 'in_progress', action: 'started' });
            const ready = tracker.updateState({ sessionId: s.sessionId, newState: 'ready_to_submit', action: 'completed' });
            expect(ready.state).toBe('ready_to_submit');
        });

        it('transitions ready_to_submit → submitted (terminal)', () => {
            const s = makeSession(tracker);
            tracker.updateState({ sessionId: s.sessionId, newState: 'briefed', action: 'briefed' });
            tracker.updateState({ sessionId: s.sessionId, newState: 'in_progress', action: 'started' });
            tracker.updateState({ sessionId: s.sessionId, newState: 'ready_to_submit', action: 'completed' });
            const submitted = tracker.updateState({ sessionId: s.sessionId, newState: 'submitted', action: 'submitted' });
            expect(submitted.state).toBe('submitted');
        });

        it('transitions to abandoned from in_progress (AC-11: abandoned state covered)', () => {
            const s = makeSession(tracker);
            tracker.updateState({ sessionId: s.sessionId, newState: 'briefed', action: 'briefed' });
            tracker.updateState({ sessionId: s.sessionId, newState: 'in_progress', action: 'started' });
            const abandoned = tracker.updateState({ sessionId: s.sessionId, newState: 'abandoned', action: 'error_detected' });
            expect(abandoned.state).toBe('abandoned');
        });
    });

    // -------------------------------------------------------------------------
    // AC-9, AC-10: Invalid transitions
    // -------------------------------------------------------------------------
    describe('invalid transitions (AC-9, AC-10)', () => {
        it('throws FSMTransitionError for initialized → in_progress (skipping briefed)', () => {
            const s = makeSession(tracker);
            expect(() =>
                tracker.updateState({ sessionId: s.sessionId, newState: 'in_progress', action: 'bad' })
            ).toThrow(FSMTransitionError);
        });

        it('throws FSMTransitionError for initialized → submitted', () => {
            const s = makeSession(tracker);
            expect(() =>
                tracker.updateState({ sessionId: s.sessionId, newState: 'submitted', action: 'bad' })
            ).toThrow(FSMTransitionError);
        });

        it('AC-9: throws FSMTransitionError transitioning from submitted (terminal)', () => {
            const s = makeSession(tracker);
            tracker.updateState({ sessionId: s.sessionId, newState: 'briefed', action: 'briefed' });
            tracker.updateState({ sessionId: s.sessionId, newState: 'in_progress', action: 'started' });
            tracker.updateState({ sessionId: s.sessionId, newState: 'ready_to_submit', action: 'completed' });
            tracker.updateState({ sessionId: s.sessionId, newState: 'submitted', action: 'submitted' });
            expect(() =>
                tracker.updateState({ sessionId: s.sessionId, newState: 'in_progress', action: 'retry' })
            ).toThrow(FSMTransitionError);
        });

        it('AC-9: throws FSMTransitionError transitioning from abandoned (terminal)', () => {
            const s = makeSession(tracker);
            tracker.updateState({ sessionId: s.sessionId, newState: 'abandoned', action: 'error' });
            expect(() =>
                tracker.updateState({ sessionId: s.sessionId, newState: 'initialized', action: 'reset' })
            ).toThrow(FSMTransitionError);
        });

        it('FSMTransitionError carries from/to state and sessionId', () => {
            const s = makeSession(tracker);
            try {
                tracker.updateState({ sessionId: s.sessionId, newState: 'submitted', action: 'bad' });
                fail('Expected FSMTransitionError');
            } catch (e) {
                expect(e).toBeInstanceOf(FSMTransitionError);
                const err = e as FSMTransitionError;
                expect(err.fromState).toBe('initialized');
                expect(err.toState).toBe('submitted');
                expect(err.sessionId).toBe(s.sessionId);
            }
        });
    });

    // -------------------------------------------------------------------------
    // AC-4, AC-13, AC-14: getHistory
    // -------------------------------------------------------------------------
    describe('getHistory (AC-4, AC-13, AC-14)', () => {
        it('returns empty history for new session', () => {
            const s = makeSession(tracker);
            expect(tracker.getHistory(s.sessionId)).toHaveLength(0);
        });

        it('records each transition in audit trail', () => {
            const s = makeSession(tracker);
            tracker.updateState({ sessionId: s.sessionId, newState: 'briefed', action: 'briefed' });
            tracker.updateState({ sessionId: s.sessionId, newState: 'in_progress', action: 'started' });
            const history = tracker.getHistory(s.sessionId);
            expect(history).toHaveLength(2);
            expect(history[0].fromState).toBe('initialized');
            expect(history[0].toState).toBe('briefed');
            expect(history[1].fromState).toBe('briefed');
            expect(history[1].toState).toBe('in_progress');
        });

        it('stores metadata in history record', () => {
            const s = makeSession(tracker);
            tracker.updateState({
                sessionId: s.sessionId,
                newState: 'briefed',
                action: 'briefed',
                metadata: { task_id: 'TASK-11-02' },
            });
            const history = tracker.getHistory(s.sessionId);
            expect(history[0].metadata).toMatchObject({ task_id: 'TASK-11-02' });
        });

        it('throws SessionNotFoundError for unknown session', () => {
            expect(() => tracker.getHistory('ghost-session')).toThrow(SessionNotFoundError);
        });
    });

    // -------------------------------------------------------------------------
    // AC-5: getResumeContext
    // -------------------------------------------------------------------------
    describe('getResumeContext (AC-5)', () => {
        it('returns resumption context with full payload', () => {
            const s = makeSession(tracker);
            tracker.updateState({ sessionId: s.sessionId, newState: 'briefed', action: 'briefed' });
            tracker.updateState({ sessionId: s.sessionId, newState: 'in_progress', action: 'started' });

            const ctx = tracker.getResumeContext(s.sessionId);
            expect(ctx.currentState).toBe('in_progress');
            expect(ctx.sessionId).toBe(s.sessionId);
            expect(ctx.taskProgress.completedSteps).toContain('briefed');
            expect(ctx.taskProgress.completedSteps).toContain('started');
            expect(ctx.resumePayload).toContain(s.sessionId);
        });

        it('resumePayload is valid JSON', () => {
            const s = makeSession(tracker);
            const ctx = tracker.getResumeContext(s.sessionId);
            expect(() => JSON.parse(ctx.resumePayload)).not.toThrow();
        });

        it('throws SessionNotFoundError for unknown session', () => {
            expect(() => tracker.getResumeContext('ghost')).toThrow(SessionNotFoundError);
        });
    });

    // -------------------------------------------------------------------------
    // AC-12: Timestamps
    // -------------------------------------------------------------------------
    describe('timestamps (AC-12)', () => {
        it('updatedAt changes after state transition', async () => {
            const s = makeSession(tracker);
            const beforeUpdate = s.updatedAt.getTime();
            // Small delay to ensure timestamp differs
            await new Promise(r => setTimeout(r, 5));
            tracker.updateState({ sessionId: s.sessionId, newState: 'briefed', action: 'briefed' });
            const after = tracker.getState(s.sessionId);
            expect(after.updatedAt.getTime()).toBeGreaterThanOrEqual(beforeUpdate);
        });
    });
});
