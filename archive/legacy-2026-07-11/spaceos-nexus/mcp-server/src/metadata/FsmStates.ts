/**
 * FSM State Machine Definition for EPIC-08: Write Layer
 * 
 * Defines all valid workflow states and transitions for the write layer.
 * 
 * Workflow lifecycle:
 *   started (initial)
 *     ↓
 *   in_progress
 *     ↓
 *   submitted
 *     ↓
 *   processed
 *     ↓
 *   closed (terminal)
 */

export type WorkflowState = 'started' | 'in_progress' | 'submitted' | 'processed' | 'closed';

export interface StateTransitionRule {
  from: WorkflowState;
  to: WorkflowState[];
  description: string;
}

/**
 * FSM transition rules for EPIC-08 write layer.
 * 
 * Key principles:
 *   - Forward transitions only (no backtracking)
 *   - "started" → "in_progress": agent begins work
 *   - "in_progress" → "submitted": artifact submitted & validated
 *   - "in_progress" → "in_progress": retry same state (allowed)
 *   - "submitted" → "processed": system processes (EPIC-12)
 *   - "processed" → "closed": finalization
 *   - "closed" is terminal (no further transitions)
 */
export const FSM_TRANSITIONS: StateTransitionRule[] = [
  {
    from: 'started',
    to: ['in_progress'],
    description: 'Agent begins work on the task',
  },
  {
    from: 'in_progress',
    to: ['submitted', 'in_progress'],
    description: 'Artifact submitted or retry current state',
  },
  {
    from: 'submitted',
    to: ['processed'],
    description: 'System processes the submitted artifact (EPIC-12 responsibility)',
  },
  {
    from: 'processed',
    to: ['closed'],
    description: 'Task finalization',
  },
  {
    from: 'closed',
    to: [],
    description: 'Terminal state — no further transitions allowed',
  },
];

/**
 * Validates whether a state transition is allowed by the FSM.
 * 
 * @param currentState The current workflow state
 * @param targetState The requested target state
 * @returns Object with valid flag and details (allowed_targets, reason)
 * 
 * @example
 *   const result = validateTransition('in_progress', 'submitted');
 *   if (result.valid) {
 *     console.log('✅ Transition allowed');
 *   } else {
 *     console.log(`❌ ${result.reason}`);
 *     console.log(`   Allowed: ${result.allowed_targets.join(', ')}`);
 *   }
 */
export function validateTransition(
  currentState: WorkflowState,
  targetState: WorkflowState
): { valid: true } | { valid: false; reason: string; allowed_targets: WorkflowState[] } {
  // Find the transition rule for current state
  const rule = FSM_TRANSITIONS.find((r) => r.from === currentState);

  if (!rule) {
    return {
      valid: false,
      reason: `Unknown state: "${currentState}" (not defined in FSM)`,
      allowed_targets: [],
    };
  }

  // Check if target state is in the allowed transitions
  if (!rule.to.includes(targetState)) {
    return {
      valid: false,
      reason: `Transition "${currentState}" → "${targetState}" is not allowed. ${rule.description}`,
      allowed_targets: rule.to,
    };
  }

  return { valid: true };
}

/**
 * Get all valid target states for a given current state.
 * 
 * @param currentState The current workflow state
 * @returns Array of valid target states
 */
export function getValidTargets(currentState: WorkflowState): WorkflowState[] {
  const rule = FSM_TRANSITIONS.find((r) => r.from === currentState);
  return rule?.to || [];
}

/**
 * Check if a state is terminal (no further transitions).
 * 
 * @param state The state to check
 * @returns true if state is terminal
 */
export function isTerminalState(state: WorkflowState): boolean {
  const rule = FSM_TRANSITIONS.find((r) => r.from === state);
  return rule?.to.length === 0;
}

/**
 * HSM initial state for all workflows in EPIC-08.
 */
export const INITIAL_STATE: WorkflowState = 'started';

/**
 * Terminal states (no further transitions).
 */
export const TERMINAL_STATES: WorkflowState[] = ['closed'];
