/**
 * Daemon Repository Unit Tests
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { createTestDb, createEmptyTestDb } from '../fixtures/testDb.js';

// Mock the database module
let mockDb = null;

vi.mock('../../src/data/database.js', () => ({
  getDb: () => mockDb
}));

// Import after mocking
const { getDaemonCount, getAllDaemons, getDaemonById } = await import('../../src/data/daemonRepository.js');

describe('daemonRepository', () => {
  beforeEach(() => {
    mockDb = createTestDb();
  });

  afterEach(() => {
    if (mockDb) {
      mockDb.close();
      mockDb = null;
    }
  });

  describe('getDaemonCount', () => {
    it('should return correct daemon count', () => {
      const count = getDaemonCount();

      expect(count).toBe(3);
    });

    it('should return 0 when database is null', () => {
      mockDb = null;
      const count = getDaemonCount();

      expect(count).toBe(0);
    });

    it('should return 0 for empty database', () => {
      mockDb = createEmptyTestDb();
      const count = getDaemonCount();

      expect(count).toBe(0);
    });
  });

  describe('getAllDaemons', () => {
    it('should return all daemons', () => {
      const daemons = getAllDaemons();

      expect(daemons.length).toBe(3);
    });

    it('should include online status', () => {
      const daemons = getAllDaemons();

      daemons.forEach(d => {
        expect(typeof d.online).toBe('boolean');
      });
    });

    it('should include pending_count', () => {
      const daemons = getAllDaemons();

      daemons.forEach(d => {
        expect(typeof d.pending_count).toBe('number');
      });
    });

    it('should mark recent daemons as online', () => {
      const daemons = getAllDaemons();
      const kernel = daemons.find(d => d.id === 'kernel');

      // kernel has heartbeat set to 'now' in fixture
      expect(kernel.online).toBe(true);
    });

    it('should mark old daemons as offline', () => {
      const daemons = getAllDaemons();
      const telegramBot = daemons.find(d => d.id === 'telegram-bot');

      // telegram-bot has heartbeat 10 minutes ago
      expect(telegramBot.online).toBe(false);
    });

    it('should return empty array when database is null', () => {
      mockDb = null;
      const daemons = getAllDaemons();

      expect(daemons).toEqual([]);
    });
  });

  describe('getDaemonById', () => {
    it('should return daemon by id', () => {
      const daemon = getDaemonById('kernel');

      expect(daemon).toBeDefined();
      expect(daemon.id).toBe('kernel');
      expect(daemon.description).toBe('Backend kernel daemon');
    });

    it('should include online status', () => {
      const daemon = getDaemonById('kernel');

      expect(typeof daemon.online).toBe('boolean');
    });

    it('should include pending_count', () => {
      const daemon = getDaemonById('kernel');

      expect(typeof daemon.pending_count).toBe('number');
    });

    it('should return null for non-existent daemon', () => {
      const daemon = getDaemonById('nonexistent');

      expect(daemon).toBeNull();
    });

    it('should return null when database is null', () => {
      mockDb = null;
      const daemon = getDaemonById('kernel');

      expect(daemon).toBeNull();
    });
  });
});
