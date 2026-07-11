/**
 * FSMTransitionValidator.ts — EPIC-11: FSM Transition Rule Enforcement
 *
 * @file src/metadata/FSMTransitionValidator.ts
 * @task TASK-11-03 (FSM Validator + Error Codes)
 * @epic EPIC-11 (Request Context Middleware, RBAC Migration & Error Standardization)
 * @author Dev A
 * @date 2026-03-08
 *
 * Responsibilities:
 *   1. Validate FSM state transitions against the VALID_TRANSITIONS map
 *   2. Enforce terminal-state immutability (submitted, failed → no transitions)
 *   3. Validate that state values are members of the FSMState union type
 *   4. Cache workflow rules for <5ms latency (NodeCache, stdTTL=3600, maxKeys=100)
 *   5. Provide detailed error context on validation failure
 *
 * IMPORTANT NOTES:
 *   - SYNCHRONOUS — better-sqlite3 is sync; do NOT use async/await
 *   - Imports from `./types` (types.ts by Dev C — SSOT for VALID_TRANSITIONS)
 *   - Uses NodeCache (node-cache@5.x), NOT lru-cache
 *   - Terminal states: 'submitted' and 'abandoned' (per types.ts SSOT, EPIC-11 spec)
 */

import NodeCache from 'node-cache';
import type { Database } from 'better-sqlite3';
import {
    FSMState,
    VALID_TRANSITIONS,
    TERMINAL_STATES,
} from './types';
import { ErrorCode } from '../shared/ErrorCode';

// ─────────────────────────────────────────────────────────────────────────────
// CUSTOM ERRORS
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Base class for all FSM validation errors.
 *
 * @example
 * throw new FSMValidationError('wf-1', 'out of range', 'State unknown', ErrorCode.FSM_INVALID_TRANSITION);
 */
export class FSMValidationError extends Error {
    public readonly name: string = 'FSMValidationError';

    constructor(
        /** Workflow identifier the error relates to. */
        public readonly workflowId: string,
        /** Machine-readable reason code. */
        public readonly reason: string,
        message: string,
        /** Shared system error code (from ErrorCode enum). */
        public readonly code: ErrorCode = ErrorCode.FSM_INVALID_TRANSITION,
    ) {
        super(message);
    }
}

/**
 * Raised when a state transition is explicitly forbidden.
 *
 * @example
 * throw new InvalidTransitionError('wf-1', 'submitted', 'in_progress', ErrorCode.FSM_INVALID_TRANSITION);
 */
export class InvalidTransitionError extends FSMValidationError {
    public override readonly name = 'InvalidTransitionError';

    constructor(
        workflowId: string,
        /** Current state of the session. */
        public readonly currentState: FSMState,
        /** The requested (forbidden) target state. */
        public readonly requestedState: FSMState,
        reason: string,
        code: ErrorCode = ErrorCode.FSM_INVALID_TRANSITION,
    ) {
        super(
            workflowId,
            reason,
            `Cannot transition '${currentState}' → '${requestedState}' in workflow '${workflowId}'. ${reason}`,
            code,
        );
    }
}

/**
 * Raised when a workflow definition cannot be found in the database.
 */
export class WorkflowDefinitionNotFoundError extends FSMValidationError {
    public override readonly name = 'WorkflowDefinitionNotFoundError';

    constructor(workflowId: string) {
        super(
            workflowId,
            'not_found',
            `Workflow definition '${workflowId}' not found in database.`,
            ErrorCode.FSM_MISSING_DEFINITION,
        );
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// INTERNAL TYPES
// ─────────────────────────────────────────────────────────────────────────────

interface WorkflowRules {
    workflowId: string;
    states: FSMState[];
    transitions: Record<string, FSMState[]>;
}

// ─────────────────────────────────────────────────────────────────────────────
// VALIDATOR CLASS
// ─────────────────────────────────────────────────────────────────────────────

/**
 * FSMTransitionValidator — enforces FSM transition rules.
 *
 * Primary validation strategy:
 *   1. Terminal states (submitted, failed) are ALWAYS forbidden from transitioning.
 *   2. State-level rules from `VALID_TRANSITIONS` (types.ts SSOT) are applied.
 *   3. If a workflowId is provided, workflow-specific rules from the DB
 *      override the global map (allows per-workflow customization).
 *
 * Caching:
 *   - NodeCache with stdTTL=3600s, maxKeys=100, useClones=false
 *   - Cache is populated lazily on first access per workflowId
 *   - `clearCache()` available for tests and schema migrations
 *
 * @example
 * const validator = new FSMTransitionValidator(db);
 * validator.validateTransition('agent-delivery-v1', 'briefed', 'in_progress'); // OK
 * validator.validateTransition('agent-delivery-v1', 'submitted', 'in_progress'); // throws
 */
export class FSMTransitionValidator {
    private readonly cache = new NodeCache({ stdTTL: 3600, maxKeys: 100, useClones: false });

    /**
     * @param db - Optional better-sqlite3 Database instance.
     *   If provided, workflow-specific DB rules are used.
     *   If omitted, only the global VALID_TRANSITIONS map is used.
     */
    constructor(private readonly db?: Database) { }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    /**
     * Validates if a state transition is allowed.
     *
     * Throws {@link InvalidTransitionError} if forbidden.
     * Returns void if valid (call-and-forget pattern).
     *
     * @param workflowId - Workflow identifier (for DB lookup + error context)
     * @param currentState - The session's current FSMState
     * @param requestedState - The desired next FSMState
     *
     * @throws {InvalidTransitionError} Transition is forbidden
     * @throws {WorkflowDefinitionNotFoundError} workflowId not in DB (when db provided)
     *
     * @example
     * validator.validateTransition('wf-1', 'briefed', 'in_progress'); // ✅
     * validator.validateTransition('wf-1', 'submitted', 'in_progress'); // ❌ throws
     */
    public validateTransition(
        workflowId: string,
        currentState: FSMState,
        requestedState: FSMState,
    ): void {
        // AC-4: validate state enum before anything else
        if (!this.validateStateEnum(currentState)) {
            throw new InvalidTransitionError(
                workflowId,
                currentState,
                requestedState,
                `'${currentState}' is not a valid FSMState.`,
            );
        }
        if (!this.validateStateEnum(requestedState)) {
            throw new InvalidTransitionError(
                workflowId,
                currentState,
                requestedState,
                `'${requestedState}' is not a valid FSMState.`,
            );
        }

        // AC-7: terminal states are immutable — always reject
        if (TERMINAL_STATES.has(currentState)) {
            throw new InvalidTransitionError(
                workflowId,
                currentState,
                requestedState,
                `State '${currentState}' is terminal — no further transitions allowed.`,
                ErrorCode.FSM_TERMINAL_STATE,
            );
        }

        // Determine allowed transitions:
        // If DB available → use workflow-specific rules (AC-2 / AC-5)
        // Otherwise → fall back to global VALID_TRANSITIONS map (AC-6)
        const allowed = this.db
            ? this.getWorkflowRules(workflowId).transitions[currentState] ?? []
            : (VALID_TRANSITIONS[currentState] ?? []);

        // AC-6 / AC-8 / AC-9: check the transition is in the allowed list
        if (!allowed.includes(requestedState)) {
            throw new InvalidTransitionError(
                workflowId,
                currentState,
                requestedState,
                `Transition not allowed. Allowed targets: [${allowed.join(', ')}].`,
            );
        }
    }

    /**
     * Returns all valid successor states from the given state.
     *
     * When a DB instance is available, workflow-specific rules take precedence.
     * Falls back to the global map if workflowId is unknown.
     *
     * @param workflowId - Workflow identifier
     * @param currentState - The current FSMState
     * @returns Ordered list of allowed next states (empty for terminal)
     *
     * @example
     * validator.getValidTransitions('wf-1', 'in_progress');
     * // → ['in_progress', 'awaiting_input', 'ready_to_submit', 'abandoned']
     */
    public getValidTransitions(workflowId: string, currentState: FSMState): FSMState[] {
        if (this.db) {
            try {
                const rules = this.getWorkflowRules(workflowId);
                return rules.transitions[currentState] ?? [];
            } catch {
                // DB lookup failed — degrade gracefully to global map
            }
        }
        return [...(VALID_TRANSITIONS[currentState] ?? [])];
    }

    /**
     * Runtime type guard — checks if a string value is a valid FSMState.
     *
     * @param state - Value to validate
     * @returns true if the value is a valid FSMState member
     *
     * @example
     * validator.validateStateEnum('in_progress'); // → true
     * validator.validateStateEnum('COMPLETED');   // → false
     */
    public validateStateEnum(state: string): state is FSMState {
        // Derive valid states from VALID_TRANSITIONS keys (SSOT)
        return Object.keys(VALID_TRANSITIONS).includes(state);
    }

    /**
     * Clears the in-memory NodeCache.
     * Use in tests or after schema migrations.
     */
    public clearCache(): void {
        this.cache.flushAll();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    /**
     * Loads workflow rules from the DB or returns the cached version.
     *
     * Cache key = workflowId. TTL = 3600s. Max = 100 entries.
     *
     * @throws {WorkflowDefinitionNotFoundError} if workflowId not in workflow_definitions
     */
    private getWorkflowRules(workflowId: string): WorkflowRules {
        const cached = this.cache.get<WorkflowRules>(workflowId);
        if (cached !== undefined) return cached;

        if (!this.db) {
            throw new WorkflowDefinitionNotFoundError(workflowId);
        }

        const definition = this.db
            .prepare('SELECT states FROM workflow_definitions WHERE workflow_id = ?')
            .get(workflowId) as { states: string } | undefined;

        if (!definition) {
            throw new WorkflowDefinitionNotFoundError(workflowId);
        }

        const statesArr = JSON.parse(definition.states) as FSMState[];

        const stateNodes = this.db
            .prepare(
                'SELECT state_name, valid_transitions FROM fsm_state_transitions WHERE workflow_id = ? ORDER BY state_order',
            )
            .all(workflowId) as Array<{ state_name: string; valid_transitions: string }>;

        const transitions: Record<string, FSMState[]> = {};
        for (const node of stateNodes) {
            transitions[node.state_name] = JSON.parse(node.valid_transitions) as FSMState[];
        }

        const rules: WorkflowRules = { workflowId, states: statesArr, transitions };
        this.cache.set(workflowId, rules);
        return rules;
    }
}
