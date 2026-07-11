import { z } from 'zod';
import { v4 as uuidv4 } from 'uuid';
import { McpContext } from '../middleware/contextMiddleware';
import { ErrorResponses } from '../ErrorResponses';
import { Plugin, Tool } from '../../plugins/PluginDecorators';
import { BasePlugin } from '../../plugins/BasePlugin';

/**
 * Bootstrap Plugin (TASK-14-04)
 *
 * Replaces the legacy bootstrap tool with a modular plugin.
 * Handles agent initialization, session creation/recovery, and context loading.
 */
@Plugin({
    name: "bootstrap",
    version: "1.0.0"
})
export class BootstrapPlugin extends BasePlugin {

    private isAdminRole(role: string | undefined): boolean {
        if (!role) return false;
        return role === 'admin' || role === 'admin_agent' || role === 'ops_admin';
    }

    @Tool({
        name: "bootstrap_agent",
        description: "Load complete agent context (roles, permissions, workflows, templates) and initialize session",
        schema: z.union([
            z.object({
                agentId: z.string().uuid("Invalid agentId: must be a valid UUID"),
                sessionId: z.string().uuid("Invalid sessionId: must be a valid UUID").optional(),
                discoveryPhase: z.enum(["discovery", "delivery", "evaluation"]),
            }),
            z.object({
                track: z.enum(["discovery", "delivery", "evaluation"]),
                role: z.string(),
                session_id: z.string().uuid().optional()
            })
        ])
    })
    async bootstrapAgent(args: any, context: McpContext) {
        const isLegacyShape = typeof args.track === 'string' && typeof args.role === 'string';
        const agentId = isLegacyShape ? uuidv4() : args.agentId;
        const providedSessionId = isLegacyShape ? args.session_id : args.sessionId;
        const discoveryPhase = isLegacyShape ? args.track : args.discoveryPhase;
        const { agentDb, sessionManager, rbacFilter, workflowTracker } = this.context;

        try {
            // AC-15: Idempotency & AC-10: Session Recovery
            let sessionId = providedSessionId;
            let session = sessionId ? sessionManager.get(sessionId) : undefined;

            if (session) {
                // If session exists, verify it matches the agent and phase (optional but good practice)
                if (session.agent_id !== agentId && session.agent_id !== null) {
                    console.warn(`[Bootstrap] Session ${sessionId} already exists for a different agent. Creating new session.`);
                    sessionId = uuidv4();
                    session = undefined;
                }
            } else {
                sessionId = sessionId || uuidv4();
            }

            // EPIC-17 TASK-17-03: resolve registry-backed domain id for session context.
            // Graceful fallback: if not found / method unavailable => null.
            let currentDomainId: string | null = null;
            try {
                if (typeof (agentDb as any).getRegisteredDomain === 'function') {
                    const domainRow = (agentDb as any).getRegisteredDomain(discoveryPhase) as { id?: string } | null;
                    currentDomainId = domainRow?.id ?? null;
                    if (!currentDomainId) {
                        console.warn(`[Bootstrap] Domain not found in domains registry: ${discoveryPhase}. current_domain_id set to null.`);
                    }
                }
            } catch (err: any) {
                console.warn(`[Bootstrap] Domain resolution failed for ${discoveryPhase}: ${err?.message || String(err)}`);
            }

            // AC-05: Load roles for the phase (EPIC-17: optional domain_id-aware filtering)
            const effectiveDomainId = context.domain_id ?? currentDomainId;
            const roles = agentDb.getRolesByDomain(discoveryPhase, effectiveDomainId);
            if (roles.length === 0) {
                return ErrorResponses.badRequest(`No roles found for phase: ${discoveryPhase}`);
            }

            // AC-06: Load MCP tool permissions + RBAC
            const primaryRole = roles[0]; // Assuming first role is primary for context
            const allowedTools = Array.from(rbacFilter.getAllowedTools(primaryRole.role_name));

            // AC-09: Session Creation (if not recovered)
            if (!session) {
                session = sessionManager.register(
                    primaryRole.role_name,
                    discoveryPhase,
                    agentId,
                    sessionId,
                    currentDomainId
                );
            } else if (typeof (sessionManager as any).setCurrentDomainId === 'function') {
                (sessionManager as any).setCurrentDomainId(sessionId, currentDomainId);
                session = sessionManager.get(sessionId) || session;
            }

            // AC-07: Workflow Loading (FSM machines)
            const workflows = agentDb.getWorkflowsByRole(discoveryPhase, primaryRole.role_name, effectiveDomainId);

            // AC-11: Validate FSM state (Ensure session is in-db)
            try {
                // Check if workflow tracker has it, or init it
                try {
                    workflowTracker.getState(sessionId);
                } catch (e) {
                    // Init if not exists
                    const defaultWorkflow = workflows.length > 0 ? workflows[0].workflow_type : 'default';
                    workflowTracker.createSession({
                        sessionId,
                        domain: discoveryPhase,
                        roleName: primaryRole.role_name,
                        workflowId: defaultWorkflow,
                        track: discoveryPhase
                    });
                }

                // Ensure track is locked for the lifetime of the session (AC-1/AC-2)
                if (typeof (workflowTracker as any).lockTrack === 'function') {
                    (workflowTracker as any).lockTrack(sessionId, discoveryPhase);
                }
            } catch (fsmErr) {
                console.error(`[Bootstrap] FSM init error: ${fsmErr}`);
            }

            // AC-08: Template Loading
            const templates = agentDb.getTemplatesByRole(discoveryPhase, primaryRole.role_name, effectiveDomainId);

            // AC-16: Context Migration (Placeholder for schema v1.0)
            const agentContext = {
                agentId,
                sessionId,
                roles: roles.map(r => r.role_name),
                permissions: allowedTools,
                workflows: workflows.map(w => ({ type: w.workflow_type, content: w.content })),
                templates: templates.map(t => ({ name: t.template_name, content: t.content })),
                schemaVersion: "1.0"
            };

            return {
                success: true,
                data: {
                    session_id: sessionId,
                    track: discoveryPhase,
                    role: primaryRole.role_name,
                    domain_id: (session as any)?.current_domain_id ?? null,
                    agentContext,
                    timestamp: new Date().toISOString(),
                }
            };

        } catch (error: any) {
            console.error(`[Bootstrap] Tool error: ${error.message}`);
            return {
                success: false,
                code: "DB_ERROR",
                message: `Database error during bootstrap: ${error.message}`
            };
        }
    }

    @Tool({
        name: 'list_available_domains',
        description: 'List registered domains available in the domain registry',
        schema: z.object({
            include_unregistered: z.boolean().optional().default(false)
        })
    })
    async listAvailableDomains(args: { include_unregistered?: boolean }, _context: McpContext) {
        const { agentDb } = this.context;

        try {
            const includeUnregistered = !!args?.include_unregistered;
            const registered = typeof (agentDb as any).listRegisteredDomains === 'function'
                ? (agentDb as any).listRegisteredDomains() as Array<{ id: string; name: string; description?: string | null }>
                : [];

            if (!includeUnregistered) {
                return {
                    success: true,
                    data: {
                        domains: registered,
                        total: registered.length
                    }
                };
            }

            const roleDomains = typeof (agentDb as any).listDomains === 'function'
                ? (agentDb as any).listDomains() as string[]
                : [];

            const byName = new Map<string, { id: string; name: string; description?: string | null }>();
            for (const row of registered) {
                byName.set(row.name, row);
            }
            for (const domain of roleDomains) {
                if (!byName.has(domain)) {
                    byName.set(domain, { id: domain, name: domain, description: null });
                }
            }

            const merged = Array.from(byName.values()).sort((a, b) => a.name.localeCompare(b.name));
            return {
                success: true,
                data: {
                    domains: merged,
                    total: merged.length
                }
            };
        } catch (error: any) {
            return ErrorResponses.internalError('Failed to list available domains', {
                message: error?.message || String(error)
            });
        }
    }

    @Tool({
        name: 'switch_domain',
        description: 'Switch current session domain context (admin only)',
        schema: z.object({
            domain_name: z.string().min(1),
            session_id: z.string().optional()
        })
    })
    async switchDomain(args: { domain_name: string; session_id?: string }, context: McpContext) {
        const { agentDb, sessionManager } = this.context;
        const targetDomain = args.domain_name;
        const sessionId = args.session_id || context.session_id;

        if (!sessionId) {
            return ErrorResponses.badRequest('switch_domain requires session_id');
        }

        if (!this.isAdminRole(context.role)) {
            return ErrorResponses.forbidden('switch_domain is admin-only', {
                role: context.role,
                session_id: sessionId
            });
        }

        try {
            const domainRow = typeof (agentDb as any).getRegisteredDomain === 'function'
                ? (agentDb as any).getRegisteredDomain(targetDomain) as { id?: string; name?: string } | null
                : null;

            if (!domainRow?.id) {
                return ErrorResponses.notFound(`Domain not found: ${targetDomain}`);
            }

            if (typeof (sessionManager as any).setCurrentDomainId === 'function') {
                (sessionManager as any).setCurrentDomainId(sessionId, domainRow.id);
            }

            const session = sessionManager.get(sessionId);
            if (!session) {
                return ErrorResponses.notFound(`Session not found: ${sessionId}`);
            }

            console.info(`[Bootstrap.switch_domain] session=${sessionId} switched to domain=${domainRow.name || targetDomain} (${domainRow.id})`);

            return {
                success: true,
                data: {
                    session_id: sessionId,
                    domain_name: domainRow.name || targetDomain,
                    domain_id: domainRow.id,
                    current_domain_id: session.current_domain_id ?? domainRow.id
                }
            };
        } catch (error: any) {
            return ErrorResponses.internalError('Failed to switch domain', {
                session_id: sessionId,
                target_domain: targetDomain,
                message: error?.message || String(error)
            });
        }
    }

    /**
     * Optional: Implement onInit for pre-load validation
     */
    async onInit() {
        console.info("[BootstrapPlugin] Initializing...");
        // Any pre-load checks can go here
    }
}

export default BootstrapPlugin;
