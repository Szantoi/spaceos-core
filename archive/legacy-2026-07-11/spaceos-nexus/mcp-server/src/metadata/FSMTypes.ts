/**
 * FSMTypes.ts — EPIC-11: FSM Schema & Data Model
 *
 * @file src/metadata/FSMTypes.ts
 * @task TASK-11-01 (FSM Schema & Data Model — SQLite Migration)
 * @epic EPIC-11 (Request Context Middleware, RBAC Migration & Error Standardization)
 * @author Dev A
 * @date 2026-03-08
 *
 * This module defines the canonical TypeScript types for the 7-state finite
 * state machine used in EPIC-11 agent workflow tracking.
 *
 * Tables covered:
 *   - agent_sessions   (active session state)
 *   - workflow_definitions (workflow blueprints)
 *   - fsm_state_transitions (per-workflow valid transitions)
 *   - session_history  (audit trail of state changes)
 *
 * Migration file: src/metadata/migrations/005_epic11_agent_session_v2.sql
 */

// ─────────────────────────────────────────────────────────────────────────────
// FSM STATE ENUM
// ─────────────────────────────────────────────────────────────────────────────

/**
 * The 7 canonical states of the EPIC-11 agent workflow FSM.
 *
 * State lifecycle:
 *   initialized → briefed → in_progress → ready_to_submit → submitted (terminal)
 *                                       ↘ awaiting_input ↗
 *                                       ↘ abandoned (terminal)
 *
 * Terminal states: 'submitted', 'abandoned'
 * Self-loop allowed: 'in_progress' → 'in_progress' (retry, max 3)
 */
export type FSMState =
    | 'initialized'       // Session created, no work started
    | 'briefed'           // Workflow seed loaded, agent is ready
    | 'in_progress'       // Agent actively executing workflow steps
    | 'awaiting_input'    // Blocked — needs user clarification
    | 'ready_to_submit'   // All steps complete, pending final approval
    | 'submitted'         // Final deliverable locked (TERMINAL)
    | 'abandoned';        // Gave up or hard error (TERMINAL)

// ─────────────────────────────────────────────────────────────────────────────
// FSM STATE CATEGORIZATION
// ─────────────────────────────────────────────────────────────────────────────

/** Terminal states — no further transitions are allowed from these states. */
export const TERMINAL_STATES: ReadonlySet<FSMState> = new Set<FSMState>([
    'submitted',
    'abandoned',
]);

/** States where the agent is actively doing work. */
export const ACTIVE_STATES: ReadonlySet<FSMState> = new Set<FSMState>([
    'initialized',
    'briefed',
    'in_progress',
    'awaiting_input',
    'ready_to_submit',
]);

/** States where the agent is paused awaiting external input. */
export const PAUSED_STATES: ReadonlySet<FSMState> = new Set<FSMState>([
    'awaiting_input',
]);

/** States where a self-transition (retry loop) is permitted. */
export const SELF_LOOP_ALLOWED: ReadonlySet<FSMState> = new Set<FSMState>([
    'in_progress',
]);

/** Initial state for any newly created session. */
export const INITIAL_FSM_STATE: FSMState = 'initialized';

// ─────────────────────────────────────────────────────────────────────────────
// FSM TRANSITION MAP
// ─────────────────────────────────────────────────────────────────────────────

/**
 * The canonical FSM transition map for EPIC-11 agent workflows.
 *
 * Key: current state
 * Value: array of allowed next states
 *
 * Rules:
 *  - No backward transitions
 *  - Terminal states have no outgoing transitions
 *  - in_progress → in_progress is the self-loop/retry mechanism
 *  - After 3 retries the WorkflowStateTracker auto-transitions to 'abandoned'
 */
export const FSM_TRANSITIONS: Readonly<Record<FSMState, FSMState[]>> = {
    initialized: ['briefed', 'abandoned'],
    briefed: ['in_progress', 'abandoned'],
    in_progress: ['in_progress', 'awaiting_input', 'ready_to_submit', 'abandoned'],
    awaiting_input: ['in_progress', 'abandoned'],
    ready_to_submit: ['submitted', 'in_progress', 'abandoned'],
    submitted: [],
    abandoned: [],
} as const;

/** Maximum number of retry self-loops before auto-abandonment. */
export const MAX_RETRIES = 3;

// ─────────────────────────────────────────────────────────────────────────────
// WORKFLOW TRACK
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Workflow execution track.
 *
 * - 'discovery': Observation, define, ideate, prototype, test phases
 * - 'delivery':  Planning, implementation, QA, review, sign-off phases
 */
export type WorkflowTrack = 'discovery' | 'delivery';

// ─────────────────────────────────────────────────────────────────────────────
// DATABASE ROW INTERFACES
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Represents a row in the `workflow_definitions` table.
 *
 * Stores the blueprint for a workflow: which states are valid,
 * which track it belongs to, and human-readable metadata.
 *
 * @example
 * const def: WorkflowDefinition = {
 *   workflow_id: 'epic-lifecycle-v1',
 *   name: 'Epic Agile Lifecycle',
 *   track: 'delivery',
 *   states: JSON.stringify(['initialized', 'briefed', 'in_progress', ...]),
 *   version: '1.0',
 *   created_at: '2026-03-08T00:00:00Z',
 * };
 */
export interface WorkflowDefinition {
    /** Unique workflow identifier (e.g., 'discovery-workflow-v1'). */
    workflow_id: string;
    /** Human-readable name. */
    name: string;
    /** Execution track — 'discovery' or 'delivery'. */
    track: WorkflowTrack;
    /** JSON-encoded ordered list of valid FSMState values for this workflow. */
    states: string;
    /** Semantic version string. */
    version: string;
    /** ISO 8601 creation timestamp. */
    created_at: string;
}

/**
 * Represents a row in the `fsm_state_transitions` table.
 *
 * Defines, per workflow, the allowed state transitions out of each state.
 */
export interface WorkflowStateRule {
    /** Foreign key → workflow_definitions.workflow_id */
    workflow_id: string;
    /** The state this rule describes. */
    state_name: FSMState;
    /** 1-based ordering of this state within the workflow. */
    state_order: number;
    /** JSON-encoded array of valid successor FSMState values. */
    valid_transitions: string;
}

/**
 * Represents a row in the `agent_sessions` table.
 *
 * Tracks all active and completed agent workflow sessions.
 * Once in a terminal state (submitted/abandoned), the row is immutable.
 *
 * @example
 * const session: AgentSession = {
 *   session_id: '550e8400-e29b-41d4-a716-446655440000',
 *   domain: 'engineering',
 *   role_name: 'backend_developer',
 *   workflow_id: 'epic-lifecycle-v1',
 *   current_state: 'in_progress',
 *   retry_count: 0,
 *   last_action: 'briefed',
 *   completed_at: null,
 *   created_at: '2026-03-11T09:00:00Z',
 *   updated_at: '2026-03-11T09:05:00Z',
 * };
 */
export interface AgentSession {
    /** UUID — primary key, crypto-strong from EPIC-10 SessionManager. */
    session_id: string;
    /** Role domain (e.g., 'engineering', 'management'). */
    domain: string;
    /** Role name (e.g., 'backend_developer', 'tech_lead'). */
    role_name: string;
    /** Foreign key → workflow_definitions.workflow_id */
    workflow_id: string;
    /** Workflow track (discovery/delivery) — used for RBAC filtering. */
    track: WorkflowTrack | null;
    /** Current FSM state. */
    current_state: FSMState;
    /** Number of in_progress→in_progress self-loops (retry counter). */
    retry_count: number;
    /** Human-readable action label from the last state transition. */
    last_action: string | null;
    /** ISO 8601 timestamp set when entering a terminal state. */
    completed_at: string | null;
    /** ISO 8601 creation timestamp. */
    created_at: string;
    /** ISO 8601 last-updated timestamp. */
    updated_at: string;
}

/**
 * Represents a row in the `session_history` table.
 *
 * Immutable audit log of every FSM state transition for a session.
 *
 * @example
 * const entry: SessionHistoryEntry = {
 *   id: 1,
 *   session_id: '550e8400-e29b-41d4-a716-446655440000',
 *   state_before: 'initialized',
 *   state_after: 'briefed',
 *   action: 'bootstrap_agent',
 *   metadata: '{"workflow": "epic-lifecycle-v1"}',
 *   timestamp: '2026-03-11T09:05:00Z',
 * };
 */
export interface SessionHistoryEntry {
    /** Auto-incrementing primary key. */
    id: number;
    /** Foreign key → agent_sessions.session_id */
    session_id: string;
    /** State before transition. */
    state_before: FSMState;
    /** State after transition. */
    state_after: FSMState;
    /** Human-readable trigger label (e.g., 'bootstrap_agent', 'retry', 'user_feedback'). */
    action: string;
    /** Optional JSON string with additional context (e.g., error details, retry attempt). */
    metadata: string | null;
    /** ISO 8601 timestamp. */
    timestamp: string;
}

// ─────────────────────────────────────────────────────────────────────────────
// DOMAIN ERRORS
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Thrown when an FSM state transition is not permitted.
 *
 * @example
 * throw new FSMTransitionError('submitted', 'in_progress', 'session-uuid');
 * // → Error: Invalid FSM transition: 'submitted' → 'in_progress' ...
 */
export class FSMTransitionError extends Error {
    public readonly name = 'FSMTransitionError';

    constructor(
        public readonly fromState: FSMState,
        public readonly toState: FSMState,
        public readonly sessionId: string,
    ) {
        super(
            `Invalid FSM transition: '${fromState}' → '${toState}' is not allowed for session '${sessionId}'.`,
        );
    }
}

/**
 * Thrown when an operation targets a terminal (immutable) session.
 *
 * @example
 * throw new FSMTerminalStateError('submitted', 'session-uuid');
 */
export class FSMTerminalStateError extends Error {
    public readonly name = 'FSMTerminalStateError';

    constructor(
        public readonly state: FSMState,
        public readonly sessionId: string,
    ) {
        super(
            `Session '${sessionId}' is in terminal state '${state}' and cannot be modified.`,
        );
    }
}

/**
 * Thrown when the maximum number of retry self-loops is exceeded.
 *
 * @example
 * throw new FSMMaxRetriesError('session-uuid', 3);
 */
export class FSMMaxRetriesError extends Error {
    public readonly name = 'FSMMaxRetriesError';

    constructor(
        public readonly sessionId: string,
        public readonly retryCount: number,
    ) {
        super(
            `Session '${sessionId}' exceeded maximum retries (${retryCount}/${MAX_RETRIES}). Auto-transitioning to 'abandoned'.`,
        );
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// UTILITY FUNCTIONS
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Type guard — checks if a value is a valid FSMState string.
 *
 * @param value - Value to check
 * @returns true if value is a valid FSMState
 *
 * @example
 * isFSMState('in_progress'); // → true
 * isFSMState('unknown');     // → false
 */
export function isFSMState(value: unknown): value is FSMState {
    const validStates: FSMState[] = [
        'initialized', 'briefed', 'in_progress',
        'awaiting_input', 'ready_to_submit', 'submitted', 'abandoned',
    ];
    return typeof value === 'string' && (validStates as string[]).includes(value);
}

/**
 * Checks if a state transition is valid according to the FSM definition.
 *
 * Note: This validates the global FSM ruleset. For workflow-specific
 * validation (using fsm_state_transitions table), use WorkflowStateTracker.
 *
 * @param from - Current state
 * @param to - Requested next state
 * @returns true if the transition is allowed
 *
 * @example
 * isValidFSMTransition('briefed', 'in_progress'); // → true
 * isValidFSMTransition('submitted', 'in_progress'); // → false (terminal)
 */
export function isValidFSMTransition(from: FSMState, to: FSMState): boolean {
    return FSM_TRANSITIONS[from].includes(to);
}

/**
 * Checks if a state is terminal (no further transitions possible).
 *
 * @param state - State to check
 * @returns true if the state is terminal
 *
 * @example
 * isTerminalFSMState('submitted'); // → true
 * isTerminalFSMState('in_progress'); // → false
 */
export function isTerminalFSMState(state: FSMState): boolean {
    return TERMINAL_STATES.has(state);
}

/**
 * Returns all valid successor states for a given state.
 *
 * @param state - Current state
 * @returns Array of allowed next states (empty if terminal)
 *
 * @example
 * getValidTransitions('in_progress');
 * // → ['in_progress', 'awaiting_input', 'ready_to_submit', 'abandoned']
 */
export function getValidTransitions(state: FSMState): FSMState[] {
    return [...FSM_TRANSITIONS[state]];
}
