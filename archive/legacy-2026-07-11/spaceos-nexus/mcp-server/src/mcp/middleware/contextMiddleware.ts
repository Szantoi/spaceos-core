import { SessionManager } from '../SessionManager';
import { WorkflowStateTracker } from '../../metadata/WorkflowStateTracker';
import { ErrorCodes } from '../ErrorCodes';
import { WorkflowTrack } from '../../metadata/FSMTypes';
import { SamplingRequest, SamplingResult, SamplingService } from '../sampling/SamplingService';

/**
 * Interface representing the context injected into every MCP tool call.
 */
export interface McpContext {
    session_id: string;
    user_id?: string;
    domain: string;
    domain_id?: string | null;
    role: string;
    phase: string;
    track: WorkflowTrack | null;
    requestSampling?: (request: SamplingRequest) => Promise<SamplingResult>;
}

/**
 * ContextMiddleware — Responsible for extracting and validating session context.
 * Injects McpContext into the request flow.
 */
export class ContextMiddleware {
    private sessionManager: SessionManager;
    private workflowTracker: WorkflowStateTracker;
    private samplingService?: SamplingService;

    constructor(sessionManager: SessionManager, workflowTracker: WorkflowStateTracker, samplingService?: SamplingService) {
        this.sessionManager = sessionManager;
        this.workflowTracker = workflowTracker;
        this.samplingService = samplingService;
    }

    /**
     * Processes an incoming MCP tool call to extract context.
     *
     * @param payload The incoming MCP request payload (callTool)
     * @param headers HTTP headers (optional fallback)
     * @returns Injected context object
     * @throws Error with ErrorCodes.UNAUTHORIZED if session is missing or invalid
     */
    public handle(payload: any, headers?: Record<string, string | string[]>): McpContext {
        // 1. Extract session_id from payload (arguments) or headers
        const sessionId = (payload.arguments?.session_id || headers?.['x-session-id']) as string;

        if (!sessionId) {
            throw this.createError(ErrorCodes.UNAUTHORIZED, 'Missing session_id in request');
        }

        // 2. Get session details from SessionManager
        const session = this.sessionManager.get(sessionId);
        if (!session) {
            throw this.createError(ErrorCodes.UNAUTHORIZED, `Invalid or expired session: ${sessionId}`);
        }

        // EPIC-17: Runtime domain context can be switched via current_domain_id.
        const resolvedDomainName = this.sessionManager.resolveDomainNameById(session.current_domain_id);
        const effectiveDomain = resolvedDomainName || session.domain;

        // 3. Get current phase from WorkflowStateTracker
        // Assuming session_id is used as project_id in WorkflowStateTracker
        const workflowState = this.workflowTracker.getState(sessionId);
        const phase = workflowState?.state || session.status || 'unknown';

        // Track is derived from workflow state or inferred from domain.
        const inferredTrack = workflowState?.track || (session.domain === 'discovery' ? 'discovery' : 'delivery');

        // Persist track for future requests if it was not already set.
        if (!workflowState?.track) {
            try {
                this.workflowTracker.lockTrack(sessionId, inferredTrack as WorkflowTrack);
            } catch {
                // Best effort: do not block request if locking fails.
            }
        }

        const track = inferredTrack;

        // 4. Extract user_id if present
        const userId = (payload.arguments?.user_id || headers?.['x-user-id']) as string | undefined;

        const context: McpContext = {
            session_id: sessionId,
            user_id: userId,
            domain: effectiveDomain,
            domain_id: session.current_domain_id ?? null,
            role: session.role,
            phase: phase,
            track: track || null
        };

        if (this.samplingService) {
            context.requestSampling = async (request: SamplingRequest): Promise<SamplingResult> => {
                return this.samplingService!.requestSampling(sessionId, request);
            };
        }

        return context;
    }

    private createError(code: ErrorCodes, message: string) {
        const err = new Error(message);
        (err as any).code = code;
        return err;
    }
}
