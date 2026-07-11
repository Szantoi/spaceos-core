import { describe, it, expect, vi, beforeEach } from 'vitest';
import NodeCache from 'node-cache';
import { RbacFilter } from '../../mcp/RbacFilter';
import { AgentDb } from '../../mcp/AgentDb';

// Mock AgentDb
vi.mock('../../mcp/AgentDb', () => {
    return {
        AgentDb: vi.fn().mockImplementation(() => {
            return {
                findSchemaByRoleName: vi.fn(),
                getSchemaVersion: vi.fn().mockReturnValue(1)
            };
        })
    };
});

describe('RbacFilter', () => {
    let mockAgentDb: any;
    let rbacFilter: RbacFilter;

    beforeEach(() => {
        vi.clearAllMocks();
        mockAgentDb = new AgentDb({} as any);
        rbacFilter = new RbacFilter(mockAgentDb);
    });

    it('should return public tools for an unknown role', () => {
        mockAgentDb.findSchemaByRoleName.mockReturnValue(null);

        const tools = rbacFilter.getAllowedTools('unknown_role');

        expect(tools.has('list_domains')).toBe(true);
        expect(tools.has('search_knowledge')).toBe(true);
        expect(tools.size).toBe(10); // Only public tools
    });

    it('should query AgentDb on cache miss and cache the result', () => {
        const permissions = JSON.stringify(['tool1', 'tool2']);
        mockAgentDb.findSchemaByRoleName.mockReturnValue({
            role_name: 'dev',
            mcp_tool_permissions: permissions
        });

        // First call: Cache miss
        const tools1 = rbacFilter.getAllowedTools('dev');
        expect(mockAgentDb.findSchemaByRoleName).toHaveBeenCalledTimes(1);
        expect(tools1.has('tool1')).toBe(true);
        expect(tools1.has('list_domains')).toBe(true);

        // Second call: Cache hit
        const tools2 = rbacFilter.getAllowedTools('dev');
        expect(mockAgentDb.findSchemaByRoleName).toHaveBeenCalledTimes(1); // Still 1
        expect(tools2).toEqual(tools1);
    });

    it('should handle malformed JSON in database and fallback to public tools', () => {
        mockAgentDb.findSchemaByRoleName.mockReturnValue({
            role_name: 'broken_role',
            mcp_tool_permissions: 'invalid-json'
        });

        const tools = rbacFilter.getAllowedTools('broken_role');
        expect(tools.has('list_domains')).toBe(true);
        expect(tools.size).toBe(10); // Only public tools
    });

    it('should flush cache when schema version changes', () => {
        mockAgentDb.getSchemaVersion.mockReturnValue(1);
        mockAgentDb.findSchemaByRoleName.mockReturnValue({
            role_name: 'dev',
            mcp_tool_permissions: JSON.stringify(['tool1'])
        });

        // Populate cache
        rbacFilter.getAllowedTools('dev');
        expect(mockAgentDb.findSchemaByRoleName).toHaveBeenCalledTimes(1);

        // Change version
        mockAgentDb.getSchemaVersion.mockReturnValue(2);

        // Next call should miss cache
        rbacFilter.getAllowedTools('dev');
        expect(mockAgentDb.findSchemaByRoleName).toHaveBeenCalledTimes(2);
    });

    it('should resolve cache hit performance benchmark', () => {
        mockAgentDb.findSchemaByRoleName.mockReturnValue({
            role_name: 'dev',
            mcp_tool_permissions: JSON.stringify(['tool1'])
        });

        // Fill cache
        rbacFilter.getAllowedTools('dev');

        const start = performance.now();
        for (let i = 0; i < 1000; i++) {
            rbacFilter.getAllowedTools('dev');
        }
        const end = performance.now();
        const avgHit = (end - start) / 1000;

        console.log(`[RbacFilter Benchmark] Cache hit avg: ${avgHit.toFixed(4)}ms`);
        expect(avgHit).toBeLessThan(1); // AC-15: <1ms for cache hit
    });

    it('should resolve DB query performance benchmark (simulated)', () => {
        mockAgentDb.findSchemaByRoleName.mockImplementation(() => {
            // Simulate common DB latency
            const start = performance.now();
            while (performance.now() - start < 2) { } // 2ms busy wait
            return {
                role_name: 'dev',
                mcp_tool_permissions: JSON.stringify(['tool1'])
            };
        });

        const start = performance.now();
        rbacFilter.getAllowedTools('dev');
        const end = performance.now();
        const totalTime = end - start;

        console.log(`[RbacFilter Benchmark] DB miss total: ${totalTime.toFixed(4)}ms`);
        expect(totalTime).toBeLessThan(10); // AC-15: <10ms for DB query
    });

    it('should enforce track restrictions (discovery vs delivery)', () => {
        // grant both tools in permissions
        mockAgentDb.findSchemaByRoleName.mockReturnValue({
            role_name: 'dev',
            mcp_tool_permissions: JSON.stringify([
                'discovery_foo',
                'delivery_bar',
                'reference_prior_discovery',
                'submit_discovery_outcome',
                'check_constraints',
                'get_phase_guidance',
                'track_blocker',
                'query_blockers'
            ])
        });
        const allowed = rbacFilter.getAllowedTools('dev');
        expect(allowed.has('discovery_foo')).toBe(true);
        expect(allowed.has('delivery_bar')).toBe(true);
        expect(allowed.has('reference_prior_discovery')).toBe(true);
        expect(allowed.has('submit_discovery_outcome')).toBe(true);
        // delivery role with discovery track should deny discovery tools
        expect(rbacFilter.hasPermission('discovery_foo', 'dev', 'delivery')).toBe(false);
        expect(rbacFilter.hasPermission('reference_prior_discovery', 'dev', 'delivery')).toBe(false);
        expect(rbacFilter.hasPermission('submit_discovery_outcome', 'dev', 'delivery')).toBe(false);
        expect(rbacFilter.hasPermission('delivery_bar', 'dev', 'delivery')).toBe(true);
        // discovery track denies delivery tool
        expect(rbacFilter.hasPermission('delivery_bar', 'dev', 'discovery')).toBe(false);
        expect(rbacFilter.hasPermission('discovery_foo', 'dev', 'discovery')).toBe(true);
        expect(rbacFilter.hasPermission('reference_prior_discovery', 'dev', 'discovery')).toBe(true);
        expect(rbacFilter.hasPermission('submit_discovery_outcome', 'dev', 'discovery')).toBe(true);
        // if no track supplied, fallback to role permissions only
        expect(rbacFilter.hasPermission('discovery_foo', 'dev')).toBe(true);
        expect(rbacFilter.hasPermission('delivery_bar', 'dev')).toBe(true);
        expect(rbacFilter.hasPermission('reference_prior_discovery', 'dev')).toBe(true);
        expect(rbacFilter.hasPermission('submit_discovery_outcome', 'dev')).toBe(true);
    });
});
