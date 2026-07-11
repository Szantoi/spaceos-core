import { beforeEach, describe, expect, it, vi } from 'vitest';
import { BootstrapPlugin } from '../../mcp/tools/bootstrap';
import { SystemContext } from '../../plugins/PluginTypes';

describe('EPIC-17 TASK-17-04: switch_domain and list_available_domains tools', () => {
    let plugin: BootstrapPlugin;
    let mockSystemContext: Partial<SystemContext>;

    beforeEach(() => {
        mockSystemContext = {
            agentDb: {
                listRegisteredDomains: vi.fn().mockReturnValue([
                    { id: 'eng', name: 'engineering', description: 'Engineering' },
                    { id: 'mgt', name: 'management', description: 'Management' },
                ]),
                listDomains: vi.fn().mockReturnValue(['engineering', 'management', 'discovery']),
                getRegisteredDomain: vi.fn().mockImplementation((name: string) => {
                    if (name === 'engineering') return { id: 'eng', name: 'engineering' };
                    if (name === 'management') return { id: 'mgt', name: 'management' };
                    return null;
                }),
            } as any,
            sessionManager: {
                setCurrentDomainId: vi.fn(),
                get: vi.fn().mockImplementation((sessionId: string) => ({
                    id: sessionId,
                    role: 'admin_agent',
                    domain: 'engineering',
                    current_domain_id: 'eng',
                    agent_id: 'agent-1',
                    status: 'started',
                    created_at: new Date().toISOString(),
                    last_updated_at: null,
                })),
            } as any,
            rbacFilter: {
                getAllowedTools: vi.fn().mockReturnValue(new Set(['bootstrap_agent', 'switch_domain', 'list_available_domains'])),
            } as any,
            workflowTracker: {
                getState: vi.fn(),
                createSession: vi.fn(),
            } as any,
            guardrailService: {} as any,
        };

        plugin = new BootstrapPlugin(mockSystemContext as SystemContext);
    });

    it('list_available_domains returns registered domains', async () => {
        const result = await plugin.handlers['list_available_domains']({ include_unregistered: false }, {} as any);
        expect(result.success).toBe(true);
        expect(result.data.domains).toHaveLength(2);
    });

    it('list_available_domains include_unregistered merges role domains', async () => {
        const result = await plugin.handlers['list_available_domains']({ include_unregistered: true }, {} as any);
        expect(result.success).toBe(true);
        expect(result.data.domains.some((d: any) => d.name === 'discovery')).toBe(true);
    });

    it('switch_domain rejects non-admin caller with forbidden', async () => {
        const result = await plugin.handlers['switch_domain'](
            { domain_name: 'management', session_id: 'sess-1' },
            { session_id: 'sess-1', role: 'backend_developer' } as any
        );

        expect(result.success).toBe(false);
        expect(result.code).toBe('FORBIDDEN');
    });

    it('switch_domain returns 404 when target domain does not exist', async () => {
        const result = await plugin.handlers['switch_domain'](
            { domain_name: 'nonexistent', session_id: 'sess-1' },
            { session_id: 'sess-1', role: 'admin_agent' } as any
        );

        expect(result.success).toBe(false);
        expect(result.code).toBe('NOT_FOUND');
    });

    it('switch_domain updates session current_domain_id for admin', async () => {
        const result = await plugin.handlers['switch_domain'](
            { domain_name: 'management', session_id: 'sess-1' },
            { session_id: 'sess-1', role: 'admin_agent' } as any
        );

        expect((mockSystemContext.sessionManager as any).setCurrentDomainId).toHaveBeenCalledWith('sess-1', 'mgt');
        expect(result.success).toBe(true);
        expect(result.data.domain_id).toBe('mgt');
    });
});
