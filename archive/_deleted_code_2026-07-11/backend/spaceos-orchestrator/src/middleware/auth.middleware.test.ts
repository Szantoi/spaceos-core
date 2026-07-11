// src/middleware/auth.middleware.test.ts
// Tests the requireAuth middleware — header validation + verifyToken integration.
// verifyToken is mocked; JWKS internals are tested in jwtVerify.test.ts.

import { describe, it, expect, vi, beforeEach } from 'vitest';
import request from 'supertest';
import express from 'express';
import type { Request, Response } from 'express';

// ─── Mock jwtVerify so requireAuth behaviour can be controlled ────────────────

const { mockVerifyToken } = vi.hoisted(() => ({
  mockVerifyToken: vi.fn(),
}));

vi.mock('./jwtVerify', () => ({
  verifyToken: mockVerifyToken,
}));

import { requireAuth } from './auth.middleware';
import type { AuthenticatedRequest } from './auth.middleware';

// ─── Test app ────────────────────────────────────────────────────────────────

const app = express();
app.use(express.json());
app.get('/protected', requireAuth, (req: Request, res: Response) => {
  const authReq = req as AuthenticatedRequest;
  res.json({ token: authReq.jwtToken, payload: authReq.jwtPayload });
});

beforeEach(() => {
  vi.clearAllMocks();
});

// ─── Header validation (no JWKS involved) ────────────────────────────────────

describe('requireAuth — header validation', () => {
  it('no Authorization header → 401 with correct error message', async () => {
    const res = await request(app).get('/protected');

    expect(res.status).toBe(401);
    expect(res.body).toEqual({ error: 'Missing or malformed Authorization header.' });
    expect(mockVerifyToken).not.toHaveBeenCalled();
  });

  it('Authorization header without "Bearer " prefix → 401', async () => {
    const res = await request(app)
      .get('/protected')
      .set('Authorization', 'token some-value');

    expect(res.status).toBe(401);
    expect(res.body).toEqual({ error: 'Missing or malformed Authorization header.' });
    expect(mockVerifyToken).not.toHaveBeenCalled();
  });

  it('Authorization header with only "Bearer" (no space/token) → 401', async () => {
    const res = await request(app)
      .get('/protected')
      .set('Authorization', 'Bearer');

    expect(res.status).toBe(401);
    expect(res.body).toEqual({ error: 'Missing or malformed Authorization header.' });
    expect(mockVerifyToken).not.toHaveBeenCalled();
  });
});

// ─── Token verification ───────────────────────────────────────────────────────

describe('requireAuth — token verification', () => {
  it('verifyToken rejects → 401 with correct error message', async () => {
    mockVerifyToken.mockRejectedValueOnce(new Error('invalid signature'));

    const res = await request(app)
      .get('/protected')
      .set('Authorization', 'Bearer some.jwt.token');

    expect(res.status).toBe(401);
    expect(res.body).toEqual({ error: 'Invalid or expired JWT token.' });
  });

  it('verifyToken resolves → 200, calls next()', async () => {
    const payload = { sub: 'test-user', role: 'admin', tid: 'tenant-abc' };
    mockVerifyToken.mockResolvedValueOnce(payload);

    const res = await request(app)
      .get('/protected')
      .set('Authorization', 'Bearer valid.jwt.token');

    expect(res.status).toBe(200);
  });

  it('verifyToken resolves → req.jwtToken set to raw token string', async () => {
    mockVerifyToken.mockResolvedValueOnce({ sub: 'test-user', tid: 'tenant-abc' });

    const res = await request(app)
      .get('/protected')
      .set('Authorization', 'Bearer my.test.token');

    expect(res.body.token).toBe('my.test.token');
  });

  it('verifyToken resolves → req.jwtPayload contains decoded claims', async () => {
    const payload = { sub: 'test-user', role: 'admin', email: 'user@example.com', tid: 'tenant-abc' };
    mockVerifyToken.mockResolvedValueOnce(payload);

    const res = await request(app)
      .get('/protected')
      .set('Authorization', 'Bearer any.valid.token');

    expect(res.body.payload).toMatchObject({ sub: 'test-user', role: 'admin' });
  });

  it('verifyToken called with the raw token (without "Bearer " prefix)', async () => {
    mockVerifyToken.mockResolvedValueOnce({ sub: 'test-user', tid: 'tenant-abc' });

    await request(app)
      .get('/protected')
      .set('Authorization', 'Bearer raw.token.value');

    expect(mockVerifyToken).toHaveBeenCalledWith('raw.token.value');
  });
});
