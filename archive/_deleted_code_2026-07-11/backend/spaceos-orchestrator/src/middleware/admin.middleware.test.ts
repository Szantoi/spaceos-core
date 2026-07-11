// src/middleware/admin.middleware.test.ts
import { describe, it, expect, vi } from 'vitest';
import type { Request, Response, NextFunction } from 'express';
import { requireAdmin } from './admin.middleware';

function makeReq(jwtPayload?: Record<string, unknown>): Request {
  return { jwtPayload } as unknown as Request;
}

function makeRes(): { status: ReturnType<typeof vi.fn>; json: ReturnType<typeof vi.fn> } {
  const json = vi.fn();
  const status = vi.fn().mockReturnValue({ json });
  return { status, json };
}

describe('requireAdmin', () => {
  it('no jwtPayload → 403', () => {
    const req = makeReq(undefined);
    const res = makeRes();
    const next = vi.fn() as unknown as NextFunction;

    requireAdmin(req, res as unknown as Response, next);

    expect(res.status).toHaveBeenCalledWith(403);
    expect(next).not.toHaveBeenCalled();
  });

  it('roles: ["Designer"] → 403', () => {
    const req = makeReq({ roles: ['Designer'] });
    const res = makeRes();
    const next = vi.fn() as unknown as NextFunction;

    requireAdmin(req, res as unknown as Response, next);

    expect(res.status).toHaveBeenCalledWith(403);
    expect(next).not.toHaveBeenCalled();
  });

  it('roles: [] → 403', () => {
    const req = makeReq({ roles: [] });
    const res = makeRes();
    const next = vi.fn() as unknown as NextFunction;

    requireAdmin(req, res as unknown as Response, next);

    expect(res.status).toHaveBeenCalledWith(403);
    expect(next).not.toHaveBeenCalled();
  });

  it('roles: ["Admin"] → calls next()', () => {
    const req = makeReq({ roles: ['Admin'] });
    const res = makeRes();
    const next = vi.fn() as unknown as NextFunction;

    requireAdmin(req, res as unknown as Response, next);

    expect(next).toHaveBeenCalled();
    expect(res.status).not.toHaveBeenCalled();
  });

  it('roles: ["Designer", "Admin"] → calls next()', () => {
    const req = makeReq({ roles: ['Designer', 'Admin'] });
    const res = makeRes();
    const next = vi.fn() as unknown as NextFunction;

    requireAdmin(req, res as unknown as Response, next);

    expect(next).toHaveBeenCalled();
  });

  it('roles not an array → 403', () => {
    const req = makeReq({ roles: 'Admin' });
    const res = makeRes();
    const next = vi.fn() as unknown as NextFunction;

    requireAdmin(req, res as unknown as Response, next);

    expect(res.status).toHaveBeenCalledWith(403);
    expect(next).not.toHaveBeenCalled();
  });
});
