import { test, expect } from '@playwright/test';

const BASE = 'http://localhost:3000/mcp';

async function listToolsForRole(roleHeader?: string): Promise<string[]> {
    const headers: Record<string, string> = {
        'Content-Type': 'application/json',
        'Accept': 'application/json, text/event-stream',
    };
    if (roleHeader) {
        headers['x-active-role'] = roleHeader;
    }

    // 1. Initialize session
    const initRes = await fetch(`${BASE}/http`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
            jsonrpc: '2.0',
            id: 1,
            method: 'initialize',
            params: {
                protocolVersion: '2024-11-05',
                capabilities: {},
                clientInfo: { name: 'rbac-e2e-test', version: '1.0' },
            },
        }),
    });

    // Fallback logic for streamable HTTP session ID from initRes
    let sessionId = initRes.headers.get('mcp-session-id');
    if (!sessionId) {
        const body: any = await initRes.json();
        sessionId = body?.result?.sessionId ?? body?.sessionId ?? null;
    }
    if (!sessionId) {
        throw new Error(`Failed to initialize session. Status: ${initRes.status}`);
    }

    headers['mcp-session-id'] = sessionId;

    // 2. Send notifications/initialized
    await fetch(`${BASE}/http`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
            jsonrpc: '2.0',
            method: 'notifications/initialized'
        }),
    });

    // 3. Call tools/list
    const toolsRes = await fetch(`${BASE}/http`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
            jsonrpc: '2.0',
            id: 2,
            method: 'tools/list'
        }),
    });

    const text = await toolsRes.text();
    const dataLine = text.split('\n').find(l => l.startsWith('data:'));
    if (!dataLine) throw new Error(`No data line in SSE response: ${text.substring(0, 100)}...`);

    const data: any = JSON.parse(dataLine.slice(5).trim());
    if (data.error) throw new Error(`MCP Error: ${JSON.stringify(data.error)}`);

    return data.result.tools.map((t: any) => t.name);
}

test.describe('MCP Tool Surface — RBAC E2E', () => {

    test('Unknown or missing role should only see public tools', async () => {
        const toolNames = await listToolsForRole();

        expect(toolNames).toContain('search_knowledge');
        expect(toolNames).toContain('get_policy');

        // These are not public
        expect(toolNames).not.toContain('request_workflow_transition');
        expect(toolNames).not.toContain('get_workflow_state');
        expect(toolNames).not.toContain('get_role');
        expect(toolNames).not.toContain('get_workflow');
        expect(toolNames).not.toContain('get_template');
    });

    test('Explorer role should see specific tools, but not mutating tools', async () => {
        const toolNames = await listToolsForRole('explorer');

        // Explorer specific
        expect(toolNames).toContain('get_role');
        expect(toolNames).toContain('get_workflow');
        expect(toolNames).toContain('list_templates');

        // Still public available
        expect(toolNames).toContain('search_knowledge');

        // Not allowed for explorer
        expect(toolNames).not.toContain('request_workflow_transition');
    });

    test('Backend developer role should see state mutation tools', async () => {
        const toolNames = await listToolsForRole('backend_developer');

        // Backend developer specific
        expect(toolNames).toContain('get_role');
        expect(toolNames).toContain('get_workflow_state');
        expect(toolNames).toContain('request_workflow_transition');
        expect(toolNames).toContain('search_knowledge'); // public
    });
});
