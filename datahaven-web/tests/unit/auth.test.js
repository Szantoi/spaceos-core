/**
 * Auth Middleware Unit Tests
 */

import { describe, it, expect, vi } from 'vitest';
import { createAuthMiddleware, createRateLimiter } from '../../src/middleware/auth.js';

describe('auth middleware', () => {
  describe('createAuthMiddleware', () => {
    it('should skip auth when disabled', () => {
      const middleware = createAuthMiddleware({ enabled: false });
      const req = { headers: {} };
      const res = {};
      const next = vi.fn();

      middleware(req, res, next);

      expect(next).toHaveBeenCalled();
    });

    it('should return 401 when no token provided and auth enabled', () => {
      const middleware = createAuthMiddleware({ enabled: true, token: 'secret' });
      const req = { headers: {}, query: {} };
      const res = {
        status: vi.fn().mockReturnThis(),
        json: vi.fn()
      };
      const next = vi.fn();

      middleware(req, res, next);

      expect(res.status).toHaveBeenCalledWith(401);
      expect(res.json).toHaveBeenCalledWith(expect.objectContaining({
        error: 'Authentication required'
      }));
      expect(next).not.toHaveBeenCalled();
    });

    it('should return 403 for invalid token', () => {
      const middleware = createAuthMiddleware({ enabled: true, token: 'secret' });
      const req = { headers: { authorization: 'Bearer wrong-token' }, query: {} };
      const res = {
        status: vi.fn().mockReturnThis(),
        json: vi.fn()
      };
      const next = vi.fn();

      middleware(req, res, next);

      expect(res.status).toHaveBeenCalledWith(403);
      expect(res.json).toHaveBeenCalledWith(expect.objectContaining({
        error: 'Invalid token'
      }));
      expect(next).not.toHaveBeenCalled();
    });

    it('should accept valid Bearer token', () => {
      const middleware = createAuthMiddleware({ enabled: true, token: 'secret' });
      const req = { headers: { authorization: 'Bearer secret' }, query: {} };
      const res = {};
      const next = vi.fn();

      middleware(req, res, next);

      expect(next).toHaveBeenCalled();
    });

    it('should accept valid token without Bearer prefix', () => {
      const middleware = createAuthMiddleware({ enabled: true, token: 'secret' });
      const req = { headers: { authorization: 'secret' }, query: {} };
      const res = {};
      const next = vi.fn();

      middleware(req, res, next);

      expect(next).toHaveBeenCalled();
    });

    it('should accept valid token from query parameter', () => {
      const middleware = createAuthMiddleware({ enabled: true, token: 'secret' });
      const req = { headers: {}, query: { token: 'secret' } };
      const res = {};
      const next = vi.fn();

      middleware(req, res, next);

      expect(next).toHaveBeenCalled();
    });

    it('should prefer header over query token', () => {
      const middleware = createAuthMiddleware({ enabled: true, token: 'secret' });
      const req = {
        headers: { authorization: 'Bearer secret' },
        query: { token: 'wrong' }
      };
      const res = {};
      const next = vi.fn();

      middleware(req, res, next);

      expect(next).toHaveBeenCalled();
    });
  });

  describe('createRateLimiter', () => {
    it('should allow requests within limit', () => {
      const limiter = createRateLimiter({ windowMs: 1000, maxRequests: 5 });
      const req = { ip: '127.0.0.1', connection: { remoteAddress: '127.0.0.1' } };
      const res = {};
      const next = vi.fn();

      // Make 5 requests
      for (let i = 0; i < 5; i++) {
        limiter(req, res, next);
      }

      expect(next).toHaveBeenCalledTimes(5);
    });

    it('should block requests exceeding limit', () => {
      const limiter = createRateLimiter({ windowMs: 60000, maxRequests: 2 });
      const req = { ip: '192.168.1.1', connection: { remoteAddress: '192.168.1.1' } };
      const res = {
        status: vi.fn().mockReturnThis(),
        json: vi.fn()
      };
      const next = vi.fn();

      // First 2 should pass
      limiter(req, res, next);
      limiter(req, res, next);

      // Third should be blocked
      limiter(req, res, next);

      expect(next).toHaveBeenCalledTimes(2);
      expect(res.status).toHaveBeenCalledWith(429);
      expect(res.json).toHaveBeenCalledWith(expect.objectContaining({
        error: 'Too many requests'
      }));
    });

    it('should track different IPs separately', () => {
      const limiter = createRateLimiter({ windowMs: 60000, maxRequests: 1 });
      const res = {};
      const next = vi.fn();

      // Request from IP 1
      limiter({ ip: '1.1.1.1' }, res, next);

      // Request from IP 2
      limiter({ ip: '2.2.2.2' }, res, next);

      expect(next).toHaveBeenCalledTimes(2);
    });
  });
});
