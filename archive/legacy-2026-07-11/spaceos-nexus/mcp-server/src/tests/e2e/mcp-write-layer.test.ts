import { test, expect } from '@playwright/test';

/**
 * MCP Write Layer — E2E Test Suite (ADR-007)
 *
 * Tests the WRITE layer endpoints:
 *   POST /mcp/session/register
 *   POST /mcp/artifact/submit
 *   POST /mcp/session/complete
 *
 * API-only tests — no browser required.
 * Uses Playwright `request` context.
 *
 * Prerequisites:
 *   - Server running: npm run dev (port 3000)
 *   - Template exists: engineering/backend_developer/implementation_report
 *   - workspaceRoot accessible (configured in .env or defaults to project root)
 */

const BASE = 'http://127.0.0.1:3000/mcp';

// ─── Helpers ─────────────────────────────────────────────────────────────────

/**
 * Build a minimal valid artifact body that passes all validations:
 * - Template exists: engineering/backend_developer/implementation_report
 * - Frontmatter has required fields: type + role (fallback)
 * - target_path is .md and has no traversal
 */
function validArtifactBody(sessionId?: string, targetPath?: string) {
    const content = [
        '---',
        'type: implementation',
        'role: backend_developer',
        'id: test-e2e-artifact',
        'title: "E2E Test Artifact"',
        'project: e2e-test',
        'date: 2026-03-02',
        'status: review_needed',
        '---',
        '',
        '# E2E Test Artifact',
        '',
        'This artifact was submitted by the Playwright write-layer E2E test suite.',
    ].join('\n');

    return {
        domain: 'engineering',
        role: 'backend_developer',
        type: 'implementation_report',
        content,
        target_path: targetPath ?? 'e2e-test-output/write-layer-test.md',
        project: 'e2e-test',
        user: 'playwright-e2e',
        ...(sessionId ? { session_id: sessionId } : {}),
    };
}

// ─── Test Suite ───────────────────────────────────────────────────────────────

test.describe('MCP Write Layer — ADR-007', () => {

    // ── Tool Discovery ────────────────────────────────────────────────────────

    test('GET /mcp/tools includes session_register, artifact_submit, session_complete', async ({ request }) => {
        const res = await request.get(`${BASE}/tools`);
        expect(res.status()).toBe(200);

        const body = await res.json();
        expect(body).toHaveProperty('tools');

        const toolNames: string[] = body.tools.map((t: { name: string }) => t.name);
        expect(toolNames).toContain('session_register');
        expect(toolNames).toContain('artifact_submit');
        expect(toolNames).toContain('session_complete');

        console.log(`✅ Write layer tools present in manifest. Total tools: ${toolNames.length}`);
    });

    // ── session/register ──────────────────────────────────────────────────────

    test('POST /mcp/session/register returns 201 with session_id', async ({ request }) => {
        const res = await request.post(`${BASE}/session/register`, {
            data: { role: 'backend_developer', domain: 'engineering', agent_name: 'playwright-test' },
        });

        expect(res.status()).toBe(201);

        const body = await res.json();
        expect(body).toHaveProperty('session_id');
        expect(typeof body.session_id).toBe('string');
        expect(body.session_id.length).toBeGreaterThan(10);
        expect(body).toHaveProperty('status', 'active');
        expect(body).toHaveProperty('role', 'backend_developer');
        expect(body).toHaveProperty('domain', 'engineering');

        console.log(`✅ session/register: session_id=${body.session_id}`);
    });

    test('POST /mcp/session/register missing body → 400', async ({ request }) => {
        const res = await request.post(`${BASE}/session/register`, {
            data: { agent_name: 'playwright-test' }, // missing role + domain
        });

        expect(res.status()).toBe(400);

        const body = await res.json();
        expect(body).toHaveProperty('error');
        console.log(`✅ session/register missing fields → 400: ${body.error}`);
    });

    // ── artifact/submit ───────────────────────────────────────────────────────

    test('POST /mcp/artifact/submit valid artifact → 200 ok', async ({ request }) => {
        const res = await request.post(`${BASE}/artifact/submit`, {
            data: validArtifactBody(),
        });

        expect(res.status()).toBe(200);

        const body = await res.json();
        expect(body).toHaveProperty('ok', true);
        expect(body).toHaveProperty('message');
        expect(body).toHaveProperty('resource');
        expect(body.resource).toHaveProperty('relative_path');

        console.log(`✅ artifact/submit valid → 200 ok. Resource path: ${body.resource.relative_path}`);
    });

    test('POST /mcp/artifact/submit with path traversal → 400', async ({ request }) => {
        const res = await request.post(`${BASE}/artifact/submit`, {
            data: validArtifactBody(undefined, '../../etc/passwd.md'),
        });

        expect(res.status()).toBe(400);

        const body = await res.json();
        expect(body).toHaveProperty('ok', false);
        console.log(`✅ artifact/submit traversal path → 400: ${body.message}`);
    });

    test('POST /mcp/artifact/submit missing frontmatter block → 400', async ({ request }) => {
        const res = await request.post(`${BASE}/artifact/submit`, {
            data: {
                domain: 'engineering',
                role: 'backend_developer',
                type: 'implementation_report',
                content: '# No frontmatter here\n\nJust body text without --- block.',
                target_path: 'e2e-test-output/no-frontmatter.md',
                project: 'e2e-test',
                user: 'playwright-e2e',
            },
        });

        expect(res.status()).toBe(400);

        const body = await res.json();
        expect(body).toHaveProperty('ok', false);
        console.log(`✅ artifact/submit missing frontmatter → 400: ${body.message}`);
    });

    test('POST /mcp/artifact/submit missing required frontmatter fields → 400', async ({ request }) => {
        // Frontmatter exists but missing required fields (type + role)
        const contentMissingFields = [
            '---',
            'title: "Missing type and role"',
            '---',
            '',
            '# Incomplete artifact',
        ].join('\n');

        const res = await request.post(`${BASE}/artifact/submit`, {
            data: {
                domain: 'engineering',
                role: 'backend_developer',
                type: 'implementation_report',
                content: contentMissingFields,
                target_path: 'e2e-test-output/missing-fields.md',
                project: 'e2e-test',
                user: 'playwright-e2e',
            },
        });

        expect(res.status()).toBe(400);

        const body = await res.json();
        expect(body).toHaveProperty('ok', false);
        expect(body).toHaveProperty('missing_fields');
        expect(Array.isArray(body.missing_fields)).toBeTruthy();
        expect(body.missing_fields.length).toBeGreaterThan(0);

        console.log(`✅ artifact/submit missing fields → 400. Missing: ${body.missing_fields?.join(', ')}`);
    });

    test('POST /mcp/artifact/submit non-.md target_path → 400', async ({ request }) => {
        const res = await request.post(`${BASE}/artifact/submit`, {
            data: validArtifactBody(undefined, 'e2e-test-output/malicious.exe'),
        });

        expect(res.status()).toBe(400);

        const body = await res.json();
        expect(body).toHaveProperty('ok', false);
        console.log(`✅ artifact/submit non-.md path → 400: ${body.message}`);
    });

    // ── session/complete ──────────────────────────────────────────────────────

    test('POST /mcp/session/complete without artifacts → 403 BLOCKED', async ({ request }) => {
        // Register fresh session
        const regRes = await request.post(`${BASE}/session/register`, {
            data: { role: 'backend_developer', domain: 'engineering' },
        });
        expect(regRes.status()).toBe(201);
        const { session_id } = await regRes.json();

        // Try to complete without submitting any artifacts
        const completeRes = await request.post(`${BASE}/session/complete`, {
            data: { session_id },
        });

        expect(completeRes.status()).toBe(403);

        const body = await completeRes.json();
        expect(body).toHaveProperty('ok', false);
        expect(body.message).toContain('BLOCKED');

        console.log(`✅ session/complete no artifacts → 403 BLOCKED: ${body.message}`);
    });

    test('POST /mcp/session/complete after artifact → 200 ok', async ({ request }) => {
        // Step 1: Register session
        const regRes = await request.post(`${BASE}/session/register`, {
            data: { role: 'backend_developer', domain: 'engineering', agent_name: 'playwright-complete-test' },
        });
        expect(regRes.status()).toBe(201);
        const { session_id } = await regRes.json();

        // Step 2: Submit artifact with session_id
        const submitRes = await request.post(`${BASE}/artifact/submit`, {
            data: validArtifactBody(session_id, `e2e-test-output/session-complete-test-${Date.now()}.md`),
        });
        expect(submitRes.status()).toBe(200);
        const submitBody = await submitRes.json();
        expect(submitBody.ok).toBe(true);

        // Step 3: Complete session
        const completeRes = await request.post(`${BASE}/session/complete`, {
            data: { session_id },
        });
        expect(completeRes.status()).toBe(200);

        const body = await completeRes.json();
        expect(body).toHaveProperty('ok', true);
        expect(body).toHaveProperty('artifact_count');
        expect(body.artifact_count).toBeGreaterThanOrEqual(1);
        expect(body.session.status).toBe('completed');

        console.log(`✅ Full write-layer flow: register → submit → complete. Artifacts: ${body.artifact_count}`);
    });

    test('POST /mcp/session/complete non-existent session → 404', async ({ request }) => {
        const res = await request.post(`${BASE}/session/complete`, {
            data: { session_id: 'non-existent-session-id-xyz-000' },
        });

        expect(res.status()).toBe(404);

        const body = await res.json();
        expect(body).toHaveProperty('ok', false);
        console.log(`✅ session/complete not found → 404: ${body.message}`);
    });

    test('POST /mcp/session/complete missing session_id → 400', async ({ request }) => {
        const res = await request.post(`${BASE}/session/complete`, {
            data: {},
        });

        expect(res.status()).toBe(400);

        const body = await res.json();
        expect(body).toHaveProperty('error');
        console.log(`✅ session/complete missing session_id → 400: ${body.error}`);
    });
});
