// src/middleware/admin.middleware.ts
// Requires the authenticated user to have the 'Admin' role.
// Must be used after requireAuth (which populates req.jwtPayload).

import type { Request, Response, NextFunction } from 'express';
import type { AuthenticatedRequest } from './auth.middleware';

export function requireAdmin(req: Request, res: Response, next: NextFunction): void {
  const payload = (req as AuthenticatedRequest).jwtPayload;
  const roles: unknown = payload?.['roles'];
  if (!Array.isArray(roles) || !roles.includes('Admin')) {
    res.status(403).json({ error: 'Admin role required.' });
    return;
  }
  next();
}
