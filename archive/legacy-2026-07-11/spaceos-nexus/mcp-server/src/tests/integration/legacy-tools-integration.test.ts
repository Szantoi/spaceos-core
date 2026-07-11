import 'reflect-metadata';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createDiscoveryToolModule, DiscoveryPlugin } from '../../mcp/tools/discovery';
import { LegacyPlugin } from '../../mcp/tools/legacy';
import { AgentDb } from '../../mcp/AgentDb';
import { McpContext } from '../../mcp/middleware/contextMiddleware';
import { SystemContext } from '../../plugins/PluginTypes';

// integration harness similar to existing discovery tests

function makeMockAgentDb() {
    return {
        getRoleSchema: vi.fn(),
        getRolesByDomain: vi.fn(),
        getWorkflowsByRole: vi.fn(),
        getTemplatesByRole: vi.fn(),
        trackBlocker: vi.fn(),
        getBlockers: vi.fn()
    };
}

function makeMockSystemContext(agentDb: ReturnType<typeof makeMockAgentDb>): SystemContext {
    return {
        agentDb: agentDb as unknown as AgentDb,
        sessionManager: { get: vi.fn(), register: vi.fn() } as any,
        rbacFilter: { getAllowedTools: vi.fn().mockReturnValue(new Set(['brainstorm','search_knowledge','search_knowledge_base'])) } as any,
        workflowTracker: { getState: vi.fn(), createSession: vi.fn() } as any,
        guardrailService: {} as any,
        pluginManager: { invokeTool: vi.fn().mockResolvedValue({ success: true, data: 'ok' }) } as any
    };
}

describe('LegacyPlugin integration', () => {
    let legacy: LegacyPlugin;
    let ctx: McpContext;
    let systemCtx: SystemContext;

    beforeEach(() => {
        const mockDb = makeMockAgentDb();
        systemCtx = makeMockSystemContext(mockDb);
        legacy = new LegacyPlugin(systemCtx);
        ctx = {
            session_id: 's1',
            user_id: 'u1',
            domain: 'discovery',
            role: 'architect',
            phase: 'ideation',
            track: 'discovery'
        } as any;
    });

    it('should expose search_knowledge and brainstorm tools', async () => {
        const toolNames = legacy.tools.map(t => t.name);
        expect(toolNames).toContain('search_knowledge');
        expect(toolNames).toContain('brainstorm');
    });

    it('search_knowledge forwards call via pluginManager', async () => {
        const res = await legacy.handlers['search_knowledge']({ q: 'x' }, ctx);
        expect(systemCtx.pluginManager.invokeTool).toHaveBeenCalledWith('search_knowledge_base', { q: 'x' }, expect.any(Object));
        expect(res.success).toBe(true);
    });

    it('brainstorm warning + forward', async () => {
        const spy = vi.spyOn(console, 'warn').mockImplementation(() => {});
        const res = await legacy.handlers['brainstorm']({}, ctx);
        expect(spy).toHaveBeenCalled();
        expect(systemCtx.pluginManager.invokeTool).toHaveBeenCalledWith('brainstorm', {}, expect.any(Object));
        spy.mockRestore();
    });
});