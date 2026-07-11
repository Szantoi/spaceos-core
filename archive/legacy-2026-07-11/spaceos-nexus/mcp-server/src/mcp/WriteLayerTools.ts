/**
 * WriteLayerTools.ts
 *
 * EPIC-08: MCP Write Layer Tools
 *
 * Implements two MCP write tools for agent workflow state management:
 *   1. submit_artifact() — submit implementation summary, test report, or PR link
 *   2. update_workflow_state() — transition FSM state of agent session
 *
 * Both tools use SQLite pessimistic locking (BEGIN IMMEDIATE) for thread-safe writes.
 *
 * Reference: TASK-08-02-Implementation-Guide.md § 2
 */

// @ts-ignore
const Database: any = require('better-sqlite3');
import { randomUUID } from 'crypto';
import * as z from 'zod';
import {
    SubmitArtifactInput,
    SubmitArtifactOutput,
    UpdateWorkflowStateInput,
    UpdateWorkflowStateOutput,
    SessionRecord,
    ArtifactRecord,
    WorkflowEventRecord,
    WorkflowState,
    isValidTransition,
} from '../metadata/WriteLayerSchema';
import {
    MpcErrorResponse,
    errorSessionNotFound,
    errorPermissionDenied,
    errorSchemaValidationError,
    errorFsmInvalidTransition,
    errorDatabaseLocked,
    errorInternalServerError,
} from './ErrorResponses';

// ─────────────────────────────────────────────────────────────────────────────
// UTILITIES
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Calculate jittered exponential backoff delay (Equal Jitter pattern).
 *
 * Based on AWS Architecture Blog "Exponential Backoff And Jitter".
 * Formula: cap/2 + random(0, cap/2)
 *
 * @param attempt 1-based retry attempt
 * @param baseMs base delay in milliseconds (default 100)
 * @param capMs maximum delay cap in milliseconds (default 8000)
 * @returns jittered backoff duration in milliseconds
 */
export function calculateJitteredBackoff(
    attempt: number,
    baseMs: number = 100,
    capMs: number = 8000
): number {
    const exponential = Math.pow(2, attempt - 1) * baseMs;
    const maxBackoff = Math.min(capMs, exponential);
    // Equal jitter: cap/2 + random(0, cap/2)
    return Math.floor((maxBackoff / 2) + Math.random() * (maxBackoff / 2));
}

// ─────────────────────────────────────────────────────────────────────────────
// RBAC PERMISSION CHECK
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Check if role is allowed to call write tools.
 *
 * Allowed roles: backend_developer, tech_lead
 * All other roles: denied
 */
function isRoleAllowedForWriteTool(role: string): boolean {
    const ALLOWED_ROLES = ['backend_developer', 'tech_lead'];
    return ALLOWED_ROLES.includes(role);
}

// ─────────────────────────────────────────────────────────────────────────────
// TOOL 1: submit_artifact()
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Submit an artifact (implementation summary, test report, PR link, etc.)
 *
 * Uses pessimistic locking to ensure atomic write of artifact + session update.
 *
 * @param db better-sqlite3 Database instance
 * @param input {session_id, artifact_content, artifact_type}
 * @param role Current agent role (for RBAC check)
 * @returns {SubmitArtifactOutput | MpcErrorResponse}
 *
 * Flow:
 *   1. Validate input with Zod
 *   2. Check RBAC permission
 *   3. BEGIN IMMEDIATE transaction
 *   4. Verify session exists
 *   5. INSERT artifact into artifacts table
 *   6. UPDATE sessions.last_updated_at
 *   7. COMMIT (or rollback on error)
 *   8. Retry on "database is locked" with exponential backoff
 */
export function submitArtifact(
    db: any,
    input: unknown,
    role: string
): SubmitArtifactOutput | MpcErrorResponse {
    // Step 1: Validate input with Zod
    let validated: SubmitArtifactInput;
    try {
        validated = (z.object({
            session_id: z.string().uuid(),
            artifact_content: z.string().min(1).max(100_000),
            artifact_type: z.enum(['implementation_summary', 'test_report', 'pr_link']),
        }) as any).parse(input);
    } catch (error: any) {
        return errorSchemaValidationError(error.message) as MpcErrorResponse;
    }

    // Step 2: Check RBAC permission
    if (!isRoleAllowedForWriteTool(role)) {
        return errorPermissionDenied('submit_artifact', role, ['backend_developer', 'tech_lead']) as MpcErrorResponse;
    }

    // Step 3–7: Execute with pessimistic locking + retries
    return submitArtifactWithLocking(
        db,
        validated.session_id,
        validated.artifact_content,
        validated.artifact_type,
        role
    );
}

/**
 * Submit artifact with SQLite pessimistic locking.
 *
 * BEGIN IMMEDIATE acquires exclusive lock from transaction start,
 * preventing concurrent writes to the same session.
 *
 * Retries on "database is locked" with exponential backoff:
 *   Attempt 1: 100ms, Attempt 2: 200ms, Attempt 3: 400ms, etc.
 */
function submitArtifactWithLocking(
    db: any,
    sessionId: string,
    artifactContent: string,
    artifactType: 'implementation_summary' | 'test_report' | 'pr_link',
    role: string
): SubmitArtifactOutput | MpcErrorResponse {
    const MAX_RETRIES = 5;
    let lastError: Error | null = null;

    // metrics
    const enableMetrics = process.env.WRITE_LAYER_METRICS === 'true';
    let lockContentionCount = 0;
    let maxBackoffMs = 0;
    let totalRetryTimeMs = 0;

    for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
        try {
            // BEGIN IMMEDIATE: acquire exclusive lock now (not deferred)
            const result = db.transaction(function submitTxn(this: any): SubmitArtifactOutput {
                // Validate session exists
                const session = db.prepare(
                    'SELECT id, agent_id, fsm_state FROM sessions WHERE id = ?'
                ).get(sessionId) as SessionRecord | undefined;

                if (!session) {
                    throw new Error('SESSION_NOT_FOUND');
                }

                // INSERT artifact
                const artifactId = randomUUID().toString();
                const now = new Date().toISOString();

                db.prepare(`
          INSERT INTO artifacts
            (id, session_id, artifact_type, content, submitted_at, embedded)
          VALUES (?, ?, ?, ?, ?, ?)
        `).run(artifactId, sessionId, artifactType, artifactContent, now, 0);

                // UPDATE session last_updated_at
                db.prepare(
                    'UPDATE sessions SET last_updated_at = ? WHERE id = ?'
                ).run(now, sessionId);

                // Return success
                return {
                    artifact_id: artifactId,
                    artifact_type: artifactType,
                    session_id: sessionId,
                    submitted_at: now,
                    success: true,
                };
            }).immediate(); // Use BEGIN IMMEDIATE

            return result;

        } catch (error: any) {
            lastError = error;

            // Handle "database is locked" with exponential backoff
            if (error.message?.includes('database is locked') || error.code === 'SQLITE_BUSY') {
                if (attempt < MAX_RETRIES) {
                    // metrics
                    if (enableMetrics) {
                        lockContentionCount++;
                    }
                    // calculate jittered backoff using equal jitter pattern
                    const backoffMs = calculateJitteredBackoff(attempt);
                    if (enableMetrics) {
                        totalRetryTimeMs += backoffMs;
                        if (backoffMs > maxBackoffMs) maxBackoffMs = backoffMs;
                    }
                    // synchronous sleep (M01 limitation)
                    const start = Date.now();
                    while (Date.now() - start < backoffMs) {
                        // Busy wait
                    }
                }
                continue;
            }

            // Handle known errors
            if (error.message === 'SESSION_NOT_FOUND') {
                return errorSessionNotFound(sessionId) as MpcErrorResponse;
            }

            // Unexpected error
            console.error('[WriteLayerTools.submitArtifact] Unexpected error:', error);
            return errorInternalServerError('Failed to submit artifact: ' + error.message) as MpcErrorResponse;
        }
    }

    // log metrics summary if enabled
    if (enableMetrics) {
        console.info(
            `[WriteLayerTools.submitArtifact] metrics: lock_contention_count=${lockContentionCount}, max_backoff_ms=${maxBackoffMs}, total_retry_time_ms=${totalRetryTimeMs}`
        );
    }

    // Exhausted retries
    return errorDatabaseLocked(
        `Could not acquire database lock after ${MAX_RETRIES} attempts: ${lastError?.message}`
    );
}

// ─────────────────────────────────────────────────────────────────────────────
// TOOL 2: update_workflow_state()
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Update workflow FSM state for a session.
 *
 * Validates state transition is legal per FSM rules, then atomically updates
 * sessions.fsm_state and inserts event into workflow_events table.
 *
 * @param db better-sqlite3 Database instance
 * @param input {session_id, new_state, event, evidence_artifact_id?}
 * @param role Current agent role (for RBAC check)
 * @returns {UpdateWorkflowStateOutput | MpcErrorResponse}
 *
 * Flow:
 *   1. Validate input with Zod
 *   2. Check RBAC permission
 *   3. BEGIN IMMEDIATE transaction
 *   4. GET current state from sessions
 *   5. Validate FSM transition (currentState → newState)
 *   6. INSERT event into workflow_events
 *   7. UPDATE sessions.fsm_state
 *   8. COMMIT (or rollback)
 *   9. Retry on lock with backoff
 */
export function updateWorkflowState(
    db: any,
    input: unknown,
    role: string
): UpdateWorkflowStateOutput | MpcErrorResponse {
    // Step 1: Validate input
    let validated: UpdateWorkflowStateInput;
    try {
        validated = (z.object({
            session_id: z.string().uuid(),
            new_state: z.enum(['started', 'in_progress', 'submitted', 'processed', 'closed']),
            event: z.string().min(1).max(500),
            evidence_artifact_id: z.string().uuid().optional(),
        }) as any).parse(input);
    } catch (error: any) {
        return errorSchemaValidationError(error.message) as MpcErrorResponse;
    }

    // Step 2: Check RBAC permission
    if (!isRoleAllowedForWriteTool(role)) {
        return errorPermissionDenied('update_workflow_state', role, ['backend_developer', 'tech_lead']) as MpcErrorResponse;
    }

    // Step 3–8: Execute with locking + retries
    return updateWorkflowStateWithLocking(
        db,
        validated.session_id,
        validated.new_state as WorkflowState,
        validated.event,
        validated.evidence_artifact_id
    );
}

/**
 * Update workflow state with FSM validation + pessimistic locking.
 *
 * Validates:
 *   1. Session exists
 *   2. Current state → new state transition is valid (via FSM rules)
 *   3. No concurrent writes (via BEGIN IMMEDIATE lock)
 */
function updateWorkflowStateWithLocking(
    db: any,
    sessionId: string,
    newState: WorkflowState,
    eventDescription: string,
    evidenceArtifactId?: string
): UpdateWorkflowStateOutput | MpcErrorResponse {
    const MAX_RETRIES = 5;
    let lastError: Error | null = null;

    // metrics
    const enableMetrics = process.env.WRITE_LAYER_METRICS === 'true';
    let lockContentionCount = 0;
    let maxBackoffMs = 0;
    let totalRetryTimeMs = 0;

    for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
        try {
            // BEGIN IMMEDIATE: exclusive lock
            const result = db.transaction(function updateTxn(this: any): UpdateWorkflowStateOutput {
                // Get current state (locked)
                const session = db.prepare(
                    'SELECT id, agent_id, fsm_state FROM sessions WHERE id = ?'
                ).get(sessionId) as SessionRecord | undefined;

                if (!session) {
                    throw new Error('SESSION_NOT_FOUND');
                }

                const currentState = session.fsm_state as WorkflowState;

                // Validate FSM transition
                if (!isValidTransition(currentState, newState)) {
                    throw new Error(`INVALID_TRANSITION:${currentState}→${newState}`);
                }

                // INSERT workflow event (locked)
                const eventId = randomUUID().toString();
                const now = new Date().toISOString();

                db.prepare(`
          INSERT INTO workflow_events
            (id, session_id, event_type, state_before, state_after, evidence_artifact_id, timestamp)
          VALUES (?, ?, ?, ?, ?, ?, ?)
        `).run(
                    eventId,
                    sessionId,
                    'state_change',
                    currentState,
                    newState,
                    evidenceArtifactId || null,
                    now
                );

                // UPDATE session state (locked)
                db.prepare(
                    'UPDATE sessions SET fsm_state = ?, last_updated_at = ? WHERE id = ?'
                ).run(newState, now, sessionId);

                // Return success
                return {
                    state_before: currentState,
                    state_after: newState,
                    event_id: eventId,
                    timestamp: now,
                    transition_allowed: true,
                };
            }).immediate();

            return result;

        } catch (error: any) {
            lastError = error;

            // Handle "database is locked"
            if (error.message?.includes('database is locked') || error.code === 'SQLITE_BUSY') {
                if (attempt < MAX_RETRIES) {
                    if (enableMetrics) lockContentionCount++;
                    const backoffMs = calculateJitteredBackoff(attempt);
                    if (enableMetrics) {
                        totalRetryTimeMs += backoffMs;
                        if (backoffMs > maxBackoffMs) maxBackoffMs = backoffMs;
                    }
                    console.warn(
                        `[WriteLayerTools.updateWorkflowState] Lock contention. Retry ${attempt}/${MAX_RETRIES} in ${backoffMs}ms (jittered)`
                    );
                    // Busy wait (M01 limitation)
                    const start = Date.now();
                    while (Date.now() - start < backoffMs) { }
                }
                continue;
            }

            // Handle known errors
            if (error.message === 'SESSION_NOT_FOUND') {
                return errorSessionNotFound(sessionId) as MpcErrorResponse;
            }

            if (error.message?.startsWith('INVALID_TRANSITION:')) {
                const [, transition] = error.message.match(/INVALID_TRANSITION:(.+)/) || [];
                return errorFsmInvalidTransition(
                    `Invalid state transition: ${transition}. Allowed transitions follow the FSM rules.`
                ) as MpcErrorResponse;
            }

            // Unexpected error
            console.error('[WriteLayerTools.updateWorkflowState] Unexpected error:', error);
            return errorInternalServerError('Failed to update workflow state: ' + error.message) as MpcErrorResponse;
        }
    }

    // log metrics summary if enabled
    if (enableMetrics) {
        console.info(
            `[WriteLayerTools.updateWorkflowState] metrics: lock_contention_count=${lockContentionCount}, max_backoff_ms=${maxBackoffMs}, total_retry_time_ms=${totalRetryTimeMs}`
        );
    }

    // Exhausted retries
    return errorDatabaseLocked(
        `Could not acquire database lock after ${MAX_RETRIES} attempts: ${lastError?.message}`
    );
}

// ─────────────────────────────────────────────────────────────────────────────
// MCP TOOL DESCRIPTORS
// ─────────────────────────────────────────────────────────────────────────────

/**
 * MCP tool descriptor for submit_artifact().
 *
 * Used by MCP SDK to expose tool to LL.
 */
export const SUBMIT_ARTIFACT_TOOL = {
    name: 'submit_artifact',
    description: `
Submit an artifact (implementation summary, test report, PR link) to an active session.

Uses SQLite pessimistic locking (BEGIN IMMEDIATE) to ensure atomic writes with no race conditions.

Allowed roles: backend_developer, tech_lead

Example:
  session_id: "550e8400-e29b-41d4-a716-446655440000"
  artifact_content: "## Implementation Summary\\n..."
  artifact_type: "implementation_summary"

Returns: artifact_id, session_id, submitted_at, success
Errors: SESSION_NOT_FOUND, PERMISSION_DENIED, SCHEMA_VALIDATION_ERROR, DATABASE_LOCKED
  `,
    inputSchema: {
        type: 'object',
        properties: {
            session_id: {
                type: 'string',
                description: 'UUID of the active session',
                pattern: '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$',
            },
            artifact_content: {
                type: 'string',
                description: 'Full artifact content (markdown, JSON, URL, etc.)',
                maxLength: 100000,
            },
            artifact_type: {
                type: 'string',
                enum: ['implementation_summary', 'test_report', 'pr_link'],
                description: 'Type of artifact being submitted',
            },
        },
        required: ['session_id', 'artifact_content', 'artifact_type'],
    },
};

/**
 * MCP tool descriptor for update_workflow_state().
 */
export const UPDATE_WORKFLOW_STATE_TOOL = {
    name: 'update_workflow_state',
    description: `
Update the FSM state of an active agent session.

Validates state transitions to prevent invalid FSM operations.
Uses SQLite pessimistic locking (BEGIN IMMEDIATE) for atomic writes.

Allowed roles: backend_developer, tech_lead

Valid state transitions:
  started → in_progress
  in_progress → in_progress (retry current state)
  in_progress → submitted
  submitted → processed
  processed → closed

Example:
  session_id: "550e8400-e29b-41d4-a716-446655440000"
  new_state: "submitted"
  event: "Artifact submitted successfully"

Returns: state_before, state_after, event_id, timestamp, transition_allowed
Errors: SESSION_NOT_FOUND, PERMISSION_DENIED, FSM_INVALID_TRANSITION, SCHEMA_VALIDATION_ERROR, DATABASE_LOCKED
  `,
    inputSchema: {
        type: 'object',
        properties: {
            session_id: {
                type: 'string',
                description: 'UUID of the active session',
                pattern: '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$',
            },
            new_state: {
                type: 'string',
                enum: ['started', 'in_progress', 'submitted', 'processed', 'closed'],
                description: 'Target FSM state',
            },
            event: {
                type: 'string',
                description: 'Human-readable event description (why the transition)',
                maxLength: 500,
            },
            evidence_artifact_id: {
                type: 'string',
                description: '(Optional) UUID of artifact that triggered this state change',
                pattern: '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$',
            },
        },
        required: ['session_id', 'new_state', 'event'],
    },
};

export default {
    submitArtifact,
    updateWorkflowState,
    SUBMIT_ARTIFACT_TOOL,
    UPDATE_WORKFLOW_STATE_TOOL,
};
