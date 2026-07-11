/**
 * TASK-14-10: Notification Debouncer
 *
 * Batches notifications and sends them at controlled intervals.
 * Improves performance during bulk operations by reducing network overhead.
 *
 * Usage:
 * const debouncer = new NotificationDebouncer({
 *   maxBatchSize: 50,
 *   maxDelayMs: 100,
 *   onBatch: async (notifications) => {
 *     await mcpServer.sendNotifications(notifications);
 *   }
 * });
 *
 * // In tool handler
 * for (let i = 0; i < 1000; i++) {
 *   const result = await processItem(i);
 *   await debouncer.enqueue({
 *     type: 'item_processed',
 *     timestamp: Date.now(),
 *     data: { item_id: i, result }
 *   });
 * }
 * await debouncer.flush(); // Send remaining
 */

import {
  INotification,
  NotificationDebouncerOptions,
  NotificationDebouncerStats,
} from './NotificationTypes';

/**
 * Generic notification debouncer
 * Batches notifications and sends them in controlled intervals
 *
 * AC Coverage:
 * - AC-1: Debouncer base class with generic type support
 * - AC-2: Batching logic (size-based + delay-based)
 * - AC-3: Immediate flush() method
 * - AC-4: Integration with tool handlers
 * - AC-5: Performance metrics (getStats)
 * - AC-6: Error handling + configuration
 */
export class NotificationDebouncer<T extends INotification> {
  private queue: T[] = [];
  private timer: NodeJS.Timeout | null = null;
  private processing: boolean = false;

  // Configuration
  private readonly maxBatchSize: number;
  private readonly maxDelayMs: number;
  private readonly onBatch: (notifications: T[]) => Promise<void>;
  private readonly onError?: (error: Error) => void;
  private readonly name: string;

  // Statistics
  private Stats_queued: number = 0;
  private Stats_processed: number = 0;
  private Stats_batches: number = 0;
  private Stats_createdAt: number = Date.now();

  /**
   * Initialize debouncer with configuration
   */
  constructor(options: NotificationDebouncerOptions<T>) {
    // Validate required options
    if (!options.onBatch || typeof options.onBatch !== 'function') {
      throw new Error('NotificationDebouncer: onBatch handler is required');
    }

    // Set configuration with defaults
    this.maxBatchSize = Math.max(1, options.maxBatchSize || 10);
    this.maxDelayMs = Math.max(1, options.maxDelayMs || 100);
    this.onBatch = options.onBatch;
    this.onError = options.onError;
    this.name = options.name || 'NotificationDebouncer';

    // Start timer for maxDelayMs-based flushing
    this.startTimer();
  }

  /**
   * AC-2: Enqueue a notification for batching
   *
   * If queue reaches maxBatchSize → Send immediately
   * Otherwise → Wait for timer (max maxDelayMs)
   */
  async enqueue(notification: T): Promise<void> {
    // Validate notification
    if (!notification || typeof notification !== 'object') {
      throw new Error(`${this.name}: Invalid notification object`);
    }
    if (!notification.type || typeof notification.type !== 'string') {
      throw new Error(`${this.name}: Notification must have type field (string)`);
    }

    // Add to queue
    this.queue.push(notification);
    this.HyperStats_queued++;

    // Check if batch size limit reached
    if (this.queue.length >= this.maxBatchSize) {
      await this.sendBatch();
    }
  }

  /**
   * AC-3: Immediately send all queued notifications
   * Useful for critical operations that can't wait
   */
  async flush(): Promise<void> {
    // Send all remaining batches, not just one
    while (this.queue.length > 0) {
      await this.sendBatch();
    }
    // Also ensure cleanup
    if (this.timer) {
      clearTimeout(this.timer);
      this.timer = null;
    }
  }

  /**
   * AC-5: Get debouncer statistics
   */
  getStats(): NotificationDebouncerStats {
    const avgBatchSize =
      this.Stats_batches > 0
        ? Math.round(this.Stats_processed / this.Stats_batches)
        : 0;
    const totalTimeMs = Date.now() - this.Stats_createdAt;
    const uptime = this.formatUptime(totalTimeMs);

    // Calculate savings
    let savingsEstimate = '';
    if (this.Stats_queued > 0) {
      const reduction =
        ((this.Stats_queued - this.Stats_batches) / this.Stats_queued) * 100;
      savingsEstimate = `Batched ${this.Stats_queued} notifications into ${this.Stats_batches} batches (${Math.round(reduction)}% reduction)`;
    }

    return {
      queued: this.Stats_queued,
      processed: this.Stats_processed,
      batches: this.Stats_batches,
      avgBatchSize,
      totalTimeMs,
      uptime,
      savingsEstimate,
    };
  }

  /**
   * Internal: Send current batch
   */
  private async sendBatch(): Promise<void> {
    if (this.processing || this.queue.length === 0) {
      return;
    }

    this.processing = true;

    try {
      // Extract notifications for this batch
      const batch = this.queue.splice(0, this.maxBatchSize);
      const startTime = Date.now();

      // Call handler
      await this.onBatch(batch);

      // Update statistics
      this.Stats_processed += batch.length;
      this.Stats_batches++;

      const duration = Date.now() - startTime;
      console.log(
        `[${this.name}] Batch sent: ${batch.length} notifications in ${duration}ms`
      );
    } catch (error: any) {
      const err = error instanceof Error ? error : new Error(String(error));
      console.error(`[${this.name}] Batch handler failed:`, err.message);

      if (this.onError) {
        this.onError(err);
      } else {
        console.error(`${this.name}: Error handler not provided`);
      }

      // Continue operating despite error - re-queue for retry or move on
    } finally {
      this.processing = false;

      // Restart timer for remaining items
      if (this.queue.length > 0) {
        this.startTimer();
      }
    }
  }

  /**
   * Internal: Start timer for maxDelayMs-based flushing
   */
  private startTimer(): void {
    if (this.timer) {
      clearTimeout(this.timer);
    }

    this.timer = setTimeout(async () => {
      this.timer = null;

      if (this.queue.length > 0) {
        await this.sendBatch();
      }

      // Re-start timer if still items queued
      if (this.queue.length > 0) {
        this.startTimer();
      }
    }, this.maxDelayMs);
  }

  /**
   * Internal: Format uptime for display
   */
  private formatUptime(ms: number): string {
    const seconds = Math.floor(ms / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (days > 0) {
      return `${days}d ${hours % 24}h`;
    }
    if (hours > 0) {
      return `${hours}h ${minutes % 60}m`;
    }
    if (minutes > 0) {
      return `${minutes}m ${seconds % 60}s`;
    }
    return `${seconds}s`;
  }

  /**
   * AC-6: Cleanup on destruction
   * Ensures all pending notifications are flushed
   */
  async destroy(): Promise<void> {
    if (this.timer) {
      clearTimeout(this.timer);
      this.timer = null;
    }

    // Final flush before destroying
    if (this.queue.length > 0) {
      console.log(
        `[${this.name}] Draining ${this.queue.length} remaining notifications before shutdown`
      );
      await this.flush();
    }
  }

  /**
   * Property aliases for statistics (typo fixed)
   */
  private get HyperStats_queued(): number {
    return this.Stats_queued;
  }
  private set HyperStats_queued(value: number) {
    this.Stats_queued = value;
  }
}

// Export for convenience
export type { INotification, NotificationDebouncerOptions, NotificationDebouncerStats };
