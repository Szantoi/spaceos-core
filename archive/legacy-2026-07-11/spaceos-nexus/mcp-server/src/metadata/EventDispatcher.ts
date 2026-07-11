import { WorkflowStateTracker } from './WorkflowStateTracker';
import { FSMState, SessionState } from './types';

export interface StateChangeEvent {
    sessionId: string;
    fromState: FSMState;
    toState: FSMState;
    triggeredBy: string;
    metadata?: Record<string, unknown>;
}

export interface PreHook {
    run(event: StateChangeEvent): Promise<void> | void;
}

export interface PostHook {
    run(event: StateChangeEvent, result: SessionState): Promise<void> | void;
}

export class EventDispatcher {
    private preHooks: Map<FSMState, PreHook[]> = new Map();
    private postHooks: Map<FSMState, PostHook[]> = new Map();

    // ⚠️ Egyszerű session-lock (async hook-ok és double-fire elkerülése miatt):
    private activeSessions = new Set<string>();

    constructor(private tracker: WorkflowStateTracker) { }

    addPreHook(state: FSMState, hook: PreHook) {
        if (!this.preHooks.has(state)) {
            this.preHooks.set(state, []);
        }
        this.preHooks.get(state)!.push(hook);
    }

    addPostHook(state: FSMState, hook: PostHook) {
        if (!this.postHooks.has(state)) {
            this.postHooks.set(state, []);
        }
        this.postHooks.get(state)!.push(hook);
    }

    async fireEvent(event: StateChangeEvent): Promise<SessionState> {
        const { sessionId } = event;

        // Idempotency guard: ha már fut erre a session-re, eldobjuk (AC-5)
        if (this.activeSessions.has(sessionId)) {
            throw new Error(`Event already in progress for session ${sessionId}`);
        }
        this.activeSessions.add(sessionId);

        try {
            // 1. Pre-hooks (lehetnek async)
            const preHooks = this.preHooks.get(event.toState) ?? [];
            for (const hook of preHooks) {
                await hook.run(event);
            }

            // 2. State change — ⚠️ SZINKRON! (better-sqlite3)
            const result = this.tracker.updateState({
                sessionId,
                newState: event.toState,
                action: 'transition', // Generic action name, updateState handles details
                triggeredBy: event.triggeredBy,
                metadata: event.metadata,
            });

            // 3. Post-hooks (lehetnek async, pl. RBAC cache invalidation)
            const postHooks = this.postHooks.get(event.toState) ?? [];
            for (const hook of postHooks) {
                await hook.run(event, result);
            }

            return result;
        } finally {
            this.activeSessions.delete(sessionId);
        }
    }
}
