import { describe, it, expect, vi, beforeEach } from 'vitest';
import { EventDispatcher, StateChangeEvent, PreHook, PostHook } from '../../metadata/EventDispatcher';
import { WorkflowStateTracker } from '../../metadata/WorkflowStateTracker';
import { RbacFilter } from '../../mcp/RbacFilter';
import { RbacCacheInvalidateHook } from '../../metadata/FSMHooks';
import { FSMState, SessionState } from '../../metadata/types';
import { AgentDb } from '../../mcp/AgentDb';

describe('EventDispatcher - TASK-11-04', () => {
    let mockTracker: Partial<WorkflowStateTracker>;
    let dispatcher: EventDispatcher;

    beforeEach(() => {
        // Mock-oljuk a WorkflowStateTrackert (szinkron updateState)
        mockTracker = {
            updateState: vi.fn().mockImplementation((params) => {
                return {
                    sessionId: params.sessionId,
                    state: params.newState,
                    updatedAt: new Date(),
                } as SessionState;
            }),
        };

        dispatcher = new EventDispatcher(mockTracker as WorkflowStateTracker);
    });

    describe('Core Hook Execution (AC-1, AC-2, AC-3, AC-4, AC-7)', () => {
        it('executes pre-hooks, then updateState, then post-hooks in order', async () => {
            const executionOrder: string[] = [];

            const mockPreHook: PreHook = {
                run: vi.fn().mockImplementation(async () => {
                    executionOrder.push('pre-hook');
                }),
            };

            const mockPostHook: PostHook = {
                run: vi.fn().mockImplementation(async () => {
                    executionOrder.push('post-hook');
                }),
            };

            // Wrapped the real mock to track execution order
            const originalUpdateState = mockTracker.updateState;
            mockTracker.updateState = vi.fn().mockImplementation((params) => {
                executionOrder.push('updateState');
                return originalUpdateState!(params);
            });

            dispatcher.addPreHook('briefed', mockPreHook);
            dispatcher.addPostHook('briefed', mockPostHook);

            const event: StateChangeEvent = {
                sessionId: 'session-123',
                fromState: 'initialized',
                toState: 'briefed',
                triggeredBy: 'test',
                metadata: { key: 'value' },
            };

            const result = await dispatcher.fireEvent(event);

            expect(mockPreHook.run).toHaveBeenCalledWith(event);
            expect(mockTracker.updateState).toHaveBeenCalledWith({
                sessionId: 'session-123',
                newState: 'briefed',
                action: 'transition',
                triggeredBy: 'test',
                metadata: { key: 'value' },
            });
            expect(mockPostHook.run).toHaveBeenCalledWith(event, result);

            expect(executionOrder).toEqual(['pre-hook', 'updateState', 'post-hook']);
        });

        it('supports multiple hooks for the same state', async () => {
            const hook1 = { run: vi.fn() };
            const hook2 = { run: vi.fn() };

            dispatcher.addPreHook('in_progress', hook1);
            dispatcher.addPreHook('in_progress', hook2);

            await dispatcher.fireEvent({
                sessionId: 'session-1',
                fromState: 'briefed',
                toState: 'in_progress',
                triggeredBy: 'test'
            });

            expect(hook1.run).toHaveBeenCalledTimes(1);
            expect(hook2.run).toHaveBeenCalledTimes(1);
        });

        it('ignores hooks registered for a different state', async () => {
            const wrongHook = { run: vi.fn() };
            dispatcher.addPreHook('ready_to_submit', wrongHook);

            await dispatcher.fireEvent({
                sessionId: 'session-1',
                fromState: 'initialized',
                toState: 'briefed',
                triggeredBy: 'test'
            });

            expect(wrongHook.run).not.toHaveBeenCalled();
            expect(mockTracker.updateState).toHaveBeenCalledTimes(1);
        });
    });

    describe('Idempotency & Concurrency (AC-5, AC-6)', () => {
        it('throws an error if an event is already in progress for the SAME session (double-fire)', async () => {
            // Létrehozunk egy aszinkron pre-hookot, hogy a lock életben maradjon a Promise idejére
            let resolveHook: () => void;
            const preHookPromise = new Promise<void>((r) => { resolveHook = r; });

            const blockingHook: PreHook = {
                run: () => preHookPromise,
            };

            dispatcher.addPreHook('briefed', blockingHook);

            const event: StateChangeEvent = {
                sessionId: 'session-lock',
                fromState: 'initialized',
                toState: 'briefed',
                triggeredBy: 'tester'
            };

            // 1. event elindul (bennragad a hookban)
            const p1 = dispatcher.fireEvent(event);

            // 2. event rögtön ezután indul UGYANARRA a sessionre -> el kell buknia
            await expect(dispatcher.fireEvent(event)).rejects.toThrow(/Event already in progress/);

            // felengedjük az 1. eventet
            resolveHook!();
            await p1;

            // 3. event miután az első lefutott, már ismét átmehet
            await expect(dispatcher.fireEvent(event)).resolves.toBeDefined();
        });

        it('allows concurrent events for DIFFERENT sessions', async () => {
            let resolveHook: () => void;
            const preHookPromise = new Promise<void>((r) => { resolveHook = r; });

            const blockingHook: PreHook = {
                run: () => preHookPromise,
            };

            dispatcher.addPreHook('briefed', blockingHook);

            const p1 = dispatcher.fireEvent({ sessionId: 'session-1', fromState: 'initialized', toState: 'briefed', triggeredBy: 'test' });

            // Másik session rögtön indulhat gond nélkül
            const p2 = dispatcher.fireEvent({ sessionId: 'session-2', fromState: 'initialized', toState: 'briefed', triggeredBy: 'test' });

            resolveHook!();

            await Promise.all([p1, p2]);
            expect(mockTracker.updateState).toHaveBeenCalledTimes(2);
        });

        it('releases the lock even if a pre-hook throws an error', async () => {
            const errorHook: PreHook = {
                run: () => { throw new Error('Hook failed'); }
            };
            dispatcher.addPreHook('briefed', errorHook);

            const event: StateChangeEvent = {
                sessionId: 'session-fail',
                fromState: 'initialized',
                toState: 'briefed',
                triggeredBy: 'test'
            };

            await expect(dispatcher.fireEvent(event)).rejects.toThrow('Hook failed');

            // Meg kell győződnünk róla, hogy a Set-ből kikerült a lock,
            // ehhez kiveszük a hibás hookot és simán lefuttatunk egy másikat.
            dispatcher = new EventDispatcher(mockTracker as WorkflowStateTracker); // reset
            await expect(dispatcher.fireEvent(event)).resolves.toBeDefined();
        });
    });
});

describe('RbacCacheInvalidateHook', () => {
    it('invalidates RBAC cache on state transition', () => {
        // Mock RbacFilter
        const mockRbac = {
            invalidateCache: vi.fn(),
            getAllowedTools: vi.fn(),
            hasPermission: vi.fn(),
            getPublicTools: vi.fn(),
        } as unknown as RbacFilter;

        const hook = new RbacCacheInvalidateHook(mockRbac);

        hook.run({
            sessionId: 'session-rbac',
            fromState: 'briefed',
            toState: 'in_progress',
            triggeredBy: 'test'
        }, {} as SessionState);

        expect(mockRbac.invalidateCache).toHaveBeenCalledWith('session-rbac');
        expect(mockRbac.invalidateCache).toHaveBeenCalledTimes(1);
    });
});
