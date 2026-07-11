import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import Database from 'better-sqlite3';
import { WorkflowStateTracker } from '../../metadata/WorkflowStateTracker';
import { EventDispatcher, StateChangeEvent } from '../../metadata/EventDispatcher';
import { RbacFilter } from '../../mcp/RbacFilter';
import { TrackRouter } from '../../metadata/TrackRouter';
import { FSMTransitionError, SessionNotFoundError } from '../../metadata/types';
import { TrackImmutabilityError, AuthorizationError } from '../../metadata/TrackRouter';
import { AgentDb } from '../../mcp/AgentDb';
import * as path from 'path';
import * as fs from 'fs';

// Mock AgentDb a tesztekhez
class MockAgentDb implements Partial<AgentDb> {
    public __isMock: boolean = true;
    constructor(private roles: Record<string, string[]>) { }

    findSchemaByRoleName(role: string): any {
        const tools = this.roles[role] || [];
        return {
            mcp_tool_permissions: JSON.stringify(tools)
        };
    }

    getSchemaVersion(): number {
        return 1;
    }
}

describe('E2E System Integration - Workflow, RBAC, Dispatcher, Routing', () => {
    let tracker: WorkflowStateTracker;
    let dispatcher: EventDispatcher;
    let rbac: RbacFilter;
    let router: TrackRouter;
    let mockAgentDb: MockAgentDb;

    beforeEach(() => {
        tracker = new WorkflowStateTracker(':memory:');
        dispatcher = new EventDispatcher(tracker);

        // Setup mock DB for RBAC - using role names as keys
        mockAgentDb = new MockAgentDb({
            'explorer': ['discovery_tools', 'read_episodes'], // Csak discovery
            'executor': ['delivery_tools', 'read_episodes'],  // Csak delivery
            'lead': ['discovery_tools', 'delivery_tools'] // Mindkettő
        });
        rbac = new RbacFilter(mockAgentDb as unknown as AgentDb);

        router = new TrackRouter(tracker, rbac);
    });

    afterEach(() => {
        // No manual close needed for :memory: tracker in this context
    });

    describe('1. Happy Path: Full Workflow (AC-1, AC-2, AC-3)', () => {
        it('full workflow: session → FSM → RBAC → routing', async () => {
            const sessionId = 'e2e-happy-path-001';

            // 1. Create session (SZINKRON)
            const session = tracker.createSession({
                sessionId,
                domain: 'engineering',
                roleName: 'explorer',
                workflowId: 'default',
                track: 'discovery'
            });
            expect(session.state).toBe('initialized');

            // 2. FSM transition (ASZINKRON DISPATCHER-en keresztül)
            // Ehelyett hívhatnánk szinkron is, de a dispatcher az éles út
            await dispatcher.fireEvent({
                sessionId,
                fromState: 'initialized',
                toState: 'briefed',
                triggeredBy: 'orchestrator'
            });

            await dispatcher.fireEvent({
                sessionId,
                fromState: 'briefed',
                toState: 'in_progress',
                triggeredBy: 'orchestrator'
            });

            // Lekérdezzük a szinkron API-val
            const updated = tracker.getState(sessionId);
            expect(updated.state).toBe('in_progress');

            // 3. RBAC check (SZINKRON)
            const allowed: Set<string> = rbac.getAllowedTools('explorer');
            expect(allowed.has('discovery_tools')).toBe(true);
            expect(allowed.has('delivery_tools')).toBe(false);

            // 4. Routing
            const track = router.route(sessionId, 'discovery');
            expect(track).toBe('discovery');

            const finalState = tracker.getState(sessionId);
            expect(finalState.track).toBe('discovery');
            expect(finalState.trackLocked).toBe(true);

            // 5. Performance check
            const start = performance.now();
            tracker.getState(sessionId);
            const elapsed = performance.now() - start;
            expect(elapsed).toBeLessThan(10); // <10ms
        });
    });

    describe('2. Error Matrix (Biztonság és Hivatkozások)', () => {
        it('hibát dob Invalid FSM transition esetén (FSM_INVALID_TRANSITION)', async () => {
            tracker.createSession({ sessionId: 'err-1', domain: 'x', roleName: 'y', workflowId: 'default' });

            // initialized -> in_progress közvetlenül nem valid
            await expect(dispatcher.fireEvent({
                sessionId: 'err-1',
                fromState: 'initialized',
                toState: 'in_progress',
                triggeredBy: 'test'
            })).rejects.toThrow(FSMTransitionError);
        });

        it('hibát dob Permission Denied esetén a routing során (RBAC_PERMISSION_DENIED)', () => {
            const sessionId = 'err-2';
            tracker.createSession({ sessionId, domain: 'engineering', roleName: 'explorer', workflowId: 'default' });

            // Explorer nem tud delivery track-re lépni, mert nincs hozzá delivery_tools joga
            expect(() => {
                router.route(sessionId, 'delivery');
            }).toThrow(AuthorizationError);
        });

        it('hibát dob Context Expired / Invalid Session esetén (CTX_SESSION_EXPIRED)', () => {
            expect(() => {
                tracker.getState('non-existent-session-1234');
            }).toThrow(SessionNotFoundError);

            expect(() => {
                router.route('non-existent-session-1234', 'discovery');
            }).toThrow(SessionNotFoundError);
        });

        it('megakadályozza a terminális állapotból (submitted) történő route-olást', async () => {
            const sessionId = 'err-3';
            tracker.createSession({ sessionId, domain: 'engineering', roleName: 'lead', workflowId: 'default' });

            // Vigyük terminális állapotba
            tracker.updateState({ sessionId, newState: 'briefed', action: 'test' });
            tracker.updateState({ sessionId, newState: 'in_progress', action: 'test' });
            tracker.updateState({ sessionId, newState: 'ready_to_submit', action: 'test' });
            tracker.updateState({ sessionId, newState: 'submitted', action: 'test' });

            expect(() => {
                router.route(sessionId, 'delivery');
            }).toThrow(/is in terminal state/);
        });

        it('megakadályozza az immutabilitás megsértését (Delivery -> Discovery)', () => {
            const sessionId = 'err-4';
            tracker.createSession({ sessionId, domain: 'engineering', roleName: 'lead', workflowId: 'default' });

            router.route(sessionId, 'delivery');

            expect(() => {
                router.route(sessionId, 'discovery');
            }).toThrow(TrackImmutabilityError);
        });
    });

    describe('3. Concurrency / Load Testing (AC-4, Idempotency)', () => {
        it('supports 100 concurrent dispatcher events with P99 < 1s', async () => {
            // Létrehozunk 100 különböző session-t
            const sessions = Array.from({ length: 100 }, (_, i) => `load-session-${i}`);

            // Szinkron session létrehozás
            sessions.forEach(id => {
                tracker.createSession({ sessionId: id, domain: 'test', roleName: 'load-agent', workflowId: 'default' });
            });

            const start = performance.now();

            // Aszinkron event dispatching (100 párhuzamos Promise)
            const promises = sessions.map(async (sessionId) => {
                const eventStart = performance.now();
                await dispatcher.fireEvent({
                    sessionId,
                    fromState: 'initialized',
                    toState: 'briefed',
                    triggeredBy: 'load-test'
                });
                return performance.now() - eventStart;
            });

            const durations = await Promise.all(promises);
            const totalElapsed = performance.now() - start;

            // Kiszámítjuk a P99-et
            durations.sort((a, b) => a - b);
            const p99Index = Math.floor(durations.length * 0.99);
            const p99Duration = durations[p99Index];

            console.log(`Load Test Result: 100 concurrent events processed in ${totalElapsed.toFixed(2)}ms`);
            console.log(`P99 Latency: ${p99Duration.toFixed(2)}ms`);

            expect(p99Duration).toBeLessThan(1000); // P99 < 1s

            // Minden session-nek briefed állapotban kell lennie
            sessions.forEach(id => {
                expect(tracker.getState(id).state).toBe('briefed');
            });
        });
    });
});
