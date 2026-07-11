// src/middleware/testGuard.ts
import { Request, Response, NextFunction } from 'express';

// SEC-TS-02: WARNING log startup-on ha enabled
if (process.env.SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER === 'true') {
  console.warn('⚠️  WARNING: Test endpoints are ENABLED. Do NOT use in production.');
}

export function testGuard(req: Request, res: Response, next: NextFunction): void {
  // Layer 1: feature flag
  if (process.env.SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER !== 'true') {
    res.status(404).json({ error: 'Not found' });
    return;
  }

  // Layer 2: seeder secret
  const secret = Array.isArray(req.headers['x-test-seeder-secret'])
    ? req.headers['x-test-seeder-secret'][0]
    : req.headers['x-test-seeder-secret'];
  if (!secret || secret !== process.env.TEST_SEEDER_SECRET) {
    res.status(403).json({ error: 'Forbidden', message: 'Invalid seeder secret' });
    return;
  }

  // Layer 3: tenant allowlist
  const allowlist = (process.env.TEST_TENANT_ALLOWLIST || '').split(',').map(s => s.trim());
  const tenantId = req.params['tenantId'] as string | undefined;
  if (tenantId && !allowlist.includes(tenantId)) {
    console.error(`SECURITY: Test reset attempted on non-test tenant ${tenantId}`);
    res.status(403).json({ error: 'Forbidden', message: 'Tenant not in test allowlist' });
    return;
  }

  next();
}
