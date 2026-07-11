import { describe, it, expect, vi, beforeEach } from 'vitest';
import { LegacyPlugin } from '../../mcp/tools/legacy';
import { SystemContext } from '../../plugins/PluginTypes';

// create minimal mock system context with PluginManager stub
const makeMockContext = (): SystemContext => {
    return {
        agentDb: {} as any,
        sessionManager: {} as any,
        rbacFilter: {} as any,
        workflowTracker: {} as any,
        guardrailService: {} as any,
        pluginManager: {
            invokeTool: vi.fn().mockResolvedValue({ success: true, data: 'ok' })
        } as any
    };
};

describe('LegacyPlugin unit tests', () => {
    let plugin: LegacyPlugin;
    let mockCtx: SystemContext;

    beforeEach(() => {
        mockCtx = makeMockContext();
        plugin = new LegacyPlugin(mockCtx);
    });

    it('should register as IToolModule with deprecation metadata', () => {
        expect(plugin.name).toBe('Legacy Tools Adapter');
        expect((plugin as any).deprecated).toBe(true);
        expect((plugin as any).deprecation_removal).toContain('2026-06-01');
    });

    it('searchKnowledge wrapper logs deprecation and forwards call', async () => {
        const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
        const res = await plugin.handlers['search_knowledge']({ foo: 'bar' }, { session_id: 's1' } as any);
        expect(consoleSpy).toHaveBeenCalledWith(expect.stringContaining('[DEPRECATED] Tool search_knowledge'));
        expect(mockCtx.pluginManager!.invokeTool).toHaveBeenCalledWith('search_knowledge_base', { foo: 'bar' }, expect.any(Object));
        expect(res.success).toBe(true);
        consoleSpy.mockRestore();
    });

    it('brainstorm wrapper warns and forwards if underlying exists', async () => {
        const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
        const res = await plugin.handlers['brainstorm']({ baz: 123 }, { session_id: 's2' } as any);
        expect(warnSpy).toHaveBeenCalled();
        expect(mockCtx.pluginManager!.invokeTool).toHaveBeenCalledWith('brainstorm', { baz: 123 }, expect.any(Object));
        expect(res.success).toBe(true);
        warnSpy.mockRestore();
    });

    it('brainstorm returns error if pluginManager missing', async () => {
        plugin = new LegacyPlugin({} as any);
        const res = await plugin.handlers['brainstorm']({}, { session_id: 'x' } as any);
        expect(res.success).toBe(false);
        expect(res.error.code).toBe(503);
    });
});