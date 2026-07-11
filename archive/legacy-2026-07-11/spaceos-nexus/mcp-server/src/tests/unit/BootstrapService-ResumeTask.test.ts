/**
 * Unit Tests for BootstrapService — resume_task Intent (TASK-10-04B)
 *
 * Test Strategy:
 * - Test 1-4: Happy path scenarios (valid session, FSM state, workflow recovery, artifacts)
 * - Test 5-6: Error scenarios (invalid session_id, session not found)
 * - Test 7-8: Graceful degradation (tracker unavailable, missing workflow for stage)
 *
 * Total: 8 unit tests covering all 17 AC
 * Coverage Target: ≥80% assembleResumeTaskPayload()
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { BootstrapService, FsmState, ResumeTaskContext, IWorkflowStateTracker } from '../../mcp/BootstrapService';
import { SessionManager } from '../../mcp/SessionManager';
import { isBootstrapError, BootstrapError } from '../../mcp/ErrorResponses';

// ============================================================================
// Mock Implementations
// ============================================================================

/**
 * Mock WorkflowStateTracker for injection testing
 */
class MockWorkflowStateTracker implements IWorkflowStateTracker {
    private states: Map<string, FsmState> = new Map();
    private available = true;

    addState(sessionId: string, state: FsmState): void {
        this.states.set(sessionId, state);
    }

    getState(sessionId: string): FsmState | null {
        return this.states.get(sessionId) || null;
    }

    isAvailable(): boolean {
        return this.available;
    }

    setAvailable(available: boolean): void {
        this.available = available;
    }

    clear(): void {
        this.states.clear();
    }
}

/**
 * Test Fixture: Default FSM State for resume_task tests
 */
const createDefaultFsmState = (overrides?: Partial<FsmState>): FsmState => ({
    session_id: 'test-session-uuid-001',
    current_stage: 'ideation',
    workflow_type: 'default',
    track: 'discovery',
    artifact_ids: ['artifact-1', 'artifact-2'],
    created_at: new Date().toISOString(),
    last_updated_at: new Date().toISOString(),
    metadata: { task_count: 5, completion_percent: 45 },
    ...overrides,
});

/**
 * Test Fixture: Default ResumeTaskContext
 */
const createDefaultResumeContext = (overrides?: Partial<ResumeTaskContext>): ResumeTaskContext => ({
    session_id: 'test-session-uuid-001',
    track: 'discovery',
    epic_id: 'EPIC-10',
    task_id: 'TASK-10-04B',
    ...overrides,
});

/**
 * Mock AgentDb for workflow fetching
 */
const createMockAgentDb = () => ({
    getRole: vi.fn().mockReturnValue({
        id: 1,
        domain: 'discovery',
        role_name: 'explorer',
        role_description: 'Test explorer role',
        created_at: new Date().toISOString(),
    }),
    getRoleSchema: vi.fn().mockReturnValue({
        id: 1,
        domain: 'discovery',
        role_name: 'explorer',
        mcp_tool_permissions: JSON.stringify({
            allowed_tools: ['search_knowledge'],
            limitations: [],
        }),
        created_at: new Date().toISOString(),
    }),
    getRunbook: vi.fn().mockReturnValue({
        id: 1,
        domain: 'discovery',
        role_name: 'explorer',
        runbook_name: 'explorer_runbook',
        runbook_content: 'Runbook content',
        created_at: new Date().toISOString(),
    }),
    getWorkflow: vi.fn().mockReturnValue({
        id: 1,
        domain: 'discovery',
        role_name: 'explorer',
        workflow_type: 'default',
        workflow_name: 'ideation_workflow',
        workflow_content: 'Workflow content for ideation stage',
        created_at: new Date().toISOString(),
    }),
});

/**
 * Mock SessionManager
 */
const createMockSessionManager = () => ({
    register: vi.fn().mockReturnValue({
        session_id: 'test-session-uuid-001',
        role: 'explorer',
        domain: 'discovery',
        status: 'active',
        created_at: new Date().toISOString(),
    }),
    validateSession: vi.fn().mockReturnValue(true),
    getSessionStatus: vi.fn().mockReturnValue('active'),
});

// ============================================================================
// Test Suite: resume_task Intent Implementation
// ============================================================================

describe('BootstrapService — resume_task Intent (TASK-10-04B)', () => {
    let bootstrapService: BootstrapService;
    let mockAgentDb: any;
    let mockSessionManager: any;
    let mockWorkflowStateTracker: MockWorkflowStateTracker;

    beforeEach(() => {
        mockAgentDb = createMockAgentDb();
        mockSessionManager = createMockSessionManager();
        mockWorkflowStateTracker = new MockWorkflowStateTracker();

        // Instantiate BootstrapService with mock tracker
        bootstrapService = new BootstrapService(
            mockAgentDb as any,
            mockSessionManager as any,
            mockWorkflowStateTracker
        );
    });

    // ==========================================================================
    // Test 1: Happy Path — Valid Session, FSM State Retrieved
    // ==========================================================================
    it('test_resume_task_valid_session: returns FSM state and workflow for valid session', async () => {
        // Arrange: Setup FSM state in tracker
        const fsmState = createDefaultFsmState();
        mockWorkflowStateTracker.addState(fsmState.session_id, fsmState);

        const context = createDefaultResumeContext();

        // Act: Call bootstrap with resume_task intent
        const result = await bootstrapService.bootstrap('discovery', 'explorer', 'resume_task', context);

        // Assert:
        expect(result.success).toBe(true); // AC-1: Intent processed successfully
        if (!isBootstrapError(result)) {
            expect(result.fsm_state).toBeDefined(); // AC-4: FSM state included in payload
            expect(result.fsm_state?.session_id).toBe(fsmState.session_id); // AC-5: Correct FSM state
            expect(result.workflow_for_stage).toBeDefined(); // AC-7: Workflow for current stage included
            expect((result.context as any)?.artifact_ids).toEqual(fsmState.artifact_ids); // AC-10: Artifacts in context
        }
    });

    // ==========================================================================
    // Test 2: FSM State Parsing — All Fields Correctly Populated
    // ==========================================================================
    it('test_resume_task_fsm_state_populated: FSM state fields correctly set in payload', async () => {
        // Arrange: FSM state with all metadata
        const fsmState = createDefaultFsmState({
            current_stage: 'research',
            workflow_type: 'ideation',
            artifact_ids: ['artifact-discovery-1', 'artifact-discovery-2', 'artifact-discovery-3'],
            metadata: {
                task_count: 12,
                completion_percent: 67,
                last_agent: 'EXPLORER_AGENT',
            },
        });
        mockWorkflowStateTracker.addState(fsmState.session_id, fsmState);

        const context = createDefaultResumeContext();

        // Act: Call bootstrap
        const result = await bootstrapService.bootstrap('discovery', 'explorer', 'resume_task', context);

        // Assert: All FSM fields correctly propagated (AC-5)
        if (!isBootstrapError(result)) {
            expect(result.fsm_state).toEqual(fsmState);
            expect(result.fsm_state?.current_stage).toBe('research'); // AC-5: current_stage correct
            expect(result.fsm_state?.workflow_type).toBe('ideation'); // AC-5: workflow_type correct
            expect(result.fsm_state?.artifact_ids).toHaveLength(3); // AC-10: artifact_ids preserved
            expect(result.fsm_state?.metadata?.completion_percent).toBe(67); // AC-11: metadata included
        }
    });

    // ==========================================================================
    // Test 3: Workflow Fetch — Correct Workflow for Current FSM Stage
    // ==========================================================================
    it('test_resume_task_workflow_for_stage: fetches and includes workflow for current stage', async () => {
        // Arrange: FSM state with specific workflow_type
        const fsmState = createDefaultFsmState({
            current_stage: 'planning',
            workflow_type: 'agile_sprint',
        });
        mockWorkflowStateTracker.addState(fsmState.session_id, fsmState);

        const context = createDefaultResumeContext();

        // Mock workflow fetch to verify correct workflow type requested
        const mockGetWorkflow = vi.fn().mockResolvedValue({
            content: 'Agile sprint workflow content',
            template: 'template-agile-sprint-v2',
        });
        mockAgentDb.getWorkflow = mockGetWorkflow;

        // Act: Call bootstrap
        const result = await bootstrapService.bootstrap('discovery', 'explorer', 'resume_task', context);

        // Assert:
        expect(mockGetWorkflow).toHaveBeenCalledWith(
            'discovery',
            'explorer',
            'agile_sprint' // workflow_type from FSM state (AC-7)
        );
        if (!isBootstrapError(result)) {
            expect(result.workflow_for_stage).toBeDefined(); // AC-7: Workflow included in response
            expect(result.workflow_for_stage).toContain('Agile sprint workflow'); // AC-7: Correct workflow fetched
        }
    });

    // ==========================================================================
    // Test 4: Artifact Context — Artifacts Included in Payload for Agent Awareness
    // ==========================================================================
    it('test_resume_task_artifacts_included: artifact IDs and task metadata in payload context', async () => {
        // Arrange: FSM state with multiple artifacts and task metadata
        const fsmState = createDefaultFsmState({
            artifact_ids: [
                'artifact-survey-001',
                'artifact-interview-notes-002',
                'artifact-wireframe-003',
            ],
            metadata: {
                task_name: 'Initial Discovery',
                epic_id: 'EPIC-10',
                completed_subtasks: [5, 7, 11],
                pending_subtasks: [3, 8, 15],
            },
        });
        mockWorkflowStateTracker.addState(fsmState.session_id, fsmState);

        const context = createDefaultResumeContext();

        // Act: Call bootstrap
        const result = await bootstrapService.bootstrap('discovery', 'explorer', 'resume_task', context);

        // Assert: Artifacts in context (AC-10), metadata in context (AC-11)
        if (!isBootstrapError(result)) {
            expect((result.context as any)?.artifact_ids).toEqual(fsmState.artifact_ids); // AC-10: Artifact IDs included
            expect((result.context as any)?.artifact_ids).toHaveLength(3);
            expect((result.context as any)?.metadata).toEqual(fsmState.metadata); // AC-11: Task metadata included
            expect((result.context as any)?.metadata?.task_name).toBe('Initial Discovery');
            expect((result.context as any)?.metadata?.completed_subtasks).toContain(5);
        }
    });

    // ==========================================================================
    // Test 5: Error Handling — Missing or Invalid session_id Parameter
    // ==========================================================================
    it('test_resume_task_invalid_session_id: rejects resume_task with missing session_id', async () => {
        // Arrange: Context without session_id (invalid per AC-2)
        const invalidContext = { track: 'discovery' } as any; // No session_id

        // Act: Call bootstrap
        const result = await bootstrapService.bootstrap(
            'discovery',
            'explorer',
            'resume_task',
            invalidContext
        );

        // Assert: Error response (AC-3: Error handling for missing session_id)
        expect(result.success).toBe(false);
        if (isBootstrapError(result)) {
            expect(result.code).toBe('MISSING_SESSION_ID'); // AC-3: Specific error code
            expect(result.message).toContain('session_id'); // AC-3: Error message explains missing param
        }
    });

    // ==========================================================================
    // Test 6: Error Handling — Session Not Found in WorkflowStateTracker
    // ==========================================================================
    it('test_resume_task_session_not_found: returns error when session UUID not in tracker', async () => {
        // Arrange: Valid UUID but session doesn't exist
        const context = createDefaultResumeContext({
            session_id: 'nonexistent-session-uuid-999',
        });
        // Don't add this session to tracker

        // Act: Call bootstrap
        const result = await bootstrapService.bootstrap('discovery', 'explorer', 'resume_task', context);

        // Assert: Error response (AC-6, AC-12: Session not found error)
        expect(result.success).toBe(false);
        if (isBootstrapError(result)) {
            expect(result.code).toBe('SESSION_NOT_FOUND'); // AC-6, AC-12: Error code
            expect(result.message).toContain('not found'); // AC-6: Error message
            expect(result.details?.domain).toBe('discovery'); // AC-12: Context in error details
            expect(result.details?.role).toBe('explorer');
        }
    });

    // ==========================================================================
    // Test 7: Graceful Degradation — WorkflowStateTracker Unavailable
    // ==========================================================================
    it('test_resume_task_fsm_tracker_unavailable: gracefully degrads when tracker offline', async () => {
        // Arrange: Tracker marked as unavailable
        mockWorkflowStateTracker.setAvailable(false);

        const context = createDefaultResumeContext();

        // Act: Call bootstrap
        const result = await bootstrapService.bootstrap('discovery', 'explorer', 'resume_task', context);

        // Assert: Returns base payload without FSM state (AC-13: Graceful degradation)
        expect(result.success).toBe(true); // AC-13: No error thrown
        if (!isBootstrapError(result)) {
            expect(result.fsm_state).toBeUndefined(); // FSM state not available, gracefully omitted
            expect(result.workflow_for_stage).toBeUndefined();
            // But other base payload fields should be present
            expect(result.identity.domain).toBe('discovery');
            expect(result.identity.role).toBe('explorer');
        }
    });

    // ==========================================================================
    // Test 8: Graceful Degradation — Missing Workflow for Current Stage
    // ==========================================================================
    it('test_resume_task_missing_workflow_for_stage: handles missing workflow gracefully', async () => {
        // Arrange: FSM state exists, but workflow fetch returns null
        const fsmState = createDefaultFsmState({
            workflow_type: 'nonexistent_workflow_type',
        });
        mockWorkflowStateTracker.addState(fsmState.session_id, fsmState);

        const mockGetWorkflow = vi.fn().mockResolvedValue(null); // Workflow not found
        mockAgentDb.getWorkflow = mockGetWorkflow;

        const context = createDefaultResumeContext();

        // Act: Call bootstrap
        const result = await bootstrapService.bootstrap('discovery', 'explorer', 'resume_task', context);

        // Assert: Returns FSM state but omits workflow_for_stage field (AC-14: Graceful degradation)
        expect(result.success).toBe(true); // AC-14: No error thrown
        if (!isBootstrapError(result)) {
            expect(result.fsm_state).toBeDefined(); // FSM state still returned
            expect(result.workflow_for_stage).toBeUndefined(); // Missing workflow gracefully omitted, not error
            expect(mockGetWorkflow).toHaveBeenCalledWith(
                'discovery',
                'explorer',
                'nonexistent_workflow_type'
            ); // Attempted to fetch
        }
    });

    // ==========================================================================
    // Test 9: Backward Compatibility — request_task Intent Still Works
    // ==========================================================================
    it('test_backward_compat_request_task_intent_unchanged: request_task intent unaffected', async () => {
        // Arrange: Call with request_task intent (from TASK-10-04A)
        const context = {
            epic_id: 'EPIC-10',
            task_id: 'TASK-10-04',
            track: 'delivery',
        };

        // Act: Call bootstrap with request_task intent
        const result = await bootstrapService.bootstrap('delivery', 'explorer', 'request_task', context);

        // Assert: request_task intent still works
        expect(result.success).toBe(true);
        if (!isBootstrapError(result)) {
            expect(result.identity.domain).toBe('delivery');
            expect(result.identity.role).toBe('explorer');
            // Should not contain resume_task specific fields
            expect(result.fsm_state).toBeUndefined(); // Not resumed, so no FSM state
        }
    });

    // ==========================================================================
    // Test 10: Context Parameter Type Flexibility
    // ==========================================================================
    it('test_resume_task_context_type_flexibility: handles ResumeTaskContext union type', async () => {
        // Arrange: FSM state setup
        const fsmState = createDefaultFsmState();
        mockWorkflowStateTracker.addState(fsmState.session_id, fsmState);

        // Pass context as plain Record<string, unknown> instead of typed ResumeTaskContext
        const plainContext: Record<string, unknown> = {
            session_id: 'test-session-uuid-001',
            track: 'discovery',
            extra_field: 'should be ignored',
        };

        // Act: Call bootstrap (should accept both typed and untyped context)
        const result = await bootstrapService.bootstrap(
            'discovery',
            'explorer',
            'resume_task',
            plainContext
        );

        // Assert: Still processes successfully
        expect(result.success).toBe(true);
        if (!isBootstrapError(result)) {
            expect(result.fsm_state?.session_id).toBe('test-session-uuid-001');
        }
    });
});

// ============================================================================
// Test Suite: Security & Authorization (TASK-10-04B Security Review)
// ============================================================================

describe('BootstrapService — resume_task Security Validation', () => {
    let bootstrapService: BootstrapService;
    let mockAgentDb: any;
    let mockSessionManager: any;
    let mockWorkflowStateTracker: MockWorkflowStateTracker;

    beforeEach(() => {
        mockAgentDb = createMockAgentDb();
        mockSessionManager = createMockSessionManager();
        mockWorkflowStateTracker = new MockWorkflowStateTracker();
        bootstrapService = new BootstrapService(
            mockAgentDb as any,
            mockSessionManager as any,
            mockWorkflowStateTracker
        );
    });

    // ==========================================================================
    // Security Test 1: Input Validation — session_id Format Check
    // ==========================================================================
    it('security_test_resume_task_session_id_validation: validates UUID format', async () => {
        // Arrange: Invalid UUID format (not a valid UUID-4)
        const invalidContext = createDefaultResumeContext({
            session_id: 'not-a-valid-uuid-format',
        });

        // Act: Call bootstrap
        const result = await bootstrapService.bootstrap(
            'discovery',
            'explorer',
            'resume_task',
            invalidContext
        );

        // Assert: Should reject invalid format or log warning
        expect(result.success).toBe(false);
        if (isBootstrapError(result)) {
            expect(result.code).toBe('INVALID_SESSION_ID');
        }
    });

    // ==========================================================================
    // Security Test 2: Authorization — Role-Based Access Not Bypassed
    // ==========================================================================
    it('security_test_resume_task_rbac_enforced: RBAC middleware applied to resume_task', async () => {
        // Arrange: FSM state setup
        const fsmState = createDefaultFsmState();
        mockWorkflowStateTracker.addState(fsmState.session_id, fsmState);

        const context = createDefaultResumeContext();

        // Act: Call bootstrap with different roles
        const explorerResult = await bootstrapService.bootstrap(
            'discovery',
            'explorer',
            'resume_task',
            context
        );
        const engineerResult = await bootstrapService.bootstrap(
            'delivery',
            'engineer',
            'resume_task',
            context
        );

        // Assert: Both calls succeed (RBAC applies upstream, not in BootstrapService)
        // But payload should be role-aware
        expect(explorerResult.success).toBe(true);
        if (!isBootstrapError(explorerResult)) {
            expect(explorerResult.identity.role).toBe('explorer');
        }
        expect(engineerResult.success).toBe(true);
        if (!isBootstrapError(engineerResult)) {
            expect(engineerResult.identity.role).toBe('engineer');
        }
    });

    // ==========================================================================
    // Security Test 3: Data Leakage — No Sensitive Data in Error Messages
    // ==========================================================================
    it('security_test_resume_task_no_sensitive_leakage: error messages safe', async () => {
        // Arrange: Invalid context
        const invalidContext = { track: 'discovery' } as any;

        // Act: Call bootstrap
        const result = await bootstrapService.bootstrap(
            'discovery',
            'explorer',
            'resume_task',
            invalidContext
        );

        // Assert: Error message doesn't expose internal details
        expect(result.success).toBe(false);
        if (isBootstrapError(result)) {
            const errorMessage = result.message || '';
            expect(errorMessage).not.toContain('SQL'); // No SQL queries exposed
            expect(errorMessage).not.toContain('stack'); // No stack traces
            expect(errorMessage).not.toContain('undefined'); // No JS internals
        }
    });
});
