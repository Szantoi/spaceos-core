import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { mkdtempSync, rmSync, existsSync } from 'fs';
import { join } from 'path';
import { tmpdir } from 'os';
import { SessionManager } from '../../mcp/SessionManager';
import { ContextMiddleware } from '../../mcp/middleware/contextMiddleware';
import { BootstrapPlugin } from '../../mcp/tools/bootstrap';
import { SystemContext } from '../../plugins/PluginTypes';

describe('EPIC-17 TASK-17-03: bootstrap domain context propagation', () => {
    let tempDir: string;
    let dbPath: string;

    beforeEach(() => {
        tempDir = mkdtempSync(join(tmpdir(), 'bootstrap-domain-'));
        dbPath = join(tempDir, 'metadata.db');
    });

    afterEach(() => {
        try {
            if (existsSync(tempDir)) {
                rmSync(tempDir, { recursive: true, force: true });
            }
        } catch {
            // ignore teardown cleanup errors
        }
    });

    it('AC-5: SessionManager.get returns current_domain_id when present', () => {
        const sessionManager = new SessionManager(dbPath);
        const session = sessionManager.register('backend_developer', 'engineering', 'agent-1', 'sess-1', 'eng');

        expect(session.current_domain_id).toBe('eng');

        const loaded = sessionManager.get('sess-1');
        expect(loaded).toBeDefined();
        expect(loaded?.current_domain_id).toBe('eng');

        sessionManager.close();
    });

    it('AC-3: ContextMiddleware maps session.current_domain_id to McpContext.domain_id', () => {
        const sessionManager = new SessionManager(dbPath);
        sessionManager.register('backend_developer', 'engineering', 'agent-2', 'sess-2', 'eng');

        const workflowTrackerMock = {
            getState: vi.fn().mockReturnValue(null),
        } as any;

        const middleware = new ContextMiddleware(sessionManager, workflowTrackerMock);
        const context = middleware.handle({ arguments: { session_id: 'sess-2' } });

        expect(context.session_id).toBe('sess-2');
        expect(context.domain).toBe('engineering');
        expect(context.domain_id).toBe('eng');

        sessionManager.close();
    });

    it('AC-1: bootstrap_agent persists resolved domain id to session register call', async () => {
        const mockSystemContext: Partial<SystemContext> = {
            agentDb: {
                getRolesByDomain: vi.fn().mockReturnValue([{ role_name: 'backend_developer', domain: 'delivery' }]),
                getWorkflowsByRole: vi.fn().mockReturnValue([]),
                getTemplatesByRole: vi.fn().mockReturnValue([]),
                getRegisteredDomain: vi.fn().mockReturnValue({ id: 'delivery-domain-id' }),
            } as any,
            sessionManager: {
                get: vi.fn().mockReturnValue(undefined),
                register: vi.fn().mockReturnValue({
                    id: 'session-uuid-1',
                    agent_id: 'agent-1',
                    domain: 'delivery',
                    current_domain_id: 'delivery-domain-id',
                    role: 'backend_developer',
                    status: 'started',
                    created_at: new Date().toISOString(),
                    last_updated_at: null,
                }),
            } as any,
            rbacFilter: {
                getAllowedTools: vi.fn().mockReturnValue(new Set(['bootstrap_agent'])),
            } as any,
            workflowTracker: {
                getState: vi.fn(),
                createSession: vi.fn(),
            } as any,
            guardrailService: {} as any,
        };

        const plugin = new BootstrapPlugin(mockSystemContext as SystemContext);
        const result = await plugin.handlers['bootstrap_agent'](
            {
                agentId: '11111111-1111-4111-8111-111111111111',
                discoveryPhase: 'delivery',
            },
            {} as any
        );

        expect(result.success).toBe(true);
        expect((mockSystemContext.sessionManager as any).register).toHaveBeenCalledWith(
            'backend_developer',
            'delivery',
            '11111111-1111-4111-8111-111111111111',
            expect.any(String),
            'delivery-domain-id'
        );
        expect(result.data.domain_id).toBe('delivery-domain-id');
    });

    it('AC-2: bootstrap_agent fallback uses null current_domain_id when domain is not seeded', async () => {
        const mockSystemContext: Partial<SystemContext> = {
            agentDb: {
                getRolesByDomain: vi.fn().mockReturnValue([{ role_name: 'backend_developer', domain: 'delivery' }]),
                getWorkflowsByRole: vi.fn().mockReturnValue([]),
                getTemplatesByRole: vi.fn().mockReturnValue([]),
                getRegisteredDomain: vi.fn().mockReturnValue(null),
            } as any,
            sessionManager: {
                get: vi.fn().mockReturnValue(undefined),
                register: vi.fn().mockReturnValue({
                    id: 'session-uuid-2',
                    agent_id: 'agent-2',
                    domain: 'delivery',
                    current_domain_id: null,
                    role: 'backend_developer',
                    status: 'started',
                    created_at: new Date().toISOString(),
                    last_updated_at: null,
                }),
            } as any,
            rbacFilter: {
                getAllowedTools: vi.fn().mockReturnValue(new Set(['bootstrap_agent'])),
            } as any,
            workflowTracker: {
                getState: vi.fn(),
                createSession: vi.fn(),
            } as any,
            guardrailService: {} as any,
        };

        const plugin = new BootstrapPlugin(mockSystemContext as SystemContext);
        const result = await plugin.handlers['bootstrap_agent'](
            {
                agentId: '22222222-2222-4222-8222-222222222222',
                discoveryPhase: 'delivery',
            },
            {} as any
        );

        expect(result.success).toBe(true);
        expect((mockSystemContext.sessionManager as any).register).toHaveBeenCalledWith(
            'backend_developer',
            'delivery',
            '22222222-2222-4222-8222-222222222222',
            expect.any(String),
            null
        );
        expect(result.data.domain_id).toBeNull();
    });
});
