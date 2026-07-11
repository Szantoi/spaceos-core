import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { SSEServerTransport } from '@modelcontextprotocol/sdk/server/sse.js';
import { StreamableHTTPServerTransport } from '@modelcontextprotocol/sdk/server/streamableHttp.js';
import { Router, Request, Response } from 'express';
import { randomUUID } from 'node:crypto';
import { z } from 'zod';
import { join } from 'node:path';
import { DocumentServer, ArtifactSubmitRequest } from './DocumentServer';
import { RbacFilter } from './RbacFilter';
import { queryKnowledge } from '../rag/VectorStore';
import { WorkflowStateTracker } from '../metadata/WorkflowStateTracker'; // WORKFLOW-STATE
import { SessionManager } from './SessionManager'; // WRITE-LAYER
import { ResourceTracker } from '../metadata/ResourceTracker'; // WRITE-LAYER
import { AgentDb } from './AgentDb'; // CONTEXT-LAYER
import { BootstrapService } from './BootstrapService'; // TASK-10-01
import { isBootstrapError } from './ErrorResponses'; // Error type guard
import { ContextMiddleware, McpContext } from './middleware/contextMiddleware';
import { IToolModule } from './tools/IToolModule';
import { PluginManager } from '../plugins/PluginManager';
import { SystemContext } from '../plugins/PluginTypes';
import { createContextToolModule } from './tools/context';
import { createDiscoveryToolModule } from './tools/discovery';
import { createEvaluatorToolModule } from './tools/evaluator';
import { createPmQueryToolModule } from './tools/pm-query';
import { GuardrailService } from '../roles/GuardrailService'; // TASK-14-06
import { AuditLogger } from '../metadata/auditLogger';
import { ErrorResponses } from './ErrorResponses';
import { ErrorCodes } from './ErrorCodes';
import { TrackRouter } from '../metadata/TrackRouter';
import { SamplingService } from './sampling/SamplingService';

// Node.js CommonJS provides __dirname by default.
const pluginsDir = join(__dirname, 'tools');

/**
 * Helper function to create a PluginManager for use by HTTPTransport and other components.
 * This extracts the plugin initialization logic from createMcpServerRouter so we can
 * initialize plugins before or independently of the main MCP router.
 *
 * TASK-14-02: Used by HTTPTransport to enable `/mcp/call` tool invocation over HTTP.
 */
export function createPluginManager(
    agentDb: AgentDb,
    sessionManager: SessionManager,
    rbacFilter: RbacFilter,
    workflowTracker: WorkflowStateTracker,
    guardrailService: GuardrailService
): PluginManager {
    const systemContext: SystemContext = {
        agentDb,
        sessionManager,
        rbacFilter,
        workflowTracker,
        guardrailService
    };
    const pluginManager = new PluginManager(systemContext);

    // Register manifests for all core plugins, then load by name.
    // Manifests must be registered BEFORE calling loadPlugin.
    pluginManager.registerManifest({
        name: 'bootstrap',
        version: '1.0.0',
        entry: join(pluginsDir, 'bootstrap.ts'),
        className: 'BootstrapPlugin'
    });
    pluginManager.registerManifest({
        name: 'context',
        version: '1.0.0',
        entry: join(pluginsDir, 'context.ts'),
        className: 'ContextPlugin'
    });
    pluginManager.registerManifest({
        name: 'discovery',
        version: '1.0.0',
        entry: join(pluginsDir, 'discovery.ts'),
        className: 'DiscoveryPlugin',
        dependencies: ['bootstrap']
    });
    pluginManager.registerManifest({
        name: 'pm-query',
        version: '1.0.0',
        entry: join(pluginsDir, 'pm-query.ts'),
        className: 'PmQueryPlugin',
        dependencies: ['bootstrap']
    });
    pluginManager.registerManifest({
        name: 'messaging',
        version: '1.0.0',
        entry: join(pluginsDir, 'messaging.ts'),
        className: 'MessagingPlugin'
    });

    // Load plugins in dependency order (bootstrap first)
    pluginManager.loadPlugin('bootstrap', true).catch(err => {
        console.error(`[PluginManager] Failed to load bootstrap plugin: ${err.message}`);
    });
    pluginManager.loadPlugin('context').catch(err => {
        console.error(`[PluginManager] Failed to load context plugin: ${err.message}`);
    });
    pluginManager.loadPlugin('discovery').catch(err => {
        console.error(`[PluginManager] Failed to load discovery plugin: ${err.message}`);
    });
    pluginManager.loadPlugin('pm-query').catch(err => {
        console.error(`[PluginManager] Failed to load pm-query plugin: ${err.message}`);
    });
    pluginManager.loadPlugin('messaging').catch(err => {
        console.error(`[PluginManager] Failed to load messaging plugin: ${err.message}`);
    });

    return pluginManager;
}

/**
 * Creates and returns an MCP-compliant server with all tools registered.
 * Supports both:
 *   - SSE transport   → GET /mcp/sse  (current VS Code Copilot support)
 *   - Streamable HTTP → POST /mcp/http (modern standard, future-proof)
 *
 * VS Code mcp.json config:
 *   { "servers": { "joinerytech": { "type": "sse", "url": "http://localhost:3000/mcp/sse" } } }
 */
export function createMcpServerRouter(
    documentServer: DocumentServer,
    workflowTracker?: WorkflowStateTracker,
    rbacFilter?: RbacFilter,
    sessionManager?: SessionManager,      // WRITE-LAYER
    resourceTracker?: ResourceTracker,    // WRITE-LAYER
    agentDb?: AgentDb,                    // CONTEXT-LAYER
    bootstrapService?: BootstrapService,  // TASK-10-01
    guardrailService?: GuardrailService   // TASK-14-06
): Router {
    const router = Router();

    // ─── TASK-11-07: Context & Audit Initialization ──────────────────────────
    const auditLogger = sessionManager && (sessionManager as any).connectionManager
        ? new AuditLogger((sessionManager as any).connectionManager)
        : null;

    const samplingService = new SamplingService();

    const contextMiddleware = sessionManager && workflowTracker
        ? new ContextMiddleware(sessionManager, workflowTracker, samplingService)
        : null;

    const trackRouter = workflowTracker && rbacFilter ? new TrackRouter(workflowTracker, rbacFilter) : null;

    // ─── TASK-14-03/04: Plugin System Initialization ────────────────────────
    // Note: As of TASK-14-02, PluginManager is created in index.ts via createPluginManager()
    // and passed to HTTPTransport for tool invocation. This mcpServerRouter now receives
    // the pre-initialized pluginManager as needed for SSE/StreamableHTTP transports.
    let pluginManager: PluginManager | null = null;
    let pluginLoadPromise: Promise<void> = Promise.resolve();
    if (agentDb && sessionManager && rbacFilter && workflowTracker && guardrailService) {
        // This block allows backward compatibility: if pluginManager hasn't been created yet,
        // we create it here. In the normal flow, index.ts creates it and passes it via parameter.
        // For now, keep this for SSE transport support where we still need plugins.
        const systemContext: SystemContext = {
            agentDb,
            sessionManager,
            rbacFilter,
            workflowTracker,
            guardrailService
        };
        pluginManager = new PluginManager(systemContext);

        // Register manifests, then load by name
        pluginManager.registerManifest({
            name: 'bootstrap',
            version: '1.0.0',
            entry: join(pluginsDir, 'bootstrap.ts'),
            className: 'BootstrapPlugin'
        });
        pluginManager.registerManifest({
            name: 'context',
            version: '1.0.0',
            entry: join(pluginsDir, 'context.ts'),
            className: 'ContextPlugin'
        });
        pluginManager.registerManifest({
            name: 'discovery',
            version: '1.0.0',
            entry: join(pluginsDir, 'discovery.ts'),
            className: 'DiscoveryPlugin',
            dependencies: ['bootstrap']
        });
        pluginManager.registerManifest({
            name: 'pm-query',
            version: '1.0.0',
            entry: join(pluginsDir, 'pm-query.ts'),
            className: 'PmQueryPlugin',
            dependencies: ['bootstrap']
        });
        pluginManager.registerManifest({
            name: 'messaging',
            version: '1.0.0',
            entry: join(pluginsDir, 'messaging.ts'),
            className: 'MessagingPlugin'
        });

        const pluginLoadTasks: Promise<void>[] = [
            pluginManager.loadPlugin('bootstrap', true).catch(err => {
                console.error(`[PluginManager] Failed to load bootstrap plugin: ${err.message}`);
            }),
            pluginManager.loadPlugin('context').catch(err => {
                console.error(`[PluginManager] Failed to load context plugin: ${err.message}`);
            }),
            pluginManager.loadPlugin('discovery').catch(err => {
                console.error(`[PluginManager] Failed to load discovery plugin: ${err.message}`);
            }),
            pluginManager.loadPlugin('pm-query').catch(err => {
                console.error(`[PluginManager] Failed to load pm-query plugin: ${err.message}`);
            }),
            pluginManager.loadPlugin('messaging').catch(err => {
                console.error(`[PluginManager] Failed to load messaging plugin: ${err.message}`);
            })
        ];

        pluginLoadPromise = Promise.allSettled(pluginLoadTasks).then(() => undefined);
    }

    // ─── Tool Definitions ─────────────────────────────────────────────────────

    function buildMcpServer(activeRole: string, headers?: Record<string, any>): McpServer {
        const server = new McpServer({
            name: 'joinerytech-agent-system',
            version: '1.0.0',
        });

        const DISCOVERY_TOOLS = new Set([
            'request_context',
            'lookup_context',
            'discovery_roles',
            'discovery_workflows',
            'discovery_templates',
            'discovery_search',
            'reference_prior_discovery',
            'submit_discovery_outcome',
            'track_blocker',
            'query_blockers',
            'agent_context',
            'search_knowledge_base',
            'list_workflows',
            'validate_input',
            'brainstorm'
        ]);
        const DELIVERY_TOOLS = new Set([
            'evaluate_compliance',
            'bootstrap_agent',
            'submit_workflow',
            'get_workflow_state',
            'list_artifacts',
            'record_result'
        ]);

        const isAllowed = (toolName: string, track?: 'discovery' | 'delivery' | null) => {
            if (!rbacFilter) return true;
            return rbacFilter.hasPermission(toolName, activeRole, track);
        };

        const registeredToolNames = new Set<string>();

        const registerTool = <T extends z.ZodRawShape>(
            name: string,
            description: string,
            schema: T,
            handler: (args: any, context: McpContext) => Promise<any>
        ) => {
            // Always register tools; RBAC is enforced at runtime per-session and per-track.
            if (registeredToolNames.has(name)) {
                console.warn(`[MCP] Skipping duplicate tool registration: ${name}`);
                return;
            }
            registeredToolNames.add(name);

            (server as any).tool(name, description, schema, async (args: any) => {
                const startTime = Date.now();
                let context: McpContext | null = null;

                try {
                    // 1. Extract & Validate Context
                    if (contextMiddleware) {
                        context = contextMiddleware.handle({ arguments: args }, headers);
                    } else {
                        // Fallback context when middleware is not installed (e.g. unit tests)
                        context = {
                            session_id: 'unknown',
                            domain: 'public',
                            role: activeRole,
                            phase: 'unknown',
                            track: null
                        };
                    }

                    // 1b. Ensure track-based RBAC (AC-2)
                    if (rbacFilter && context) {
                        if (!rbacFilter.hasPermission(name, activeRole, context.track)) {
                            const err: any = new Error(`Unauthorized tool "${name}" for track ${context.track}`);
                            err.code = ErrorCodes.UNAUTHORIZED;
                            throw err;
                        }
                    }

                    // 2. TASK-11-08: Two-Track Routing Check
                    if (trackRouter && context && context.session_id !== 'unknown') {
                        const requiredTrack = DISCOVERY_TOOLS.has(name) ? 'discovery' : (DELIVERY_TOOLS.has(name) ? 'delivery' : null);

                        if (requiredTrack) {
                            try {
                                trackRouter.route(context.session_id, requiredTrack);
                            } catch (err: any) {
                                if (err.name === 'TrackImmutabilityError' || err.name === 'AuthorizationError') {
                                    return ErrorResponses.unauthorizedTrackAccess(
                                        name,
                                        err.name === 'TrackImmutabilityError' ? 'locked' : 'unauthorized',
                                        requiredTrack
                                    );
                                }
                                return ErrorResponses.internalError(err.message, { code: 'TRACK_ERROR' });
                            }
                        }
                    }

                    // 3. Execute Handler
                    // Context is guaranteed to be set by now (either from middleware or fallback)
                    if (!context) throw new Error('Context failed to initialize');

                    const result = await handler(args, context);

                    // 3. Log Audit (SUCCESS) - Non-blocking
                    if (auditLogger && context) {
                        setImmediate(() => {
                            auditLogger.log({
                                session_id: context!.session_id,
                                domain: context!.domain,
                                role: context!.role,
                                tool_name: name,
                                input: args,
                                output: result,
                                latency_ms: Date.now() - startTime,
                                status_code: 'SUCCESS'
                            }).catch(err => console.error(`[AuditLog Error] ${err.message}`));
                        });
                    }

                    return result;

                } catch (err: any) {
                    const statusCode = err.code || 'INTERNAL_ERROR';
                    const errorResponse = err.isError ? err : ErrorResponses.internalError(err.message, { code: statusCode });

                    // Log Audit (ERROR) - Non-blocking
                    if (auditLogger) {
                        setImmediate(() => {
                            auditLogger.log({
                                session_id: context?.session_id || 'unknown',
                                domain: context?.domain || 'unknown',
                                role: context?.role || activeRole,
                                tool_name: name,
                                input: args,
                                output: errorResponse,
                                latency_ms: Date.now() - startTime,
                                status_code: statusCode
                            }).catch(err => console.error(`[AuditLog Error] ${err.message}`));
                        });
                    }

                    return errorResponse;
                }
            });
        };

        const registerModule = (module: IToolModule) => {
            for (const tool of module.tools) {
                const handler = module.handlers[tool.name];
                if (handler) {
                    const schemaShape =
                        (tool.inputSchema as any)?.shape && typeof (tool.inputSchema as any).shape === 'object'
                            ? (tool.inputSchema as any).shape
                            : {};
                    registerTool(tool.name, tool.description, schemaShape as z.ZodRawShape, handler);
                }
            }
        };

        // ── Plugins ────────────────────────────────────────────────────────────
        // Register Context Module
        registerModule(createContextToolModule());

        // Register Discovery Module
        if (agentDb) {
            registerModule(createDiscoveryToolModule(agentDb));
            registerModule(createPmQueryToolModule(agentDb));
        }

        // Register Evaluator Module
        if (guardrailService) {
            registerModule(createEvaluatorToolModule(guardrailService));
        }

        // --- PLUGIN SYSTEM TOOLS (TASK-14-04) ---
        if (pluginManager) {
            const pluginRegisteredElsewhere = new Set(['bootstrap', 'context', 'discovery', 'pm-query']);
            const loadedModules = pluginManager
                .getLoadedPluginModules()
                .filter(module => !pluginRegisteredElsewhere.has(module.name));
            for (const module of loadedModules) {
                registerModule(module);
            }
        }

        if (pluginManager) {
            registerTool(
                'bootstrap_agent',
                'Load complete agent context (roles, permissions, workflows, templates) and initialize session.',
                {
                    agentId: z.string().uuid().optional(),
                    sessionId: z.string().uuid().optional(),
                    discoveryPhase: z.enum(['discovery', 'delivery', 'evaluation']).optional(),
                    track: z.enum(['discovery', 'delivery', 'evaluation']).optional(),
                    role: z.string().optional(),
                    session_id: z.string().uuid().optional()
                },
                async (args, context) => pluginManager.invokeTool('bootstrap_agent', args, context)
            );

            registerTool(
                'list_available_domains',
                'List registered domains available in the domain registry.',
                {
                    include_unregistered: z.boolean().optional().default(false)
                },
                async (args, context) => pluginManager.invokeTool('list_available_domains', args, context)
            );

            registerTool(
                'switch_domain',
                'Switch current session domain context (admin only).',
                {
                    domain_name: z.string().min(1),
                    session_id: z.string().optional()
                },
                async (args, context) => pluginManager.invokeTool('switch_domain', args, context)
            );
        }



        // ── get_core ──────────────────────────────────────────────────────────
        registerTool(
            'get_core',
            'Returns a core/standards document (e.g. runbook, constraints, definition_of_done). These must always be loaded in full — a single missing rule can cause critical process errors.',
            {
                doc: z.string().describe('Document name without extension (e.g. "runbook", "constraints", "definition_of_done", "error_recovery")'),
                domain: z.string().optional().describe('Domain for role-specific core docs (e.g. "engineering")'),
                role: z.string().optional().describe('Role for role-specific core docs (e.g. "backend_developer")'),
            },
            async ({ doc, domain, role }) => {
                try {
                    const content = documentServer.getCore(doc, domain, role);
                    return { content: [{ type: 'text' as const, text: content }] };
                } catch (e: any) {
                    return ErrorResponses.internalError(e.message);
                }
            }
        );

        // ── get_policy ────────────────────────────────────────────────────────
        registerTool(
            'get_policy',
            'Returns a general policy document (e.g. orchestrator.policy.md) from the docs folder.',
            {
                name: z.string().describe('Policy name without extension (e.g. "orchestrator", "git-workflow")'),
            },
            async ({ name }) => {
                try {
                    const content = documentServer.getPolicy(name);
                    return { content: [{ type: 'text' as const, text: content }] };
                } catch (e: any) {
                    return ErrorResponses.internalError(e.message);
                }
            }
        );

        // ── list_domains ──────────────────────────────────────────────────────
        registerTool(
            'list_domains',
            'Lists all available agent domains in the system (e.g. engineering, management, discovery, agentops).',
            {},
            async () => {
                const domains = documentServer.listDomains();
                return { content: [{ type: 'text' as const, text: domains.join('\n') }] };
            }
        );

        // --- WORKFLOW-STATE START ---
        // Tools: get_workflow_state, validate_workflow_step, request_workflow_transition
        // Rollback: delete this entire block and the import/param above
        if (workflowTracker) {
            // ── get_workflow_state ──────────────────────────────────────────
            registerTool(
                'get_workflow_state',
                'Returns the current FSM workflow state (current_state, retry_count, workflow_id) for a project.',
                {
                    project_id: z.string().describe('Unique project identifier'),
                    workflow_id: z.string().optional().describe('Optional workflow schema ID (e.g. agile-epic-lifecycle-v1) if the project is not yet initialized.')
                },
                async ({ project_id, workflow_id }) => {
                    try {
                        const legacyTracker = workflowTracker as any;
                        const state = legacyTracker.getState(project_id);
                        if (!state) {
                            const init = legacyTracker.initProject(project_id, workflow_id);
                            return { content: [{ type: 'text' as const, text: JSON.stringify({ initialized: true, state: init }) }] };
                        }
                        let output = JSON.stringify(state);
                        if (state.retry_count >= 2) {
                            output += '\n\n🚨 WARNING: Step-Back Injection Active. You have failed this state multiple times. You MUST step back and propose 3 radically different architectural approaches.';
                        }
                        return { content: [{ type: 'text' as const, text: output }] };
                    } catch (err: any) {
                        return ErrorResponses.internalError(err.message);
                    }
                }
            );

            // ── request_workflow_transition ──────────────────────────────────
            registerTool(
                'request_workflow_transition',
                'Requests an FSM phase transition via a success or fail action. The server evaluates prerequisites based on the FSM schema.',
                {
                    project_id: z.string().describe('Unique project identifier'),
                    action: z.enum(['success', 'fail']).describe('The action outcome of the current state'),
                },
                async ({ project_id, action }) => {
                    try {
                        const legacyTracker = workflowTracker as any;
                        const result = legacyTracker.requestTransition(project_id, action);
                        let output = result.ok
                            ? `${result.message}\nNew State: ${result.state.current_state} (Retries: ${result.state.retry_count})`
                            : result.message;
                        if (result.state && result.state.retry_count >= 2) {
                            output += '\n\n🚨 WARNING: Step-Back Injection Active for the next execution. You MUST step back and propose 3 radically different architectural approaches instead of quick fixes.';
                        }
                        return { content: [{ type: 'text' as const, text: output }], isError: !result.ok };
                    } catch (err: any) {
                        return ErrorResponses.internalError(err.message);
                    }
                }
            );
        }
        // --- WORKFLOW-STATE END ---

        // --- WRITE-LAYER START ---
        // session_register, artifact_submit, session_complete
        // These tools close the control gap: agents can no longer write files directly.
        // All artifact output goes through the server (validate → write → register).

        if (sessionManager) {
            // ── session_register ──────────────────────────────────────────
            registerTool(
                'session_register',
                'Register a new agent session before starting work. Returns a session_id you must include in artifact_submit calls. ' +
                'Without registration, sessions cannot be verified for documentation compliance.',
                {
                    role: z.string().describe('The active role name (e.g. "backend_developer")'),
                    domain: z.string().describe('The domain (e.g. "engineering", "management")'),
                    agent_name: z.string().optional().describe('Optional: a human-readable name or agent instance identifier'),
                },
                async ({ role, domain, agent_name }) => {
                    try {
                        const session = sessionManager.register(role, domain, agent_name);
                        return {
                            content: [{
                                type: 'text' as const, text: JSON.stringify({
                                    session_id: session.id,
                                    role: session.role,
                                    domain: session.domain,
                                    status: session.status,
                                    created_at: session.created_at,
                                    message: 'Session registered. Include session_id in all artifact_submit calls.'
                                })
                            }]
                        };
                    } catch (e: any) {
                        return ErrorResponses.internalError(e.message);
                    }
                }
            );

            // ── artifact_submit ───────────────────────────────────────────
            registerTool(
                'artifact_submit',
                'Submit a completed artifact (Markdown document with frontmatter) to the server. ' +
                'The server validates the template type, checks required frontmatter fields, writes the file, ' +
                'and registers it in the ResourceTracker. ' +
                'You MUST submit at least one artifact before calling session_complete.',
                {
                    domain: z.string().describe('Domain of the submitting role'),
                    role: z.string().describe('Role name'),
                    type: z.string().describe('Template type (e.g. "implementation_report", "qa_signoff")'),
                    content: z.string().describe('Full Markdown content including YAML frontmatter (--- blocks)'),
                    target_path: z.string().describe('Destination path relative to workspace root, e.g. "docs/joinerytech-flow/epics/EPIC-01/implementation-summary/TASK-01-fix.md"'),
                    project: z.string().describe('Project identifier for ResourceTracker (e.g. "mcp-maintenance")'),
                    user: z.string().describe('Submitting agent or user identifier'),
                    session_id: z.string().optional().describe('session_id from session_register — required for session_complete checkpoint'),
                },
                async ({ domain, role, type, content, target_path, project, user, session_id }) => {
                    try {
                        const result = documentServer.submitArtifact(
                            { domain, role, type, content, target_path, project, user, session_id } as ArtifactSubmitRequest
                        );
                        return {
                            content: [{ type: 'text' as const, text: JSON.stringify(result) }],
                            isError: !result.ok
                        };
                    } catch (e: any) {
                        return ErrorResponses.internalError(e.message);
                    }
                }
            );

            // ── session_complete ───────────────────────────────────────────
            registerTool(
                'session_complete',
                'Declare your task complete and close your session. ' +
                'The server verifies that at least one artifact was submitted this session. ' +
                'If no artifacts exist, the session is BLOCKED and you must submit documentation first.',
                {
                    session_id: z.string().describe('session_id returned by session_register'),
                },
                async ({ session_id }) => {
                    try {
                        const session = sessionManager.get(session_id);
                        if (!session) return ErrorResponses.notFound(`Session not found: ${session_id}`);

                        if (session.status !== 'active') {
                            return ErrorResponses.badRequest(`Session already in terminal state: ${session.status}`, { session });
                        }
                        const artifacts = resourceTracker ? resourceTracker.getBySession(session_id) : [];
                        if (artifacts.length === 0) {
                            const blocked = sessionManager.block(session_id);
                            return ErrorResponses.forbidden('BLOCKED: no documentation submitted. Call artifact_submit before session_complete.', { session: blocked });
                        }
                        const completed = sessionManager.complete(session_id);
                        return {
                            content: [{
                                type: 'text' as const, text: JSON.stringify({
                                    ok: true,
                                    message: `Session completed. ${artifacts.length} artifact(s) registered.`,
                                    session: completed,
                                    artifact_count: artifacts.length
                                })
                            }]
                        };
                    } catch (e: any) {
                        return ErrorResponses.internalError(e.message);
                    }
                }
            );
        }
        // --- WRITE-LAYER END ---

        // ── search_audit_log ───────────────────────────────────────────────────
        registerTool(
            'search_audit_log',
            'Search the audit logs for specified matching criteria. Returns latest 50 entries by default.',
            {
                session_id: z.string().optional().describe('Filter by Session ID'),
                timestamp_since: z.string().optional().describe('Filter by timestamps after this ISO date'),
                limit: z.number().optional().describe('Max number of results to return (default 50, max 100)')
            },
            async ({ session_id, timestamp_since, limit }) => {
                try {
                    if (!agentDb) {
                        return ErrorResponses.internalError('AgentDb is not available');
                    }
                    const results = agentDb.searchAuditLog({ session_id, timestamp_since, limit });
                    return {
                        content: [{ type: 'text' as const, text: JSON.stringify(results, null, 2) }]
                    };
                } catch (e: any) {
                    return ErrorResponses.internalError(e.message);
                }
            }
        );

        // ── is_allowed (RBAC) ──────────────────────────────────────────────────
        registerTool(
            'is_allowed',
            'Checks if a specific tool is allowed for a user/session/role combo.',
            { tool: z.string().describe('Tool name to check') },
            async ({ tool }) => {
                return { content: [{ type: 'text' as const, text: JSON.stringify({ tool, allowed: isAllowed(tool) }) }] };
            }
        );

        // ── submit_workflow (delivery-only; returns 403 on non-delivery track) ───
        registerTool(
            'submit_workflow',
            'Submit a workflow for delivery processing. Only available on the delivery track.',
            { workflow: z.string().optional().describe('Workflow identifier or payload') },
            async (_args, context) => {
                if (context.track !== 'delivery') {
                    return { success: false, error: { code: 403, message: 'submit_workflow only available on delivery track' } };
                }
                return { success: true, data: { message: 'Workflow submitted' } };
            }
        );

        return server;
    }

    // ─── SSE Transport (VS Code Copilot, current standard) ───────────────────
    /**
     * GET /mcp/sse — SSE connection endpoint
     * Clients connect here and keep the connection open for messages.
     *
     * VS Code mcp.json:
     * { "servers": { "joinerytech": { "type": "sse", "url": "http://localhost:3000/mcp/sse" } } }
     */
    const sseTransports: Record<string, SSEServerTransport> = {};

    router.get('/sse', async (req: Request, res: Response) => {
        console.log('🔌 [MCP] New SSE client connected');
        const activeRole = req.headers['x-active-role'] as string || 'public';
        const transport = new SSEServerTransport('/mcp/sse-message', res);
        sseTransports[transport.sessionId] = transport;

        res.on('close', () => {
            console.log(`🔌 [MCP] SSE client disconnected: ${transport.sessionId}`);
            delete sseTransports[transport.sessionId];
        });

        await pluginLoadPromise;
        const server = buildMcpServer(activeRole, req.headers);
        await server.connect(transport);
    });

    router.post('/sse-message', async (req: Request, res: Response) => {
        const sessionId = req.query.sessionId as string;
        const transport = sseTransports[sessionId];
        if (!transport) {
            res.status(400).json({ error: `No active SSE session: ${sessionId}` });
            return;
        }
        await transport.handlePostMessage(req, res, req.body);
    });

    // ─── Streamable HTTP Transport (modern standard) ──────────────────────────
    /**
     * POST /mcp/http — Streamable HTTP endpoint (stateless or stateful with sessions)
     *
     * VS Code mcp.json (future):
     * { "servers": { "joinerytech": { "type": "http", "url": "http://localhost:3000/mcp/http" } } }
     */
    const httpTransports: Record<string, StreamableHTTPServerTransport> = {};

    router.post('/http', async (req: Request, res: Response) => {
        const sessionId = req.headers['mcp-session-id'] as string | undefined;
        const activeRole = req.headers['x-active-role'] as string || 'public';

        let transport: StreamableHTTPServerTransport;

        if (sessionId && httpTransports[sessionId]) {
            // Reuse existing session
            transport = httpTransports[sessionId];
        } else {
            // New session
            const newSessionId = randomUUID();
            transport = new StreamableHTTPServerTransport({
                sessionIdGenerator: () => newSessionId,
            });
            httpTransports[newSessionId] = transport;

            // Clean up on close
            transport.onclose = () => {
                delete httpTransports[newSessionId];
                console.log(`🔌 [MCP] HTTP session closed: ${newSessionId}`);
            };

            await pluginLoadPromise;
            const server = buildMcpServer(activeRole, req.headers);
            await server.connect(transport);
            console.log(`🔌 [MCP] New HTTP session created: ${newSessionId}`);
        }

        await transport.handleRequest(req, res, req.body);
    });

    router.get('/http', async (req: Request, res: Response) => {
        const sessionId = req.headers['mcp-session-id'] as string | undefined;
        if (!sessionId || !httpTransports[sessionId]) {
            res.status(400).json({ error: 'Invalid or missing mcp-session-id' });
            return;
        }
        await httpTransports[sessionId].handleRequest(req, res);
    });

    return router;
}
