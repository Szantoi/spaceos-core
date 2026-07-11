import { describe, test, expect } from 'vitest';
import fetch from 'node-fetch';
import { HTTPTransport } from '../../mcp/transports/HTTPTransport';
import { TransportType } from '../../mcp/transports/ITransport';

// integration tests hit a real port, so we use network calls instead of supertest

describe('HTTPTransport integration', () => {
    test('IT-01: listens on ephemeral port and responds to /health', async () => {
        const transport = new HTTPTransport({ type: TransportType.HTTP, port: 0, host: '127.0.0.1' });
        await transport.connect();

        const port = transport.getPort();
        expect(typeof port).toBe('number');
        expect(port).toBeGreaterThan(0);

        const res = await fetch(`http://127.0.0.1:${port}/health`);
        const body = await res.json();
        expect(res.status).toBe(200);
        expect(body.status).toBe('HEALTHY');

        await transport.disconnect();
    });

    test('IT-02: port released after disconnect', async () => {
        const transport = new HTTPTransport({ type: TransportType.HTTP, port: 0, host: '127.0.0.1' });
        await transport.connect();
        const port = transport.getPort()!;
        await transport.disconnect();

        // try to bind a new temporary server to the same port - should succeed if released
        const srv = require('http').createServer((req: any, res: any) => res.end('ok'));
        await new Promise<void>((resolve, reject) => {
            srv.listen(port, '127.0.0.1', () => resolve());
            srv.on('error', reject);
        });
        await new Promise<void>((r) => srv.close(() => r()));
    });
});
