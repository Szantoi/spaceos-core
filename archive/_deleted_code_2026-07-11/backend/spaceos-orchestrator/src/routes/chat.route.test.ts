// src/routes/chat.route.test.ts
import { vi, describe, it, expect, beforeEach } from 'vitest';
import http from 'node:http';

// Mock streamChat as an async generator that yields a text chunk then done
async function* mockStreamGenerator() {
  yield { type: 'text' as const, text: 'Hello stream' };
  yield { type: 'done' as const, toolsUsed: [], iterations: 1 };
}

vi.mock('../interpreter/interpreter.service', () => ({
  interpret: vi.fn().mockResolvedValue({ reply: 'Hello', toolsUsed: [], iterations: 1 }),
  streamChat: vi.fn().mockImplementation(() => mockStreamGenerator()),
}));

vi.mock('../middleware/auth.middleware', () => ({
  requireAuth: (req: any, res: any, next: any) => {
    if (!req.headers['authorization']?.startsWith('Bearer ')) {
      return res.status(401).json({ error: 'Missing or malformed Authorization header.' });
    }
    req.jwtToken = req.headers['authorization'].slice(7);
    req.jwtPayload = { sub: 'test-user' };
    next();
  },
}));

import request from 'supertest';
import express from 'express';
import rateLimit from 'express-rate-limit';
import { chatRouter } from './chat.route';
import { streamChat } from '../interpreter/interpreter.service';
import { requireAuth } from '../middleware/auth.middleware';

const validToken = 'test-jwt-token';

const app = express();
app.use(express.json());
app.use('/bff/chat', chatRouter);

describe('POST /bff/chat', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('no Authorization header → 401', async () => {
    const res = await request(app)
      .post('/bff/chat')
      .send({ messages: [{ role: 'user', content: 'Hello' }] });

    expect(res.status).toBe(401);
    expect(res.body).toMatchObject({ error: 'Missing or malformed Authorization header.' });
  });

  it('valid JWT + empty messages array → 422', async () => {
    const res = await request(app)
      .post('/bff/chat')
      .set('Authorization', `Bearer ${validToken}`)
      .send({ messages: [] });

    expect(res.status).toBe(422);
  });

  it('history with empty assistant turn → 200 (portal sends content:"" after JSON response read as SSE)', async () => {
    // Reproduces BUG-005: portal sends assistant messages with content:'' because
    // useStreamingChat reads /bff/chat JSON as SSE (no data: lines → content stays '').
    // Without the schema fix, z.string().min(1) rejected '' → 422 on 2nd message.
    const res = await request(app)
      .post('/bff/chat')
      .set('Authorization', `Bearer ${validToken}`)
      .send({
        messages: [
          { role: 'user',      content: 'First message' },
          { role: 'assistant', content: '' },          // empty — portal bug, must not 422
          { role: 'user',      content: 'Second message' },
        ],
      });

    expect(res.status).toBe(200);
    expect(res.body).toMatchObject({ reply: 'Hello', toolsUsed: [], iterations: 1 });
  });

  it('valid JWT + valid body → 200 with reply field', async () => {
    const res = await request(app)
      .post('/bff/chat')
      .set('Authorization', `Bearer ${validToken}`)
      .send({ messages: [{ role: 'user', content: 'Hello' }] });

    expect(res.status).toBe(200);
    expect(res.body).toMatchObject({ reply: 'Hello', toolsUsed: [], iterations: 1 });
  });

  it('POST /bff/chat → 429 when rate limit exceeded', async () => {
    const limiter = rateLimit({ windowMs: 60_000, max: 1, standardHeaders: true, legacyHeaders: false });
    const limitedApp = express();
    limitedApp.use(express.json());
    limitedApp.use('/bff/chat', limiter, chatRouter);

    const validBody = { messages: [{ role: 'user', content: 'Hello' }] };

    // First request exhausts the limit (max: 1)
    await request(limitedApp)
      .post('/bff/chat')
      .set('Authorization', `Bearer ${validToken}`)
      .send(validBody);

    // Second request must be rate-limited
    const res = await request(limitedApp)
      .post('/bff/chat')
      .set('Authorization', `Bearer ${validToken}`)
      .send(validBody);

    expect(res.status).toBe(429);
  });
});

// ─── POST /bff/chat/stream (SSE) ──────────────────────────────────────────────

describe('POST /bff/chat/stream', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('no Authorization header → 401', async () => {
    const res = await request(app)
      .post('/bff/chat/stream')
      .send({ messages: [{ role: 'user', content: 'Hello' }] });

    expect(res.status).toBe(401);
  });

  it('invalid body → 422', async () => {
    const res = await request(app)
      .post('/bff/chat/stream')
      .set('Authorization', `Bearer ${validToken}`)
      .send({ messages: [] });

    expect(res.status).toBe(422);
  });

  it('valid request → Content-Type: text/event-stream', async () => {
    const res = await request(app)
      .post('/bff/chat/stream')
      .set('Authorization', `Bearer ${validToken}`)
      .send({ messages: [{ role: 'user', content: 'Hello' }] });

    expect(res.status).toBe(200);
    expect(res.headers['content-type']).toContain('text/event-stream');
  });

  it('valid request → body contains SSE data chunks and [DONE] sentinel', async () => {
    const res = await request(app)
      .post('/bff/chat/stream')
      .set('Authorization', `Bearer ${validToken}`)
      .send({ messages: [{ role: 'user', content: 'Hello' }] });

    expect(res.text).toContain('data:');
    expect(res.text).toContain('[DONE]');
  });

  it('valid request → streamChat() is called with correct args', async () => {
    await request(app)
      .post('/bff/chat/stream')
      .set('Authorization', `Bearer ${validToken}`)
      .send({ messages: [{ role: 'user', content: 'Hello' }] });

    expect(vi.mocked(streamChat)).toHaveBeenCalledOnce();
    const [_request, _jwt, signal] = vi.mocked(streamChat).mock.calls[0]!;
    expect(signal).toBeInstanceOf(AbortSignal);
  });

  it('no-auth request → 401 even when rate limit is exhausted (auth fires before limiter)', async () => {
    // Reproduces the E2E failure from MSG-ORCH-058: chatLimiter was firing before requireAuth,
    // causing no-auth requests to get 429 instead of 401.
    const limiter = rateLimit({ windowMs: 60_000, max: 1, standardHeaders: true, legacyHeaders: false });
    const orderedApp = express();
    orderedApp.use(express.json());
    // Correct order: requireAuth first, then limiter
    orderedApp.use('/bff/chat', requireAuth, limiter, chatRouter);

    const validBody = { messages: [{ role: 'user', content: 'Hello' }] };

    // Exhaust the rate limit with an authenticated request
    await request(orderedApp)
      .post('/bff/chat/stream')
      .set('Authorization', `Bearer ${validToken}`)
      .send(validBody);

    // Unauthenticated request must still get 401, not 429
    const res = await request(orderedApp)
      .post('/bff/chat/stream')
      .send(validBody);

    expect(res.status).toBe(401);
  });

  it('SSE stream error → [DONE] sentinel is always written', async () => {
    vi.mocked(streamChat).mockImplementationOnce(async function* () {
      throw new Error('LLM exploded');
    });

    const res = await request(app)
      .post('/bff/chat/stream')
      .set('Authorization', `Bearer ${validToken}`)
      .send({ messages: [{ role: 'user', content: 'Hello' }] });

    // Even on error, [DONE] must be the last line
    expect(res.text).toContain('[DONE]');
  });

  it('client disconnect → AbortController.abort() fires (no socket leak)', (done) => {
    // Resolves when the abort signal fires inside the stream generator
    let resolveAbort!: () => void;
    const abortFired = new Promise<void>((r) => { resolveAbort = r; });

    vi.mocked(streamChat).mockImplementationOnce(async function* (_req, _jwt, signal) {
      // Register abort listener — this is what we assert
      signal.addEventListener('abort', () => resolveAbort());
      // Block until the client disconnects
      await abortFired;
      // Generator ends without yielding — stream closes cleanly
    });

    const server = app.listen(0, () => {
      const { port } = server.address() as { port: number };

      const clientReq = http.request({
        method:  'POST',
        host:    '127.0.0.1',
        port,
        path:    '/bff/chat/stream',
        headers: {
          'Content-Type':  'application/json',
          'Authorization': `Bearer ${validToken}`,
        },
      });

      clientReq.write(JSON.stringify({ messages: [{ role: 'user', content: 'abort-test' }] }));
      clientReq.end();

      // Give the server time to start the stream, then disconnect
      setTimeout(() => {
        clientReq.destroy(); // triggers req 'close' event on the server side

        // Wait for abort propagation then clean up
        abortFired.then(() => server.close(done));
      }, 80);
    });
  });
});
