/**
 * TASK-14-10: Notification Types & Interface
 *
 * Defines the contract for notifications that can be debounced.
 */

/**
 * Base notification interface
 * All debounced notifications must conform to this structure
 */
export interface INotification {
  type: string;                    // Notification type: 'tool_invoked', 'resource_updated', etc.
  timestamp: number;               // Unix timestamp (ms)
  data: Record<string, any>;       // Payload
}

/**
 * Debouncer configuration options
 */
export interface NotificationDebouncerOptions<T extends INotification> {
  maxBatchSize?: number;                               // Default: 10 (send batch when this size reached)
  maxDelayMs?: number;                                 // Default: 100 (max time before sending batch)
  onBatch: (notifications: T[]) => Promise<void>;      // Required: Handler for batches
  onError?: (error: Error) => void;                    // Optional: Error handler
  name?: string;                                       // Optional: For logging
}

/**
 * Debouncer statistics
 * Tracks performance and throughput
 */
export interface NotificationDebouncerStats {
  queued: number;           // Total notifications queued
  processed: number;        // Total notifications sent in batches
  batches: number;          // Number of batches sent
  avgBatchSize: number;     // Average size per batch
  totalTimeMs: number;      // Time since debouncer created
  uptime: string;           // Human-readable uptime
  savingsEstimate: string;  // e.g., "Batched 1000 into 20 (95% reduction)"
}

/**
 * Tool notification specific type
 * Used when tools emit notifications
 */
export interface ToolNotification extends INotification {
  type: 'tool_invoked' | 'tool_completed' | 'tool_failed';
  data: {
    tool_name: string;
    execution_id: string;
    duration_ms?: number;
    error?: string;
    [key: string]: any;
  };
}

/**
 * Resource notification specific type
 * Used when resources are updated
 */
export interface ResourceNotification extends INotification {
  type: 'resource_created' | 'resource_updated' | 'resource_deleted';
  data: {
    resource_uri: string;
    resource_name?: string;
    change_summary?: string;
    [key: string]: any;
  };
}
