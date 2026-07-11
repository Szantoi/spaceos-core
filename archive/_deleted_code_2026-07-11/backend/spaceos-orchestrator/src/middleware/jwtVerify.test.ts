// src/middleware/jwtVerify.test.ts
// T4 — JWKS token verification tests (BE-03: key rotation retry).
// jwks-rsa is mocked; tokens are real RS256 JWTs signed with in-memory key pairs.

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { generateKeyPairSync } from 'node:crypto';
import jwt from 'jsonwebtoken';

// ─── Generate RSA key pair for tests ─────────────────────────────────────────

const { privateKey, publicKey } = generateKeyPairSync('rsa', {
  modulusLength: 2048,
  publicKeyEncoding:  { type: 'spki',  format: 'pem' },
  privateKeyEncoding: { type: 'pkcs8', format: 'pem' },
});

// ─── Mock jwks-rsa ────────────────────────────────────────────────────────────

const { mockGetSigningKey, mockGetSigningKeys } = vi.hoisted(() => ({
  mockGetSigningKey:  vi.fn(),
  mockGetSigningKeys: vi.fn(),
}));

vi.mock('jwks-rsa', () => ({
  default: vi.fn(() => ({
    getSigningKey:  mockGetSigningKey,
    getSigningKeys: mockGetSigningKeys,
  })),
}));

import { verifyToken } from './jwtVerify';

// ─── Helpers ─────────────────────────────────────────────────────────────────

function makeValidToken(claims: Record<string, unknown> = {}, options: jwt.SignOptions = {}): string {
  return jwt.sign({ sub: 'test-user', ...claims }, privateKey, {
    algorithm: 'RS256',
    audience:  'kernel-api',   // must match env.JWT_AUDIENCE default
    expiresIn: '1h',
    ...options,
  });
}

/** Simulates JWKS returning the correct public key. */
function mockValidKey(): void {
  mockGetSigningKey.mockImplementation((_kid: string, cb: (err: Error | null, key?: { getPublicKey(): string }) => void) => {
    cb(null, { getPublicKey: () => publicKey });
  });
}

beforeEach(() => {
  vi.clearAllMocks();
  mockGetSigningKeys.mockResolvedValue([]);
});

// ─── JwksVerify_ValidToken ────────────────────────────────────────────────────

describe('JwksVerify_ValidToken', () => {
  it('valid RS256 token → resolves with decoded payload', async () => {
    mockValidKey();
    const token = makeValidToken({ email: 'alice@example.com' });

    const payload = await verifyToken(token);

    expect(payload.sub).toBe('test-user');
    expect(payload.email).toBe('alice@example.com');
  });

  it('valid RS256 token → resolved payload contains standard JWT claims', async () => {
    mockValidKey();
    const token = makeValidToken({ given_name: 'Alice', family_name: 'Smith' });

    const payload = await verifyToken(token);

    expect(payload).toMatchObject({
      sub:         'test-user',
      given_name:  'Alice',
      family_name: 'Smith',
    });
    expect(typeof payload.iat).toBe('number');
    expect(typeof payload.exp).toBe('number');
  });
});

// ─── JwksVerify_ExpiredToken ──────────────────────────────────────────────────

describe('JwksVerify_ExpiredToken', () => {
  it('expired RS256 token → rejects with TokenExpiredError', async () => {
    mockValidKey();
    const token = makeValidToken({}, { expiresIn: -1 });

    await expect(verifyToken(token)).rejects.toThrow();
  });
});

// ─── JwksVerify_KeyRotation_Retry (BE-03) ────────────────────────────────────

describe('JwksVerify_KeyRotation_Retry', () => {
  it('first verify fails with signature error → cache flushed → retry succeeds', async () => {
    // First call: JWKS returns a wrong key → jwt.verify gets wrong public key → signature error
    const wrongKey = generateKeyPairSync('rsa', {
      modulusLength: 2048,
      publicKeyEncoding:  { type: 'spki',  format: 'pem' },
      privateKeyEncoding: { type: 'pkcs8', format: 'pem' },
    });

    let callCount = 0;
    mockGetSigningKey.mockImplementation((_kid: string, cb: (err: Error | null, key?: { getPublicKey(): string }) => void) => {
      callCount++;
      if (callCount === 1) {
        // Return wrong key on first attempt → signature mismatch
        cb(null, { getPublicKey: () => wrongKey.publicKey });
      } else {
        // Return correct key on retry
        cb(null, { getPublicKey: () => publicKey });
      }
    });

    const token = makeValidToken();

    const payload = await verifyToken(token);

    expect(payload.sub).toBe('test-user');
    expect(mockGetSigningKeys).toHaveBeenCalledOnce(); // cache flush triggered
    expect(callCount).toBe(2);                         // getSigningKey called twice
  });
});
