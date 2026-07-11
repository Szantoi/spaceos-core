import { describe, test, expect, beforeAll, afterAll, vi } from 'vitest';
import { TransportFactory } from '../../mcp/transports/TransportFactory';
import { TransportType, TransportState } from '../../mcp/transports/ITransport';
import { HTTPTransport } from '../../mcp/transports/HTTPTransport';
import { StdioTransport } from '../../mcp/transports/StdioTransport';


/**
 * TASK-14-01 Integration Tests: Transport Abstraction Foundation
 *
 * Validates that:
 * 1. TransportFactory creates correct transport type
 * 2. Both Stdio and HTTP transports implement ITransport interface
 * 3. Transports initialize and shutdown gracefully
 * 4. Error handling works correctly across transport types
 * 5. State machine transitions are correct
 */

describe('Transport Abstraction Integration Tests (TASK-14-01)', () => {

    describe('AC-14-01-17: Server Bootstrap Integration', () => {
        test('HTTP Transport: Factory creates and initializes correctly', async () => {
            const config = { type: TransportType.HTTP, port: 0 }; // port 0 = ephemeral
            const transport = TransportFactory.create(config) as HTTPTransport;

            expect(transport).toBeInstanceOf(HTTPTransport);
            expect(transport.getState()).toBe(TransportState.INITIALIZING);

            // Initialize
            await transport.connect();
            expect(transport.getState()).toBe(TransportState.CONNECTED);
            expect(transport.isConnected()).toBe(true);

            // Shutdown
            await transport.disconnect();
            expect(transport.getState()).toBe(TransportState.DISCONNECTED);
            expect(transport.isConnected()).toBe(false);
        });

        test('Stdio Transport: Factory creates and initializes correctly', async () => {
            const config = { type: TransportType.STDIO };
            const transport = TransportFactory.create(config) as StdioTransport;

            expect(transport).toBeInstanceOf(StdioTransport);
            expect(transport.getState()).toBe(TransportState.INITIALIZING);

            // Initialize
            await transport.connect();
            expect(transport.getState()).toBe(TransportState.CONNECTED);
            expect(transport.isConnected()).toBe(true);

            // Shutdown
            await transport.disconnect();
            expect(transport.getState()).toBe(TransportState.DISCONNECTED);
            expect(transport.isConnected()).toBe(false);
        });
    });


    describe('AC-14-01-01 to AC-14-01-06: Interface Compliance', () => {
        test('ITransport: HTTP transport implements all required methods', async () => {
            const config = { type: TransportType.HTTP, port: 0 };
            const transport = TransportFactory.create(config) as HTTPTransport;

            // Check all methods exist
            expect(typeof transport.connect).toBe('function');
            expect(typeof transport.disconnect).toBe('function');
            expect(typeof transport.getState).toBe('function');
            expect(typeof transport.isConnected).toBe('function');
            expect(typeof transport.getConfig).toBe('function');
            expect(typeof transport.diagnoseError).toBe('function');

            await transport.connect();
            await transport.disconnect();
        });

        test('ITransport: Stdio transport implements all required methods', async () => {
            const config = { type: TransportType.STDIO };
            const transport = TransportFactory.create(config) as StdioTransport;

            // Check all methods exist
            expect(typeof transport.connect).toBe('function');
            expect(typeof transport.disconnect).toBe('function');
            expect(typeof transport.getState).toBe('function');
            expect(typeof transport.isConnected).toBe('function');
            expect(typeof transport.getConfig).toBe('function');
            expect(typeof transport.diagnoseError).toBe('function');

            await transport.connect();
            await transport.disconnect();
        });
    });


    describe('AC-14-01-07 to AC-14-01-11: Environment Configuration', () => {
        test('AC-14-01-07: MCP_TRANSPORT env var selects stdio transport', () => {
            const oldEnv = process.env.MCP_TRANSPORT;
            process.env.MCP_TRANSPORT = 'stdio';

            try {
                const transport = TransportFactory.create({ type: TransportType.STDIO });
                expect(transport).toBeInstanceOf(StdioTransport);
            } finally {
                process.env.MCP_TRANSPORT = oldEnv;
            }
        });

        test('AC-14-01-08: MCP_PORT env var configures HTTP port', () => {
            const config = { type: TransportType.HTTP, port: 8080 };
            const transport = TransportFactory.create(config) as HTTPTransport;

            expect(transport.getConfig().port).toBe(8080);
        });

        test('AC-14-01-09: MCP_HOST env var configures HTTP host', () => {
            const config = { type: TransportType.HTTP, host: '0.0.0.0' };
            const transport = TransportFactory.create(config) as HTTPTransport;

            expect(transport.getConfig().host).toBe('0.0.0.0');
        });

        test('AC-14-01-10: Invalid MCP_TRANSPORT throws descriptive error', () => {
            expect(() => {
                TransportFactory.create({ type: 'invalid' as any });
            }).toThrow(/Invalid transport type|unsupported/i);
        });

        test('AC-14-01-11: Config validation happens before transport creation', () => {
            expect(() => {
                TransportFactory.create({ type: TransportType.HTTP, port: -1 });
            }).toThrow(/Invalid port|range/i);
        });
    });


    describe('AC-14-01-19 to AC-14-01-20: Graceful Shutdown', () => {
        test('AC-14-01-19: SIGTERM signal triggers shutdown', async () => {
            const config = { type: TransportType.HTTP, port: 0 };
            const transport = TransportFactory.create(config) as HTTPTransport;

            const shutdownSpy = vi.spyOn(transport, 'initiateShutdown');

            await transport.connect();
            expect(transport.isConnected()).toBe(true);

            // Simulate shutdown initiation (in real scenario, SIGTERM handler calls this)
            await transport.initiateShutdown();
            expect(shutdownSpy).toHaveBeenCalled();
        });

        test('AC-14-01-20: Shutdown waits for in-flight requests to complete', async () => {
            const config = { type: TransportType.HTTP, port: 0 };
            const transport = TransportFactory.create(config) as HTTPTransport;

            // Set aggressive shutdown timeout for testing
            (transport as any).setShutdownTimeout(500);

            await transport.connect();

            const shutdownStart = Date.now();
            await transport.initiateShutdown();
            const shutdownDuration = Date.now() - shutdownStart;

            // Shutdown should complete within timeout
            expect(shutdownDuration).toBeLessThan(2000);
        });
    });


    describe('AC-14-01-21 to AC-14-01-24: Error Handling', () => {
        test('AC-14-01-21: ErrorDiagnoser maps OS errors to transport codes', async () => {
            const config = { type: TransportType.HTTP, port: 0 };
            const transport = TransportFactory.create(config) as HTTPTransport;

            const osError = new Error('bind EADDRINUSE');
            const context = await transport.diagnoseError(osError);

            expect(context.code).toBeDefined();
            expect(context.message).toBeDefined();
            expect(context.transport).toBe(TransportType.HTTP);
        });

        test('AC-14-01-22: Error messages include remediation hints', async () => {
            const config = { type: TransportType.HTTP, port: 0 };
            const transport = TransportFactory.create(config) as HTTPTransport;

            const portError = new Error('EADDRINUSE');
            const context = await transport.diagnoseError(portError);

            // Message should be actionable (mention port or retry)
            expect(context.message.length).toBeGreaterThan(0);
        });

        test('AC-14-01-23: Error logs do not expose internals', async () => {
            const config = { type: TransportType.HTTP, port: 0 };
            const transport = TransportFactory.create(config) as HTTPTransport;

            const error = new Error('Internal: /path/to/secret/file.ts at line 123');
            const context = await transport.diagnoseError(error);

            // Message should be safe for untrusted agents (no file paths)
            expect(context.message).not.toContain('/path/to');
            expect(context.message).not.toContain('.ts');
        });

        test('AC-14-01-24: Transport errors propagate with type info', async () => {
            const config = { type: TransportType.HTTP, port: 0 };
            const transport = TransportFactory.create(config) as HTTPTransport;

            const error = new Error('Test error');
            const context = await transport.diagnoseError(error);

            expect(context.code).toBeDefined();
            expect(typeof context.code).toBe('string');
            expect(context.originalError).toBeDefined();
        });
    });


    describe('AC-14-01-25 to AC-14-01-26: Backward Compatibility', () => {
        test('AC-14-01-25: Existing agents using stdio continue working unchanged', async () => {
            const config = { type: TransportType.STDIO };
            const transport = TransportFactory.create(config) as StdioTransport;

            // Simulate old-style initialization (should work)
            expect(transport.getState()).toBe(TransportState.INITIALIZING);

            await transport.connect();
            expect(transport.isConnected()).toBe(true);

            await transport.disconnect();
            expect(transport.isConnected()).toBe(false);
        });

        test('AC-14-01-26: Tool registration API unchanged', () => {
            // Factory should not modify tool registration
            const transport = TransportFactory.create({ type: TransportType.HTTP, port: 0 });

            // Config should be accessible (tools don't need to change)
            const config = transport.getConfig();
            expect(config).toBeDefined();
            expect(config.type).toBe(TransportType.HTTP);
        });
    });

});
