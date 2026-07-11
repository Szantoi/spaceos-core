import { test, expect } from '@playwright/test';

/**
 * E2E tests for the resource template system (TASK-14-08).
 *
 * These tests validate that the HTTP transport exposes the expected
 * resource discovery endpoints and resolves dynamic URIs correctly.
 */

test.describe('Resource Template E2E', () => {
    test('GET /mcp/resources returns known URI patterns', async ({ request }) => {
        const res = await request.get('/mcp/resources');
        expect(res.status()).toBe(200);

        const body = await res.json();
        expect(body).toHaveProperty('status', 'success');
        expect(Array.isArray(body.resources)).toBe(true);

        const patterns = body.resources.map((r: any) => r.uriPattern);
        expect(patterns).toContain('resource://role/{domain}/{role}');
        expect(patterns).toContain('resource://workflow/{type}');
        expect(patterns).toContain('resource://template/{category}');
        expect(patterns).toContain('resource://discovery/{phase}');
        expect(patterns).toContain('resource://task/{task_id}');
    });

    test('GET /mcp/resources/resolve resolves a role resource', async ({ request }) => {
        const uri = encodeURIComponent('resource://role/engineering/backend_developer');
        const res = await request.get(`/mcp/resources/resolve?uri=${uri}`);
        expect(res.status()).toBe(200);

        const body = await res.json();
        expect(body).toHaveProperty('status', 'success');
        expect(body.resource).toBeDefined();
        expect(body.resource.uri).toBe('resource://role/engineering/backend_developer');
        expect(body.resource.mimeType).toBe('application/json');

        // Ensure response does not leak filesystem paths
        const payload = JSON.stringify(body);
        expect(payload).not.toContain('database');
        expect(payload).not.toContain('Roles');
    });

    test('GET /mcp/resources/resolve returns 404 for missing resource', async ({ request }) => {
        const uri = encodeURIComponent('resource://role/engineering/does_not_exist');
        const res = await request.get(`/mcp/resources/resolve?uri=${uri}`);
        expect(res.status()).toBe(404);

        const body = await res.json();
        expect(body).toHaveProperty('status', 'error');
        expect(body.error).toContain('Role not found');
    });
});
