// src/middleware/requireAuth.matrix.test.ts
// Security matrix — every negative auth case must return the correct HTTP status.
//
// Cases covered:
//  1. No Authorization header         → 401
//  2. Expired token                    → 401
//  3. Wrong issuer                     → 401
//  4. Wrong audience                   → 401
//  5. Missing tid claim                → 401
//  6. tid mismatch with route param    → 403  (requireTenantScope guard)

import { describe, it, expect, vi, beforeEach } from 'vitest';
import request from 'supertest';
import express from 'express';

// ─── Mock jwtVerify ───────────────────────────────────────────────────────────

const { mockVerifyToken } = vi.hoisted(() => ({
  mockVerifyToken: vi.fn(),
}));

vi.mock('./jwtVerify', () => ({
  verifyToken: mockVerifyToken,
}));

import { requireAuth, requireTenantScope } from './auth.middleware';

// ─── Test apps ────────────────────────────────────────────────────────────────

// Basic protected endpoint (cases 1-5)
const app = express();
app.use(express.json());
app.get('/protected', requireAuth, (_req, res) => res.json({ ok: true }));

// Tenant-scoped endpoint (case 6)
const scopedApp = express();
scopedApp.use(express.json());
scopedApp.get(
  '/tenants/:tenantId/data',
  requireAuth,
  requireTenantScope('tenantId'),
  (_req, res) => res.json({ ok: true }),
);

beforeEach(() => vi.clearAllMocks());

// ─── Case 1: No Authorization header ─────────────────────────────────────────

describe('Auth matrix — 1. no Authorization header', () => {
  it('returns 401 without calling verifyToken', async () => {
    const res = await request(app).get('/protected');

    expect(res.status).toBe(401);
    expect(res.body.error).toMatch(/Missing or malformed/i);
    expect(mockVerifyToken).not.toHaveBeenCalled();
  });
});

// ─── Case 2: Expired token ────────────────────────────────────────────────────

describe('Auth matrix — 2. expired token', () => {
  it('returns 401 when verifyToken throws TokenExpiredError', async () => {
    const err = Object.assign(new Error('jwt expired'), { name: 'TokenExpiredError' });
    mockVerifyToken.mockRejectedValueOnce(err);

    const res = await request(app)
      .get('/protected')
      .set('Authorization', 'Bearer expired.jwt.token');

    expect(res.status).toBe(401);
    expect(res.body.error).toMatch(/Invalid or expired/i);
  });
});

// ─── Case 3: Wrong issuer ─────────────────────────────────────────────────────

describe('Auth matrix — 3. wrong issuer', () => {
  it('returns 401 when verifyToken throws issuer mismatch error', async () => {
    const err = Object.assign(new Error('jwt issuer invalid. expected: https://spaceos.keycloak'), {
      name: 'JsonWebTokenError',
    });
    mockVerifyToken.mockRejectedValueOnce(err);

    const res = await request(app)
      .get('/protected')
      .set('Authorization', 'Bearer wrong-issuer.jwt.token');

    expect(res.status).toBe(401);
    expect(res.body.error).toMatch(/Invalid or expired/i);
  });
});

// ─── Case 4: Wrong audience ───────────────────────────────────────────────────

describe('Auth matrix — 4. wrong audience', () => {
  it('returns 401 when verifyToken throws audience mismatch error', async () => {
    const err = Object.assign(new Error('jwt audience invalid'), {
      name: 'JsonWebTokenError',
    });
    mockVerifyToken.mockRejectedValueOnce(err);

    const res = await request(app)
      .get('/protected')
      .set('Authorization', 'Bearer wrong-audience.jwt.token');

    expect(res.status).toBe(401);
    expect(res.body.error).toMatch(/Invalid or expired/i);
  });
});

// ─── Case 5: Missing tid claim ────────────────────────────────────────────────

describe('Auth matrix — 5. missing tid claim', () => {
  it('returns 401 when JWT payload has no tid field', async () => {
    // Token is cryptographically valid but missing the tenant claim
    mockVerifyToken.mockResolvedValueOnce({ sub: 'user-abc', role: 'admin' });

    const res = await request(app)
      .get('/protected')
      .set('Authorization', 'Bearer valid-but-no-tid.jwt');

    expect(res.status).toBe(401);
    expect(res.body.error).toMatch(/tid/i);
  });

  it('valid token WITH tid → 200 (tid alone is sufficient)', async () => {
    mockVerifyToken.mockResolvedValueOnce({ sub: 'user-abc', tid: 'tenant-xyz' });

    const res = await request(app)
      .get('/protected')
      .set('Authorization', 'Bearer valid.jwt.token');

    expect(res.status).toBe(200);
  });
});

// ─── Case 6: tid mismatch with route param ────────────────────────────────────

describe('Auth matrix — 6. tid mismatch → 403 (requireTenantScope)', () => {
  it('tid in JWT does not match :tenantId route param → 403', async () => {
    mockVerifyToken.mockResolvedValueOnce({ sub: 'user-abc', tid: 'tenant-A' });

    const res = await request(scopedApp)
      .get('/tenants/tenant-B/data')           // route param: tenant-B
      .set('Authorization', 'Bearer valid.jwt'); // JWT tid:     tenant-A

    expect(res.status).toBe(403);
    expect(res.body.error).toMatch(/tenant/i);
  });

  it('tid in JWT matches :tenantId route param → 200', async () => {
    mockVerifyToken.mockResolvedValueOnce({ sub: 'user-abc', tid: 'tenant-A' });

    const res = await request(scopedApp)
      .get('/tenants/tenant-A/data')
      .set('Authorization', 'Bearer valid.jwt');

    expect(res.status).toBe(200);
  });
});
