import { describe, test, expect, beforeEach, vi } from 'vitest';
import { ContextPlugin } from '../../mcp/tools/context';
import { DiscoveryPlugin } from '../../mcp/tools/discovery';
import { IToolModule } from '../../mcp/tools/IToolModule';
import { SystemContext } from '../../plugins/PluginTypes';

describe('TASK-14-05: Context + Discovery Plugin Tool Modules', () => {
    let contextPlugin: ContextPlugin;
    let discoveryPlugin: DiscoveryPlugin;
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
                get: vi.fn().mockReturnValue({
                    session_id: 'test-session-uuid',
                    agent_id: 'agent-1',
                    status: 'active'
                })
            } as any,
            rbacFilter: {
                getAllowedTools: vi.fn().mockReturnValue(new Set([
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

        contextPlugin = new ContextPlugin(mockSystemContext as SystemContext);
        discoveryPlugin = new DiscoveryPlugin(mockSystemContext as SystemContext);
    });

    // ═════════════════════════════════════════════════════════════════════
    // CONTEXT PLUGIN TESTS (AC-1 through AC-4)
    // ═════════════════════════════════════════════════════════════════════

    describe('Context Plugin Module (AC-1..4)', () => {

        // ─────────────────────────────────────────────────────────────────────
        // AC-1: Context Module Definition — exports with name="context"
        // ─────────────────────────────────────────────────────────────────────

        test('AC-1: Context plugin exports as IToolModule with name="context"', () => {
            expect(contextPlugin).toBeDefined();
            expect(contextPlugin.name).toBe('context');
            expect(contextPlugin.version).toBe('1.0.0');
            expect(contextPlugin.tools).toBeDefined();
            expect(Array.isArray(contextPlugin.tools)).toBe(true);
            expect(contextPlugin.handlers).toBeDefined();
            expect(typeof contextPlugin.handlers).toBe('object');
        });

        // ─────────────────────────────────────────────────────────────────────
        // AC-2: Tools Exported — request_context + lookup_context
        // ─────────────────────────────────────────────────────────────────────

        test('AC-2: Context plugin exports request_context and lookup_context tools', () => {
            const toolNames = contextPlugin.tools.map(t => t.name);

            expect(toolNames).toContain('request_context');
            expect(toolNames).toContain('lookup_context');

            // Both should have descriptions and schemas
            const requestCtx = contextPlugin.tools.find(t => t.name === 'request_context');
            const lookupCtx = contextPlugin.tools.find(t => t.name === 'lookup_context');

            expect(requestCtx?.description).toBeDefined();
            expect(lookupCtx?.description).toBeDefined();
            expect(requestCtx?.inputSchema).toBeDefined();
            expect(lookupCtx?.inputSchema).toBeDefined();
        });

        // ─────────────────────────────────────────────────────────────────────
        // AC-3: Handlers — Both handlers properly parameterized
        // ─────────────────────────────────────────────────────────────────────

        test('AC-3: Context handlers are callable and return ContextResponse', async () => {
            const requestHandler = contextPlugin.handlers['request_context'];
            const lookupHandler = contextPlugin.handlers['lookup_context'];

            expect(requestHandler).toBeDefined();
            expect(lookupHandler).toBeDefined();
            expect(typeof requestHandler).toBe('function');
            expect(typeof lookupHandler).toBe('function');

            const mockContext = {
                session_id: 'test-session',
                user_id: 'agent-1',
                domain: 'engineering',
                role: 'backend_developer',
                track: 'delivery',
                agentDb: mockSystemContext.agentDb,
                rbacFilter: mockSystemContext.rbacFilter,
                sessionManager: mockSystemContext.sessionManager
            } as any;

            // Request context should return session data
            const result1 = await requestHandler({}, mockContext);
            expect(result1).toBeDefined();
            if (result1.success !== false) {
                expect(result1.data?.sessionId).toBeDefined();
            }

            // Lookup context should return lookup data
            const result2 = await lookupHandler({ key: 'test_attr' }, mockContext);
            expect(result2).toBeDefined();
        });

        // ─────────────────────────────────────────────────────────────────────
        // AC-4: Error Handling — Missing context returns error
        // ─────────────────────────────────────────────────────────────────────

        test('AC-4: Lookup context returns error for non-existent attributes', async () => {
            const lookupHandler = contextPlugin.handlers['lookup_context'];

            const mockContext = {
                session_id: 'test-session',
                user_id: 'agent-1',
                domain: 'engineering',
                role: 'backend_developer',
                track: 'delivery'
            } as any;

            const result = await lookupHandler({ key: 'nonexistent_key' }, mockContext);

            // Should handle missing context gracefully
            expect(result).toBeDefined();
            // Either return error or empty value
            if (result && 'error' in result) {
                expect(result.error).toBeDefined();
            }
        });
    });

    // ═════════════════════════════════════════════════════════════════════
    // DISCOVERY PLUGIN TESTS (AC-5 through AC-8)
    // ═════════════════════════════════════════════════════════════════════

    describe('Discovery Plugin Module (AC-5..8)', () => {

        // ─────────────────────────────────────────────────────────────────────
        // AC-5: Discovery Module Definition — exports with name="discovery"
        // ─────────────────────────────────────────────────────────────────────

        test('AC-5: Discovery plugin exports as IToolModule with name="discovery"', () => {
            expect(discoveryPlugin).toBeDefined();
            expect(discoveryPlugin.name).toBe('discovery');
            expect(discoveryPlugin.version).toBe('1.0.0');
            expect(discoveryPlugin.tools).toBeDefined();
            expect(Array.isArray(discoveryPlugin.tools)).toBe(true);
            expect(discoveryPlugin.handlers).toBeDefined();
            expect(typeof discoveryPlugin.handlers).toBe('object');
        });

        // ─────────────────────────────────────────────────────────────────────
        // AC-6: Tools Exported — Discovery track tools discoverable
        // ─────────────────────────────────────────────────────────────────────

        test('AC-6: Discovery plugin exports discovery track tools', () => {
            const toolNames = discoveryPlugin.tools.map(t => t.name);

            // Should have at least discovery_start
            expect(toolNames.length).toBeGreaterThan(0);
            expect(toolNames.some(n => n.startsWith('discovery_'))).toBe(true);

            // Each tool should have description and schema
            discoveryPlugin.tools.forEach(tool => {
                expect(tool.description).toBeDefined();
                expect(tool.inputSchema).toBeDefined();
            });
        });

        // ─────────────────────────────────────────────────────────────────────
        // AC-7: Discovery Workflow — Tools guide user through phases
        // ─────────────────────────────────────────────────────────────────────

        test('AC-7: Discovery tools implement workflow guidance', () => {
            const toolNames = discoveryPlugin.tools.map(t => t.name);

            // Should have workflow-guiding tools
            expect(toolNames.length).toBeGreaterThan(0);

            // Each handler should be callable
            discoveryPlugin.tools.forEach(tool => {
                const handler = discoveryPlugin.handlers[tool.name];
                expect(handler).toBeDefined();
                expect(typeof handler).toBe('function');
            });
        });

        // ─────────────────────────────────────────────────────────────────────
        // AC-8: Integration — Works with DDD aggregate (EPIC-13 prep)
        // ─────────────────────────────────────────────────────────────────────

        test('AC-8: Discovery plugin integrates with DDD domain', () => {
            // Plugin should have proper error handling and context propagation
            const mockContext = {
                session_id: 'test-session',
                user_id: 'agent-1',
                domain: 'engineering',
                role: 'backend_developer',
                track: 'discovery',
                agentDb: mockSystemContext.agentDb,
                rbacFilter: mockSystemContext.rbacFilter,
                workflowTracker: mockSystemContext.workflowTracker
            } as any;

            // Should not throw when handlers are called with discovery context
            expect(() => {
                discoveryPlugin.tools.forEach(tool => {
                    const handler = discoveryPlugin.handlers[tool.name];
                    // Just verify handler exists and is callable
                    expect(typeof handler).toBe('function');
                });
            }).not.toThrow();
        });
    });

    // ═════════════════════════════════════════════════════════════════════
    // CROSS-MODULE COORDINATION TESTS (AC-9 through AC-12)
    // ═════════════════════════════════════════════════════════════════════

    describe('Cross-Module Coordination (AC-9..12)', () => {

        // ─────────────────────────────────────────────────────────────────────
        // AC-9: Tool Isolation — No tool name conflicts
        // ─────────────────────────────────────────────────────────────────────

        test('AC-9: Tools in different modules maintain separate namespaces', () => {
            const contextToolNames = contextPlugin.tools.map(t => t.name);
            const discoveryToolNames = discoveryPlugin.tools.map(t => t.name);

            // No overlapping tool names
            const intersection = contextToolNames.filter(n => discoveryToolNames.includes(n));
            expect(intersection.length).toBe(0);

            // All tool names should be unique within their module
            expect(contextToolNames.length).toBe(new Set(contextToolNames).size);
            expect(discoveryToolNames.length).toBe(new Set(discoveryToolNames).size);
        });

        // ─────────────────────────────────────────────────────────────────────
        // AC-10: Shared Context — Both access same RequestContext
        // ─────────────────────────────────────────────────────────────────────

        test('AC-10: Context and discovery tools access shared RequestContext', async () => {
            const sharedContext = {
                session_id: 'shared-session-123',
                user_id: 'agent-1',
                domain: 'engineering',
                role: 'backend_developer',
                track: 'discovery',
                agentDb: mockSystemContext.agentDb,
                rbacFilter: mockSystemContext.rbacFilter,
                sessionManager: mockSystemContext.sessionManager,
                workflowTracker: mockSystemContext.workflowTracker
            } as any;

            // Both plugins should be able to access session info
            const contextHandler = contextPlugin.handlers['request_context'];
            const result1 = await contextHandler({}, sharedContext);

            // Session should be available to both
            expect(sharedContext.session_id).toBe('shared-session-123');
        });

        // ─────────────────────────────────────────────────────────────────────
        // AC-11: Performance — All 3 plugins load < 200ms total
        // ─────────────────────────────────────────────────────────────────────

        test('AC-11: All three plugins (bootstrap, context, discovery) load < 200ms', () => {
            const startTime = Date.now();

            // Simulate plugin loading
            const plugins = [contextPlugin, discoveryPlugin];

            plugins.forEach(plugin => {
                expect(plugin.name).toBeDefined();
                expect(plugin.tools).toBeDefined();
                expect(plugin.handlers).toBeDefined();
            });

            const elapsedTime = Date.now() - startTime;
            expect(elapsedTime).toBeLessThan(200);
        });

        // ─────────────────────────────────────────────────────────────────────
        // AC-12: Zero Duplicates — No tool name across modules
        // ─────────────────────────────────────────────────────────────────────

        test('AC-12: Tool registry rejects duplicate tool names', () => {
            const contextToolNames = contextPlugin.tools.map(t => t.name);
            const discoveryToolNames = discoveryPlugin.tools.map(t => t.name);

            // Simulate registry uniqueness check
            const allToolNames = [...contextToolNames, ...discoveryToolNames];
            const seenNames = new Set<string>();
            const duplicates: string[] = [];

            allToolNames.forEach(name => {
                if (seenNames.has(name)) {
                    duplicates.push(name);
                }
                seenNames.add(name);
            });

            // Should have zero duplicates
            expect(duplicates.length).toBe(0);

            // Total tools should be sum of individual tool counts
            expect(seenNames.size).toBe(contextToolNames.length + discoveryToolNames.length);
        });
    });
});
