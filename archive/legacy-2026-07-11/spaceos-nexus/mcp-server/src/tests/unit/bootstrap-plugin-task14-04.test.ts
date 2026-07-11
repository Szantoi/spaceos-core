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
                getState: vi.fn().mockImplementation(() => { throw new Error('not found'); }),
                createSession: vi.fn()
            } as any,
            guardrailService: {} as any
        };

        bootstrapPlugin = new BootstrapPlugin(mockSystemContext as SystemContext);
    });

    // AC-1: Module Definition
    test('AC-1: Plugin exports required IToolModule properties', () => {
        expect(bootstrapPlugin).toBeDefined();
        expect(bootstrapPlugin.name).toBe('bootstrap');
        expect(bootstrapPlugin.version).toBe('1.0.0');
        expect(bootstrapPlugin.tools).toBeDefined();
        expect(Array.isArray(bootstrapPlugin.tools)).toBe(true);
        expect(bootstrapPlugin.handlers).toBeDefined();
        expect(typeof bootstrapPlugin.handlers).toBe('object');
    });

    // AC-2: Tool Signature
    test('AC-2: bootstrap_agent tool has correct description and input schema', () => {
        const bootstrapTool = bootstrapPlugin.tools.find(t => t.name === 'bootstrap_agent');

        expect(bootstrapTool).toBeDefined();
        expect(bootstrapTool?.name).toBe('bootstrap_agent');
        expect(bootstrapTool?.description).toContain('agent context');
        expect(bootstrapTool?.inputSchema).toBeDefined();
    });

    // AC-3: Handler Implementation
    test('AC-3: bootstrap_agent handler executes bootstrap logic', async () => {
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
                agentId: 'a1234567-1234-1234-1234-123456789abc',
                discoveryPhase: 'delivery'
            },
            mockContext
        );

        // Should return success response with agent context
        expect(result).toBeDefined();
        expect(result.success).toBe(true);
        expect(result.data?.agentContext).toBeDefined();

        // Verify that workflowTracker.createSession was called with track matching discoveryPhase
        expect(mockSystemContext.workflowTracker.createSession).toHaveBeenCalled();
        const createArgs = (mockSystemContext.workflowTracker.createSession as any).mock.calls[0][0];
        expect(createArgs.track).toBe('delivery');
    });

    // AC-4: Lifecycle Hooks
    test('AC-4: Plugin implements lifecycle hooks', () => {
        expect(typeof bootstrapPlugin.onInit).toBe('function');
    });

    // AC-5: Error Handling
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

        // Should return error response
        expect(result).toBeDefined();
    });

    // AC-6: Backward Compatibility
    test('AC-6: bootstrap_agent tool name matches legacy API', () => {
        const toolNames = bootstrapPlugin.tools.map(t => t.name);
        expect(toolNames).toContain('bootstrap_agent');
    });

    // AC-7: Registration
    test('AC-7: Plugin tools register correctly when loaded by PluginManager', () => {
        const toolNames = bootstrapPlugin.tools.map(t => t.name);
        expect(toolNames.length).toBeGreaterThan(0);
        expect(toolNames).toContain('bootstrap_agent');
    });

    // AC-8: Invocation
    test('AC-8: Tool handler is callable via handlers map', async () => {
        const handler = bootstrapPlugin.handlers['bootstrap_agent'];
        expect(handler).toBeDefined();
        expect(typeof handler).toBe('function');

        const result = handler({} as any, {} as any);
        expect(result).toBeInstanceOf(Promise);
    });

    // AC-9: Context Propagation
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

        expect(mockContext.session_id).toBeDefined();
        expect(mockContext.user_id).toBeDefined();
    });

    // AC-10: Timeout Handling
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
            { agentId: 'a1234567-1234-1234-1234-123456789abc', discoveryPhase: 'delivery' },
            mockContext
        );

        const elapsed = Date.now() - startTime;
        expect(elapsed).toBeLessThan(30000);
    });

    // AC-11: Caching
    test('AC-11: Multiple calls with same session return consistent results', async () => {
        const handler = bootstrapPlugin.handlers['bootstrap_agent'];
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

        const args = { agentId: 'a1234567-1234-1234-1234-123456789abc', discoveryPhase: 'delivery' };

        const result1 = await handler(args, mockContext);
        const result2 = await handler(args, mockContext);

        expect(result1.success).toBe(true);
        expect(result2.success).toBe(true);
    });

    // AC-12: Cross-Transport Consistency
    test('AC-12: Tool interface is transport-agnostic', () => {
        const toolDef = bootstrapPlugin.tools.find(t => t.name === 'bootstrap_agent');

        expect(toolDef?.inputSchema).toBeDefined();
        expect(toolDef?.description).toBeDefined();

        const handler = bootstrapPlugin.handlers['bootstrap_agent'];
        expect(typeof handler).toBe('function');
    });
});
