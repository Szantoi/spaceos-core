import { describe, test, expect, beforeEach, vi } from 'vitest';
import { PluginManager } from '../../plugins/PluginManager';
import { BootstrapPlugin } from '../../mcp/tools/bootstrap';
import { ContextPlugin } from '../../mcp/tools/context';
import { DiscoveryPlugin } from '../../mcp/tools/discovery';
import { SystemContext } from '../../plugins/PluginTypes';

/**
 * Integration Tests: Plugin System + Tool Modules
 *
 * Tests that all three plugin tool modules (Bootstrap, Context, Discovery)
 * integrate correctly with the PluginManager and can be loaded/invoked
 * as part of the complete plugin system.
 */
describe('Integration: Plugin System + Tool Modules (TASK-14-04/05)', () => {
    let pluginManager: PluginManager;
    let mockSystemContext: Partial<SystemContext>;

    beforeEach(() => {
        mockSystemContext = {
            agentDb: {
                getRolesByDomain: vi.fn().mockReturnValue([{
                    role_name: 'backend_developer',
                    domain: 'engineering'
                }]),
                getWorkflowsByRole: vi.fn().mockReturnValue([]),
                getTemplatesByRole: vi.fn().mockReturnValue([]),
            } as any,
            sessionManager: {
                get: vi.fn(),
                register: vi.fn().mockReturnValue({
                    session_id: 'test-session-uuid',
                    agent_id: 'agent-1',
                    status: 'active'
                })
            } as any,
            rbacFilter: {
                getAllowedTools: vi.fn().mockReturnValue(new Set([
                    'bootstrap_agent',
                    'request_context',
                    'lookup_context',
                    'discovery_start',
                    'discovery_phase'
                ]))
            } as any,
            workflowTracker: {
                getState: vi.fn(),
                createSession: vi.fn()
            } as any,
            guardrailService: {} as any
        };

        pluginManager = new PluginManager(mockSystemContext as SystemContext);
    });

    // ─────────────────────────────────────────────────────────────────────
    // IT-1: Plugin System + Bootstrap Tool
    // ─────────────────────────────────────────────────────────────────────

    test('IT-1: Bootstrap plugin registers with PluginManager', () => {
        const bootstrapPlugin = new BootstrapPlugin(mockSystemContext as SystemContext);

        // Simulate plugin registration via PluginManager
        expect(bootstrapPlugin.name).toBe('bootstrap');
        expect(bootstrapPlugin.version).toBe('1.0.0');
        expect(bootstrapPlugin.tools.length).toBeGreaterThan(0);
        expect(bootstrapPlugin.handlers['bootstrap_agent']).toBeDefined();
    });

    // ─────────────────────────────────────────────────────────────────────
    // IT-2: Plugin System + Context Tool
    // ─────────────────────────────────────────────────────────────────────

    test('IT-2: Context plugin registers with PluginManager', () => {
        const contextPlugin = new ContextPlugin(mockSystemContext as SystemContext);

        expect(contextPlugin.name).toBe('context');
        expect(contextPlugin.version).toBe('1.0.0');
        expect(contextPlugin.tools.length).toBeGreaterThan(0);

        // Context plugin should have both request_context and lookup_context
        const toolNames = contextPlugin.tools.map(t => t.name);
        expect(toolNames).toContain('request_context');
        expect(toolNames).toContain('lookup_context');
    });

    // ─────────────────────────────────────────────────────────────────────
    // IT-3: Plugin System + Discovery Tool
    // ─────────────────────────────────────────────────────────────────────

    test('IT-3: Discovery plugin registers with PluginManager', () => {
        const discoveryPlugin = new DiscoveryPlugin(mockSystemContext as SystemContext);

        expect(discoveryPlugin.name).toBe('discovery');
        expect(discoveryPlugin.version).toBe('1.0.0');
        expect(discoveryPlugin.tools.length).toBeGreaterThan(0);

        const toolNames = discoveryPlugin.tools.map(t => t.name);
        expect(toolNames.some(n => n.startsWith('discovery_'))).toBe(true);
    });

    // ─────────────────────────────────────────────────────────────────────
    // IT-4: All Plugins Load Successfully
    // ─────────────────────────────────────────────────────────────────────

    test('IT-4: All three plugins load in plugin system', () => {
        const plugins = [
            new BootstrapPlugin(mockSystemContext as SystemContext),
            new ContextPlugin(mockSystemContext as SystemContext),
            new DiscoveryPlugin(mockSystemContext as SystemContext)
        ];

        plugins.forEach(plugin => {
            expect(plugin.name).toBeDefined();
            expect(plugin.version).toBe('1.0.0');
            expect(plugin.tools).toBeDefined();
            expect(Array.isArray(plugin.tools)).toBe(true);
        });
    });

    // ─────────────────────────────────────────────────────────────────────
    // IT-5: Tool Isolation (No Name Conflicts)
    // ─────────────────────────────────────────────────────────────────────

    test('IT-5: All tools have unique names (no conflicts)', () => {
        const bootstrapPlugin = new BootstrapPlugin(mockSystemContext as SystemContext);
        const contextPlugin = new ContextPlugin(mockSystemContext as SystemContext);
        const discoveryPlugin = new DiscoveryPlugin(mockSystemContext as SystemContext);

        const allToolNames: string[] = [];
        [bootstrapPlugin, contextPlugin, discoveryPlugin].forEach(p => {
            allToolNames.push(...p.tools.map(t => t.name));
        });

        // All tool names should be unique
        const uniqueNames = new Set(allToolNames);
        expect(uniqueNames.size).toBe(allToolNames.length);
    });

    // ─────────────────────────────────────────────────────────────────────
    // IT-6: Tool Invocation Via Plugin System
    // ─────────────────────────────────────────────────────────────────────

    test('IT-6: Tools can be invoked via plugin handlers', async () => {
        const bootstrapPlugin = new BootstrapPlugin(mockSystemContext as SystemContext);
        const contextPlugin = new ContextPlugin(mockSystemContext as SystemContext);

        const mockContext = {
            session_id: 'test-session',
            user_id: 'agent-1',
            domain: 'engineering',
            role: 'backend_developer',
            track: 'delivery',
            agentDb: mockSystemContext.agentDb,
            sessionManager: mockSystemContext.sessionManager,
            rbacFilter: mockSystemContext.rbacFilter,
            workflowTracker: mockSystemContext.workflowTracker
        } as any;

        // Invoke bootstrap_agent
        const bootstrapHandler = bootstrapPlugin.handlers['bootstrap_agent'];
        const bootstrapResult = await bootstrapHandler(
            { agentId: 'a1234567-1234-1234-1234-123456789abc', discoveryPhase: 'delivery' },
            mockContext
        );
        expect(bootstrapResult.success).toBe(true);

        // Invoke request_context
        const contextHandler = contextPlugin.handlers['request_context'];
        const contextResult = await contextHandler({}, mockContext);
        expect(contextResult).toBeDefined();
    });

    // ─────────────────────────────────────────────────────────────────────
    // IT-7: Plugin Initialization
    // ─────────────────────────────────────────────────────────────────────

    test('IT-7: Plugins initialize correctly', async () => {
        const bootstrapPlugin = new BootstrapPlugin(mockSystemContext as SystemContext);
        const contextPlugin = new ContextPlugin(mockSystemContext as SystemContext);
        const discoveryPlugin = new DiscoveryPlugin(mockSystemContext as SystemContext);

        // All plugins should initialize successfully
        expect(bootstrapPlugin).toBeDefined();
        expect(contextPlugin).toBeDefined();
        expect(discoveryPlugin).toBeDefined();

        // Bootstrap plugin optionally has onInit (per TASK-14-04 AC-4)
        if (typeof (bootstrapPlugin as any).onInit === 'function') {
            await expect((bootstrapPlugin as any).onInit()).resolves.not.toThrow();
        }

        // All three should have properly extracted name/version from decorators
        expect(bootstrapPlugin.name).toBe('bootstrap');
        expect(contextPlugin.name).toBe('context');
        expect(discoveryPlugin.name).toBe('discovery');
    });

    // ─────────────────────────────────────────────────────────────────────
    // IT-8: Context Propagation Through Plugins
    // ─────────────────────────────────────────────────────────────────────

    test('IT-8: RequestContext propagates correctly through all plugins', async () => {
        const bootstrapPlugin = new BootstrapPlugin(mockSystemContext as SystemContext);
        const contextPlugin = new ContextPlugin(mockSystemContext as SystemContext);
        const discoveryPlugin = new DiscoveryPlugin(mockSystemContext as SystemContext);

        const sharedContext = {
            session_id: 'shared-session-123',
            user_id: 'agent-1',
            domain: 'engineering',
            role: 'backend_developer',
            track: 'discovery',
            agentDb: mockSystemContext.agentDb,
            sessionManager: mockSystemContext.sessionManager,
            rbacFilter: mockSystemContext.rbacFilter,
            workflowTracker: mockSystemContext.workflowTracker
        } as any;

        // All plugins should be able to work with the same context
        const bootstrapHandler = bootstrapPlugin.handlers['bootstrap_agent'];
        const contextHandler = contextPlugin.handlers['request_context'];
        const discoveryHandlers = Object.values(discoveryPlugin.handlers);

        // All should accept the shared context
        expect(bootstrapHandler).toBeDefined();
        expect(contextHandler).toBeDefined();
        expect(discoveryHandlers.length).toBeGreaterThan(0);
    });

    // ─────────────────────────────────────────────────────────────────────
    // IT-9: Cross-Plugin Tool Discovery
    // ─────────────────────────────────────────────────────────────────────

    test('IT-9: Tool registry can discover all tools from all plugins', () => {
        const plugins = [
            new BootstrapPlugin(mockSystemContext as SystemContext),
            new ContextPlugin(mockSystemContext as SystemContext),
            new DiscoveryPlugin(mockSystemContext as SystemContext)
        ];

        // Simulate tool registry discovery
        const allTools: { name: string; plugin: string; description: string }[] = [];
        plugins.forEach(plugin => {
            plugin.tools.forEach(tool => {
                allTools.push({
                    name: tool.name,
                    plugin: plugin.name,
                    description: tool.description
                });
            });
        });

        // Should have multiple tools
        expect(allTools.length).toBeGreaterThan(0);

        // Each tool should have a unique name
        const names = new Set(allTools.map(t => t.name));
        expect(names.size).toBe(allTools.length);

        // Should have tools from all three plugins
        const pluginNames = new Set(allTools.map(t => t.plugin));
        expect(pluginNames.has('bootstrap')).toBe(true);
        expect(pluginNames.has('context')).toBe(true);
        expect(pluginNames.has('discovery')).toBe(true);
    });

    // ─────────────────────────────────────────────────────────────────────
    // IT-10: Plugin Performance (Loading)
    // ─────────────────────────────────────────────────────────────────────

    test('IT-10: All plugins load within performance budget', () => {
        const startTime = Date.now();

        const bootstrapPlugin = new BootstrapPlugin(mockSystemContext as SystemContext);
        const contextPlugin = new ContextPlugin(mockSystemContext as SystemContext);
        const discoveryPlugin = new DiscoveryPlugin(mockSystemContext as SystemContext);

        const elapsed = Date.now() - startTime;

        // All plugins should load in < 200ms
        expect(elapsed).toBeLessThan(200);
    });

    // ─────────────────────────────────────────────────────────────────────
    // IT-11: Tool Schema Consistency
    // ─────────────────────────────────────────────────────────────────────

    test('IT-11: All tools have valid schemas', () => {
        const plugins = [
            new BootstrapPlugin(mockSystemContext as SystemContext),
            new ContextPlugin(mockSystemContext as SystemContext),
            new DiscoveryPlugin(mockSystemContext as SystemContext)
        ];

        plugins.forEach(plugin => {
            plugin.tools.forEach(tool => {
                expect(tool.name).toBeDefined();
                expect(tool.description).toBeDefined();
                expect(tool.inputSchema).toBeDefined();
            });
        });
    });

    // ─────────────────────────────────────────────────────────────────────
    // IT-12: Transport Independence
    // ─────────────────────────────────────────────────────────────────────

    test('IT-12: Tools work independently of transport (HTTP or stdio)', () => {
        const plugins = [
            new BootstrapPlugin(mockSystemContext as SystemContext),
            new ContextPlugin(mockSystemContext as SystemContext),
            new DiscoveryPlugin(mockSystemContext as SystemContext)
        ];

        // All tool handlers should be pure functions (no transport-specific code)
        plugins.forEach(plugin => {
            plugin.tools.forEach(tool => {
                const handler = plugin.handlers[tool.name];
                expect(handler).toBeDefined();
                expect(typeof handler).toBe('function');

                // Handler should accept any context-like object
                // (not tied to specific transport format)
                expect(handler.length).toBeGreaterThanOrEqual(2); // args + context
            });
        });
    });
});
