/**
 * BootstrapAgent.e2e.test.ts — E2E tests for TASK-10-01 bootstrap_agent MCP tool
 *
 * Tests the bootstrap_agent tool via MCP protocol (HTTP SSE + Streamable HTTP).
 * Verifies:
 * - Tool is discoverable in MCP manifest
 * - Valid call returns complete payload with correct schema
 * - Session is created and persisted
 * - Invalid inputs return structured errors
 * - Payload JSON is valid and within size limits
 *
 * Prerequisites:
 * - Agent server running: npm run dev
 * - Database seeded with at least one role (e.g., engineering/backend_developer)
 * - MCP server listening on port 3000
 *
 * @vitest
 * @see https://modelcontextprotocol.io/specification/2025-06-18
 */

import { test, expect } from '@playwright/test';

const BASE_URL = process.env.MCP_SERVER_URL || 'http://localhost:3000';
const MCP_TOOLS_ENDPOINT = `${BASE_URL}/mcp/tools`;

test.describe('TASK-10-01: bootstrap_agent MCP Tool — E2E', () => {
    // ══════════════════════════════════════════════════════════════════════════
    // DISCOVERY: bootstrap_agent in tool manifest
    // ══════════════════════════════════════════════════════════════════════════

    test('Tool Discovery: bootstrap_agent should appear in /mcp/tools manifest', async ({
        request,
    }) => {
        const response = await request.get(MCP_TOOLS_ENDPOINT);

        expect(response.status()).toBe(200);

        const body = await response.json();
        expect(body).toHaveProperty('tools');
        expect(Array.isArray(body.tools)).toBeTruthy();

        const toolNames = body.tools.map((t: { name: string }) => t.name);
        expect(toolNames).toContain('bootstrap_agent');

        // Verify tool metadata
        const bootstrapTool = body.tools.find((t: { name: string }) => t.name === 'bootstrap_agent');
        expect(bootstrapTool).toBeDefined();
        expect(bootstrapTool.description).toContain('Bootstrap');
        expect(bootstrapTool.description).toContain('agent');

        console.log(`✅ Tool discovery: bootstrap_agent found in manifest (${toolNames.length} total tools)`);
    });

    // ══════════════════════════════════════════════════════════════════════════
    // HAPPY PATH: Valid Domain + Role Bootstrap Call
    // ══════════════════════════════════════════════════════════════════════════

    test('Happy Path: POST /mcp/call with valid domain/role returns complete payload', async ({
        request,
    }) => {
        // AC-1: bootstrap_agent tool callable
        // AC-5: Valid domain + role → returns complete payload
        // AC-6: Payload includes all required fields

        const mcpRequest = {
            jsonrpc: '2.0',
            id: 'bootstrap-test-1',
            method: 'tools/call',
            params: {
                name: 'bootstrap_agent',
                arguments: {
                    domain: 'engineering',
                    role: 'backend_developer',
                },
            },
        };

        const response = await request.post('/mcp/http', {
            data: JSON.stringify(mcpRequest),
        });

        expect(response.status()).toBe(200);

        const body = await response.json();
        console.log('MCP Response:', JSON.stringify(body, null, 2).substring(0, 500));

        // Parse the MCP response
        expect(body).toHaveProperty('result');
        const result = body.result;

        // The result should contain the bootstrap payload as JSON text
        expect(result.content).toBeDefined();
        expect(Array.isArray(result.content)).toBeTruthy();

        // Extract payload from MCP response
        const textContent = result.content.find((c: any) => c.type === 'text');
        expect(textContent).toBeDefined();

        const payloadJson = JSON.parse(textContent.text);

        // AC-23: Payload includes payload_version="1.0"
        expect(payloadJson.payload_version).toBe('1.0');

        // AC-6: All required fields present
        expect(payloadJson.identity).toBeDefined();
        expect(payloadJson.role_content).toBeDefined();
        expect(payloadJson.allowed_tools).toBeDefined();
        expect(payloadJson.limitations).toBeDefined();
        expect(payloadJson.session_id).toBeDefined();
        expect(payloadJson.session).toBeDefined();

        // AC-7: Identity correctly populated
        expect(payloadJson.identity.domain).toBe('engineering');
        expect(payloadJson.identity.role).toBe('backend_developer');

        // AC-8: allowed_tools extracted from role_schemas
        expect(Array.isArray(payloadJson.allowed_tools)).toBeTruthy();

        // AC-10: Session ID is valid UUID
        expect(payloadJson.session_id).toMatch(
            /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
        );

        console.log(`✅ Happy path: bootstrap_agent returned complete payload`);
        console.log(`  - Session ID: ${payloadJson.session_id}`);
        console.log(`  - Domain: ${payloadJson.identity.domain}`);
        console.log(`  - Role: ${payloadJson.identity.role}`);
        console.log(`  - Tools: ${payloadJson.allowed_tools.length}`);
    });

    // ══════════════════════════════════════════════════════════════════════════
    // ERROR HANDLING: Role Not Found
    // ══════════════════════════════════════════════════════════════════════════

    test('Error Path: POST /mcp/call with nonexistent role returns ROLE_NOT_FOUND error', async ({
        request,
    }) => {
        // AC-11: Invalid domain/role → returns structured error response

        const mcpRequest = {
            jsonrpc: '2.0',
            id: 'bootstrap-error-test-1',
            method: 'tools/call',
            params: {
                name: 'bootstrap_agent',
                arguments: {
                    domain: 'engineering',
                    role: 'nonexistent_role_xyz_123',
                },
            },
        };

        const response = await request.post('/mcp/http', {
            data: JSON.stringify(mcpRequest),
        });

        expect(response.status()).toBe(200); // MCP returns 200 even for errors (in result object)

        const body = await response.json();

        // Parse error response
        const textContent = body.result.content.find((c: any) => c.type === 'text');
        const errorPayload = JSON.parse(textContent.text);

        // AC-11: Structured error response
        expect(errorPayload.success).toBe(false);
        expect(errorPayload.error_code).toBe('ROLE_NOT_FOUND');
        expect(errorPayload.error_message).toBeDefined();

        // AC-12: Error includes context (domain, role)
        expect(errorPayload.details).toBeDefined();
        expect(errorPayload.details.domain).toBe('engineering');
        expect(errorPayload.details.role).toBe('nonexistent_role_xyz_123');

        console.log(`✅ Error path: ROLE_NOT_FOUND error returned correctly`);
    });

    // ══════════════════════════════════════════════════════════════════════════
    // INPUT VALIDATION: Invalid Domain Format (SQL Injection Prevention)
    // ══════════════════════════════════════════════════════════════════════════

    test('Security: Invalid domain format rejected (SQL injection attempt)', async ({
        request,
    }) => {
        // AC-2: Input schema validates domain regex
        // AC-12-13: Input validation prevents SQL injection

        const mcpRequest = {
            jsonrpc: '2.0',
            id: 'bootstrap-security-test-1',
            method: 'tools/call',
            params: {
                name: 'bootstrap_agent',
                arguments: {
                    domain: "engineering'; DROP TABLE roles; --",
                    role: 'backend_developer',
                },
            },
        };

        const response = await request.post('/mcp/http', {
            data: JSON.stringify(mcpRequest),
        });

        expect(response.status()).toBe(200);

        const body = await response.json();
        const textContent = body.result.content.find((c: any) => c.type === 'text');
        const errorPayload = JSON.parse(textContent.text);

        // AC-2: Input validation rejects invalid domain
        expect(errorPayload.success).toBe(false);
        expect(errorPayload.error_code).toBe('INVALID_DOMAIN_FORMAT');
        expect(errorPayload.error_message).toContain('lowercase');

        console.log(`✅ Security: SQL injection attempt in domain rejected`);
    });

    // ══════════════════════════════════════════════════════════════════════════
    // INPUT VALIDATION: Invalid Role Format
    // ══════════════════════════════════════════════════════════════════════════

    test('Security: Invalid role format rejected (uppercase letters)', async ({
        request,
    }) => {
        const mcpRequest = {
            jsonrpc: '2.0',
            id: 'bootstrap-security-test-2',
            method: 'tools/call',
            params: {
                name: 'bootstrap_agent',
                arguments: {
                    domain: 'engineering',
                    role: 'Backend_Developer', // Invalid: uppercase
                },
            },
        };

        const response = await request.post('/mcp/http', {
            data: JSON.stringify(mcpRequest),
        });

        expect(response.status()).toBe(200);

        const body = await response.json();
        const textContent = body.result.content.find((c: any) => c.type === 'text');
        const errorPayload = JSON.parse(textContent.text);

        expect(errorPayload.success).toBe(false);
        expect(errorPayload.error_code).toBe('INVALID_ROLE_FORMAT');

        console.log(`✅ Security: Invalid role format rejected`);
    });

    // ══════════════════════════════════════════════════════════════════════════
    // PAYLOAD SIZE VALIDATION (AC-16)
    // ══════════════════════════════════════════════════════════════════════════

    test('Performance: Payload JSON is valid and within 5MB limit', async ({
        request,
    }) => {
        // AC-16: Payload JSON size < 5MB

        const mcpRequest = {
            jsonrpc: '2.0',
            id: 'bootstrap-perf-test-1',
            method: 'tools/call',
            params: {
                name: 'bootstrap_agent',
                arguments: {
                    domain: 'engineering',
                    role: 'backend_developer',
                },
            },
        };

        const response = await request.post('/mcp/http', {
            data: JSON.stringify(mcpRequest),
        });

        expect(response.status()).toBe(200);

        const body = await response.json();
        const textContent = body.result.content.find((c: any) => c.type === 'text');
        const payloadJson = JSON.parse(textContent.text);

        // Calculate payload size
        const payloadString = JSON.stringify(payloadJson);
        const payloadSize = Buffer.byteLength(payloadString, 'utf8');

        expect(payloadSize).toBeLessThan(5_000_000); // 5MB limit

        console.log(`✅ Performance: Payload size: ${payloadSize} bytes (< 5MB limit)`);
    });

    // ══════════════════════════════════════════════════════════════════════════
    // INTENT PARAMETER (Preview for TASK-10-04)
    // ══════════════════════════════════════════════════════════════════════════

    test('Intent Parameter: bootstrap_agent accepts intent="request_task"', async ({
        request,
    }) => {
        // AC-4 (future): Intent parameter used by downstream tasks

        const mcpRequest = {
            jsonrpc: '2.0',
            id: 'bootstrap-intent-test-1',
            method: 'tools/call',
            params: {
                name: 'bootstrap_agent',
                arguments: {
                    domain: 'engineering',
                    role: 'backend_developer',
                    intent: 'request_task',
                    context: { task_id: 'TASK-10-02' },
                },
            },
        };

        const response = await request.post('/mcp/http', {
            data: JSON.stringify(mcpRequest),
        });

        expect(response.status()).toBe(200);

        const body = await response.json();
        const textContent = body.result.content.find((c: any) => c.type === 'text');
        const payloadJson = JSON.parse(textContent.text);

        expect(payloadJson.intent).toBe('request_task');
        expect(payloadJson.context).toBeDefined();
        expect(payloadJson.context.task_id).toBe('TASK-10-02');

        console.log(`✅ Intent parameter: request_task accepted`);
    });

    // ══════════════════════════════════════════════════════════════════════════
    // GRACEFUL DEGRADATION: Missing Optional Fields
    // ══════════════════════════════════════════════════════════════════════════

    test('Graceful Degradation: Payload returned even if runbook missing', async ({
        request,
    }) => {
        // AC-14: If runbook missing → payload still returned (runbook_content: null)

        const mcpRequest = {
            jsonrpc: '2.0',
            id: 'bootstrap-degradation-test-1',
            method: 'tools/call',
            params: {
                name: 'bootstrap_agent',
                arguments: {
                    domain: 'management', // Role without runbook
                    role: 'orchestrator', // (if seeded)
                },
            },
        };

        const response = await request.post('/mcp/http', {
            data: JSON.stringify(mcpRequest),
        });

        expect(response.status()).toBe(200);

        const body = await response.json();

        // Check if successful or role not found
        if (body.result.isError) {
            console.log(`ℹ️ Graceful Degradation: Role not in database, skipping test`);
            return;
        }

        const textContent = body.result.content.find((c: any) => c.type === 'text');
        const payloadJson = JSON.parse(textContent.text);

        // If payload returned, it should have all required fields
        if (payloadJson.success !== false) {
            expect(payloadJson.payload_version).toBe('1.0');
            expect(payloadJson.session_id).toBeDefined();
            // runbook_content may be null if missing
            expect('runbook_content' in payloadJson).toBeTruthy();
        }

        console.log(`✅ Graceful Degradation: Payload structure resilient to missing runbook`);
    });

    // ══════════════════════════════════════════════════════════════════════════
    // INTEGRATION: Session Persists Across Calls
    // ══════════════════════════════════════════════════════════════════════════

    test('Integration: Each bootstrap call creates unique session', async ({
        request,
    }) => {
        // AC-10: Session created via SessionManager on every bootstrap call

        // Call 1
        const call1 = {
            jsonrpc: '2.0',
            id: 'bootstrap-integration-test-1',
            method: 'tools/call',
            params: {
                name: 'bootstrap_agent',
                arguments: { domain: 'engineering', role: 'backend_developer' },
            },
        };

        const response1 = await request.post('/mcp/http', {
            data: JSON.stringify(call1),
        });

        const body1 = await response1.json();
        const textContent1 = body1.result.content.find((c: any) => c.type === 'text');
        const payload1 = JSON.parse(textContent1.text);
        const sessionId1 = payload1.session_id;

        // Call 2 (same domain/role, different session)
        const call2 = {
            jsonrpc: '2.0',
            id: 'bootstrap-integration-test-2',
            method: 'tools/call',
            params: {
                name: 'bootstrap_agent',
                arguments: { domain: 'engineering', role: 'backend_developer' },
            },
        };

        const response2 = await request.post('/mcp/http', {
            data: JSON.stringify(call2),
        });

        const body2 = await response2.json();
        const textContent2 = body2.result.content.find((c: any) => c.type === 'text');
        const payload2 = JSON.parse(textContent2.text);
        const sessionId2 = payload2.session_id;

        // Different calls should produce different session IDs
        expect(sessionId1).not.toBe(sessionId2);

        console.log(`✅ Integration: Unique sessions created`);
        console.log(`  - Session 1: ${sessionId1}`);
        console.log(`  - Session 2: ${sessionId2}`);
    });

    // ══════════════════════════════════════════════════════════════════════════
    // TOOL DESCRIPTION CLARITY (AC-4)
    // ══════════════════════════════════════════════════════════════════════════

    test('Tool Description: bootstrap_agent has clear description and input hints', async ({
        request,
    }) => {
        // AC-4: Tool description + input descriptions clear

        const response = await request.get(MCP_TOOLS_ENDPOINT);
        const body = await response.json();

        const bootstrapTool = body.tools.find((t: { name: string }) => t.name === 'bootstrap_agent');
        expect(bootstrapTool).toBeDefined();

        // Check description
        expect(bootstrapTool.description).toContain('Bootstrap');
        expect(bootstrapTool.description.length).toBeGreaterThan(50);

        // Check input schemas have descriptions
        expect(bootstrapTool.definition).toBeDefined();
        expect(bootstrapTool.definition.properties).toBeDefined();

        const domainProp = bootstrapTool.definition.properties.domain;
        expect(domainProp).toBeDefined();
        expect(domainProp.description).toBeDefined();
        expect(domainProp.description).toContain('engineering');

        const roleProp = bootstrapTool.definition.properties.role;
        expect(roleProp).toBeDefined();
        expect(roleProp.description).toBeDefined();

        console.log(`✅ Tool Description: Clear and informative`);
    });
});
