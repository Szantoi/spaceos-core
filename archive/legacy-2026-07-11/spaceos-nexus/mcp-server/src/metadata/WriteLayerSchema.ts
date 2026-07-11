/**
 * WriteLayerSchema.ts
 *
 * TypeScript type definitions for EPIC-08 Write Layer SQLite tables.
 *
 * Tables:
 *   - sessions: Agent session-öket nyomon követi
 *   - artifacts: Submitted artifact-okat tárolo
 *   - workflow_events: FSM state-change event-eket naplózza
 *   - checkpoints: Session recovery checkpoint-okat tárolo
 *
 * Export: All interfaces for use in TASK-08-02 (MCP write tools)
 *
 * Reference: src/metadata/migrations/002_write_layer_schema.sql
 */

// ─────────────────────────────────────────────────────────────────────────────
// SESSION RECORD
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Agent session record.
 *
 * Represents a single agent session from start to end.
 * One active session per agent at any given time.
 *
 * Lifecycle:
 *   1. Start: Session created with fsm_state = 'started'
 *   2. Work: Agent performs tasks, artifacts submitted → fsm_state = 'in_progress'
 *   3. Submit: Agent submits final artifact → fsm_state = 'submitted'
 *   4. Process: System processes (M02 EPIC-12) → fsm_state = 'processed'
 *   5. Close: Session ends → fsm_state = 'closed' (terminal)
 */
export interface SessionRecord {
  /** UUID: unique session identifier */
  id: string;

  /** Unique agent identifier (backend_developer, tech_lead, explorer, stb.) */
  agent_id: string;

  /** Role domain (engineering, management, discovery) */
  domain: string;

  /** Role name (backend_developer, tech_lead, explorer, stb.) */
  role: string;

  /** ISO 8601: session indulásának időpont */
  started_at: string;

  /** ISO 8601: utolsó update időpont (nullable) */
  last_updated_at: string | null;

  /**
   * Current FSM state in agent workflow.
   *
   * Valid transitions:
   *   started → in_progress → submitted → processed → closed
   */
  fsm_state: 'started' | 'in_progress' | 'submitted' | 'processed' | 'closed';

  /** Final outcome or error message (advisory). Nullable. */
  outcome: string | null;
}

// ─────────────────────────────────────────────────────────────────────────────
// ARTIFACT RECORD
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Artifact record.
 *
 * Agent kézüleg submitálja az artifact-okat a workflowja során:
 *   - implementation_summary: munka leírása + módosított fájlok
 *   - test_report: test eredmények
 *   - pr_link: GitHub PR link az implementációhoz
 *   - checkpoint: session state checkpoint (M02 EPIC-12)
 */
export interface ArtifactRecord {
  /** UUID: unique artifact identifier */
  id: string;

  /** UUID FK: melyik session-höz tartozik */
  session_id: string;

  /**
   * Artifact type.
   *
   * Supported types:
   *   - implementation_summary: Implementation Summary markdown (from task completion)
   *   - test_report: Test Results report (from test suite)
   *   - pr_link: GitHub Pull Request link
   *   - checkpoint: Session recovery checkpoint JSON (M02 feature)
   */
  artifact_type: 'implementation_summary' | 'test_report' | 'pr_link' | 'checkpoint';

  /** Full artifact content (markdown, JSON, URL, stb.) */
  content: string;

  /** ISO 8601: submission időpont */
  submitted_at: string;

  /** ChromaDB embedding flag (M02 EPIC-12 integration) */
  embedded: boolean;
}

// ─────────────────────────────────────────────────────────────────────────────
// WORKFLOW EVENT RECORD
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Workflow event record.
 *
 * Naplózza az agent workflow-ban történő event-eket:
 *   - FSM state-change
 *   - Artifact submission
 *   - Error events
 *
 * Audit trail: teljes event history a session-höz.
 */
export interface WorkflowEventRecord {
  /** UUID: unique event identifier */
  id: string;

  /** UUID FK: melyik session-höz tartozik */
  session_id: string;

  /**
   * Event type.
   *
   * Common types:
   *   - state_change: FSM state transition
   *   - artifact_submitted: Artifact submission
   *   - error: Error event
   *   - checkpoint_created: Checkpoint saved (M02)
   */
  event_type: string;

  /** Previous FSM state (nullable for first event) */
  state_before: string | null;

  /** New FSM state after transition */
  state_after: string | null;

  /** UUID FK to artifact: mely artifact indította az event-et (nullable) */
  evidence_artifact_id: string | null;

  /** ISO 8601: event időpont */
  timestamp: string;
}

// ─────────────────────────────────────────────────────────────────────────────
// CHECKPOINT RECORD
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Checkpoint record.
 *
 * Session recovery checkpoint. Az agent elmentheti a work-in-progress state-et,
 * és később vissza tudja tölteni belőle (M02 EPIC-12).
 *
 * checkpoint_data tartalmaz:
 *   - Jelenlegi workflow státusza
 *   - Eddig submittált artifact-ok
 *   - Workflow event history
 *   - Agent context (roles, permissions, stb.)
 */
export interface CheckpointRecord {
  /** UUID: unique checkpoint identifier */
  id: string;

  /** UUID FK: melyik session-höz tartozik */
  session_id: string;

  /** JSON-serialized checkpoint data object */
  checkpoint_data: string;  // Stored as JSON string; parse with JSON.parse()

  /** ISO 8601: checkpoint időpont */
  created_at: string;
}

/**
 * Parsed checkpoint data type (after JSON.parse()).
 *
 * Flexible schema — checkpoint_data bármi lehet, amire az agent szüksége van.
 * Common fields:
 *   - workflow_state: Agent workflow FSM state
 *   - artifacts: Array of submitted artifact IDs
 *   - events: Array of workflow event records
 *   - context: Agent context (role, permissions, stb.)
 */
export interface CheckpointData {
  [key: string]: any;
}

// ─────────────────────────────────────────────────────────────────────────────
// FSM STATE TYPE
// ─────────────────────────────────────────────────────────────────────────────

export type WorkflowState = 'started' | 'in_progress' | 'submitted' | 'processed' | 'closed';

/**
 * Valid FSM state transitions.
 *
 * Define which states are reachable from each state.
 */
export const FSM_VALID_TRANSITIONS: Record<WorkflowState, WorkflowState[]> = {
  'started': ['in_progress'],
  'in_progress': ['in_progress', 'submitted'],  // Can retry current state
  'submitted': ['processed'],
  'processed': ['closed'],
  'closed': [],  // Terminal state
};

/**
 * Check if a state transition is valid.
 *
 * @param fromState Current state
 * @param toState Target state
 * @returns true if transition is allowed, false otherwise
 */
export function isValidTransition(fromState: WorkflowState, toState: WorkflowState): boolean {
  const validStates = FSM_VALID_TRANSITIONS[fromState];
  return validStates.includes(toState);
}

// ─────────────────────────────────────────────────────────────────────────────
// CREATE INPUT/OUTPUT TYPES (For TASK-08-02 MCP Tools)
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Input for creating a new session.
 * Used by SessionManager in bootstrap_agent() (EPIC-10).
 */
export interface CreateSessionInput {
  agent_id: string;
  domain: string;
  role: string;
}

/**
 * Input for submitting an artifact.
 * Used by submit_artifact() tool (TASK-08-02).
 *
 * M01 Scope: artifact_type is restricted to implementation_summary | test_report | pr_link
 * (checkpoint is deferred to M02 EPIC-12)
 */
export interface SubmitArtifactInput {
  session_id: string;
  artifact_type: 'implementation_summary' | 'test_report' | 'pr_link';
  artifact_content: string;
}

/**
 * Output for artifact submission.
 * Returned by submit_artifact() tool.
 *
 * M01 Scope: artifact_type restricted to implementation_summary | test_report | pr_link
 */
export interface SubmitArtifactOutput {
  artifact_id: string;
  session_id: string;
  artifact_type: 'implementation_summary' | 'test_report' | 'pr_link';
  submitted_at: string;
  success: true;
}

/**
 * Input for updating workflow state.
 * Used by update_workflow_state() tool (TASK-08-02).
 */
export interface UpdateWorkflowStateInput {
  session_id: string;
  new_state: WorkflowState;
  event: string;  // Event description
  evidence_artifact_id?: string;  // Optional: which artifact triggered this
}

/**
 * Output for workflow state update.
 * Returned by update_workflow_state() tool.
 */
export interface UpdateWorkflowStateOutput {
  state_before: WorkflowState;
  state_after: WorkflowState;
  event_id: string;
  timestamp: string;
  transition_allowed: true;
}

/**
 * Input for storing a checkpoint.
 * Used by store_session_checkpoint() tool (M02 EPIC-12).
 */
export interface StoreSessionCheckpointInput {
  session_id: string;
  checkpoint_data: CheckpointData;
  checkpoint_label?: string;  // Optional: human-readable label
}

/**
 * Output for checkpoint storage.
 * Returned by store_session_checkpoint() tool.
 */
export interface StoreSessionCheckpointOutput {
  checkpoint_id: string;
  session_id: string;
  created_at: string;
  success: true;
}
