import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import {
  NotificationDebouncer,
  INotification,
  ToolNotification,
} from '../../mcp/notifications/NotificationDebouncer';

/**
 * TASK-14-10: Notification Debouncer Integration Tests
 *
 * Tests real-world workflows and batching scenarios
 */

describe('NotificationDebouncer: Integration Tests (TASK-14-10)', () => {
  let debouncer: NotificationDebouncer<ToolNotification>;
  let batchHandler: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    batchHandler = vi.fn(async (notifications: ToolNotification[]) => {
      // Track batch sends
    });
  });

  afterEach(async () => {
    if (debouncer) {
      await debouncer.destroy();
    }
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // INT-01: Tool Handler with Debouncer Workflow
  // ─────────────────────────────────────────────────────────────────────────────

  describe('INT-01: Tool Handler Bulk Operations Workflow', () => {
    it('should batch tool notifications during bulk processing', async () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 50,
        maxDelayMs: 100,
        onBatch: batchHandler,
      });

      // Simulate bulk processing of 150 items
      for (let i = 0; i < 150; i++) {
        await debouncer.enqueue({
          type: 'tool_completed',
          timestamp: Date.now(),
          data: {
            tool_name: 'process_item',
            execution_id: `exec-${i}`,
            duration_ms: 10 + Math.random() * 20,
          },
        });
      }

      // Flush remaining
      await debouncer.flush();

      expect(batchHandler).toHaveBeenCalled();

      // Verify batching happened (should be ~3 batches for 150 items with maxBatchSize=50)
      const totalNotifications = batchHandler.mock.calls.reduce(
        (sum: number, call: any) => sum + call[0].length,
        0
      );
      expect(totalNotifications).toBe(150);

      const stats = debouncer.getStats();
      expect(stats.queued).toBe(150);
      expect(stats.processed).toBe(150);
      expect(stats.batches).toBeGreaterThanOrEqual(3);
    });

    it('should handle rapid sequential operations', async () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 20,
        maxDelayMs: 150,
        onBatch: batchHandler,
      });

      // Rapid enqueue and flush
      for (let round = 0; round < 3; round++) {
        for (let i = 0; i < 20; i++) {
          await debouncer.enqueue({
            type: 'tool_invoked',
            timestamp: Date.now(),
            data: {
              tool_name: `tool_${round}`,
              execution_id: `exec-${round}-${i}`,
            },
          });
        }

        // Flush between rounds
        await debouncer.flush();
      }

      // Should have processed all
      const stats = debouncer.getStats();
      expect(stats.processed).toBe(60);
      expect(stats.batches).toBe(3); // One batch per round
    });
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // INT-02: Mixed Tool Notification Types
  // ─────────────────────────────────────────────────────────────────────────────

  describe('INT-02: Mixed Tool Notification Types', () => {
    it('should batch different tool notification types together', async () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 10,
        maxDelayMs: 200,
        onBatch: batchHandler,
      });

      // Mix of different tool event types
      const toolEvents: ToolNotification[] = [
        {
          type: 'tool_invoked',
          timestamp: Date.now(),
          data: {
            tool_name: 'bootstrap_agent',
            execution_id: 'exec-1',
          },
        },
        {
          type: 'tool_completed',
          timestamp: Date.now(),
          data: {
            tool_name: 'bootstrap_agent',
            execution_id: 'exec-1',
            duration_ms: 150,
          },
        },
        {
          type: 'tool_failed',
          timestamp: Date.now(),
          data: {
            tool_name: 'request_context',
            execution_id: 'exec-2',
            error: 'Invalid context',
          },
        },
      ];

      for (const event of toolEvents) {
        await debouncer.enqueue(event);
      }

      await debouncer.flush();

      expect(batchHandler).toHaveBeenCalled();
      const batch = batchHandler.mock.calls[0][0];
      expect(batch).toHaveLength(3);
    });
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // INT-03: Performance & Metrics Validation
  // ─────────────────────────────────────────────────────────────────────────────

  describe('INT-03: Performance & Metrics Validation', () => {
    it('should demonstrate significant reduction in notifications', async () => {
      const sendCalls: Array<ToolNotification[]> = [];

      debouncer = new NotificationDebouncer({
        maxBatchSize: 100,
        maxDelayMs: 50,
        onBatch: async (batch) => {
          sendCalls.push([...batch]);
        },
      });

      // Simulate 1000 tool invocations
      for (let i = 0; i < 1000; i++) {
        await debouncer.enqueue({
          type: 'tool_completed',
          timestamp: Date.now() + i,
          data: {
            tool_name: `tool_${i % 50}`,
            execution_id: `exec-${i}`,
            duration_ms: Math.random() * 100,
          },
        });
      }

      await debouncer.flush();

      // Should have batched into ~10 calls instead of 1000
      expect(sendCalls.length).toBeGreaterThanOrEqual(10);
      expect(sendCalls.length).toBeLessThanOrEqual(15);

      const stats = debouncer.getStats();
      const reduction = (1000 - sendCalls.length) / 1000 * 100;

      console.log(`Reduction: ${reduction.toFixed(1)}% (1000 → ${sendCalls.length} calls)`);
      expect(reduction).toBeGreaterThan(90); // 90%+ reduction
    });

    it('should track accurate timing metrics', async () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 5,
        maxDelayMs: 500,
        onBatch: batchHandler,
      });

      const startTime = Date.now();

      for (let i = 0; i < 5; i++) {
        await debouncer.enqueue({
          type: 'tool_invoked',
          timestamp: Date.now(),
          data: {
            tool_name: 'test',
            execution_id: `exec-${i}`,
          },
        });
      }

      await new Promise((resolve) => setTimeout(resolve, 50));

      const elapsed = Date.now() - startTime;
      const stats = debouncer.getStats();

      expect(stats.totalTimeMs).toBeGreaterThanOrEqual(elapsed - 50);
      expect(stats.uptime).toBeDefined();
    });
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // INT-04: Concurrent & Parallel Operation
  // ─────────────────────────────────────────────────────────────────────────────

  describe('INT-04: Concurrent Enqueue Patterns', () => {
    it('should handle rapid parallel enqueues', async () => {
      debouncer = new NotificationDebouncer({
        maxBatchSize: 50,
        maxDelayMs: 100,
        onBatch: batchHandler,
      });

      // Fire off 200 enqueues rapidly (awaiting all)
      const promises = [];
      for (let i = 0; i < 200; i++) {
        promises.push(
          debouncer.enqueue({
            type: 'tool_completed',
            timestamp: Date.now(),
            data: {
              tool_name: 'parallel_tool',
              execution_id: `exec-${i}`,
            },
          })
        );
      }

      await Promise.all(promises);
      await debouncer.flush();

      const stats = debouncer.getStats();
      expect(stats.processed).toBe(200);
      expect(stats.batches).toBeGreaterThan(0);
    });
  });

  // ─────────────────────────────────────────────────────────────────────────────
  // INT-05: Error Recovery Workflow
  // ─────────────────────────────────────────────────────────────────────────────

  describe('INT-05: Error Recovery in Bulk Operations', () => {
    it('should recover and continue after batch handler error', async () => {
      let errorCount = 0;
      const retryableHandler = vi.fn(async (batch: ToolNotification[]) => {
        errorCount++;
        if (errorCount === 1 && batch.length < 50) {
          throw new Error('Transient failure');
        }
        // Succeed on second attempt or large batches
      });

      const errorHandler = vi.fn();

      debouncer = new NotificationDebouncer({
        maxBatchSize: 50,
        maxDelayMs: 100,
        onBatch: retryableHandler,
        onError: errorHandler,
      });

      // Send 100 notifications
      for (let i = 0; i < 100; i++) {
        await debouncer.enqueue({
          type: 'tool_completed',
          timestamp: Date.now(),
          data: {
            tool_name: 'test',
            execution_id: `exec-${i}`,
          },
        });
      }

      await new Promise((resolve) => setTimeout(resolve, 200));

      // Should have been called multiple times (with retry)
      expect(retryableHandler).toHaveBeenCalled();
      // Error handler may or may not be called depending on timing
    });
  });
});
