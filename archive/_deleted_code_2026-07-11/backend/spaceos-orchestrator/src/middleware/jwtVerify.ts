// src/middleware/jwtVerify.ts
// JWKS-based RS256 token verification — all environments (D-04: Keycloak-only).
// BE-03: key rotation retry — on signature failure, flush JWKS cache and retry once.

import jwksRsa from 'jwks-rsa';
import jwt, { type JwtHeader, type JwtPayload, type SigningKeyCallback } from 'jsonwebtoken';
import { env } from '../config/env';

const jwksClient = jwksRsa({
  jwksUri: env.JWKS_URI || 'http://localhost/jwks', // placeholder for dev startup without Keycloak
  cache: true,
  cacheMaxEntries: 5,
  cacheMaxAge: 600_000,          // 10 minutes
  jwksRequestsPerMinute: 10,     // BE-03: burst tolerance on key rotation
  rateLimit: true,
});

function getKey(header: JwtHeader, callback: SigningKeyCallback): void {
  jwksClient.getSigningKey(header.kid, (err, key) => {
    if (err) return callback(err);
    callback(null, key?.getPublicKey());
  });
}

export async function verifyToken(token: string): Promise<JwtPayload> {
  try {
    return await verifyOnce(token);
  } catch (err: unknown) {
    // BE-03: key rotation — flush cache and retry once
    if (
      err instanceof Error &&
      err.name === 'JsonWebTokenError' &&
      err.message.includes('signature')
    ) {
      await jwksClient.getSigningKeys(); // force fresh JWKS fetch
      return verifyOnce(token);
    }
    throw err;
  }
}

function verifyOnce(token: string): Promise<JwtPayload> {
  return new Promise((resolve, reject) => {
    jwt.verify(
      token,
      getKey,
      {
        issuer:     env.JWT_ISSUER   || undefined,
        audience:   env.JWT_AUDIENCE || undefined,
        algorithms: ['RS256'],
      },
      (err, decoded) => {
        if (err) return reject(err);
        resolve(decoded as JwtPayload);
      },
    );
  });
}
