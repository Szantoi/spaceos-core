import { test, expect } from '@playwright/test';

const BASE = 'http://localhost:3000/mcp';

test.describe('MCP Session Role Switch Limitation', () => {

    test('Server caches tool list per session, ignoring mid-session role changes', async () => {
        // 1. Initialize session as backend_developer
        const headers: Record<string, string> = {
            'Content-Type': 'application/json',
            'Accept': 'application/json, text/event-stream',
            'x-active-role': 'backend_developer'
        };

        const initRes = await fetch(`${BASE}/http`, {
            method: 'POST',
            headers,
            body: JSON.stringify({
                jsonrpc: '2.0', id: 1, method: 'initialize',
                params: { protocolVersion: '2024-11-05', capabilities: {}, clientInfo: { name: 'rbac-test', version: '1.0' } }
            }),
        });

        // Extract session ID
        let sessionId = initRes.headers.get('mcp-session-id');
        if (!sessionId) {
            const body: any = await initRes.json();
            sessionId = body?.result?.sessionId ?? body?.sessionId ?? null;
        }
        if (!sessionId) throw new Error('No session ID');

        headers['mcp-session-id'] = sessionId;

        await fetch(`${BASE}/http`, {
            method: 'POST', headers,
            body: JSON.stringify({ jsonrpc: '2.0', method: 'notifications/initialized' }),
        });

        // 2. List tools as backend_developer
        const toolsRes1 = await fetch(`${BASE}/http`, {
            method: 'POST', headers,
            body: JSON.stringify({ jsonrpc: '2.0', id: 2, method: 'tools/list' }),
        });

        const data1: any = JSON.parse((await toolsRes1.text()).split('\n').find(l => l.startsWith('data:'))!.slice(5).trim());
        const tools1 = data1.result.tools.map((t: any) => t.name);

        expect(tools1).toContain('get_workflow_state'); // backend developer has this

        // 3. Change role mid-session to explorer
        headers['x-active-role'] = 'explorer';

        // 4. List tools again on the SAME session
        const toolsRes2 = await fetch(`${BASE}/http`, {
            method: 'POST', headers,
            body: JSON.stringify({ jsonrpc: '2.0', id: 3, method: 'tools/list' }),
        });

        const data2: any = JSON.parse((await toolsRes2.text()).split('\n').find(l => l.startsWith('data:'))!.slice(5).trim());
        const tools2 = data2.result.tools.map((t: any) => t.name);

        // BEHAVIOR DEMONSTRATION:
        // Even though we changed the header to 'explorer', the session still yields the backend developer tools
        // because the Server object was instantiated and cached unconditionally on the sessionId!
        expect(tools2).toContain('get_workflow_state');
        expect(tools2).toEqual(tools1); // It's completely identical
    });
});
