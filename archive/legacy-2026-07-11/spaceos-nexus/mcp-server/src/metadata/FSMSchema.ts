/**
 * EPIC-11: FSM Schema & Data Model
 * TypeScript types for workflow state management.
 */

/**
 * Valid states for any agent workflow.
 * Standard 7-state FSM as per EPIC-11 design.
 */
export type FSMState =
    | 'INITIALIZED'      // Session created, no work started
    | 'IN_PROGRESS'      // Active work (Phase 1-3)
    | 'UNDER_REVIEW'     // Waiting for peer/coordinator review (Phase 4-5)
    | 'NEEDS_REVISION'   // Rejected, back to development
    | 'APPROVED'         // Review passed (Phase 6)
    | 'READY_FOR_MERGE'  // Final sign-off complete
    | 'COMPLETED';       // Merged and closed (Phase 7)

/**
 * Workflow definition track.
 */
export type WorkflowTrack = 'discovery' | 'delivery';

/**
 * Represents a workflow definition from the database.
 */
export interface WorkflowDefinition {
    workflow_id: string;
    name: string;
    track: WorkflowTrack;
    states: string[]; // JSON array in DB
    version: string;
}

/**
 * Represents a specific state's transition rules.
 */
export interface WorkflowStateRule {
    workflow_id: string;
    state_name: string;
    state_order: number;
    valid_transitions: string[]; // JSON array in DB
}

/**
 * Active agent session snapshot.
 */
export interface AgentSession {
    session_id: string;
    domain: string;
    role_name: string;
    workflow_id: string;
    current_state: FSMState;
    last_action?: string;
    created_at: string;
    updated_at: string;
}

/**
 * Audit log entry for session state transitions.
 */
export interface SessionHistory {
    id: number;
    session_id: string;
    state_before: FSMState;
    state_after: FSMState;
    action: string;
    metadata?: string; // JSON string
    timestamp: string;
}
