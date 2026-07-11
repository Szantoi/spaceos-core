import { test, expect } from '@playwright/test';

/**
 * MCP API E2E Test Suite
 *
 * Tests the MCP tool surface at the HTTP REST level.
 * No browser is launched — all tests use Playwright's `request` API context.
 *
 * Prerequisites:
 *   - Agent server running: npm run dev (in src/agent-system/server)
 *   - At least one .policy.md file in database/standards/core/core/
 *   - At least one role in database/roles/engineering/
 */

test.describe('MCP Tool Surface — API E2E', () => {

    // ── Test 1: Tool Discovery ─────────────────────────────────────────────────
    test('GET /mcp/tools returns manifest including get_policy', async ({ request }) => {
        const response = await request.get('/mcp/tools');

        expect(response.status()).toBe(200);

        const body = await response.json();
        expect(body).toHaveProperty('tools');
        expect(Array.isArray(body.tools)).toBeTruthy();

        const toolNames = body.tools.map((t: { name: string }) => t.name);
        expect(toolNames).toContain('get_policy');
        expect(toolNames).toContain('get_role');
        expect(toolNames).toContain('get_workflow');
        expect(toolNames).toContain('search_knowledge');
        expect(toolNames).toContain('get_role_context');
        expect(toolNames).toContain('get_project_summary');
        expect(toolNames).toContain('get_epic_summary');

        console.log(`✅ Tool discovery passed. Found ${toolNames.length} tools: ${toolNames.join(', ')}`);
    });

    // ── Test 2: get_policy — Happy Path ───────────────────────────────────────
    test('GET /mcp/policy?name=knowledge_structure returns 200 with content', async ({ request }) => {
        const response = await request.get('/mcp/policy?name=knowledge_structure');

        expect(response.status()).toBe(200);

        const body = await response.json();
        expect(body).toHaveProperty('name', 'knowledge_structure');
        expect(body).toHaveProperty('content');
        expect(typeof body.content).toBe('string');
        expect(body.content.length).toBeGreaterThan(100);

        console.log(`✅ get_policy happy path passed. Content length: ${body.content.length} chars`);
    });

    // ── Test 3: get_role — Happy Path ─────────────────────────────────────────
    test('GET /mcp/role returns 200 for a known domain/role', async ({ request }) => {
        // First discover what domains/roles exist
        const domainsRes = await request.get('/mcp/domains');
        expect(domainsRes.status()).toBe(200);
        const { domains } = await domainsRes.json();
        expect(domains.length).toBeGreaterThan(0);

        const firstDomain = domains[0];
        const rolesRes = await request.get(`/mcp/roles?domain=${firstDomain}`);
        expect(rolesRes.status()).toBe(200);
        const { roles } = await rolesRes.json();
        expect(roles.length).toBeGreaterThan(0);

        const firstRole = roles[0];
        const roleRes = await request.get(`/mcp/role?domain=${firstDomain}&role=${firstRole}`);
        expect(roleRes.status()).toBe(200);

        const body = await roleRes.json();
        expect(body).toHaveProperty('content');
        expect(body.content.length).toBeGreaterThan(50);

        console.log(`✅ get_role happy path passed for: ${firstDomain}/${firstRole}`);
    });

    // ── Test 4: get_policy — Not Found ────────────────────────────────────────
    test('GET /mcp/policy?name=nonexistent returns 404 with error', async ({ request }) => {
        const response = await request.get('/mcp/policy?name=nonexistent_policy_xyz_abc');

        expect(response.status()).toBe(404);

        const body = await response.json();
        expect(body).toHaveProperty('error');
        expect(body.error).toContain('nonexistent_policy_xyz_abc');

        console.log(`✅ get_policy 404 path passed. Error: ${body.error.substring(0, 80)}...`);
    });

    // ── Test 5: get_role_context endpoint ───────────────────────────────────
    test('GET /mcp/role_context produces combined sections', async ({ request }) => {
        // use a known domain/role from the database/roles directory
        const domainsRes = await request.get('/mcp/domains');
        const { domains } = await domainsRes.json();
        const domain = domains[0];
        const rolesRes = await request.get(`/mcp/roles?domain=${domain}`);
        const { roles } = await rolesRes.json();
        const role = roles[0];

        const res = await request.get(`/mcp/role_context?domain=${domain}&role=${role}`);
        expect(res.status()).toBe(200);
        const body = await res.json();
        expect(body.content).toContain('## Role');
        expect(body.content).toContain('## Schema');
        expect(body.content).toContain('## Runbook');
        console.log(`✅ role_context returned sections for ${domain}/${role}`);
    });

    // ── Test 6: project/epic summary endpoints (basic smoke) ───────────────
    test('GET /mcp/project_summary returns JSON array', async ({ request }) => {
        const res = await request.get('/mcp/project_summary');
        expect(res.status()).toBe(200);
        const body = await res.json();
        expect(body).toHaveProperty('epics');
        expect(Array.isArray(body.epics)).toBe(true);
        console.log(`✅ project_summary returned ${body.epics.length} epics`);
    });

    test('GET /mcp/epic_summary without param returns 400', async ({ request }) => {
        const res = await request.get('/mcp/epic_summary');
        expect(res.status()).toBe(400);
    });

    // we can't predict an existing epic name reliably, but the tool should still return 404 if not found
    test('GET /mcp/epic_summary?epic=NONEXISTENT returns 404', async ({ request }) => {
        const res = await request.get('/mcp/epic_summary?epic=NONEXISTENT');
        expect(res.status()).toBe(404);
    });

});
