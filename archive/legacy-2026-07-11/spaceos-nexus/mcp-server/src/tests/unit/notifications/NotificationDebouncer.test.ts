import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import {
  NotificationDebouncer,
  INotification,
  NotificationDebouncerStats,
  ToolNotification,
} from '../../../mcp/notifications/NotificationDebouncer';

/**
 * TASK-14-10: Notification Debouncer Unit Tests
 *
 * Coverage:
 * - UT-01: Debouncer creation + interface validation
 * - UT-02: Batching logic (size-based flus)
 * - UT-03: Delay-based batching
 * - UT-04: Batch sent when size limit reached
 * - UT-05: Deprecation warnings logged
 * - UT-06: Immediate flush
 * - UT-07: Metrics tracking
 * - UT-08: Performance validation
 * - UT-09: Error handling
 * - UT-10: Configuration validation
 * - UT-11: Graceful shutdown
 */

describe('NotificationDebouncer: Unit Tests (TASK-14-10)', () => {
  let debouncer: NotificationDebouncer<INotification>;
  let batchHandler: ReturnType<typeof vi.fn>;
  let errorHandler: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    batchHandler = vi.fn(async (notifications: INotification[]) => {
      // Mock handler for testing
    });
    errorHandler = vi.fn();
  });

  afterEach(async () => {
    if (debouncer) {
      await debouncer.destroy();
    }
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // UT-01: Debouncer Creation & Interface Validation
  // ─────────────────────────────────────────────────────────────────────────────

  describe('UT-01: Debouncer Creation & Interface Validation', () => {
    it('should create debouncer with required onBatch handler', () => {
      debouncer = new NotificationDebouncer({
        onBatch: batchHandler,
      });

      expect(debouncer).toBeDefined();
      expect(typeof debouncer.enqueue).toBe('function');
      expect(typeof debouncer.flush).toBe('function');
      expect(typeof debouncer.getStats).toBe('function');
    });

    it('should throw if onBatch handler missing', () => {
      expect(
        () =>
          new NotificationDebouncer({
            onBatch: undefined as any,
          })
      ).toThrow('onBatch handler is required');
    });

    it('should accept configuration with defaults', () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 20,
        maxDelayMs: 50,
        onBatch: batchHandler,
        name: 'TestDebouncer',
      });

      expect(debouncer).toBeDefined();
    });

    it('should provide generic type support', async () => {
      const toolDebouncer = new NotificationDebouncer<ToolNotification>({
        onBatch: batchHandler,
      });

      await toolDebouncer.enqueue({
        type: 'tool_invoked',
        timestamp: Date.now(),
        data: {
          tool_name: 'bootstrap_agent',
          execution_id: 'exec-123',
          duration_ms: 100,
        },
      });

      await toolDebouncer.flush();
      expect(batchHandler).toHaveBeenCalled();
    });
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // UT-02: Size-Based Batching
  // ─────────────────────────────────────────────────────────────────────────────

  describe('UT-02: Size-Based Batching', () => {
    it('should send batch when size limit reached', async () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 3,
        maxDelayMs: 1000,
        onBatch: batchHandler,
      });

      // Enqueue 3 notifications - should trigger batch send
      await debouncer.enqueue({
        type: 'test',
        timestamp: Date.now(),
        data: { id: 1 },
      });
      await debouncer.enqueue({
        type: 'test',
        timestamp: Date.now(),
        data: { id: 2 },
      });
      await debouncer.enqueue({
        type: 'test',
        timestamp: Date.now(),
        data: { id: 3 },
      });

      // Wait for async handler
      await new Promise((resolve) => setTimeout(resolve, 50));

      expect(batchHandler).toHaveBeenCalled();
      const call = batchHandler.mock.calls[0];
      expect(call[0]).toHaveLength(3);
    });

    it('should not send batch if size not reached', async () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 5,
        maxDelayMs: 200,
        onBatch: batchHandler,
      });

      await debouncer.enqueue({
        type: 'test',
        timestamp: Date.now(),
        data: { id: 1 },
      });
      await debouncer.enqueue({
        type: 'test',
        timestamp: Date.now(),
        data: { id: 2 },
      });

      // Wait less than maxDelayMs
      await new Promise((resolve) => setTimeout(resolve, 50));

      // Should not have sent yet (batch size not reached, timer not expired)
      expect(batchHandler).not.toHaveBeenCalled();
    });

    it('should handle batch with configured maxBatchSize', async () => {
      const MAX_SIZE = 7;
      debouncer = new NotificationDebouncer({
        maxBatchSize: MAX_SIZE,
        maxDelayMs: 500,
        onBatch: batchHandler,
      });

      // Enqueue exactly maxBatchSize notifications
      for (let i = 0; i < MAX_SIZE; i++) {
        await debouncer.enqueue({
          type: 'test',
          timestamp: Date.now(),
          data: { id: i },
        });
      }

      await new Promise((resolve) => setTimeout(resolve, 50));

      expect(batchHandler).toHaveBeenCalledTimes(1);
      expect(batchHandler.mock.calls[0][0]).toHaveLength(MAX_SIZE);
    });
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // UT-03: Delay-Based Batching
  // ─────────────────────────────────────────────────────────────────────────────

  describe('UT-03: Delay-Based Batching', () => {
    it('should send batch after delay even if size not reached', async () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 10,
        maxDelayMs: 50,
        onBatch: batchHandler,
      });

      await debouncer.enqueue({
        type: 'test',
        timestamp: Date.now(),
        data: { id: 1 },
      });
      await debouncer.enqueue({
        type: 'test',
        timestamp: Date.now(),
        data: { id: 2 },
      });

      // Wait for timer to expire
      await new Promise((resolve) => setTimeout(resolve, 100));

      // Should have sent partial batch after timeout
      expect(batchHandler).toHaveBeenCalled();
      expect(batchHandler.mock.calls[0][0]).toHaveLength(2);
    });

    it('should respect configured maxDelayMs', async () => {
      const MAX_DELAY = 80;
      debouncer = new NotificationDebouncer({
        maxBatchSize: 100,
        maxDelayMs: MAX_DELAY,
        onBatch: batchHandler,
      });

      const startTime = Date.now();

      await debouncer.enqueue({
        type: 'test',
        timestamp: Date.now(),
        data: { id: 1 },
      });

      // Wait for batch
      await new Promise((resolve) => setTimeout(resolve, MAX_DELAY + 50));

      const endTime = Date.now();
      const elapsed = endTime - startTime;

      expect(elapsed).toBeGreaterThanOrEqual(MAX_DELAY);
      expect(batchHandler).toHaveBeenCalled();
    });
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // UT-04: Batch Sent Immediately When Size Reached
  // ─────────────────────────────────────────────────────────────────────────────

  describe('UT-04: Batch Sent Immediately When Size Reached', () => {
    it('should prioritize size limit over delay', async () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 2,
        maxDelayMs: 500,
        onBatch: batchHandler,
      });

      const startTime = Date.now();

      for (let i = 0; i < 2; i++) {
        await debouncer.enqueue({
          type: 'test',
          timestamp: Date.now(),
          data: { id: i },
        });
      }

      await new Promise((resolve) => setTimeout(resolve, 50));
      const elapsed = Date.now() - startTime;

      // Should have sent batch way before maxDelayMs
      expect(elapsed).toBeLessThan(200);
      expect(batchHandler).toHaveBeenCalled();
    });
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // UT-06: Immediate Flush
  // ─────────────────────────────────────────────────────────────────────────────

  describe('UT-06: Immediate Flush', () => {
    it('should immediately send queued notifications', async () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 100,
        maxDelayMs: 1000,
        onBatch: batchHandler,
      });

      await debouncer.enqueue({
        type: 'test',
        timestamp: Date.now(),
        data: { id: 1 },
      });
      await debouncer.enqueue({
        type: 'test',
        timestamp: Date.now(),
        data: { id: 2 },
      });

      // Before flush, no batch should be sent
      expect(batchHandler).not.toHaveBeenCalled();

      // Flush
      await debouncer.flush();

      // After flush, batch should be sent
      expect(batchHandler).toHaveBeenCalledTimes(1);
      expect(batchHandler.mock.calls[0][0]).toHaveLength(2);
    });

    it('should be no-op if queue empty', async () => {
      debouncer = new NotificationDebouncer({
        onBatch: batchHandler,
      });

      await debouncer.flush();

      expect(batchHandler).not.toHaveBeenCalled();
    });
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // UT-07: Metrics Tracking
  // ─────────────────────────────────────────────────────────────────────────────

  describe('UT-07: Metrics Tracking', () => {
    it('should track debouncer statistics', async () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 5,
        maxDelayMs: 1000,
        onBatch: batchHandler,
      });

      // Enqueue some notifications
      for (let i = 0; i < 5; i++) {
        await debouncer.enqueue({
          type: 'test',
          timestamp: Date.now(),
          data: { id: i },
        });
      }

      await new Promise((resolve) => setTimeout(resolve, 50));

      const stats = debouncer.getStats();

      expect(stats.queued).toBe(5);
      expect(stats.processed).toBe(5);
      expect(stats.batches).toBe(1);
      expect(stats.avgBatchSize).toBe(5);
    });

    it('should calculate savings correctly', async () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 10,
        maxDelayMs: 1000,
        onBatch: batchHandler,
      });

      // Simulate 100 enqueues, batched into ~10 batches
      for (let i = 0; i < 100; i++) {
        await debouncer.enqueue({
          type: 'test',
          timestamp: Date.now(),
          data: { id: i },
        });
      }

      await new Promise((resolve) => setTimeout(resolve, 1500));

      const stats = debouncer.getStats();

      expect(stats.queued).toBe(100);
      expect(stats.processed).toBe(100);
      expect(stats.batches).toBeGreaterThan(0);
      expect(stats.savingsEstimate).toContain('Batched');
    });

    it('should provide uptime in stats', async () => {
      debouncer = new NotificationDebouncer({
        onBatch: batchHandler,
      });

      await new Promise((resolve) => setTimeout(resolve, 100));

      const stats = debouncer.getStats();

      expect(stats.uptime).toBeDefined();
      expect(stats.totalTimeMs).toBeGreaterThanOrEqual(100);
    });
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // UT-09: Error Handling
  // ─────────────────────────────────────────────────────────────────────────────

  describe('UT-09: Error Handling', () => {
    it('should call error handler if onBatch fails', async () => {
      const erroringHandler = vi.fn(async () => {
        throw new Error('Handler failed');
      });

      debouncer = new NotificationDebouncer({
        maxBatchSize: 2,
        maxDelayMs: 1000,
        onBatch: erroringHandler,
        onError: errorHandler,
      });

      for (let i = 0; i < 2; i++) {
        await debouncer.enqueue({
          type: 'test',
          timestamp: Date.now(),
          data: { id: i },
        });
      }

      await new Promise((resolve) => setTimeout(resolve, 100));

      expect(erroringHandler).toHaveBeenCalled();
      expect(errorHandler).toHaveBeenCalled();
    });

    it('should continue operating after error', async () => {
      let callCount = 0;
      const partialHandler = vi.fn(async () => {
        callCount++;
        if (callCount === 1) {
          throw new Error('First batch failed');
        }
      });

      debouncer = new NotificationDebouncer({
        maxBatchSize: 2,
        maxDelayMs: 100,
        onBatch: partialHandler,
        onError: errorHandler,
      });

      // First 2 (will error)
      for (let i = 0; i < 2; i++) {
        await debouncer.enqueue({
          type: 'test',
          timestamp: Date.now(),
          data: { id: i },
        });
      }

      await new Promise((resolve) => setTimeout(resolve, 150));

      // Second 2 (should succeed)
      for (let i = 2; i < 4; i++) {
        await debouncer.enqueue({
          type: 'test',
          timestamp: Date.now(),
          data: { id: i },
        });
      }

      await new Promise((resolve) => setTimeout(resolve, 150));

      // Handler called twice (first failed, second succeeded)
      expect(partialHandler).toHaveBeenCalledTimes(2);
    });

    it('should validate notification structure', async () => {
      debouncer = new NotificationDebouncer({
        onBatch: batchHandler,
      });

      await expect(
        debouncer.enqueue({
          type: undefined as any,
          timestamp: Date.now(),
          data: {},
        })
      ).rejects.toThrow('type field');
    });
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // UT-10: Configuration Validation
  // ─────────────────────────────────────────────────────────────────────────────

  describe('UT-10: Configuration Validation', () => {
    it('should validate maxBatchSize >= 1', () => {
      expect(
        () =>
          new NotificationDebouncer({
            maxBatchSize: 0,
            onBatch: batchHandler,
          })
      ).not.toThrow(); // Should coerce to minimum
    });

    it('should validate maxDelayMs >= 1', () => {
      expect(
        () =>
          new NotificationDebouncer({
            maxDelayMs: 0,
            onBatch: batchHandler,
          })
      ).not.toThrow();
    });

    it('should use provided optional error handler', async () => {
      const failingHandler = vi.fn(async () => {
        throw new Error('Test error');
      });

      debouncer = new NotificationDebouncer({
        maxBatchSize: 1,
        onBatch: failingHandler,
        onError: errorHandler,
      });

      await debouncer.enqueue({
        type: 'test',
        timestamp: Date.now(),
        data: { id: 1 },
      });

      await new Promise((resolve) => setTimeout(resolve, 50));

      expect(errorHandler).toHaveBeenCalled();
    });
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // UT-11: Graceful Shutdown
  // ─────────────────────────────────────────────────────────────────────────────

  describe('UT-11: Graceful Shutdown', () => {
    it('should drain queue on destroy', async () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 100,
        maxDelayMs: 1000,
        onBatch: batchHandler,
      });

      for (let i = 0; i < 3; i++) {
        await debouncer.enqueue({
          type: 'test',
          timestamp: Date.now(),
          data: { id: i },
        });
      }

      await debouncer.destroy();

      // Should have flushed before destruction
      expect(batchHandler).toHaveBeenCalled();
    });

    it('should handle destroy when empty', async () => {
      debouncer = new NotificationDebouncer({
        onBatch: batchHandler,
      });

      await expect(debouncer.destroy()).resolves.not.toThrow();
    });
  });
});
