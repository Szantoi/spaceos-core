/**
 * API Integration Tests
 */

import { describe, it, expect, beforeAll, afterAll, vi } from 'vitest';
import express from 'express';
import request from 'supertest';
import { createTestDb } from '../fixtures/testDb.js';

// Mock database
let mockDb = null;

vi.mock('../../src/data/database.js', () => ({
  initDatabase: () => mockDb,
  getDb: () => mockDb,
  closeDatabase: () => {}
}));

// Create test app
async function createTestApp() {
  const app = express();
  app.use(express.json());

  // Import routes after mocking
  const statsRoutes = (await import('../../src/routes/statsRoutes.js')).default;
  const daemonRoutes = (await import('../../src/routes/daemonRoutes.js')).default;
  const messageRoutes = (await import('../../src/routes/messageRoutes.js')).default;

  // Health check
  app.get('/health', (req, res) => {
    res.json({ status: 'ok' });
  });

  // Mount routes
  app.use('/api/stats', statsRoutes);
  app.use('/api/daemons', daemonRoutes);
  app.use('/api/messages', messageRoutes);

  return app;
}

describe('API Integration Tests', () => {
  let app;

  beforeAll(async () => {
    mockDb = createTestDb();
    app = await createTestApp();
  });

  afterAll(() => {
    if (mockDb) {
      mockDb.close();
      mockDb = null;
    }
  });

  describe('GET /health', () => {
    it('should return ok status', async () => {
      const res = await request(app).get('/health');

      expect(res.status).toBe(200);
      expect(res.body.status).toBe('ok');
    });
  });

  describe('GET /api/stats', () => {
    it('should return statistics', async () => {
      const res = await request(app).get('/api/stats');

      expect(res.status).toBe(200);
      expect(res.body).toHaveProperty('total');
      expect(res.body).toHaveProperty('pending');
      expect(res.body).toHaveProperty('acked');
      expect(res.body).toHaveProperty('daemons');
    });

    it('should return correct counts', async () => {
      const res = await request(app).get('/api/stats');

      expect(res.body.total).toBe(5);
      expect(res.body.pending).toBe(3);
      expect(res.body.acked).toBe(2);
      expect(res.body.daemons).toBe(3);
    });
  });

  describe('GET /api/daemons', () => {
    it('should return list of daemons', async () => {
      const res = await request(app).get('/api/daemons');

      expect(res.status).toBe(200);
      expect(Array.isArray(res.body)).toBe(true);
      expect(res.body.length).toBe(3);
    });

    it('should include daemon properties', async () => {
      const res = await request(app).get('/api/daemons');

      res.body.forEach(daemon => {
        expect(daemon).toHaveProperty('id');
        expect(daemon).toHaveProperty('description');
        expect(daemon).toHaveProperty('online');
        expect(daemon).toHaveProperty('pending_count');
      });
    });
  });

  describe('GET /api/daemons/summary', () => {
    it('should return daemon summary', async () => {
      const res = await request(app).get('/api/daemons/summary');

      expect(res.status).toBe(200);
      expect(res.body).toHaveProperty('total');
      expect(res.body).toHaveProperty('online');
      expect(res.body).toHaveProperty('offline');
      expect(res.body).toHaveProperty('daemons');
    });
  });

  describe('GET /api/daemons/:id', () => {
    it('should return specific daemon', async () => {
      const res = await request(app).get('/api/daemons/kernel');

      expect(res.status).toBe(200);
      expect(res.body.id).toBe('kernel');
      expect(res.body.description).toBe('Backend kernel daemon');
    });

    it('should return 404 for non-existent daemon', async () => {
      const res = await request(app).get('/api/daemons/nonexistent');

      expect(res.status).toBe(404);
      expect(res.body.error).toBe('Daemon not found');
    });
  });

  describe('GET /api/messages', () => {
    it('should return list of messages', async () => {
      const res = await request(app).get('/api/messages');

      expect(res.status).toBe(200);
      expect(res.body).toHaveProperty('messages');
      expect(res.body).toHaveProperty('pagination');
      expect(Array.isArray(res.body.messages)).toBe(true);
    });

    it('should filter by status', async () => {
      const res = await request(app).get('/api/messages?status=pending');

      expect(res.status).toBe(200);
      res.body.messages.forEach(msg => {
        expect(msg.status).toBe('pending');
      });
    });

    it('should filter by daemon', async () => {
      const res = await request(app).get('/api/messages?daemon=kernel');

      expect(res.status).toBe(200);
      res.body.messages.forEach(msg => {
        expect(msg.from_daemon === 'kernel' || msg.to_daemon === 'kernel').toBe(true);
      });
    });

    it('should respect limit parameter', async () => {
      const res = await request(app).get('/api/messages?limit=2');

      expect(res.status).toBe(200);
      expect(res.body.messages.length).toBeLessThanOrEqual(2);
      expect(res.body.pagination.limit).toBe(2);
    });
  });

  describe('GET /api/messages/:id', () => {
    it('should return specific message', async () => {
      const res = await request(app).get('/api/messages/1');

      expect(res.status).toBe(200);
      expect(res.body.id).toBe(1);
    });

    it('should return 404 for non-existent message', async () => {
      const res = await request(app).get('/api/messages/999');

      expect(res.status).toBe(404);
      expect(res.body.error).toBe('Message not found');
    });
  });

  describe('GET /api/messages/pending', () => {
    it('should return pending counts by daemon', async () => {
      const res = await request(app).get('/api/messages/pending');

      expect(res.status).toBe(200);
      expect(typeof res.body).toBe('object');
    });
  });

  describe('GET /api/messages/inbox/:daemon', () => {
    it('should return inbox for daemon', async () => {
      const res = await request(app).get('/api/messages/inbox/kernel');

      expect(res.status).toBe(200);
      expect(res.body).toHaveProperty('messages');
      expect(res.body).toHaveProperty('total');
      expect(res.body).toHaveProperty('byPriority');
    });

    it('should only return pending messages to target daemon', async () => {
      const res = await request(app).get('/api/messages/inbox/kernel');

      res.body.messages.forEach(msg => {
        expect(msg.to_daemon).toBe('kernel');
        expect(msg.status).toBe('pending');
      });
    });
  });
});
