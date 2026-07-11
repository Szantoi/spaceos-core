import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { StdioTransport } from '../../mcp/transports/StdioTransport';
import { TransportType, TransportError } from '../../mcp/transports/ITransport';
import { PassThrough } from 'stream';

describe('StdioTransport — Line-based JSON protocol', () => {
    let transport: StdioTransport;
    let input: PassThrough;
    let output: PassThrough;

    beforeEach(async () => {
        input = new PassThrough();
        output = new PassThrough();
        transport = new StdioTransport({ type: TransportType.STDIO }, input, output);
        await transport.connect();
    });

    afterEach(async () => {
        await transport.disconnect();
    });

    it('starts in connected state and transitions to disconnected when disconnected', async () => {
        expect(transport.getState()).toBe('CONNECTED');
        await transport.disconnect();
        expect(transport.getState()).toBe('DISCONNECTED');
    });

    it('parses valid JSON lines and routes by id', async () => {
        const id = 'msg1';
        const payload = { id, foo: 'bar' };
        const promise = new Promise<any>((resolve) => {
            transport.receive(id, resolve);
        });

        // simulate incoming data by writing to input stream
        input.write(JSON.stringify(payload) + '\n');
        const received = await promise;
        expect(received).toEqual(payload);
    });

    it('recovers from malformed JSON by emitting error', async () => {
        let errorCaught: any = null;
        transport.onError((err) => { errorCaught = err; });
        // write malformed JSON to input stream
        input.write('not json\n');
        await new Promise(r => setTimeout(r, 10));
        expect(errorCaught).not.toBeNull();
        expect(errorCaught.code).toBe(TransportError.INVALID_JSON);
    });

    it('handles EOF by closing state (simulated)', async () => {
        // After closing input, state should change to DISCONNECTED
        input.end();
        // Wait for close event processing
        await new Promise((r) => setTimeout(r, 10));
        expect(transport.getState()).toBe('DISCONNECTED');
    });
});