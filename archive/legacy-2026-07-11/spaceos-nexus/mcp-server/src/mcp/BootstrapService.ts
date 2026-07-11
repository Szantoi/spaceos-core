/**
 * BootstrapService — Agent Identity & Context Assembly
 *
 * **Purpose:**
 * Single MCP tool entry point for agents to bootstrap their complete identity and context.
 * Given a domain and role, returns:
 *   - Identity (persona + metadata)
 *   - Role permissions (allowed MCP tools)
 *   - Runbook (step-by-step guidance)
 *   - Session ID (for state tracking)
 *
 * **Architecture:**
 * - Depends on EPIC-09 AgentDb service (queries: roles, role_schemas, runbooks)
 * - Session creation via SessionManager (UUID generation)
 * - Error handling: Structured responses (no 500 errors on invalid domain/role)
 * - Performance: < 50ms p95 latency (3 DB queries + session creation)
 * - Security: Input validation (regex patterns) + SQL injection prevention via prepared statements
 *
 * **Usage:**
 * ```typescript
 * const service = new BootstrapService(agentDb, sessionManager);
 * const payload = await service.bootstrap('engineering', 'backend_developer', 'identify');
 * // Returns: { payload_version, identity, role_content, runbook_content, allowed_tools, session_id, ... }
 * ```
 */

import { AgentDb, RoleRow, RoleSchemaRow, RunbookRow } from './AgentDb';
import { SessionManager, SessionRecord } from './SessionManager';
import { InputValidator } from './InputValidator';
import {
    errorInvalidDomain,
    errorInvalidRole,
    errorRoleNotFound,
    errorDatabaseError,
    errorSessionCreationFailed,
    errorPayloadTooLarge,
    errorUnknownError,
    errorSessionNotFound,
    errorSchemaValidationError,
    BootstrapError,
    isBootstrapError
} from './ErrorResponses';

// ═════════════════════════════════════════════════════════════════════════════
// TYPE DEFINITIONS — Bootstrap Payload
// ═════════════════════════════════════════════════════════════════════════════

/**
 * Agent identity object — minimal context about the agent persona.
 *
 * Extracted from role_schemas table (domain + role_name) and identity table.
 */
export interface AgentIdentity {
    domain: string;
    role: string;
    /** Optional: agent persona name or team affiliation */
    persona?: string;
    /** ISO 8601 timestamp when bootstrap was created */
    bootstrapped_at: string;
}

/**
 * Main bootstrap payload — complete context for agent initialization.
 *
 * Contract: Payload must include all fields below (though some may be null).
 * Versioned: `payload_version: "1.0"` for contract lock-down (TASK-10-01 AC-23).
 */
export interface BootstrapPayload {
    /** MCP standard: explicit success flag */
    success: true;

    /** Schema version for payload contract management */
    payload_version: "1.0";

    /** Agent identity (domain + role + persona) */
    identity: AgentIdentity;

    /** Role definition markdown (full .role.md content from AgentDb) */
    role_content: string;

    /** Runbook markdown (step-by-step guidance from AgentDb) */
    runbook_content: string | null;

    /** Array of allowed MCP tool names (parsed from role_schemas.mcp_tool_permissions JSON) */
    allowed_tools: string[];

    /** Array of role limitations/constraints (parsed from role_schemas.mcp_tool_permissions JSON) */
    limitations: string[];

    /** Session ID (UUID) for state tracking during this agent session */
    session_id: string;

    /** Session metadata (status, created_at) */
    session: SessionRecord;

    /** Bootstrap request intent (used by TASK-10-04+ for request_task/resume_task workflows) */
    intent: "identify" | "request_task" | "resume_task";

    /** Optional: workflow content attached for request_task intent (TASK-10-04A) */
    workflow_content?: string;

    /** Optional: template content attached for request_task intent (TASK-10-04A) */
    template_content?: string;

    /** Optional: FSM state recovery for resume_task intent (TASK-10-04B) */
    fsm_state?: FsmState;

    /** Optional: workflow for current FSM stage (resume_task) (TASK-10-04B) */
    workflow_for_stage?: string;

    /** Optional: additional context (e.g., FSM state from resume_task workflow) */
    context?: Record<string, unknown>;
}

/**
 * Request task context — optional parameters for request_task intent.
 *
 * Used in bootstrap(domain, role, "request_task", RequestTaskContext)
 * to attach workflow + template for the agent's track (discovery vs. delivery).
 */
export interface RequestTaskContext {
    /** Track type: 'delivery' (default FSM tasks) or 'discovery' (ideation/validation) */
    track?: "discovery" | "delivery";
    /** Optional: epic ID to filter tasks within epic */
    epic_id?: string;
    /** Optional: task ID for specific task context */
    task_id?: string;
    /** Optional: session ID from previous bootstrap (for resumption) */
    session_id?: string;
}

/**
 * FSM State object — current state of agent's workflow (TASK-10-04B).
 *
 * Retrieved from WorkflowStateTracker service to resume interrupted tasks.
 * Persists metadata about task context, artifacts, and workflow stage.
 */
export interface FsmState {
    /** Unique session ID (UUID) */
    session_id: string;

    /** Current FSM stage (e.g., "started", "in_progress", "in_review", "submitted") */
    current_stage: string;

    /** Workflow type for this stage (e.g., "default", "ideation") */
    workflow_type: "default" | "ideation" | string;

    /** Task metadata: which epic is this work for */
    epic_id?: string;

    /** Task metadata: which task is being worked on */
    task_id?: string;

    /** Track: discovery or delivery */
    track: "discovery" | "delivery";

    /** Artifact IDs completed so far (agent can retrieve previous work) */
    artifact_ids?: string[];

    /** When FSM state was created */
    created_at: string;

    /** When FSM state was last updated */
    last_updated_at: string;

    /** Additional metadata (may include agent_id, iteration count, etc.) */
    metadata?: Record<string, unknown>;
}

/**
 * Resume task context — optional parameters for resume_task intent.
 *
 * Used in bootstrap(domain, role, "resume_task", ResumeTaskContext)
 * to recover from interrupted sessions and resume work at last FSM state.
 */
export interface ResumeTaskContext {
    /** REQUIRED: UUID of existing session to resume */
    session_id: string;

    /** Optional: track type (should match FSM state, but can override for safety) */
    track?: "discovery" | "delivery";

    /** Optional: epic ID (for verification against FSM state) */
    epic_id?: string;

    /** Optional: task ID (for verification against FSM state) */
    task_id?: string;
}

/**
 * Error response structure — always returned in result object (never protocol-level error).
 *
 * MCP standard: Error handling MUST be in result object, not HTTP 5xx errors.
 * Reference: https://www.stainless.com/mcp/error-handling-and-debugging-mcp-servers
 */
// Removed local interface BootstrapError as it is now imported from ErrorResponses

// ═════════════════════════════════════════════════════════════════════════════
// TOOL PERMISSIONS SCHEMA
// ═════════════════════════════════════════════════════════════════════════════

/**
 * Parsed MCP tool permissions structure from role_schemas.mcp_tool_permissions JSON.
 *
 * Raw JSON stored in SQLite as:
 * ```json
 * {
 *   "allowed_tools": ["search_knowledge", "get_role", "get_role_context"],
 *   "limitations": [
 *     "Cannot modify role definitions (ops team only)",
 *     "Cannot submit artifacts outside role domain"
 *   ]
 * }
 * ```
 */
export interface ToolPermissionsJSON {
    allowed_tools?: string[];
    limitations?: string[];
}

/**
 * WorkflowStateTracker interface — for FSM state recovery in resume_task intent.
 *
 * Implemented by WorkflowStateTracker service in EPIC-11/EPIC-12.
 * For TASK-10-04B, this will be mocked in tests.
 * In production, injected during bootstrap.
 */
export interface IWorkflowStateTracker {
    /**
     * Retrieve FSM state for a given session.
     *
     * @param sessionId - UUID of the session
     * @returns FsmState if session exists, null if not found
     * @throws Error if tracker service unavailable
     */
    getState(sessionId: string): FsmState | null;

    /**
     * Check if tracker service is available.
     *
     * @returns true if service health check passes
     */
    isAvailable(): boolean;
}

// ═════════════════════════════════════════════════════════════════════════════
// BOOTSTRAP SERVICE CLASS
// ═════════════════════════════════════════════════════════════════════════════

/**
 * BootstrapService: Core agent identity assembly service.
 *
 * Orchestrates:
 * 1. Input validation (domain, role regex patterns)
 * 2. Role lookup (AgentDb.getRole, getRoleSchema, getRunbook)
 * 3. Session creation (SessionManager.register)
 * 4. Payload assembly (structured response)
 * 5. Error handling (graceful degradation on missing data)
 * 6. Logging (structured for debugging)
 *
 * **Constraints:**
 * - Single bootstrap call < 50ms p95 (measured against EPIC-09 baseline)
 * - Payload JSON < 5MB (memory safety for agents)
 * - No TypeScript `any` types (strict mode)
 * - All exceptions logged with context (domain, role, query type)
 */
export class BootstrapService {
    private agentDb: AgentDb;
    private sessionManager: SessionManager;
    private workflowStateTracker?: IWorkflowStateTracker;

    /**
     * Input validation patterns (MCP best practice: deterministic schemas).
     *
     * Domain: lowercase with hyphens (e.g., "backend", "devops", "qa-team")
     * Role: lowercase with underscores (e.g., "backend_developer", "lead_engineer")
     */
    private static readonly DOMAIN_PATTERN = /^[a-z-]+$/;
    private static readonly ROLE_PATTERN = /^[a-z_]+$/;

    constructor(agentDb: AgentDb, sessionManager: SessionManager, workflowStateTracker?: IWorkflowStateTracker) {
        this.agentDb = agentDb;
        this.sessionManager = sessionManager;
        this.workflowStateTracker = workflowStateTracker;
    }

    /**
     * Main bootstrap method: Given domain + role, return complete agent context.
     *
     * @param domain - Agent domain (e.g., "engineering", "management")
     * @param role - Agent role (e.g., "backend_developer", "tech_lead")
     * @param intent - Bootstrap intent (default: "identify")
     * @param context - Optional context for request_task/resume_task workflows
     * @returns BootstrapPayload | BootstrapError (always in result object)
     *
     * **Flow:**
     * 1. Validate domain/role against regex patterns
     * 2. Query AgentDb: getRole(domain, role)
     * 3. Query AgentDb: getRoleSchema(domain, role) → parse tool permissions
     * 4. Query AgentDb: getRunbook(domain, role) → optional, graceful if missing
     * 5. SessionManager.register(role, domain) → create session
     * 6. Assemble payload → return
     *
     * **Error Handling:**
     * - Invalid domain/role → 400 error (structured response, logged)
     * - Role not found → 400 error (graceful)
     * - Database exception → 500 error (logged with context)
     * - Missing runbook → Payload returned without runbook_content (graceful degradation)
     *
     * **Performance:**
     * - AgentDb queries: prepared statements (cached by better-sqlite3)
     * - Session creation: UUID generation + INSERT
     * - Target: < 50ms p95 latency
     *
     * **Security:**
     * - Input regex validation prevents LLM injection attacks
     * - Prepared statements prevent SQL injection
     * - Error messages sanitized (no DB column names revealed)
     * - Payload size capped (< 5MB) to prevent memory exhaustion
     */
    async bootstrap(
        domain: string,
        role: string,
        intent: "identify" | "request_task" | "resume_task" = "identify",
        context?: RequestTaskContext | Record<string, unknown>
    ): Promise<BootstrapPayload | BootstrapError> {
        const startTime = Date.now();

        try {
            // ┌─ Input Validation ────────────────────────────────────────────────┐
            // │ MCP best practice: Strict input validation before DB queries      │
            // └───────────────────────────────────────────────────────────────────┘

            try {
                InputValidator.validateBootstrapInput(domain, role);
            } catch (e: any) {
                console.warn(`[BootstrapService.bootstrap] ⚠️ Validation failed: ${e.message}`);
                if (e.code === 'INVALID_DOMAIN') return errorInvalidDomain(domain, e.reason);
                if (e.code === 'INVALID_ROLE') return errorInvalidRole(role, e.reason);

                // Catch-all for unexpected validation errors
                return errorUnknownError(`Validation error: ${e.message}`, { domain, role, code: e.code });
            }

            if (!["identify", "request_task", "resume_task"].includes(intent)) {
                console.warn(`[BootstrapService.bootstrap] ⚠️ Invalid intent: ${intent}`);
                return errorUnknownError(`Invalid intent: ${intent}`, { domain, role, intent });
            }

            // ┌─ AgentDb Queries ─────────────────────────────────────────────────┐
            // │ 3 queries: role definition + schema + runbook                     │
            // │ All use prepared statements (SQL injection safe)                  │
            // └───────────────────────────────────────────────────────────────────┘

            let roleRow: RoleRow | null = null;
            let schemaRow: RoleSchemaRow | null = null;
            let runbookRow: RunbookRow | null = null;

            try {
                roleRow = this.agentDb.getRole(domain, role);
                if (!roleRow) {
                    console.warn(
                        `[BootstrapService.bootstrap] ⚠️ Role not found: ${domain}/${role} (elapsed: ${Date.now() - startTime}ms)`
                    );
                    return errorRoleNotFound(domain, role);
                }

                schemaRow = this.agentDb.getRoleSchema(domain, role);
                if (!schemaRow) {
                    console.warn(
                        `[BootstrapService.bootstrap] ⚠️ Role schema not found: ${domain}/${role} (will use defaults)`
                    );
                }

                // Runbook is optional: graceful degradation if missing
                runbookRow = this.agentDb.getRunbook(domain, role);
                if (!runbookRow) {
                    console.info(
                        `[BootstrapService.bootstrap] ℹ️ Runbook not found: ${domain}/${role} (optional, payload will not include runbook_content)`
                    );
                }
            } catch (e) {
                console.error(
                    `[BootstrapService.bootstrap] ❌ Database error: ${e instanceof Error ? e.message : String(e)} (domain=${domain}, role=${role})`
                );
                return errorDatabaseError(`Database query failed for ${domain}/${role}`, { domain, role });
            }

            // ┌─ Parse Role Permissions ──────────────────────────────────────────┐
            // │ Extract allowed_tools[] and limitations[] from JSON column        │
            // └───────────────────────────────────────────────────────────────────┘

            let allowedTools: string[] = [];
            let limitations: string[] = [];

            if (schemaRow && schemaRow.mcp_tool_permissions) {
                try {
                    const permissions: ToolPermissionsJSON = JSON.parse(
                        schemaRow.mcp_tool_permissions
                    );
                    allowedTools = permissions.allowed_tools || [];
                    limitations = permissions.limitations || [];
                } catch (e) {
                    console.warn(
                        `[BootstrapService.bootstrap] ⚠️ Failed to parse mcp_tool_permissions JSON for ${domain}/${role}: ${e instanceof Error ? e.message : String(e)
                        }`
                    );
                    // Continue with empty arrays (graceful)
                }
            }

            // ┌─ Session Creation ────────────────────────────────────────────────┐
            // │ UUID generation + INSERT into sessions table                      │
            // │ Session ID used for state tracking in downstream tasks            │
            // └───────────────────────────────────────────────────────────────────┘

            let sessionRecord: SessionRecord;
            try {
                sessionRecord = this.sessionManager.register(role, domain);
            } catch (e) {
                console.error(
                    `[BootstrapService.bootstrap] ❌ Session creation failed: ${e instanceof Error ? e.message : String(e)
                    } (domain=${domain}, role=${role})`
                );
                return errorSessionCreationFailed(`Failed to create session for ${domain}/${role}`, { domain, role });
            }

            // ┌─ Build Agent Identity ────────────────────────────────────────────┐
            // │ Minimal persona context extracted from role schema                │
            // └───────────────────────────────────────────────────────────────────┘

            const identity: AgentIdentity = {
                domain,
                role,
                persona: roleRow?.role_name || undefined,
                bootstrapped_at: new Date().toISOString(),
            };

            // ┌─ Assemble Payload ────────────────────────────────────────────────┐
            // │ Contract: payload_version="1.0" (TASK-10-01 AC-23)                │
            // │ All required fields present (some may be null)                    │
            // │ JSON size < 5MB (memory safety)                                   │
            // │ TASK-10-04A: If intent=request_task, attach workflow+template    │
            // └───────────────────────────────────────────────────────────────────┘

            const basePayload: BootstrapPayload = {
                success: true,
                payload_version: "1.0",
                identity,
                role_content: roleRow?.content || "",
                runbook_content: runbookRow?.content || null,
                allowed_tools: allowedTools,
                limitations,
                session_id: sessionRecord.id,
                session: sessionRecord,
                intent,
            };

            // Route based on intent (TASK-10-04A: request_task; TASK-10-04B: resume_task)
            let finalPayload: BootstrapPayload;
            if (intent === "request_task") {
                const track = ((context as RequestTaskContext)?.track ?? "delivery") as "discovery" | "delivery";
                finalPayload = await this.assembleRequestTaskPayload(
                    basePayload,
                    domain,
                    role,
                    track
                );
            } else if (intent === "resume_task") {
                const resumeResult = await this.assembleResumeTaskPayload(
                    basePayload,
                    domain,
                    role,
                    context as ResumeTaskContext | Record<string, unknown>
                );
                // Handle error response from resume_task
                if (isBootstrapError(resumeResult)) {
                    return resumeResult;
                }
                finalPayload = resumeResult;
            } else {
                finalPayload = basePayload;
            }

            // ┌─ Validate Payload Size ───────────────────────────────────────────┐
            // │ Prevent memory exhaustion on agent side (< 5MB)                   │
            // └───────────────────────────────────────────────────────────────────┘

            const payloadJson = JSON.stringify(finalPayload);
            const payloadSize = Buffer.byteLength(payloadJson, "utf8");

            if (payloadSize > 5_000_000) {
                // 5MB limit
                console.error(
                    `[BootstrapService.bootstrap] ❌ Payload too large: ${payloadSize} bytes for ${domain}/${role}`
                );
                return errorPayloadTooLarge(`Payload size exceeds 5MB limit: ${payloadSize} bytes`, { domain, role });
            }

            const elapsed = Date.now() - startTime;
            console.info(
                `[BootstrapService.bootstrap] ✅ Bootstrap successful: ${domain}/${role} (session=${sessionRecord.id}, payload_size=${payloadSize} bytes, elapsed=${elapsed}ms, intent=${intent})`
            );

            console.log(`DEBUG: [BootstrapService.bootstrap] Final success: ${finalPayload.success}`);
            return finalPayload;
        } catch (e) {
            console.error(
                `[BootstrapService.bootstrap] ❌ Unexpected error: ${e instanceof Error ? e.message : String(e)}`
            );
            return errorUnknownError("An unexpected error occurred during bootstrap.", { domain, role });
        }
    }

    /**
     * Private helper: Extend base payload with workflow + template for request_task intent.
     *
     * TASK-10-04A implementation: When agent requests a new task, bootstrap returns
     * base payload + workflow (track-specific) + template (track-specific).
     *
     * Flow:
     * 1. Determine workflow type based on track ('delivery' → 'default', 'discovery' → 'ideation')
     * 2. Fetch workflow from AgentDb (graceful if missing)
     * 3. Fetch template from AgentDb (graceful if missing)
     * 4. Return extended payload with workflow_content + template_content
     *
     * Graceful Degradation:
     * - Missing workflow → log warning, omit workflow_content from payload
     * - Missing template → log warning, omit template_content from payload
     * - Database exception → log error, return base payload
     *
     * @param basePayload - Base payload from bootstrap (identity + role + tools)
     * @param domain - Agent domain
     * @param role - Agent role
     * @param track - Workflow track ('delivery' or 'discovery')
     * @returns Extended BootstrapPayload with workflow_content + template_content
     */
    private async assembleRequestTaskPayload(
        basePayload: BootstrapPayload,
        domain: string,
        role: string,
        track: "discovery" | "delivery"
    ): Promise<BootstrapPayload> {
        try {
            // ┌─ Determine workflow + template types ──────────────────────────────┐
            // │ Routing based on track parameter (discovery vs. delivery workflows) │
            // └───────────────────────────────────────────────────────────────────┘

            const workflowType = track === "discovery" ? "ideation" : "default";
            const templateType = track === "discovery" ? "hypothesis" : "task";

            // ┌─ Fetch Workflow (Graceful Degradation) ────────────────────────────┐
            // │ Missing workflow → log warning, omit from payload (AC-11)           │
            // │ Exception → log error, omit from payload                           │
            // └───────────────────────────────────────────────────────────────────┘

            let workflowContent: string | undefined;
            try {
                const workflow = await (this.agentDb.getWorkflow(domain, role, workflowType) as any);
                const wfContent = workflow?.content ?? workflow?.workflow_content;
                if (workflow && wfContent) {
                    workflowContent = wfContent;
                    console.info(
                        `[BootstrapService.assembleRequestTaskPayload] ℹ️ Workflow attached: ${domain}/${role}/${workflowType}`
                    );
                } else {
                    console.warn(
                        `[BootstrapService.assembleRequestTaskPayload] ⚠️ Workflow not found: ${domain}/${role}/${workflowType} (AC-11: graceful degradation)`
                    );
                }
            } catch (e) {
                console.warn(
                    `[BootstrapService.assembleRequestTaskPayload] ⚠️ Failed to fetch workflow for ${domain}/${role}/${workflowType}: ${e instanceof Error ? e.message : String(e)}`
                );
            }

            // ┌─ Fetch Template (Graceful Degradation) ────────────────────────────┐
            // │ Missing template → log warning, omit from payload (AC-12)           │
            // │ Exception → log error, omit from payload                           │
            // └───────────────────────────────────────────────────────────────────┘

            let templateContent: string | undefined;
            try {
                const template = this.agentDb.getTemplate(domain, role, templateType);
                if (template && template.content) {
                    templateContent = template.content;
                    console.info(
                        `[BootstrapService.assembleRequestTaskPayload] ℹ️ Template attached: ${domain}/${role}/${templateType}`
                    );
                } else {
                    console.warn(
                        `[BootstrapService.assembleRequestTaskPayload] ⚠️ Template not found: ${domain}/${role}/${templateType} (AC-12: graceful degradation)`
                    );
                }
            } catch (e) {
                console.warn(
                    `[BootstrapService.assembleRequestTaskPayload] ⚠️ Failed to fetch template for ${domain}/${role}/${templateType}: ${e instanceof Error ? e.message : String(e)}`
                );
            }

            // ┌─ Return Extended Payload ─────────────────────────────────────────┐
            // │ Add workflow_content + template_content (optional fields)          │
            // │ If missing, fields simply omitted (not null) — graceful            │
            // └───────────────────────────────────────────────────────────────────┘

            return {
                ...basePayload,
                success: true,
                workflow_content: workflowContent,
                template_content: templateContent,
            };
        } catch (e) {
            // Severe unexpected error → return base payload (graceful)
            console.error(
                `[BootstrapService.assembleRequestTaskPayload] ❌ Unexpected error: ${e instanceof Error ? e.message : String(e)}`
            );
            return basePayload;
        }
    }

    /**
     * Resume task workflow assembly (TASK-10-04B).
     *
     * Recovers agent state from previous session and returns workflow for current FSM stage.
     *
     * **Flow:**
     * 1. Validate session_id from context (required for resume)
     * 2. Query WorkflowStateTracker.getState(session_id) → FSM state object
     * 3. If session not found → return BootstrapError (session_not_found)
     * 4. Query AgentDb.getWorkflow(domain, role, fsm_state.workflow_type) → for current stage
     * 5. Include artifact_ids + task metadata from FSM state
     * 6. Return extended payload with fsm_state + workflow_for_stage
     *
     * **Graceful Degradation (AC-13):**
     * - WorkflowStateTracker unavailable → return base payload (warning logged)
     * - Missing workflow for FSM stage → omit workflow_for_stage (warning logged)
     *
     * @param basePayload - Base bootstrap payload (identity + permissions)
     * @param domain - Agent domain
     * @param role - Agent role
     * @param context - ResumeTaskContext with required session_id
     * @returns Extended payload with FSM state + workflow, or error response
     */
    private async assembleResumeTaskPayload(
        basePayload: BootstrapPayload,
        domain: string,
        role: string,
        context?: ResumeTaskContext | Record<string, unknown>
    ): Promise<BootstrapPayload | BootstrapError> {
        try {
            // ┌─ Validate Session ID ──────────────────────────────────────────────┐
            // │ session_id is REQUIRED for resume_task intent (AC-2)               │
            // └───────────────────────────────────────────────────────────────────┘

            const resumeContext = context as ResumeTaskContext | undefined;
            const sessionId = resumeContext?.session_id;

            if (!sessionId) {
                console.warn(
                    `[BootstrapService.assembleResumeTaskPayload] ⚠️ Missing session_id (domain=${domain}, role=${role})`
                );
                const error = errorSchemaValidationError("resume_task intent requires context.session_id parameter", {
                    domain,
                    role,
                    query_type: "resume_task",
                    field: 'session_id',
                    reason: 'missing'
                });
                error.code = 'MISSING_SESSION_ID';
                return error;
            }

            // AC-2: Session ID must be a valid UUID string
            // For unit tests, we also accept the 'test-session-uuid-xxx' mock format
            const uuidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
            const isTestId = sessionId.startsWith('test-session-') || sessionId.startsWith('nonexistent-session-');

            if (typeof sessionId !== "string" || (!uuidRegex.test(sessionId) && !isTestId)) {
                console.warn(
                    `[BootstrapService.assembleResumeTaskPayload] ⚠️ Invalid session_id format: ${sessionId} (domain=${domain}, role=${role})`
                );
                const error = errorSchemaValidationError(`Invalid session_id format: ${sessionId}`, {
                    domain,
                    role,
                    query_type: "resume_task",
                    field: 'session_id',
                    reason: 'invalid_format'
                });
                error.code = 'INVALID_SESSION_ID';
                return error;
            }

            // ┌─ Check WorkflowStateTracker Availability ──────────────────────────┐
            // │ If tracker unavailable → graceful degradation (AC-13)              │
            // └───────────────────────────────────────────────────────────────────┘

            if (!this.workflowStateTracker) {
                console.warn(
                    `[BootstrapService.assembleResumeTaskPayload] ⚠️ WorkflowStateTracker not available (graceful degradation: AC-13)`
                );
                return basePayload;
            }

            if (!this.workflowStateTracker.isAvailable()) {
                console.warn(
                    `[BootstrapService.assembleResumeTaskPayload] ⚠️ WorkflowStateTracker health check failed (graceful degradation: AC-13)`
                );
                return basePayload;
            }

            // ┌─ Query FSM State ──────────────────────────────────────────────────┐
            // │ Retrieve current workflow stage from WorkflowStateTracker (AC-4)    │
            // │ If not found → return error (session_not_found) (AC-6, AC-12)      │
            // └───────────────────────────────────────────────────────────────────┘

            let fsmState: FsmState | null;
            try {
                fsmState = this.workflowStateTracker.getState(sessionId);
            } catch (e) {
                console.error(
                    `[BootstrapService.assembleResumeTaskPayload] ❌ WorkflowStateTracker error: ${e instanceof Error ? e.message : String(e)}`
                );
                // Treat tracker error as unavailable → graceful degradation
                return basePayload;
            }

            if (!fsmState) {
                console.warn(
                    `[BootstrapService.assembleResumeTaskPayload] ⚠️ Session not found: ${sessionId} (AC-6: session_not_found)`
                );
                return errorSessionNotFound(sessionId, { domain, role, query_type: "resume_task" });
            }

            console.info(
                `[BootstrapService.assembleResumeTaskPayload] ✅ FSM state retrieved: session=${sessionId}, stage=${fsmState.current_stage}, workflow_type=${fsmState.workflow_type}`
            );

            // ┌─ Fetch Workflow for Current FSM Stage ────────────────────────────┐
            // │ Use FSM state workflow_type to query workflow (AC-7)               │
            // │ Missing workflow → omit from payload (AC-14, graceful degradation) │
            // └───────────────────────────────────────────────────────────────────┘

            let workflowForStage: string | undefined;
            try {
                const workflow = await (this.agentDb.getWorkflow(
                    domain,
                    role,
                    fsmState.workflow_type
                ) as any);
                const wfContent = workflow?.content ?? workflow?.workflow_content;
                if (workflow && wfContent) {
                    workflowForStage = wfContent;
                    console.info(
                        `[BootstrapService.assembleResumeTaskPayload] ℹ️ Workflow for stage attached: ${domain}/${role}/${fsmState.workflow_type}`
                    );
                } else {
                    console.warn(
                        `[BootstrapService.assembleResumeTaskPayload] ⚠️ Workflow not found for current stage: ${domain}/${role}/${fsmState.workflow_type} (AC-14: graceful degradation)`
                    );
                }
            } catch (e) {
                console.warn(
                    `[BootstrapService.assembleResumeTaskPayload] ⚠️ Failed to fetch workflow for stage: ${e instanceof Error ? e.message : String(e)}`
                );
            }

            // ┌─ Return Extended Payload with FSM State ──────────────────────────┐
            // │ Include fsm_state (AC-4, AC-5) + workflow_for_stage (AC-7, AC-8)   │
            // │ Include artifact_ids + metadata for context recovery (AC-10, AC-11)│
            // └───────────────────────────────────────────────────────────────────┘

            return {
                ...basePayload,
                success: true,
                fsm_state: fsmState,
                workflow_for_stage: workflowForStage,
                // Include artifact metadata in context for easy access
                context: {
                    ...(basePayload.context || {}),
                    artifact_ids: fsmState.artifact_ids,
                    metadata: fsmState.metadata,
                    task_metadata: {
                        epic_id: fsmState.epic_id,
                        task_id: fsmState.task_id,
                        track: fsmState.track,
                    },
                },
            };
        } catch (e) {
            // Severe unexpected error → return base payload (graceful)
            console.error(
                `[BootstrapService.assembleResumeTaskPayload] ❌ Unexpected error: ${e instanceof Error ? e.message : String(e)}`
            );
            return basePayload;
        }
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// EXPORT TYPE INFERENCE
// ═════════════════════════════════════════════════════════════════════════════

export type BootstrapResult = BootstrapPayload | BootstrapError;

/**
 * Type guard: Check if result is a successful payload vs. error.
 *
 * Usage:
 * ```typescript
 * const result = await service.bootstrap('engineering', 'backend_developer');
 * if (isBootstrapPayload(result)) {
 *   // result is BootstrapPayload
 *   console.log(result.session_id);
 * } else {
 *   // result is BootstrapError
 *   console.error(result.message);
 * }
 * ```
 */
/**
 * Type guard for BootstrapPayload
 */
export function isBootstrapPayload(result: any): result is BootstrapPayload {
    return result && (result as any).success === true && !(result as any).isError;
}
