/**
 * Message Repository Unit Tests
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { createTestDb, createEmptyTestDb } from '../fixtures/testDb.js';

// Mock the database module
let mockDb = null;

vi.mock('../../src/data/database.js', () => ({
  getDb: () => mockDb
}));

// Import after mocking
const { getStats, getMessages, getMessageById, getInbox, getPendingByDaemon } = await import('../../src/data/messageRepository.js');

describe('messageRepository', () => {
  beforeEach(() => {
    mockDb = createTestDb();
  });

  afterEach(() => {
    if (mockDb) {
      mockDb.close();
      mockDb = null;
    }
  });

  describe('getStats', () => {
    it('should return correct statistics', () => {
      const stats = getStats();

      expect(stats.total).toBe(5);
      expect(stats.pending).toBe(3);
      expect(stats.acked).toBe(2);
    });

    it('should return zeros when database is null', () => {
      mockDb = null;
      const stats = getStats();

      expect(stats.total).toBe(0);
      expect(stats.pending).toBe(0);
      expect(stats.acked).toBe(0);
    });

    it('should return zeros for empty database', () => {
      mockDb = createEmptyTestDb();
      const stats = getStats();

      expect(stats.total).toBe(0);
      expect(stats.pending).toBe(0);
      expect(stats.acked).toBe(0);
    });
  });

  describe('getMessages', () => {
    it('should return all messages with default limit', () => {
      const messages = getMessages();

      expect(messages.length).toBe(5);
    });

    it('should filter by status', () => {
      const pending = getMessages({ status: 'pending' });
      const acked = getMessages({ status: 'acked' });

      expect(pending.length).toBe(3);
      expect(acked.length).toBe(2);
    });

    it('should filter by daemon', () => {
      const kernelMessages = getMessages({ daemon: 'kernel' });

      // kernel is sender or receiver in multiple messages
      expect(kernelMessages.length).toBeGreaterThan(0);
      kernelMessages.forEach(m => {
        expect(m.from_daemon === 'kernel' || m.to_daemon === 'kernel').toBe(true);
      });
    });

    it('should filter by message type', () => {
      const tasks = getMessages({ type: 'task' });

      expect(tasks.length).toBeGreaterThan(0);
      tasks.forEach(m => {
        expect(m.msg_type).toBe('task');
      });
    });

    it('should respect limit parameter', () => {
      const messages = getMessages({ limit: 2 });

      expect(messages.length).toBe(2);
    });

    it('should respect offset parameter', () => {
      const all = getMessages();
      const withOffset = getMessages({ offset: 2 });

      expect(withOffset.length).toBe(all.length - 2);
    });

    it('should return empty array when database is null', () => {
      mockDb = null;
      const messages = getMessages();

      expect(messages).toEqual([]);
    });
  });

  describe('getMessageById', () => {
    it('should return message by id', () => {
      const message = getMessageById(1);

      expect(message).toBeDefined();
      expect(message.id).toBe(1);
    });

    it('should return null for non-existent id', () => {
      const message = getMessageById(999);

      expect(message).toBeUndefined();
    });

    it('should return null when database is null', () => {
      mockDb = null;
      const message = getMessageById(1);

      expect(message).toBeNull();
    });
  });

  describe('getInbox', () => {
    it('should return pending messages for daemon', () => {
      const inbox = getInbox('kernel');

      expect(inbox.length).toBeGreaterThan(0);
      inbox.forEach(m => {
        expect(m.to_daemon).toBe('kernel');
        expect(m.status).toBe('pending');
      });
    });

    it('should sort by priority', () => {
      const inbox = getInbox('conductor');

      // Check that critical comes before others
      const priorities = inbox.map(m => m.priority);
      const criticalIndex = priorities.indexOf('critical');
      const lowIndex = priorities.indexOf('low');

      if (criticalIndex !== -1 && lowIndex !== -1) {
        expect(criticalIndex).toBeLessThan(lowIndex);
      }
    });

    it('should return empty array for daemon with no pending messages', () => {
      const inbox = getInbox('nonexistent');

      expect(inbox).toEqual([]);
    });

    it('should return empty array when database is null', () => {
      mockDb = null;
      const inbox = getInbox('kernel');

      expect(inbox).toEqual([]);
    });
  });

  describe('getPendingByDaemon', () => {
    it('should return pending counts grouped by daemon', () => {
      const pending = getPendingByDaemon();

      expect(typeof pending).toBe('object');
      Object.values(pending).forEach(count => {
        expect(typeof count).toBe('number');
        expect(count).toBeGreaterThan(0);
      });
    });

    it('should return empty object when database is null', () => {
      mockDb = null;
      const pending = getPendingByDaemon();

      expect(pending).toEqual({});
    });

    it('should return empty object for empty database', () => {
      mockDb = createEmptyTestDb();
      const pending = getPendingByDaemon();

      expect(pending).toEqual({});
    });
  });
});
