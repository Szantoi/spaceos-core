// src/middleware/auth.middleware.ts
// Validates the JWT from the Authorization header using JWKS (RS256).
// All environments use Keycloak JWKS — D-04 (no dev ES256 fallback).

import type { Request, Response, NextFunction } from 'express';
import { verifyToken } from './jwtVerify';

export interface AuthenticatedRequest extends Request {
  jwtToken?: string;
  jwtPayload?: Record<string, unknown>;
}

export function requireAuth(
  req: AuthenticatedRequest,
  res: Response,
  next: NextFunction,
): void {
  const authHeader = req.headers['authorization'];

  if (!authHeader?.startsWith('Bearer ')) {
    res.status(401).json({ error: 'Missing or malformed Authorization header.' });
    return;
  }

  const token = authHeader.slice(7);

  verifyToken(token)
    .then((payload) => {
      // Every SpaceOS token must carry a tenant ID claim (tid).
      // A missing tid means the token was not issued for a tenant session
      // — silently defaulting would risk cross-tenant data leakage.
      if (!payload.tid) {
        res.status(401).json({ error: 'JWT missing required tid claim.' });
        return;
      }
      req.jwtToken  = token;
      req.jwtPayload = payload as Record<string, unknown>;
      next();
    })
    .catch(() => {
      res.status(401).json({ error: 'Invalid or expired JWT token.' });
    });
}

/**
 * requireTenantScope — route-level tenant isolation guard.
 *
 * Compares the JWT `tid` claim to the named route parameter.
 * Must be mounted AFTER requireAuth (jwtPayload must already be set).
 *
 * Usage: router.get('/tenants/:tenantId/...', requireAuth, requireTenantScope('tenantId'), handler)
 */
export function requireTenantScope(paramName: string) {
  return (req: AuthenticatedRequest, res: Response, next: NextFunction): void => {
    const routeTenantId = req.params[paramName];
    const jwtTenantId   = req.jwtPayload?.tid as string | undefined;

    if (routeTenantId && jwtTenantId !== routeTenantId) {
      res.status(403).json({ error: 'JWT tenant does not match requested resource.' });
      return;
    }
    next();
  };
}
