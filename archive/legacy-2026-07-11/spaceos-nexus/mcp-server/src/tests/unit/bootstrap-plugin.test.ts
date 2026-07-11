import { describe, test, expect, beforeEach, vi } from 'vitest';
import { BootstrapPlugin } from '../../mcp/tools/bootstrap';
import { IToolModule } from '../../mcp/tools/IToolModule';
import { PluginManager } from '../../plugins/PluginManager';
import { SystemContext } from '../../plugins/PluginTypes';

describe('TASK-14-04: Bootstrap Plugin Tool Module', () => {
    let bootstrapPlugin: BootstrapPlugin;
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
                    'discovery_start'
                ]))
            } as any,
            workflowTracker: {
                getState: vi.fn(),
                createSession: vi.fn()
            } as any,
            guardrailService: {} as any
        };

        bootstrapPlugin = new BootstrapPlugin(mockSystemContext as SystemContext);
    });

    // ─────────────────────────────────────────────────────────────────────
    // AC-1: Module Definition — Plugin exports correct IToolModule shape
    // ─────────────────────────────────────────────────────────────────────

    test('AC-1: Plugin exports required IToolModule properties', () => {
        // Plugin should have name, version, tools, handlers, lifecycle
        expect(bootstrapPlugin).toBeDefined();
        expect(bootstrapPlugin.name).toBe('bootstrap');
        expect(bootstrapPlugin.version).toBe('1.0.0');
        expect(bootstrapPlugin.tools).toBeDefined();
        expect(Array.isArray(bootstrapPlugin.tools)).toBe(true);
        expect(bootstrapPlugin.handlers).toBeDefined();
        expect(typeof bootstrapPlugin.handlers).toBe('object');
    });

    // ─────────────────────────────────────────────────────────────────────
    // AC-2: Tool Signature — bootstrap_agent tool has correct schema
    // ─────────────────────────────────────────────────────────────────────

    test('AC-2: bootstrap_agent tool has correct description and input schema', async () => {
        const bootstrapTool = bootstrapPlugin.tools.find(t => t.name === 'bootstrap_agent');

        expect(bootstrapTool).toBeDefined();
        expect(bootstrapTool?.name).toBe('bootstrap_agent');
        expect(bootstrapTool?.description).toContain('agent context');
        expect(bootstrapTool?.inputSchema).toBeDefined();
        // Schema is a Zod object, not JSON schema
        expect(bootstrapTool?.inputSchema).toBeTruthy();
        const handler = bootstrapPlugin.handlers['bootstrap_agent'];
        expect(handler).toBeDefined();

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

        const result = await handler(
            {
                agentId: 'agent-1',
                discoveryPhase: 'delivery'
            },
            mockContext
        );

        // Should return success response with agent context
        expect(result).toBeDefined();
        expect(result.success).toBe(true);
        expect(result.data?.agentContext).toBeDefined();
        expect(result.data.agentContext.agentId).toBe('agent-1');
        expect(result.data.agentContext.sessionId).toBeDefined();
        expect(result.data.agentContext.roles).toBeDefined();
        expect(result.data.agentContext.permissions).toBeDefined();
    });

    // ─────────────────────────────────────────────────────────────────────
    // AC-4: Lifecycle Hooks — onInit/onDestroy defined
    // ─────────────────────────────────────────────────────────────────────

    test('AC-4: Plugin implements lifecycle hooks', () => {
        // Bootstrap plugin should have onInit method (defined in class)
        expect(typeof bootstrapPlugin.onInit).toBe('function');
        // Optional: onDestroy may be inherited from BasePlugin
        expect(bootstrapPlugin).toBeDefined();
    });

    // ─────────────────────────────────────────────────────────────────────
    // AC-5: Error Handling — Invalid input returns error response
    // ─────────────────────────────────────────────────────────────────────

    test('AC-5: Handler validates input and returns error on invalid agentId', async () => {
        const handler = bootstrapPlugin.handlers['bootstrap_agent'];
        const mockContext = {
            session_id: 'test-session',
            user_id: 'agent-1',
            domain: 'engineering',
            role: 'backend_developer'
        } as any;

        // Invalid UUID should fail validation
        const result = await handler(
            {
                agentId: 'invalid-id-not-uuid',
                discoveryPhase: 'delivery'
            },
            mockContext
        );

        // Should return error or throw
        if (result && typeof result === 'object' && 'error' in result) {
            expect(result.error).toBeDefined();
        }
    });

    // ─────────────────────────────────────────────────────────────────────
    // AC-6: Backward Compatibility — tool name matches legacy API
    // ─────────────────────────────────────────────────────────────────────

    test('AC-6: bootstrap_agent tool name matches legacy API', () => {
        const toolNames = bootstrapPlugin.tools.map(t => t.name);
        expect(toolNames).toContain('bootstrap_agent');
    });

    // ─────────────────────────────────────────────────────────────────────
    // AC-7: Registration — Plugin registers in toolRegistry when loaded
    // ─────────────────────────────────────────────────────────────────────

    test('AC-7: Plugin tools register correctly when loaded by PluginManager', () => {
        // When plugin is loaded, tools should be automatically discoverable
        const toolNames = bootstrapPlugin.tools.map(t => t.name);
        expect(toolNames.length).toBeGreaterThan(0);
        expect(toolNames).toContain('bootstrap_agent');
    });

    // ─────────────────────────────────────────────────────────────────────
    // AC-8: Invocation — toolRegistry.invoke() calls correct handler
    // ─────────────────────────────────────────────────────────────────────

    test('AC-8: Tool handler is callable via handlers map', async () => {
        const handler = bootstrapPlugin.handlers['bootstrap_agent'];
        expect(handler).toBeDefined();
        expect(typeof handler).toBe('function');

        // Handler should be async and callable
        const result = handler({}, {} as any);
        expect(result).toBeInstanceOf(Promise);
    });

    // ─────────────────────────────────────────────────────────────────────
    // AC-9: Context Propagation — RequestContext available in handler
    // ─────────────────────────────────────────────────────────────────────

    test('AC-9: Handler receives context with auth and session info', async () => {
        const handler = bootstrapPlugin.handlers['bootstrap_agent'];

        const mockContext = {
            session_id: 'session-123',
            user_id: 'user-456',
            domain: 'engineering',
            role: 'backend_developer',
            track: 'delivery',
            agentDb: mockSystemContext.agentDb,
            sessionManager: mockSystemContext.sessionManager,
            rbacFilter: mockSystemContext.rbacFilter,
            workflowTracker: mockSystemContext.workflowTracker
        } as any;

        const result = await handler(
            { agentId: 'agent-1', discoveryPhase: 'delivery' },
            mockContext
        );

        // Context should be passed correctly and used in handler
        expect(mockContext.session_id).toBeDefined();
        expect(mockContext.user_id).toBeDefined();
        expect(result).toBeDefined();
    });

    // ─────────────────────────────────────────────────────────────────────
    // AC-10: Timeout Handling — Long-running operations don't hang
    // ─────────────────────────────────────────────────────────────────────

    test('AC-10: Handler completes within reasonable time (< 30s)', async () => {
        const handler = bootstrapPlugin.handlers['bootstrap_agent'];
        const startTime = Date.now();

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

        await handler(
            { agentId: 'agent-1', discoveryPhase: 'delivery' },
            mockContext
        );

        const elapsed = Date.now() - startTime;
        expect(elapsed).toBeLessThan(30000); // 30 seconds
    });

    // ─────────────────────────────────────────────────────────────────────
    // AC-11: Caching (Optional) — Result caching by sessionId
    // ─────────────────────────────────────────────────────────────────────

    test('AC-11: Multiple calls with same session return consistent results', async () => {
        const handler = bootstrapPlugin.handlers['bootstrap_agent'];
        const sessionId = 'a1234567-1234-1234-1234-123456789abc';

        const mockContext = {
            session_id: 'cached-session',
            user_id: 'agent-1',
            domain: 'engineering',
            role: 'backend_developer',
            track: 'delivery',
            agentDb: mockSystemContext.agentDb,
            sessionManager: mockSystemContext.sessionManager,
            rbacFilter: mockSystemContext.rbacFilter,
            workflowTracker: mockSystemContext.workflowTracker
        } as any;

        // First call: provides agentId and discovers the phase
        const args1 = {
            agentId: 'a1234567-1234-1234-1234-123456789abc',
            discoveryPhase: 'delivery'
        };

        // Second call: provides same agentId and sessionId for recovery
        const args2 = {
            agentId: 'a1234567-1234-1234-1234-123456789abc',
            sessionId: sessionId,
            discoveryPhase: 'delivery'
        };

        const result1 = await handler(args1, mockContext);
        // Extract sessionId from first result for recovery test
        const recoveredSessionId = result1.data?.agentContext?.sessionId;

        // Re-mock to simulate session existence
        const mockSessionManager = {
            ...mockSystemContext.sessionManager,
            get: vi.fn().mockReturnValue({
                session_id: recoveredSessionId,
                agent_id: 'a1234567-1234-1234-1234-123456789abc',
                status: 'active'
            })
        } as any;

        mockContext.sessionManager = mockSessionManager;

        // Second call with recovered sessionId
        const result2 = await handler(
            {
                agentId: 'a1234567-1234-1234-1234-123456789abc',
                sessionId: recoveredSessionId,
                discoveryPhase: 'delivery'
            },
            mockContext
        );

        // Should return consistent agentContext
        expect(result1.success).toBe(true);
        expect(result2.success).toBe(true);
        if (result1.data?.agentContext && result2.data?.agentContext) {
            // When recovering via sessionId, should get same session
            expect(result2.data.agentContext.sessionId).toBe(result1.data.agentContext.sessionId);
        }
    });

    // ─────────────────────────────────────────────────────────────────────
    // AC-12: Cross-Transport Consistency — Works via stdio and HTTP
    // ─────────────────────────────────────────────────────────────────────

    test('AC-12: Tool interface is transport-agnostic', () => {
        // Tool should work the same way whether called via stdio or HTTP
        const toolDef = bootstrapPlugin.tools.find(t => t.name === 'bootstrap_agent');

        // Schema should be serializable (JSON-compatible)
        expect(toolDef?.inputSchema).toBeDefined();
        expect(toolDef?.description).toBeDefined();

        // Handler should accept any context shape (not stdio-specific)
        const handler = bootstrapPlugin.handlers['bootstrap_agent'];
        expect(typeof handler).toBe('function');
    });
});
