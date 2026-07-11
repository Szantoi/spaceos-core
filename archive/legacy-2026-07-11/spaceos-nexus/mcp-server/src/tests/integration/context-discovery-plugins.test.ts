import 'reflect-metadata';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createContextToolModule, ContextPlugin } from '../../mcp/tools/context';
import { createDiscoveryToolModule, DiscoveryPlugin } from '../../mcp/tools/discovery';
import { AgentDb } from '../../mcp/AgentDb';
import { McpContext } from '../../mcp/middleware/contextMiddleware';
import { SystemContext } from '../../plugins/PluginTypes';

// ─── Shared mock data ────────────────────────────────────────────────────────

const mockContext: McpContext = {
    session_id: 'test-session-id',
    user_id: 'test-agent-id',
    domain: 'engineering',
    role: 'backend_developer',
    phase: 'validation',
    track: 'discovery'
};

function makeMockAgentDb() {
    const storage: any[] = [];
    return {
        getRoleSchema: vi.fn(),
        getRolesByDomain: vi.fn(),
        getWorkflowsByRole: vi.fn(),
        getTemplatesByRole: vi.fn(),
        trackBlocker: vi.fn((sid, phase, severity, text) => storage.push({ session_id: sid, phase, severity, text })),
        getBlockers: vi.fn((sid, phase) => storage.filter(b => b.session_id === sid && (!phase || b.phase === phase)))
    };
}

function makeMockSystemContext(agentDb: ReturnType<typeof makeMockAgentDb>): SystemContext {
    return {
        agentDb: agentDb as unknown as AgentDb,
        sessionManager: { get: vi.fn(), register: vi.fn() } as any,
        rbacFilter: { getAllowedTools: vi.fn().mockReturnValue(new Set(['tool1', 'tool2'])) } as any,
        workflowTracker: { getState: vi.fn(), createSession: vi.fn() } as any,
        guardrailService: { validate: vi.fn().mockResolvedValue(true) } as any,
    };
}

// stub EpisodeStore & FTS search for new discovery tools
vi.mock('../../episodic/EpisodeStore', () => {
    class FakeStore {
        public initialize = async () => { };
        public storeExperience = async (params: any) => ({ episodeId: 'int-ep', createdAt: new Date(), track: 'discovery' });
    }
    return { EpisodeStore: FakeStore };
});
vi.mock('../../episodic/FtsSearch', () => ({
    searchExperience: vi.fn().mockResolvedValue([])
}));

// ═══════════════════════════════════════════════════════════════════════════════
// SECTION 1 — Factory Module Tests (backward-compat)
// ═══════════════════════════════════════════════════════════════════════════════

describe('Context Plugin (factory)', () => {
    const contextModule = createContextToolModule();

    it('should return current context in request_context', async () => {
        const handler = contextModule.handlers['request_context'];
        const result = await handler({}, mockContext);

        expect(result.success).toBe(true);
        expect(result.data.sessionId).toBe(mockContext.session_id);
        expect(result.data.agentId).toBe(mockContext.user_id);
        expect(result.data.domain).toBe(mockContext.domain);
        expect(result.data.track).toBe(mockContext.track);
    });

    it('factory request_context returns basic phase payload when phase supplied', async () => {
        const handler = contextModule.handlers['request_context'];
        const result = await handler({ phase: 'ideation' }, mockContext);
        expect(result.success).toBe(true);
        // ensures factory branch executed: returns workflow_template string
        expect(typeof result.data.workflow_template).toBe('string');
    });

    it('should allow session lookup in lookup_context', async () => {
        const handler = contextModule.handlers['lookup_context'];
        const result = await handler({ sessionId: 'other-session' }, mockContext);

        expect(result.success).toBe(true);
        expect(result.data.sessionId).toBe('other-session');
        expect(result.data.isCurrentSession).toBe(false);
    });

    it('should use current session when no sessionId provided', async () => {
        const handler = contextModule.handlers['lookup_context'];
        const result = await handler({}, mockContext);

        expect(result.success).toBe(true);
        expect(result.data.sessionId).toBe(mockContext.session_id);
        expect(result.data.isCurrentSession).toBe(true);
    });
});

describe('Discovery Plugin (factory)', () => {
    let mockAgentDb: ReturnType<typeof makeMockAgentDb>;

    beforeEach(() => {
        mockAgentDb = makeMockAgentDb();
    });

    it('should list roles for a domain', async () => {
        const roles = [{ role_name: 'architect' }, { role_name: 'dev' }];
        mockAgentDb.getRolesByDomain.mockReturnValue(roles);

        const discoveryModule = createDiscoveryToolModule(mockAgentDb as unknown as AgentDb);
        const handler = discoveryModule.handlers['discovery_roles'];
        const result = await handler({ domain: 'engineering' }, mockContext);

        expect(result.success).toBe(true);
        expect(result.data).toEqual(roles);
        expect(mockAgentDb.getRolesByDomain).toHaveBeenCalledWith('engineering');
    });

    it('should get specific role details', async () => {
        const roleSchema = { role_name: 'architect', responsibilities: 'lead' };
        mockAgentDb.getRoleSchema.mockReturnValue(roleSchema);

        const discoveryModule = createDiscoveryToolModule(mockAgentDb as unknown as AgentDb);
        const handler = discoveryModule.handlers['discovery_roles'];
        const result = await handler({ domain: 'engineering', roleName: 'architect' }, mockContext);

        expect(result.success).toBe(true);
        expect(result.data).toEqual(roleSchema);
    });

    it('should list workflows for a role', async () => {
        const workflows = [{ workflow_type: 'epic', content: '...' }];
        mockAgentDb.getWorkflowsByRole.mockReturnValue(workflows);

        const discoveryModule = createDiscoveryToolModule(mockAgentDb as unknown as AgentDb);
        const handler = discoveryModule.handlers['discovery_workflows'];
        const result = await handler({ domain: 'engineering', roleName: 'dev' }, mockContext);

        expect(result.success).toBe(true);
        expect(result.data).toEqual(workflows);
    });

    // new tests for EPIC-13 discovery tools will be added later in class tests section

});

// ═══════════════════════════════════════════════════════════════════════════════
// SECTION 2 — Plugin Class Tests (new architecture)
// ═══════════════════════════════════════════════════════════════════════════════

describe('ContextPlugin class (TASK-14-05)', () => {
    let plugin: ContextPlugin;

    beforeEach(() => {
        const mockDb = makeMockAgentDb();
        plugin = new ContextPlugin(makeMockSystemContext(mockDb));
    });

    it('should register as IToolModule with correct name/version', () => {
        expect(plugin.name).toBe('context');
        expect(plugin.version).toBe('1.0.0');
    });

    it('should expose request_context and lookup_context tools', () => {
        const toolNames = plugin.tools.map(t => t.name);
        expect(toolNames).toContain('request_context');
        expect(toolNames).toContain('lookup_context');
    });

    it('should return context data via request_context handler', async () => {
        const result = await plugin.handlers['request_context']({}, mockContext);
        expect(result.success).toBe(true);
        expect(result.data.sessionId).toBe(mockContext.session_id);
        expect(result.data.domain).toBe(mockContext.domain);
        expect(result.data.track).toBe(mockContext.track);
    });

    it('should detect non-current session in lookup_context handler', async () => {
        const result = await plugin.handlers['lookup_context']({ sessionId: 'other-id' }, mockContext);
        expect(result.success).toBe(true);
        expect(result.data.isCurrentSession).toBe(false);
    });

    it('should include timestamp in result', async () => {
        const result = await plugin.handlers['request_context']({}, mockContext);
        expect(result.data.timestamp).toBeDefined();
        expect(new Date(result.data.timestamp).toISOString()).toBe(result.data.timestamp);
    });

    it('request_context with phase returns workflow and templates', async () => {
        const result = await plugin.handlers['request_context']({ phase: 'ideation' }, mockContext);
        expect(result.success).toBe(true);
        expect(result.data.workflow_template).toContain('Phase 1: IDEATION');
        expect(result.data.artifact_templates.length).toBeGreaterThan(0);
    });

    it('request_context resolves ambiguous filters using sampling', async () => {
        const contextWithSampling: McpContext = {
            ...mockContext,
            requestSampling: async () => ({
                requestId: 'req-1',
                selected: ['role']
            })
        };

        const result = await plugin.handlers['request_context']({ filters: 'ambiguous' }, contextWithSampling);
        expect(result.success).toBe(true);
        expect(result.data.selected_filters).toEqual(['role']);
    });

    it('request_context returns needs_clarification when sampling times out', async () => {
        const contextWithSampling: McpContext = {
            ...mockContext,
            requestSampling: async () => ({
                requestId: 'req-timeout',
                selected: [],
                error: 'Sampling timed out',
                needsClarification: true
            })
        };

        const result = await plugin.handlers['request_context']({ filters: 'everything' }, contextWithSampling);
        expect(result.success).toBe(false);
        expect(result.error.needs_clarification).toBe(true);
    });
});

describe('DiscoveryPlugin class (TASK-14-05)', () => {
    let plugin: DiscoveryPlugin;
    let mockAgentDb: ReturnType<typeof makeMockAgentDb>;

    beforeEach(() => {
        mockAgentDb = makeMockAgentDb();
        plugin = new DiscoveryPlugin(makeMockSystemContext(mockAgentDb));
    });

    it('should register as IToolModule with correct name/version', () => {
        expect(plugin.name).toBe('discovery');
        expect(plugin.version).toBe('1.0.0');
    });

    it('should expose all 4 discovery tools and new EPIC-13 tools', () => {
        const toolNames = plugin.tools.map(t => t.name);
        expect(toolNames).toContain('discovery_roles');
        expect(toolNames).toContain('discovery_workflows');
        expect(toolNames).toContain('discovery_templates');
        expect(toolNames).toContain('discovery_search');
        expect(toolNames).toContain('reference_prior_discovery');
        expect(toolNames).toContain('submit_discovery_outcome');
    });

    it('should list roles via discovery_roles handler', async () => {
        const roles = [{ role_name: 'architect' }];
        mockAgentDb.getRolesByDomain.mockReturnValue(roles);

        const result = await plugin.handlers['discovery_roles']({ domain: 'engineering' }, mockContext);
        expect(result.success).toBe(true);
        expect(result.data).toEqual(roles);
        expect(mockAgentDb.getRolesByDomain).toHaveBeenCalledWith('engineering');
    });

    it('should return role detail via discovery_roles handler with roleName', async () => {
        const roleSchema = { role_name: 'architect', responsibilities: 'lead' };
        mockAgentDb.getRoleSchema.mockReturnValue(roleSchema);

        const result = await plugin.handlers['discovery_roles']({ domain: 'engineering', roleName: 'architect' }, mockContext);
        expect(result.success).toBe(true);
        expect(result.data).toEqual(roleSchema);
    });

    it('should return 404 when role not found', async () => {
        mockAgentDb.getRoleSchema.mockReturnValue(null);

        const result = await plugin.handlers['discovery_roles']({ domain: 'engineering', roleName: 'unknown' }, mockContext);
        expect(result.success).toBe(false);
    });

    it('should list workflows via discovery_workflows handler', async () => {
        const workflows = [{ workflow_type: 'epic', content: '...' }];
        mockAgentDb.getWorkflowsByRole.mockReturnValue(workflows);

        const result = await plugin.handlers['discovery_workflows']({ domain: 'engineering', roleName: 'dev' }, mockContext);
        expect(result.success).toBe(true);
        expect(result.data).toEqual(workflows);
    });

    it('reference_prior_discovery returns empty structure', async () => {
        const result = await plugin.handlers['reference_prior_discovery']({ search_text: 'foo' }, mockContext);
        expect(result.success).toBe(true);
        expect(result.data.total_found).toBe(0);
    });

    it('fallback flag is set when semantic search throws', async () => {
        const spy = vi.spyOn(plugin as any, 'trySemanticSearch').mockRejectedValue(new Error('fail'));
        const res = await plugin.handlers['reference_prior_discovery']({ search_text: 'silent' }, mockContext);
        expect(res.success).toBe(true);
        expect(res.data.fallback_used).toBe(true);
        spy.mockRestore();
    });

    it('submit_discovery_outcome stores episode via EpisodeStore', async () => {
        // EpisodeStore mock defined at top returns episodeId 'int-ep'
        const handler = plugin.handlers['submit_discovery_outcome'];
        const res = await handler({ outcome_type: 'HANDOFF', summary: 'integration test', blockers: [], phase: 'validation', artifacts: [] }, { ...mockContext, track: 'discovery' });
        expect(res.success).toBe(true);
        expect(res.data.episode_id).toBe('int-ep');
        expect(res.data.track).toBe('discovery');
    });

    it('phase gating prevents iteration submission early', async () => {
        const handler = plugin.handlers['submit_discovery_outcome'];
        const res = await handler({ outcome_type: 'LEARNING', summary: 'short', blockers: [], phase: 'iteration' }, { ...mockContext, track: 'discovery' });
        expect(res.success).toBe(false);
        expect(res.error.message).toContain('Cannot submit outcome');
    });

    it('should filter workflows by type when workflowType provided', async () => {
        const workflows = [
            { workflow_type: 'epic', content: 'a' },
            { workflow_type: 'task', content: 'b' }
        ];
        mockAgentDb.getWorkflowsByRole.mockReturnValue(workflows);

        const result = await plugin.handlers['discovery_workflows']({ domain: 'engineering', roleName: 'dev', workflowType: 'epic' }, mockContext);
        expect(result.success).toBe(true);
        expect(result.data).toHaveLength(1);
        expect(result.data[0].workflow_type).toBe('epic');
    });

    it('should list templates via discovery_templates handler', async () => {
        const templates = [{ template_name: 'adr', content: '...' }];
        mockAgentDb.getTemplatesByRole.mockReturnValue(templates);

        const result = await plugin.handlers['discovery_templates']({ domain: 'engineering', roleName: 'architect' }, mockContext);
        expect(result.success).toBe(true);
        expect(result.data).toEqual(templates);
    });

    it('phase tools check_constraints and guidance should work', async () => {
        const check = await plugin.handlers['check_constraints']({ idea_summary: 'foo', constraint_set: 'technical' }, mockContext);
        expect(check.success).toBe(true);
        expect(check.data.go_no_go).toBe(true);

        const guide = await plugin.handlers['get_phase_guidance']({ current_phase: 'ideation' }, mockContext);
        expect(guide.success).toBe(true);
        expect(guide.data.next_phase).toBe('validation');

        const badCtx = { ...mockContext, track: 'delivery' as any };
        const chk2 = await plugin.handlers['check_constraints']({ idea_summary: 'foo', constraint_set: 'technical' }, badCtx);
        expect(chk2.success).toBe(false);
        const g2 = await plugin.handlers['get_phase_guidance']({ current_phase: 'ideation' }, badCtx);
        expect(g2.success).toBe(false);

        // blocker tracking
        const b1 = await plugin.handlers['track_blocker']({ blocker_text: 'issue1', severity: 'LOW', blocking_phase: 'ideation' }, mockContext);
        expect(b1.success).toBe(true);
        const listAll = await plugin.handlers['query_blockers']({}, mockContext);
        expect(listAll.data.length).toBeGreaterThan(0);
        const listPhase = await plugin.handlers['query_blockers']({ phase: 'ideation' }, mockContext);
        expect(listPhase.data.every((b: any) => b.phase === 'ideation')).toBe(true);
    });

    it('should return discovery_search placeholder result', async () => {
        const result = await plugin.handlers['discovery_search']({ query: 'test', limit: 5 }, mockContext);
        expect(result.success).toBe(true);
        expect(Array.isArray(result.data)).toBe(true);
    });

    it('should fall back to context.domain when no domain specified', async () => {
        mockAgentDb.getRolesByDomain.mockReturnValue([]);

        const result = await plugin.handlers['discovery_roles']({}, mockContext);
        expect(result.success).toBe(true);
        expect(mockAgentDb.getRolesByDomain).toHaveBeenCalledWith(mockContext.domain);
    });
});
