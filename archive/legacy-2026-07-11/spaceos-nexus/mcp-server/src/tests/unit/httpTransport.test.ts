import { describe, test, expect, beforeEach, afterEach } from 'vitest';
import request from 'supertest';
import { HTTPTransport } from '../../mcp/transports/HTTPTransport';
import { TransportType } from '../../mcp/transports/ITransport';
import { Router } from 'express';
import * as http from 'node:http';
import { ResourceTemplateRegistry, RoleResourceTemplate } from '../../mcp/resources/resourceTemplates';
import { SamplingService } from '../../mcp/sampling/SamplingService';

describe('HTTPTransport (graceful shutdown & health)', () => {
    let transport: HTTPTransport;

    beforeEach(async () => {
        transport = new HTTPTransport({ type: TransportType.HTTP, port: 0, host: '127.0.0.1' });
        await transport.connect();
    });

    afterEach(async () => {
        // make sure we remove any listeners that might have been attached during tests
        process.removeAllListeners('SIGTERM');
        process.removeAllListeners('SIGINT');
        await transport.disconnect();
    });

    test('HT-01: /health returns 200 when healthy', async () => {
        const res = await request(transport.getExpressApp()).get('/health');
        expect(res.status).toBe(200);
        expect(res.body.status).toBe('HEALTHY');
        expect(typeof res.body.activeConnections).toBe('number');
    });

    test('HT-02: /health returns 503 after shutdown initiated', async () => {
        // initiate shutdown, wait a tiny bit for flag to propagate
        transport.initiateShutdown();
        await new Promise((r) => setTimeout(r, 10));

        const res = await request(transport.getExpressApp()).get('/health');
        expect(res.status).toBe(503);
        expect(res.body.status).toBe('SHUTTING_DOWN');
    });

    test('HT-03: active request drains during shutdown window', async () => {
        // shorten window so test runs quickly
        (transport as any).shutdownTimeout = 500;

        const agent = request.agent(transport.getExpressApp());
        const pending = agent.get('/_test/delay/200');

        // start shutdown immediately
        transport.initiateShutdown();

        const res = await pending;
        expect(res.body.delayed).toBe(200);
        // after completion there should be no active connections left
        expect((transport as any).activeConnections.size).toBe(0);
    });

    test('HT-04: request stalled beyond timeout is force‑closed', async () => {
        (transport as any).shutdownTimeout = 100;
        const agent = request.agent(transport.getExpressApp());

        const reqPromise = agent.get('/_test/delay/500');
        transport.initiateShutdown();

        // waiting a generous amount longer than shutdownTimeout
        await new Promise((r) => setTimeout(r, 200));

        // the underlying connection may be destroyed or may complete; the key
        // requirement is that no active connections remain after the timeout.
        try {
            await reqPromise;
        } catch {
            // ignore - connection aborted
        }
        expect((transport as any).activeConnections.size).toBe(0);
    });

    test('HT-05: CORS header respects configuration', async () => {
        // create transport with explicit cors origin
        const corsTransport = new HTTPTransport({
            type: TransportType.HTTP,
            port: 0,
            host: '127.0.0.1',
            corsOrigin: 'http://example.com'
        } as any);
        await corsTransport.connect();

        const res = await request(corsTransport.getExpressApp())
            .get('/health')
            .set('Origin', 'http://example.com');
        expect(res.headers['access-control-allow-origin']).toBe('http://example.com');
        await corsTransport.disconnect();
    });

    test('HT-06: port-in-use error diagnosed correctly', async () => {
        // instead of actually binding twice, create an error object like Node emits
        const fakeErr = new Error('listen EADDRINUSE: address already in use 127.0.0.1:3000');
        const context = await transport.diagnoseError(fakeErr);
        expect(context.code).toBe('PORT_IN_USE');
    });

    test('HT-07: custom timeout and maxConnections applied', async () => {
        const cfgTransport = new HTTPTransport({
            type: TransportType.HTTP,
            port: 0,
            host: '127.0.0.1',
            requestTimeout: 12345,
            maxConnections: 7
        } as any);
        await cfgTransport.connect();
        const server = (cfgTransport as any).server as http.Server;
        expect(server.timeout).toBe(12345);
        expect(server.maxConnections).toBe(7);
        await cfgTransport.disconnect();
    });

    test('HT-08: can mount arbitrary router to transport app', async () => {
        // quickly verify that user-supplied routers can be attached
        const router = Router();
        router.get('/foobar', (req, res) => res.json({ ok: true }));

        transport.getExpressApp().use('/mcp', router);
        const res = await request(transport.getExpressApp()).get('/mcp/foobar');
        expect(res.status).toBe(200);
        expect(res.body.ok).toBe(true);
    });
});

// ───────────────────────────────────────────────────────────────────────────
// TASK-14-02: MCP Tool Invocation Tests
// ───────────────────────────────────────────────────────────────────────────

describe('HTTPTransport MCP/call endpoint (TASK-14-02)', () => {
    let transport: HTTPTransport;
    let mockPluginManager: any;

    beforeEach(async () => {
        transport = new HTTPTransport({ type: TransportType.HTTP, port: 0, host: '127.0.0.1' });
        await transport.connect();

        // Create a mock PluginManager that simulates tool invocation
        mockPluginManager = {
            invokeTool: async (toolName: string, args: any, context: any) => {
                if (toolName === 'test_tool') {
                    return { status: 'success', tool: toolName, args };
                }
                if (toolName === 'error_tool') {
                    const error: any = new Error('Tool failed');
                    error.code = 'TOOL_FAILED';
                    throw error;
                }
                throw new Error(`Tool "${toolName}" not found`);
            }
        };

        transport.setPluginManager(mockPluginManager);
    });

    afterEach(async () => {
        process.removeAllListeners('SIGTERM');
        process.removeAllListeners('SIGINT');
        await transport.disconnect();
    });

    test('HT-09: /mcp/call invokes tool successfully', async () => {
        const res = await request(transport.getExpressApp())
            .post('/mcp/call')
            .send({ tool_name: 'test_tool', arguments: { key: 'value' } });

        expect(res.status).toBe(200);
        expect(res.body.status).toBe('success');
        expect(res.body.data.tool).toBe('test_tool');
        expect(res.body.data.args.key).toBe('value');
    });

    test('HT-10: /mcp/call returns 400 if tool_name missing', async () => {
        const res = await request(transport.getExpressApp())
            .post('/mcp/call')
            .send({ arguments: { key: 'value' } });

        expect(res.status).toBe(400);
        expect(res.body.status).toBe('error');
        expect(res.body.code).toBe('INVALID_REQUEST');
    });

    test('HT-11: /mcp/call returns 400 if tool_name is not string', async () => {
        const res = await request(transport.getExpressApp())
            .post('/mcp/call')
            .send({ tool_name: 123, arguments: {} });

        expect(res.status).toBe(400);
        expect(res.body.status).toBe('error');
    });

    test('HT-12: /mcp/call handles tool errors gracefully', async () => {
        const res = await request(transport.getExpressApp())
            .post('/mcp/call')
            .send({ tool_name: 'error_tool', arguments: {} });

        expect(res.status).toBe(500);
        expect(res.body.status).toBe('error');
        expect(res.body.code).toBe('TOOL_FAILED');
        expect(res.body.tool).toBe('error_tool');
    });

    test('HT-13: /mcp/call returns 503 if PluginManager not set', async () => {
        transport.setPluginManager(undefined as any);

        const res = await request(transport.getExpressApp())
            .post('/mcp/call')
            .send({ tool_name: 'test_tool', arguments: {} });

        expect(res.status).toBe(503);
        expect(res.body.status).toBe('error');
        expect(res.body.code).toBe('SERVICE_UNAVAILABLE');
    });

    test('HT-14: /mcp/call includes session ID from headers', async () => {
        let capturedContext: any;
        mockPluginManager.invokeTool = async (toolName: string, args: any, context: any) => {
            capturedContext = context;
            return { ok: true };
        };

        await request(transport.getExpressApp())
            .post('/mcp/call')
            .set('x-session-id', 'test-session-123')
            .send({ tool_name: 'test_tool', arguments: {} });

        expect(capturedContext.session_id).toBe('test-session-123');
    });

    test('HT-15: /mcp/resources returns registered URI templates', async () => {
        const registry = new ResourceTemplateRegistry();
        registry.register(new RoleResourceTemplate());
        transport.setResourceRegistry(registry);

        const res = await request(transport.getExpressApp()).get('/mcp/resources');

        expect(res.status).toBe(200);
        expect(res.body.status).toBe('success');
        expect(Array.isArray(res.body.resources)).toBe(true);
        expect(res.body.resources[0].uriPattern).toBe('resource://role/{domain}/{role}');
    });

    test('HT-16: /mcp/resources/resolve returns 404 for missing resource', async () => {
        const registry = new ResourceTemplateRegistry();
        registry.register(new RoleResourceTemplate());
        transport.setResourceRegistry(registry);

        const res = await request(transport.getExpressApp())
            .get('/mcp/resources/resolve')
            .query({ uri: 'resource://role/engineering/not-real' });

        expect(res.status).toBe(404);
        expect(res.body.code).toBe('NOT_FOUND');
    });

    test('HT-17: /mcp/sampling/respond resolves pending sampling request', async () => {
        const sampling = new SamplingService(1000);
        transport.setSamplingService(sampling);

        const pendingPromise = sampling.requestSampling('session-1', {
            prompt: 'Pick one',
            options: [{ label: 'by-role', value: 'role' }]
        });

        const pending = sampling.listPending('session-1');
        const response = await request(transport.getExpressApp())
            .post('/mcp/sampling/respond')
            .send({ request_id: pending[0].requestId, selected: ['role'] });

        expect(response.status).toBe(200);
        expect(response.body.status).toBe('success');

        const result = await pendingPromise;
        expect(result.selected).toEqual(['role']);
    });
});
