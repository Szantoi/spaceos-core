/**
 * Zod Schema Definitions for Write Layer Tools
 *
 * Central schema definitions for all input/output validation in TASK-08-02 tools:
 *   - submit_artifact()
 *   - update_workflow_state()
 *   - store_session_checkpoint() [M02 preview]
 *
 * All schemas include:
 *   - Type inference (z.infer<typeof Schema>)
 *   - Custom error messages (for LLM understanding)
 *   - Constraints (min/max lengths, formats)
 */

import { z } from 'zod';

// ─────────────────────────────────────────────────────────────────────────────
// SUBMIT_ARTIFACT SCHEMAS
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Input validation for submit_artifact() MCP tool.
 *
 * Validates:
 *   - artifact_content: non-empty string, max 100KB
 *   - session_id: valid UUID
 *   - artifact_type: one of allowed enum values
 */
export const SubmitArtifactInputSchema = z.object({
  artifact_content: z.string()
    .min(1, 'artifact_content cannot be empty')
    .max(100_000, 'artifact_content exceeds 100KB limit'),

  session_id: z.string()
    .uuid('session_id must be a valid UUID (e.g., 550e8400-e29b-41d4-a716-446655440000)'),

  artifact_type: z.enum(
    ['implementation_summary', 'test_report', 'pr_link', 'checkpoint'],
    {
      message: 'artifact_type must be one of: implementation_summary, test_report, pr_link, checkpoint',
    }
  ),
});

export type SubmitArtifactInput = z.infer<typeof SubmitArtifactInputSchema>;

/**
 * Output schema for successful submit_artifact() call.
 */
export const SubmitArtifactOutputSchema = z.object({
  artifact_id: z.string().uuid(),
  artifact_type: z.enum(['implementation_summary', 'test_report', 'pr_link', 'checkpoint']),
  session_id: z.string().uuid(),
  submitted_at: z.string().datetime(),
  success: z.literal(true),
});

export type SubmitArtifactOutput = z.infer<typeof SubmitArtifactOutputSchema>;

// ─────────────────────────────────────────────────────────────────────────────
// UPDATE_WORKFLOW_STATE SCHEMAS
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Input validation for update_workflow_state() MCP tool.
 *
 * Validates:
 *   - session_id: valid UUID
 *   - new_state: one of allowed FSM states
 *   - event: event description (1-500 chars)
 *   - evidence_artifact_id: optional reference to artifact
 */
export const UpdateWorkflowStateInputSchema = z.object({
  session_id: z.string()
    .uuid('session_id must be a valid UUID'),

  new_state: z.enum(['started', 'in_progress', 'submitted', 'processed', 'closed'], {
    message: 'new_state must be one of: started, in_progress, submitted, processed, closed',
  }),

  event: z.string()
    .min(1, 'event description cannot be empty')
    .max(500, 'event description exceeds 500 chars'),

  evidence_artifact_id: z.string()
    .uuid('evidence_artifact_id must be a valid UUID')
    .optional()
    .describe('Optional: reference an artifact that triggered this state change'),
});

export type UpdateWorkflowStateInput = z.infer<typeof UpdateWorkflowStateInputSchema>;

/**
 * Output schema for successful update_workflow_state() call.
 */
export const UpdateWorkflowStateOutputSchema = z.object({
  state_before: z.enum(['started', 'in_progress', 'submitted', 'processed', 'closed']),
  state_after: z.enum(['started', 'in_progress', 'submitted', 'processed', 'closed']),
  event_id: z.string().uuid(),
  transition_allowed: z.literal(true),
  timestamp: z.string().datetime(),
});

export type UpdateWorkflowStateOutput = z.infer<typeof UpdateWorkflowStateOutputSchema>;

// ─────────────────────────────────────────────────────────────────────────────
// STORE_SESSION_CHECKPOINT SCHEMAS (M02 Preview)
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Input validation for store_session_checkpoint() MCP tool. [M02 scope]
 *
 * Validates:
 *   - session_id: valid UUID
 *   - checkpoint_data: JSON-serializable object
 *   - checkpoint_label: optional human-readable label
 */
export const StoreSessionCheckpointInputSchema = z.object({
  session_id: z.string()
    .uuid('session_id must be a valid UUID'),

  checkpoint_data: z.record(z.string(), z.unknown())
    .refine(
      (obj) => {
        try {
          JSON.stringify(obj);
          return true;
        } catch {
          return false;
        }
      },
      'checkpoint_data must be JSON-serializable (no circular references)'
    ),

  checkpoint_label: z.string()
    .min(1, 'checkpoint_label cannot be empty')
    .max(100, 'checkpoint_label exceeds 100 chars')
    .optional()
    .describe('Optional: human-readable label (e.g., "After artifact submission")'),
});

export type StoreSessionCheckpointInput = z.infer<typeof StoreSessionCheckpointInputSchema>;

/**
 * Output schema for successful store_session_checkpoint() call.
 */
export const StoreSessionCheckpointOutputSchema = z.object({
  checkpoint_id: z.string().uuid(),
  session_id: z.string().uuid(),
  created_at: z.string().datetime(),
  label: z.string().optional(),
});

export type StoreSessionCheckpointOutput = z.infer<typeof StoreSessionCheckpointOutputSchema>;

// ─────────────────────────────────────────────────────────────────────────────
// COMMON SCHEMAS
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Common session record schema (read from database).
 */
export const SessionRecordSchema = z.object({
  id: z.number(),
  session_id: z.string().uuid(),
  role: z.string(),
  domain: z.string(),
  agent_name: z.string().nullable(),
  fsm_state: z.enum(['started', 'in_progress', 'submitted', 'processed', 'closed']),
  created_at: z.string().datetime(),
  last_updated_at: z.string().datetime().nullable(),
});

export type SessionRecord = z.infer<typeof SessionRecordSchema>;

/**
 * Common artifact record schema (read from database).
 */
export const ArtifactRecordSchema = z.object({
  id: z.number(),
  session_id: z.string().uuid(),
  artifact_id: z.string().uuid(),
  artifact_type: z.enum(['implementation_summary', 'test_report', 'pr_link', 'checkpoint']),
  content: z.string(),
  submitted_at: z.string().datetime(),
  embedded: z.number().default(0),
});

export type ArtifactRecord = z.infer<typeof ArtifactRecordSchema>;

/**
 * Common workflow event record schema (read from database).
 */
export const WorkflowEventRecordSchema = z.object({
  id: z.number(),
  session_id: z.string().uuid(),
  event_id: z.string().uuid(),
  event_type: z.string(),
  state_before: z.enum(['started', 'in_progress', 'submitted', 'processed', 'closed']),
  state_after: z.enum(['started', 'in_progress', 'submitted', 'processed', 'closed']),
  evidence_artifact_id: z.string().uuid().nullable(),
  timestamp: z.string().datetime(),
});

export type WorkflowEventRecord = z.infer<typeof WorkflowEventRecordSchema>;
