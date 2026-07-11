import { Router, Request, Response } from 'express';
import { DocumentServer, ArtifactSubmitRequest } from './DocumentServer';
import { ResourceTracker } from '../metadata/ResourceTracker';
import { WorkflowStateTracker } from '../metadata/WorkflowStateTracker';
import { SessionManager } from './SessionManager';
import { AgentDb } from './AgentDb'; // EPIC-09
import { updateWorkflowState, submitArtifact } from './WriteLayerTools';
import { EpisodeStore } from '../episodic/EpisodeStore'; // TASK-12-01
import { StoreExperienceParams } from '../episodic/types'; // TASK-12-01
import { searchExperience } from '../episodic/FtsSearch'; // TASK-12-02

/**
 * MCP Tool Surface Router
 *
 * Implements the MCP tool endpoints as defined in MCP_Server_Architecture.md:
 *
 *   GET  /mcp/tools                          — list available MCP tools
 *   GET  /mcp/domains                        — list available domains
 *   GET  /mcp/roles?domain=...               — list roles in domain
 *   GET  /mcp/role?domain=...&role=...       — get_role()
 *   GET  /mcp/schema?domain=...&role=...     — get_schema()
 *   GET  /mcp/workflow?domain=...&role=...&type=...   — get_workflow()
 *   GET  /mcp/template?domain=...&role=...&name=...   — get_template()
 *   GET  /mcp/templates?domain=...&role=...  — list_templates()
 *   GET  /mcp/message?domain=...&role=...&name=...    — get_message()
 *   GET  /mcp/core?doc=...&domain=...&role=... — get_core()
 *
 *   POST /mcp/session/register               — register agent session (returns session_id)
 *   POST /mcp/artifact/submit                — validate + write + register artifact
 *   POST /mcp/session/complete               — close session (blocked if no artifacts)
 *
 * All endpoints return { content: string } on success or { error: string } on failure.
 */
export function createMcpRouter(
    documentServer: DocumentServer,
    resourceTracker?: ResourceTracker,
    workflowTracker?: WorkflowStateTracker,
    sessionManager?: SessionManager,
    agentDb?: AgentDb
): Router {
    const router = Router();

    // TASK-12-01: Lazy-init EpisodeStore from admin pool (writable)
    let episodeStore: EpisodeStore | undefined;
    async function getEpisodeStore(): Promise<EpisodeStore> {
        if (!episodeStore) {
            if (!agentDb) {
                throw new Error('AgentDb is not configured — cannot create EpisodeStore');
            }
            episodeStore = new EpisodeStore(agentDb.getRawDatabase());
            await episodeStore.initialize();
        }
        return episodeStore;
    }

    // ─── Tool Discovery ──────────────────────────────────────────────────────

    router.get('/tools', (_req: Request, res: Response) => {
        res.json({
            tools: [
                {
                    name: 'get_role',
                    description: 'Returns the full .role.md content for a specific domain/role.',
                    input: { domain: 'string', role: 'string' },
                    endpoint: 'GET /mcp/role?domain=&role='
                },
                {
                    name: 'get_schema',
                    description: 'Returns the full .schema.yaml content for a specific domain/role.',
                    input: { domain: 'string', role: 'string' },
                    endpoint: 'GET /mcp/schema?domain=&role='
                },
                {
                    name: 'get_workflow',
                    description: 'Returns the full .workflow.md content. type is optional (default|multi_workspace).',
                    input: { domain: 'string', role: 'string', type: 'string (optional)' },
                    endpoint: 'GET /mcp/workflow?domain=&role=&type='
                },
                {
                    name: 'get_template',
                    description: 'Returns the full .template.md content by name.',
                    input: { domain: 'string', role: 'string', name: 'string' },
                    endpoint: 'GET /mcp/template?domain=&role=&name='
                },
                {
                    name: 'list_templates',
                    description: 'Lists all available template names for a role.',
                    input: { domain: 'string', role: 'string' },
                    endpoint: 'GET /mcp/templates?domain=&role='
                },
                {
                    name: 'get_core',
                    description: 'Returns a core/standards document (e.g. constraints, runbook, dod). Optionally scoped to domain+role.',
                    input: { doc: 'string', domain: 'string (optional)', role: 'string (optional)' },
                    endpoint: 'GET /mcp/core?doc=&domain=&role='
                },
                {
                    name: 'get_policy',
                    description: 'Returns a general policy file from the docs folder by name (e.g. "orchestrator" for orchestrator.policy.md).',
                    input: { name: 'string' },
                    endpoint: 'GET /mcp/policy?name='
                },
                {
                    name: 'search_knowledge',
                    description: 'Semantic search over indexed .knowledge.md chunks. Returns relevant passages.',
                    input: { query: 'string', domain: 'string (optional)', topK: 'number (optional)' },
                    endpoint: 'POST /api/knowledge/search { query, domain?, topK? }'
                },
                {
                    name: 'submit_artifact',
                    description: 'Submit artifact (implementation_summary|test_report|pr_link) with pessimistic locking and audit trail.',
                    input: { session_id: 'string (UUID)', artifact_type: 'enum: implementation_summary|test_report|pr_link', artifact_content: 'string (1-100KB)' },
                    endpoint: 'POST /mcp/write/artifact/submit { session_id, artifact_type, artifact_content }'
                },
                {
                    name: 'update_workflow_state',
                    description: 'Update FSM state with validation and atomic writes. Allowed transitions: started→in_progress→submitted→processed→closed.',
                    input: { session_id: 'string (UUID)', new_state: 'enum: started|in_progress|submitted|processed|closed', event: 'string (event description)', evidence_artifact_id: 'string (UUID, optional)' },
                    endpoint: 'POST /mcp/write/state/update { session_id, new_state, event, evidence_artifact_id? }'
                },
                {
                    name: 'session_register',
                    description: 'Register a new agent session. Returns a session_id required for artifact submission and session completion.',
                    input: { role: 'string', domain: 'string', agent_name: 'string (optional)' },
                    endpoint: 'POST /mcp/session/register { role, domain, agent_name? }'
                },
                {
                    name: 'artifact_submit',
                    description: 'Submit a completed artifact (Markdown document). Server validates frontmatter, writes the file, and registers it in ResourceTracker.',
                    input: {
                        domain: 'string', role: 'string', type: 'string (template name)',
                        content: 'string (full Markdown with frontmatter)',
                        target_path: 'string (relative to workspace root, .md only)',
                        project: 'string', user: 'string', session_id: 'string (optional)'
                    },
                    endpoint: 'POST /mcp/artifact/submit { domain, role, type, content, target_path, project, user, session_id? }'
                },
                {
                    name: 'session_complete',
                    description: 'Close an agent session. Returns 403 BLOCKED if no artifacts were submitted during the session.',
                    input: { session_id: 'string' },
                    endpoint: 'POST /mcp/session/complete { session_id }'
                },
                {
                    name: 'get_role_context',
                    description: 'Return a combined role context bundle (role + schema + runbook) for a domain/role.',
                    input: { domain: 'string', role: 'string' },
                    endpoint: 'GET /mcp/role_context?domain=&role='
                },
                {
                    name: 'get_project_summary',
                    description: 'List all epics in the project along with their FSM state.',
                    input: {},
                    endpoint: 'GET /mcp/project_summary'
                },
                {
                    name: 'get_epic_summary',
                    description: 'Return task-level details for a specific epic.',
                    input: { epic: 'string' },
                    endpoint: 'GET /mcp/epic_summary?epic='
                },
                {
                    name: 'store_experience',
                    description: 'Record an agent experience (episode) for later retrieval and learning. Returns episodeId and createdAt.',
                    input: {
                        session_id: 'string',
                        domain: 'enum: discovery|engineering|testing|deployment',
                        track: 'enum: fast_track|standard',
                        phase: 'string',
                        outcome_summary: 'string',
                        tool_calls: 'optional array',
                        artifacts: 'optional array'
                    },
                    endpoint: 'POST /mcp/experience/store'
                },
                {
                    endpoint: 'POST /mcp/experience/search/keyword'
                },
                {
                    name: 'search_episodes_semantic',
                    description: 'Perform a semantic (vector) search against past episodes to find related experiences based on meaning. Returns episodes that exceed the similarity threshold.',
                    input: {
                        query: 'string',
                        domain_filter: 'optional string',
                        limit: 'optional number (default: 5)',
                        threshold: 'optional number (0.0-1.0)'
                    },
                    endpoint: 'POST /mcp/experience/search/semantic'
                },
                {
                    name: 'search_episodes_hybrid',
                    description: 'Perform a hybrid search (Keywords + Semantic) against past episodes. Best overall retrieval quality.',
                    input: {
                        query: 'string',
                        domain_filter: 'optional string',
                        limit: 'optional number (default: 10)',
                        threshold: 'optional number (0.0-1.0)'
                    },
                    endpoint: 'POST /mcp/experience/search/hybrid'
                }
            ]
        });
    });

    // ─── Domain / Role Discovery ─────────────────────────────────────────────

    router.get('/domains', (_req: Request, res: Response) => {
        try {
            const domains = documentServer.listDomains();
            res.json({ domains });
        } catch (e: any) {
            res.status(500).json({ error: e.message });
        }
    });

    router.get('/roles', (req: Request, res: Response) => {
        const { domain } = req.query as { domain?: string };
        if (!domain) {
            res.status(400).json({ error: 'Missing required query param: domain' });
            return;
        }
        try {
            const roles = documentServer.listRoles(domain);
            res.json({ domain, roles });
        } catch (e: any) {
            res.status(500).json({ error: e.message });
        }
    });

    // ─── Role ────────────────────────────────────────────────────────────────

    router.get('/role', (req: Request, res: Response) => {
        const { domain, role } = req.query as { domain?: string; role?: string };
        if (!domain || !role) {
            res.status(400).json({ error: 'Missing required query params: domain, role' });
            return;
        }
        try {
            const content = documentServer.getRole(domain, role);
            res.json({ domain, role, content });
        } catch (e: any) {
            res.status(404).json({ error: e.message });
        }
    });

    // ─── Schema ──────────────────────────────────────────────────────────────

    router.get('/schema', (req: Request, res: Response) => {
        const { domain, role } = req.query as { domain?: string; role?: string };
        if (!domain || !role) {
            res.status(400).json({ error: 'Missing required query params: domain, role' });
            return;
        }
        try {
            const content = documentServer.getSchema(domain, role);
            res.json({ domain, role, content });
        } catch (e: any) {
            res.status(404).json({ error: e.message });
        }
    });

    // ─── Workflow ─────────────────────────────────────────────────────────────

    router.get('/workflow', (req: Request, res: Response) => {
        const { domain, role, type } = req.query as { domain?: string; role?: string; type?: string };
        if (!domain || !role) {
            res.status(400).json({ error: 'Missing required query params: domain, role' });
            return;
        }
        try {
            const content = documentServer.getWorkflow(domain, role, type);
            res.json({ domain, role, type: type || 'default', content });
        } catch (e: any) {
            res.status(404).json({ error: e.message });
        }
    });

    // ─── Template ─────────────────────────────────────────────────────────────

    router.get('/templates', (req: Request, res: Response) => {
        const { domain, role } = req.query as { domain?: string; role?: string };
        if (!domain || !role) {
            res.status(400).json({ error: 'Missing required query params: domain, role' });
            return;
        }
        try {
            const templates = documentServer.listTemplates(domain, role);
            res.json({ domain, role, templates });
        } catch (e: any) {
            res.status(500).json({ error: e.message });
        }
    });

    router.get('/template', (req: Request, res: Response) => {
        const { domain, role, name } = req.query as { domain?: string; role?: string; name?: string };
        if (!domain || !role || !name) {
            res.status(400).json({ error: 'Missing required query params: domain, role, name' });
            return;
        }
        try {
            const content = documentServer.getTemplate(domain, role, name);
            res.json({ domain, role, name, content });
        } catch (e: any) {
            res.status(404).json({ error: e.message });
        }
    });


    // ─── Core ─────────────────────────────────────────────────────────────────

    router.get('/core', (req: Request, res: Response) => {
        const { doc, domain, role } = req.query as { doc?: string; domain?: string; role?: string };
        if (!doc) {
            res.status(400).json({ error: 'Missing required query param: doc' });
            return;
        }
        try {
            const content = documentServer.getCore(doc, domain, role);
            res.json({ doc, domain, role, content });
        } catch (e: any) {
            res.status(404).json({ error: e.message });
        }
    });

    // ─── Policy ───────────────────────────────────────────────────────────────

    router.get('/policy', (req: Request, res: Response) => {
        const { name } = req.query as { name?: string };
        if (!name) {
            res.status(400).json({ error: 'Missing required query param: name' });
            return;
        }
        try {
            const content = documentServer.getPolicy(name);
            res.json({ name, content });
        } catch (e: any) {
            res.status(404).json({ error: e.message });
        }
    });
    // ─── Role Context Bundle ─────────────────────────────────────────────────

    router.get('/role_context', (req: Request, res: Response) => {
        const { domain, role } = req.query as { domain?: string; role?: string };
        if (!domain || !role) {
            res.status(400).json({ error: 'Missing required query params: domain, role' });
            return;
        }
        try {
            const content = documentServer.getRoleContext(domain, role);
            res.json({ domain, role, content });
        } catch (e: any) {
            res.status(404).json({ error: e.message });
        }
    });

    // ─── Project / Epic Summaries ─────────────────────────────────────────────

    router.get('/project_summary', (_req: Request, res: Response) => {
        try {
            const summary = documentServer.getProjectSummary();
            res.json(summary);
        } catch (e: any) {
            res.status(500).json({ error: e.message });
        }
    });

    router.get('/epic_summary', (req: Request, res: Response) => {
        const { epic } = req.query as { epic?: string };
        if (!epic) {
            res.status(400).json({ error: 'Missing required query param: epic' });
            return;
        }
        try {
            const summary = documentServer.getEpicSummary(epic);
            res.json(summary);
        } catch (e: any) {
            res.status(404).json({ error: e.message });
        }
    });
    // ─── WRITE LAYER (ADR-007) ────────────────────────────────────────────────

    /**
     * POST /mcp/session/register
     *
     * Register a new agent session. Returns a session_id token that the agent
     * must include in subsequent artifact/submit calls for audit traceability.
     *
     * Body: { role: string, domain: string, agent_name?: string }
     * Response: { session_id, role, domain, status, created_at }
     */
    router.post('/session/register', (req: Request, res: Response) => {
        if (!sessionManager) {
            res.status(501).json({ error: 'SessionManager is not configured on this server.' });
            return;
        }

        const { role, domain, agent_name } = req.body as {
            role?: string;
            domain?: string;
            agent_name?: string;
        };

        if (!role || !domain) {
            res.status(400).json({ error: 'Missing required body fields: role, domain' });
            return;
        }

        try {
            const session = sessionManager.register(role, domain, agent_name);
            res.status(201).json({
                session_id: session.id,
                role: session.role,
                domain: session.domain,
                status: session.status,
                created_at: session.created_at
            });
        } catch (e: any) {
            res.status(500).json({ error: e.message });
        }
    });

    /**
     * POST /mcp/artifact/submit
     *
     * Agent submits a completed artifact. Server:
     *   1. Validates template exists for type
     *   2. Validates frontmatter required fields
     *   3. Writes file to target_path (relative to workspaceRoot)
     *   4. Registers resource in ResourceTracker
     *
     * Body: ArtifactSubmitRequest
     * Response 200: { ok: true, message, resource }
     * Response 400: { ok: false, message, missing_fields? }
     */
    router.post('/artifact/submit', (req: Request, res: Response) => {
        const submitReq = req.body as Partial<ArtifactSubmitRequest>;
        const required: (keyof ArtifactSubmitRequest)[] = ['domain', 'role', 'type', 'content', 'target_path', 'project', 'user'];
        const missing = required.filter(k => !submitReq[k]);

        if (missing.length > 0) {
            res.status(400).json({ error: `Missing required body fields: ${missing.join(', ')}` });
            return;
        }

        try {
            const result = documentServer.submitArtifact(submitReq as ArtifactSubmitRequest);
            if (!result.ok) {
                res.status(400).json(result);
                return;
            }
            res.status(200).json(result);
        } catch (e: any) {
            res.status(500).json({ ok: false, message: e.message });
        }
    });

    /**
     * POST /mcp/session/complete
     *
     * Agent declares their task is done. Server:
     *   - Checks if at least 1 artifact was submitted this session
     *   - If YES → marks session COMPLETED, FSM transition to CLOSED_DONE (if workflow tracked)
     *   - If NO  → returns 403 BLOCKED: no documentation submitted
     *
     * Body: { session_id: string }
     * Response 200: { ok: true, session }
     * Response 403: { ok: false, message: 'BLOCKED: no documentation submitted' }
     */
    router.post('/session/complete', (req: Request, res: Response) => {
        if (!sessionManager) {
            res.status(501).json({ error: 'SessionManager is not configured on this server.' });
            return;
        }

        const { session_id } = req.body as { session_id?: string };
        if (!session_id) {
            res.status(400).json({ error: 'Missing required body field: session_id' });
            return;
        }

        try {
            // Verify session exists
            const session = sessionManager.get(session_id);
            if (!session) {
                res.status(404).json({ ok: false, message: `Session not found: ${session_id}` });
                return;
            }

            if (session.status !== 'active') {
                res.status(409).json({
                    ok: false,
                    message: `Session is already in terminal state: ${session.status}`
                });
                return;
            }

            // Check artifact count for this session
            const artifacts = resourceTracker
                ? resourceTracker.getBySession(session_id)
                : [];

            if (artifacts.length === 0) {
                // Gate: no artifacts → block the session
                const blocked = sessionManager.block(session_id);
                res.status(403).json({
                    ok: false,
                    message: 'BLOCKED: no documentation submitted. Submit at least one artifact via POST /mcp/artifact/submit before completing the session.',
                    session: blocked
                });
                return;
            }

            // All good → close session
            const completed = sessionManager.complete(session_id);

            // Optional: advance FSM state to CLOSED_DONE if a project_id is attached
            // (future extension: body could include project_id for workflowTracker integration)

            res.status(200).json({
                ok: true,
                message: `Session completed. ${artifacts.length} artifact(s) registered.`,
                session: completed,
                artifact_count: artifacts.length
            });
        } catch (e: any) {
            res.status(500).json({ ok: false, message: e.message });
        }
    });

    // ─── WRITE LAYER TOOLS (EPIC-08: MCP Write Tools) ────────────────────────

    /**
     * POST /mcp/write/artifact/submit
     *
     * Submit an artifact (implementation_summary, test_report, or PR link) with pessimistic locking.
     *
     * Body: { session_id: UUID, artifact_type: enum, artifact_content: string }
     * Headers: x-agent-role: backend_developer | tech_lead
     *
     * Response on success: { ok: true, artifact_id, session_id, artifact_type, submitted_at }
     * Response on error: { isError: true, error: { code, message } }
     */
    router.post('/write/artifact/submit', (req: Request, res: Response) => {
        if (!workflowTracker) {
            res.status(501).json({
                isError: true,
                error: { code: 'DATABASE_NOT_CONFIGURED', message: 'Write layer database not initialized' }
            });
            return;
        }

        const role = (req.headers['x-agent-role'] as string) || 'unknown';
        const result = submitArtifact(workflowTracker, req.body, role);

        if ((result as any).isError) {
            res.status(400).json(result);
        } else {
            res.status(201).json({ ok: true, ...result });
        }
    });

    /**
     * POST /mcp/write/state/update
     *
     * Update FSM state with validation and atomic writes.
     * Validates state transitions: started→in_progress→submitted→processed→closed
     *
     * Body: { session_id: UUID, new_state: enum, event: string, evidence_artifact_id?: UUID }
     * Headers: x-agent-role: backend_developer | tech_lead
     *
     * Response on success: { ok: true, state_before, state_after, event_id, timestamp, transition_allowed: true }
     * Response on error: { isError: true, error: { code, message } }
     */
    router.post('/write/state/update', (req: Request, res: Response) => {
        if (!workflowTracker) {
            res.status(501).json({
                isError: true,
                error: { code: 'DATABASE_NOT_CONFIGURED', message: 'Write layer database not initialized' }
            });
            return;
        }

        const role = (req.headers['x-agent-role'] as string) || 'unknown';
        const result = updateWorkflowState(workflowTracker, req.body, role);

        if ((result as any).isError) {
            res.status(400).json(result);
        } else {
            res.status(200).json({ ok: true, ...result });
        }
    });

    // ─── EPISODIC MEMORY (TASK-12-01) ─────────────────────────────────────────

    /**
     * POST /mcp/experience/store
     *
     * Store an agent experience (episode) for later retrieval and learning.
     *
     * Body: { session_id, domain, track, phase, outcome_summary, tool_calls?, artifacts? }
     * Response 201: { ok: true, episodeId, createdAt }
     * Response 400: { ok: false, error: 'Missing required body fields: ...' }
     * Response 413: { ok: false, error: 'Episode payload exceeds 5MB limit (got X bytes)' }
     * Response 500: { ok: false, error: '...' }
     */
    router.post('/experience/store', async (req: Request, res: Response) => {
        const body = req.body as Partial<StoreExperienceParams & {
            session_id: string;
            outcome_summary: string;
            tool_calls?: unknown[];
            artifacts?: unknown[];
        }>;

        const requiredFields = ['session_id', 'domain', 'track', 'phase', 'outcome_summary'] as const;
        const missing = requiredFields.filter(f => !body[f]);
        if (missing.length > 0) {
            res.status(400).json({ ok: false, error: `Missing required body fields: ${missing.join(', ')}` });
            return;
        }

        try {
            const store = await getEpisodeStore();
            const result = await store.storeExperience({
                sessionId: body.session_id!,
                domain: body.domain!,
                track: body.track!,
                phase: body.phase!,
                toolCalls: (body.tool_calls ?? []) as StoreExperienceParams['toolCalls'],
                artifacts: (body.artifacts ?? []) as StoreExperienceParams['artifacts'],
                outcomeSummary: body.outcome_summary!,
            });

            res.status(201).json({ ok: true, ...result });
        } catch (e: unknown) {
            const err = e as NodeJS.ErrnoException;
            if (err.code === 'episode_size_exceeded') {
                res.status(413).json({ ok: false, error: err.message });
            } else {
                res.status(500).json({ ok: false, error: err.message ?? String(e) });
            }
        }
    });

    /**
     * POST /mcp/experience/search/semantic
     *
     * Semantic (vector) search across stored episodes.
     *
     * Body: { query, domain_filter?, limit?, threshold? }
     * Response 200: { ok: true, matches: Episode[] }
     */
    router.post('/experience/search/semantic', async (req: Request, res: Response) => {
        const body = req.body as {
            query?: string;
            domain_filter?: string;
            limit?: number;
            threshold?: number;
        };

        if (!body.query || typeof body.query !== 'string') {
            res.status(400).json({ ok: false, error: 'Missing or invalid "query" field in body.' });
            return;
        }

        try {
            const store = await getEpisodeStore();
            const matches = await store.searchSemantic({
                query: body.query,
                domain: body.domain_filter,
                limit: body.limit,
                threshold: body.threshold
            });

            res.status(200).json({ ok: true, matches });
        } catch (e: unknown) {
            const err = e as Error;
            res.status(500).json({ ok: false, error: err.message ?? String(e) });
        }
    });

    /**
     * POST /mcp/experience/search/hybrid
     *
     * Hybrid search across stored episodes.
     *
     * Body: { query, domain_filter?, limit?, threshold? }
     * Response 200: { ok: true, matches: Episode[] }
     */
    router.post('/experience/search/hybrid', async (req: Request, res: Response) => {
        const body = req.body as {
            query?: string;
            domain_filter?: string;
            limit?: number;
            threshold?: number;
        };

        if (!body.query || typeof body.query !== 'string') {
            res.status(400).json({ ok: false, error: 'Missing or invalid "query" field in body.' });
            return;
        }

        try {
            const store = await getEpisodeStore();
            const matches = await store.searchHybrid({
                query: body.query,
                domain: body.domain_filter,
                limit: body.limit,
                threshold: body.threshold
            });

            res.status(200).json({ ok: true, matches });
        } catch (e: unknown) {
            const err = e as Error;
            res.status(500).json({ ok: false, error: err.message ?? String(e) });
        }
    });

    /**
     * POST /mcp/experience/search/keyword
     *
     * Keyword-based (FTS5) search across stored episodes.
     *
     * Body: { query: string, domain_filter?: string }
     * Response 200: { ok: true, matches: Episode[] }
     * Response 400: { ok: false, error: '...' }
     */
    router.post('/experience/search/keyword', (req: Request, res: Response) => {
        const body = req.body as { query?: string; domain_filter?: string };

        if (!body.query || typeof body.query !== 'string') {
            res.status(400).json({ ok: false, error: 'Missing or invalid "query" field in body.' });
            return;
        }

        try {
            if (!agentDb) {
                throw new Error('AgentDb is not configured — cannot perform search');
            }
            const db = agentDb.getRawDatabase();

            const matches = searchExperience(db, body.query, body.domain_filter);

            res.status(200).json({ ok: true, matches });
        } catch (e: unknown) {
            const err = e as Error;
            res.status(500).json({ ok: false, error: err.message ?? String(e) });
        }
    });

    return router;
}
