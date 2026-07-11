import { describe, test, expect } from 'vitest';
import { TransportFactory, ConfigurationError } from '../../mcp/transports/TransportFactory';
import { TransportType, TransportError, TransportState } from '../../mcp/transports/ITransport';
import { ErrorDiagnoser } from '../../mcp/transports/ErrorDiagnoser';
import { StdioTransport } from '../../mcp/transports/StdioTransport';
import { HTTPTransport } from '../../mcp/transports/HTTPTransport';

describe("TransportFactory", () => {
    test("UT-01: Valid stdio config", () => {
        const transport = TransportFactory.create({ type: TransportType.STDIO });
        expect(transport).toBeInstanceOf(StdioTransport);
    });

    test("UT-02: Valid HTTP config", () => {
        const transport = TransportFactory.create({ type: TransportType.HTTP, port: 8080 });
        expect(transport).toBeInstanceOf(HTTPTransport);
    });

    test("UT-03: Invalid transport type (BLOCKED BY TYPESCRIPT)", () => {
        // This is caught at compile-time, not runtime, but we fake it dynamically
        // to ensure runtime validation works for untyped inputs
        expect(() => {
            TransportFactory.create({ type: "htp" as any });
        }).toThrow(ConfigurationError);
    });

    test("UT-04: HTTP port < 1 (negative) rejected", () => {
        expect(() => {
            TransportFactory.create({ type: TransportType.HTTP, port: -1 });
        }).toThrow(ConfigurationError);
    });

    test("UT-05: HTTP port > 65535 rejected", () => {
        expect(() => {
            TransportFactory.create({ type: TransportType.HTTP, port: 99999 });
        }).toThrow(ConfigurationError);
    });

    test("UT-05a: HTTP port 0 (ephemeral) is allowed", () => {
        const transport = TransportFactory.create({ type: TransportType.HTTP, port: 0 }) as HTTPTransport;
        expect(transport.getConfig().port).toBe(0);
    });

    test("UT-06: Default port applied", () => {
        const transport = TransportFactory.create({ type: TransportType.HTTP }) as HTTPTransport;
        expect(transport.getConfig().port).toBe(3000);
    });

    test("UT-07: Initial state is INITIALIZING", () => {
        const transport = TransportFactory.create({ type: TransportType.STDIO });
        expect(transport.getState()).toBe(TransportState.INITIALIZING);
    });

    test("UT-08: Enum prevents typos at compile-time", () => {
        const validTypes = Object.values(TransportType);
        expect(validTypes).toContain("stdio");
        expect(validTypes).toContain("http");
    });
});

describe("ErrorDiagnoser", () => {
    test("Diagnose EPIPE error (stdio-specific)", () => {
        const error = new Error("EPIPE: write EPIPE");
        const context = ErrorDiagnoser.diagnoseStdioError(error);
        expect(context.code).toBe(TransportError.EPIPE);
        expect(context.transport).toBe(TransportType.STDIO);
        expect(context.retryable).toBe(true);
    });

    test("Diagnose PORT_IN_USE error (HTTP-specific)", () => {
        const error = new Error("bind EADDRINUSE");
        const context = ErrorDiagnoser.diagnoseHTTPError(error);
        expect(context.code).toBe(TransportError.PORT_IN_USE);
        expect(context.transport).toBe(TransportType.HTTP);
        expect(context.retryable).toBe(false);
    });
});
