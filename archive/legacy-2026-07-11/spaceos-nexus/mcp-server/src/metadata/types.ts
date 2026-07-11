// ==========================================================================
// EPIC-11: FSM Types — WorkflowStateTracker
// ==========================================================================
// Date: 2026-03-11
// Author: Dev C
// Task: TASK-11-02
// ==========================================================================

import { WorkflowTrack } from './FSMTypes';

/**
 * The 7 canonical states of the EPIC-11 agent workflow FSM.
 * Terminal states: 'submitted', 'abandoned'.
 * Ref: EPIC-11-FSM-MODEL.md
 */
export type FSMState =
    | 'initialized'
    | 'briefed'
    | 'in_progress'
    | 'awaiting_input'
    | 'ready_to_submit'
    | 'submitted'
    | 'abandoned';

/** The set of terminal states from which no further transitions are allowed. */
export const TERMINAL_STATES: ReadonlySet<FSMState> = new Set<FSMState>([
    'submitted',
    'abandoned',
]);

/**
 * The valid FSM transition map.
 * Key: current state; Value: array of allowed next states.
 * Ref: EPIC-11-FSM-MODEL.md State Transition Matrix
 */
export const VALID_TRANSITIONS: Readonly<Record<FSMState, FSMState[]>> = {
    initialized: ['briefed', 'abandoned'],
    briefed: ['in_progress', 'abandoned'],
    in_progress: ['in_progress', 'awaiting_input', 'ready_to_submit', 'abandoned'],
    awaiting_input: ['in_progress', 'ready_to_submit', 'abandoned'],
    ready_to_submit: ['submitted', 'in_progress', 'abandoned'],
    submitted: [],
    abandoned: [],
};

// --------------------------------------------------------------------------
// Transfer Objects
// --------------------------------------------------------------------------

/**
 * Represents a single snapshot of a session's current FSM state.
 */
export interface SessionState {
    sessionId: string;
    state: FSMState;
    workflowId: string;
    domain: string;
    roleName: string;
    track: 'discovery' | 'delivery' | null;
    trackLocked: boolean;
    createdAt: Date;
    updatedAt: Date;
}

/**
 * A single entry in the session_history audit trail.
 */
export interface StateTransitionHistory {
    id: number;
    sessionId: string;
    fromState: FSMState;
    toState: FSMState;
    action: string;
    metadata: Record<string, unknown> | null;
    timestamp: Date;
}

/**
 * The resumption context returned by getResumeContext().
 * Contains everything bootstrap_agent needs to restore an agent's workflow.
 */
export interface ResumptionContext {
    sessionId: string;
    currentState: FSMState;
    workflowId: string;
    domain: string;
    roleName: string;
    taskProgress: {
        completedSteps: string[];
        previousToolResults: Record<string, unknown>;
    };
    /** Raw JSON string for embedding directly in the bootstrap_agent response payload. */
    resumePayload: string;
}

/**
 * Parameters for creating a new tracked session.
 */
export interface CreateSessionParams {
    sessionId: string;
    domain: string;
    roleName: string;
    workflowId: string;
    /** Optional execution track; if omitted, database may default or derive from domain/role */
    track?: WorkflowTrack;
}

/**
 * Parameters for transitioning a session's FSM state.
 */
export interface UpdateStateParams {
    sessionId: string;
    newState: FSMState;
    /** Short action label, e.g. "briefed", "started", "paused", "resumed", "failed" */
    action: string;
    /** Optional metadata stored in session_history for audit purposes. */
    metadata?: Record<string, unknown>;
    /** Optional actor that triggered the transition (user / system / tool) */
    triggeredBy?: string;
}

/**
 * Thrown when an FSM state transition is not permitted by VALID_TRANSITIONS.
 */
export class FSMTransitionError extends Error {
    constructor(
        public readonly fromState: FSMState,
        public readonly toState: FSMState,
        public readonly sessionId: string,
    ) {
        super(
            `Invalid FSM transition: '${fromState}' → '${toState}' is not allowed for session '${sessionId}'.`,
        );
        this.name = 'FSMTransitionError';
    }
}

/**
 * Thrown when a session cannot be found for the given sessionId.
 */
export class SessionNotFoundError extends Error {
    constructor(public readonly sessionId: string) {
        super(`Session not found: '${sessionId}'.`);
        this.name = 'SessionNotFoundError';
    }
}
