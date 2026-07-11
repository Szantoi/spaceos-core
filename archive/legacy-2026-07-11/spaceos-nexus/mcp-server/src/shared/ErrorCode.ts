/**
 * ErrorCode.ts — EPIC-11: Shared System Error Codes
 *
 * @file src/shared/ErrorCode.ts
 * @task TASK-11-03 (FSM Validator + Error Codes)
 * @epic EPIC-11 (Request Context Middleware, RBAC Migration & Error Standardization)
 * @author Dev A
 * @date 2026-03-08
 *
 * Shared enum of machine-readable error codes used across all EPIC-11 modules.
 * These codes are consumed by:
 *   - FSMTransitionValidator (Dev A)
 *   - ContextMiddleware (Dev A/B)
 *   - RbacFilter (Dev A)
 *   - WorkflowStateTracker (Dev C)
 *
 * Referenced by: AC-8 (TASK-11-03)
 */

/**
 * System-wide error code enum for EPIC-11.
 *
 * Format: <DOMAIN>_<NUMBER>
 *   FSM_*  — Finite State Machine transition errors
 *   RBAC_* — Role-Based Access Control errors
 *   CTX_*  — Request Context Middleware errors
 */
export enum ErrorCode {
    // ─────────────────────────────────────────────────────────────
    // FSM Validator errors (FSM_001 – FSM_009)
    // ─────────────────────────────────────────────────────────────

    /**
     * A state transition was attempted but is not in the allowed list.
     * @example 'briefed' → 'submitted' is not allowed
     */
    FSM_INVALID_TRANSITION = 'FSM_001',

    /**
     * A transition was attempted from a terminal state (submitted / failed).
     * Terminal states are immutable — no writes allowed.
     */
    FSM_TERMINAL_STATE = 'FSM_002',

    /**
     * The requested workflow_id was not found in workflow_definitions table.
     */
    FSM_MISSING_DEFINITION = 'FSM_003',

    /**
     * A state value provided for validation is not a member of FSMState.
     */
    FSM_INVALID_STATE_VALUE = 'FSM_004',

    /**
     * The session's retry counter exceeded MAX_RETRIES.
     * WorkflowStateTracker auto-transitions to 'abandoned'.
     */
    FSM_MAX_RETRIES_EXCEEDED = 'FSM_005',

    // ─────────────────────────────────────────────────────────────
    // RBAC errors (RBAC_001 – RBAC_009)
    // ─────────────────────────────────────────────────────────────

    /**
     * Agent role does not have permission to use the requested tool.
     */
    RBAC_PERMISSION_DENIED = 'RBAC_001',

    /**
     * The requested role was not found in the role_schemas table.
     */
    RBAC_ROLE_NOT_FOUND = 'RBAC_002',

    /**
     * The requested domain does not exist in the database.
     */
    RBAC_DOMAIN_NOT_FOUND = 'RBAC_003',

    // ─────────────────────────────────────────────────────────────
    // Context Middleware errors (CTX_001 – CTX_009)
    // ─────────────────────────────────────────────────────────────

    /**
     * The session referenced in the request has expired or been terminated.
     */
    CTX_SESSION_EXPIRED = 'CTX_001',

    /**
     * Required metadata (domain, role, session_id) is missing from context.
     */
    CTX_MISSING_METADATA = 'CTX_002',

    /**
     * The session_id provided is not a valid UUID.
     */
    CTX_INVALID_SESSION_ID = 'CTX_003',

    /**
     * Session context could not be found for the given session_id.
     */
    CTX_SESSION_NOT_FOUND = 'CTX_004',
}
