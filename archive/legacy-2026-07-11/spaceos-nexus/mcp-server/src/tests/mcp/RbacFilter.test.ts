import { describe, it, expect, vi, beforeEach } from 'vitest';
import { RbacFilter } from '../../mcp/RbacFilter';
import { AgentDb } from '../../mcp/AgentDb';

describe('RbacFilter', () => {
    let mockAgentDb: any;
    let rbacFilter: RbacFilter;

    beforeEach(() => {
        mockAgentDb = {
            findSchemaByRoleName: vi.fn()
        };
        rbacFilter = new RbacFilter(mockAgentDb as unknown as AgentDb);
    });

    it('should return public tools if role is not found', async () => {
        mockAgentDb.findSchemaByRoleName.mockReturnValue(null);

        const tools = await rbacFilter.getAllowedTools('non-existent-role');

        expect(tools).toContain('search_knowledge');
        expect(tools).toContain('list_domains');
        expect(tools).not.toContain('write_resource');
        expect(mockAgentDb.findSchemaByRoleName).toHaveBeenCalledWith('non-existent-role');
    });

    it('should return specific tools from the schema', async () => {
        const mockSchema = {
            role_name: 'test-role',
            mcp_tool_permissions: JSON.stringify(['tool1', 'tool2'])
        };
        mockAgentDb.findSchemaByRoleName.mockReturnValue(mockSchema);

        const tools = await rbacFilter.getAllowedTools('test-role');

        expect(tools).toContain('tool1');
        expect(tools).toContain('tool2');
        expect(tools).toContain('search_knowledge'); // Public tools should still be included
    });

    it('should cache results and not query DB again for the same role', async () => {
        const mockSchema = {
            role_name: 'cached-role',
            mcp_tool_permissions: JSON.stringify(['tool1'])
        };
        mockAgentDb.findSchemaByRoleName.mockReturnValue(mockSchema);

        // First call
        await rbacFilter.getAllowedTools('cached-role');
        // Second call
        await rbacFilter.getAllowedTools('cached-role');

        expect(mockAgentDb.findSchemaByRoleName).toHaveBeenCalledTimes(1);
    });

    it('should handle malformed JSON in database gracefully', async () => {
        mockAgentDb.findSchemaByRoleName.mockReturnValue({
            role_name: 'broken-role',
            mcp_tool_permissions: 'invalid-json'
        });

        const tools = await rbacFilter.getAllowedTools('broken-role');

        expect(tools).toContain('search_knowledge');
        expect(tools.size).toBeGreaterThan(0);
    });

    it('should handle database errors gracefully', async () => {
        mockAgentDb.findSchemaByRoleName.mockImplementation(() => {
            throw new Error('DB Error');
        });

        const tools = await rbacFilter.getAllowedTools('error-role');

        expect(tools).toContain('search_knowledge');
    });

    it('should invalidate cache for a specific role', async () => {
        const mockSchema = {
            role_name: 'invalidate-role',
            mcp_tool_permissions: JSON.stringify(['tool1'])
        };
        mockAgentDb.findSchemaByRoleName.mockReturnValue(mockSchema);

        await rbacFilter.getAllowedTools('invalidate-role');
        rbacFilter.invalidateCache('invalidate-role');
        await rbacFilter.getAllowedTools('invalidate-role');

        expect(mockAgentDb.findSchemaByRoleName).toHaveBeenCalledTimes(2);
    });
});
