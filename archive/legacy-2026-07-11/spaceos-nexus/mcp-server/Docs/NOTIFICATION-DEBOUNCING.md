---
id: NOTIFICATION-DEBOUNCING
title: Notification Debouncer — Usage Guide
description: How to use the NotificationDebouncer for batching bulk operations
tags: [TASK-14-10, debouncing, bulk-operations, performance]
---

# Notification Debouncer — Usage Guide

## Overview

The `NotificationDebouncer` is a generic utility for batching high-volume notifications into efficient bulk operations. It's designed for scenarios where MCP tools need to send many notifications (e.g., logging tool invocations, tracking resource updates) without overwhelming the system.

**Key Benefits:**

- **Reduces network overhead**: 1,000 notifications → ~10 batches (99% reduction)
- **Configurable batching**: Trigger by size (maxBatchSize) or delay (maxDelayMs)
- **Type-safe**: Generic `<T extends INotification>` for custom notification types
- **Error recoverable**: Batch failures don't crash the debouncer
- **Observable**: Built-in metrics for performance monitoring

---

## Installation

The debouncer is part of the MCP server core:

```typescript
import { NotificationDebouncer, INotification } from '@joinerytech/mcp';
```

Or import directly:

```typescript
import { NotificationDebouncer } from 'src/mcp/notifications/NotificationDebouncer';
```

---

## Basic Usage

### 1. Define Your Notification Type

```typescript
import type { ToolNotification, INotification } from '@joinerytech/mcp';

interface MyNotification extends INotification {
  type: 'my_event';
  data: {
    event_id: string;
    severity: 'info' | 'warn' | 'error';
  };
}
```

### 2. Create a Debouncer Instance

```typescript
const debouncer = new NotificationDebouncer<ToolNotification>({
  maxBatchSize: 50,        // Send when 50 notifications are queued
  maxDelayMs: 100,         // Or send after 100ms, whichever comes first
  onBatch: async (notifications) => {
    // Handle batch of notifications
    console.log(`Processing ${notifications.length} notifications`);
    await logNotifications(notifications);
  },
  onError: (error) => {
    // Optional: custom error handling
    console.error('Batch handler failed:', error.message);
  },
});
```

### 3. Enqueue Notifications

```typescript
// In your tool handler
async function toolInvocationHandler(request: ToolRequest) {
  const tool = request.params.name;
  const executionId = generateId();

  // Notify debouncer of tool invocation
  await debouncer.enqueue({
    type: 'tool_invoked',
    timestamp: Date.now(),
    data: {
      tool_name: tool,
      execution_id: executionId,
      user_id: context.userId,
    },
  });

  // Execute tool...
  const result = await executeTool(tool, request.params.arguments);

  // Notify of completion
  await debouncer.enqueue({
    type: 'tool_completed',
    timestamp: Date.now(),
    data: {
      tool_name: tool,
      execution_id: executionId,
      duration_ms: Date.now() - startTime,
      result_size_bytes: JSON.stringify(result).length,
    },
  });

  return result;
}
```

### 4. Flush When Necessary

```typescript
// At end of request/session
async function cleanupSession() {
  // Ensure all pending notifications are sent
  await debouncer.flush();

  // Gracefully shutdown
  await debouncer.destroy();
}
```

---

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `maxBatchSize` | number | 10 | Send batch when queue reaches this size |
| `maxDelayMs` | number | 100 | Maximum time (ms) to wait before sending buffered batch |
| `onBatch` | async function | **required** | Handler called with array of notifications |
| `onError` | function | optional | Error handler for batch failures |
| `name` | string | 'NotificationDebouncer' | Debug name (appears in logs) |

---

## Real-World Pattern: Tool Handler Integration

```typescript
export class ToolHandler {
  private debouncer: NotificationDebouncer<ToolNotification>;

  constructor() {
    this.debouncer = new NotificationDebouncer({
      maxBatchSize: 100,
      maxDelayMs: 500,
      onBatch: async (notifications) => {
        // Send to logging/monitoring system
        await this.persistNotifications(notifications);
      },
      onError: (error) => {
        // Log errors but keep debouncer alive
        console.error('Failed to persist notifications:', error);
      },
    });
  }

  async handleToolInvocation(request: ToolRequest): Promise<ToolResponse> {
    const { name: toolName, arguments: args } = request.params;
    const executionId = generateId();
    const startTime = Date.now();

    try {
      // Pre-execution notification
      await this.debouncer.enqueue({
        type: 'tool_invoked',
        timestamp: Date.now(),
        data: { tool_name: toolName, execution_id: executionId },
      });

      // Execute
      const result = await this.executeTool(toolName, args);

      // Post-execution notification
      await this.debouncer.enqueue({
        type: 'tool_completed',
        timestamp: Date.now(),
        data: {
          tool_name: toolName,
          execution_id: executionId,
          duration_ms: Date.now() - startTime,
        },
      });

      return result;
    } catch (error) {
      // Error notification
      await this.debouncer.enqueue({
        type: 'tool_failed',
        timestamp: Date.now(),
        data: {
          tool_name: toolName,
          execution_id: executionId,
          error: error?.message ?? 'Unknown error',
        },
      });
      throw error;
    }
  }

  async shutdown() {
    await this.debouncer.flush();
    await this.debouncer.destroy();
  }

  private async persistNotifications(notifications: ToolNotification[]): Promise<void> {
    // Example: batch insert into database
    await db.notifications.insertMany(notifications);
  }
}
```

---

## Performance Characteristics

### Throughput & Batching

```
Input: 1,000 tool notifications
Configuration: maxBatchSize=100, maxDelayMs=100

Output:
  - Batches sent: 10
  - Network calls: 10 (vs 1,000 without debouncer)
  - Reduction: 99%
  - Latency: ~100ms (worst-case, if last batch waits for timeout)
```

### Timing Guarantees

1. **Size trigger** (immediate): When queue reaches `maxBatchSize`, send immediately (< 1ms after threshold)
2. **Delay trigger** (eventual): If queue has items and `maxDelayMs` elapses, send batch
3. **Flush trigger** (manual): `flush()` sends all queued items synchronously

### Memory Usage

```
Per debouncer instance:
  - Notification queue: O(maxBatchSize) — Reused on each batch
  - Timers: 1 active timer (cleared on each batch)
  - Metrics: ~500 bytes (static after creation)

Example:
  - maxBatchSize=100, ~1KB per notification
  - Memory: ~100KB + 500 bytes = ~100.5KB per debouncer
```

---

## Error Handling

### Batch Handler Failures

If `onBatch()` throws, the debouncer **continues operating** without crashing:

```typescript
const debouncer = new NotificationDebouncer({
  maxBatchSize: 50,
  onBatch: async (notifications) => {
    // If this throws, the debouncer catches it
    await persistToDatabase(notifications);
  },
  onError: (error) => {
    // Your app can react to failures
    console.error('Batch failed:', error);
    metrics.recordBatchFailure(error);
  },
});

// Even if onBatch fails, you can continue enqueueing
try {
  await debouncer.enqueue(notification1);
  await debouncer.enqueue(notification2);
  // ... if onBatch threw, items are still queued and will retry
} catch (e) {
  // Only thrown if configuration is invalid (e.g., missing onBatch handler)
}
```

### Validation Errors

The debouncer validates notifications at enqueue time:

```typescript
// Throws: INotification must have a 'type' field
await debouncer.enqueue({
  timestamp: Date.now(),
  data: { /* ... */ },
});

// OK: 'type' field present
await debouncer.enqueue({
  type: 'tool_completed',
  timestamp: Date.now(),
  data: { /* ... */ },
});
```

---

## Monitoring & Metrics

### Get Real-Time Statistics

```typescript
const stats = debouncer.getStats();

console.log(`
  Queued: ${stats.queued}
  Processed: ${stats.processed}
  Batches: ${stats.batches}
  Avg batch size: ${stats.avgBatchSize}
  Uptime: ${stats.uptime}
  Savings: ${stats.savingsEstimate}
`);
```

**Output Example:**

```
  Queued: 1000
  Processed: 1000
  Batches: 10
  Avg batch size: 100
  Uptime: 2s
  Savings: Batched 1000 notifications into 10 batches (99% reduction)
```

---

## Best Practices

### ✅ DO

1. **Flush explicitly** before critical operations or shutdown:

   ```typescript
   await debouncer.flush();
   ```

2. **Use error handler** for observability:

   ```typescript
   onError: (error) => {
     metrics.recordBatchFailure(error);
     alertOps('Notification batching failed');
   }
   ```

3. **Configure appropriate batch sizes** based on target system:
   - High-throughput logging: `maxBatchSize: 100–500`
   - Real-time notifications: `maxBatchSize: 10–50`
   - Low-traffic systems: `maxBatchSize: 5–10`

4. **Call destroy() on shutdown**:

   ```typescript
   process.on('SIGTERM', async () => {
     await debouncer.destroy();
     process.exit(0);
   });
   ```

### ❌ DON'T

1. **Don't ignore errors** — use the `onError` handler:

   ```typescript
   // Bad: Silent failures
   new NotificationDebouncer({ onBatch, /* no onError */ });

   // Good: Observable failures
   new NotificationDebouncer({ onBatch, onError: (e) => logger.error(e) });
   ```

2. **Don't create many debouncers** — reuse one per notification category:

   ```typescript
   // Bad: Wasteful
   for (const tool of tools) {
     new NotificationDebouncer({ ... });
   }

   // Good: Single debouncer handles all tool notifications
   const debouncer = new NotificationDebouncer({ ... });
   ```

3. **Don't rely on immediate flush** — timing is not guaranteed:

   ```typescript
   // Not guaranteed: Depends on maxDelayMs and queue state
   debouncer.enqueue(notification);
   // Notification may not be sent yet

   // Guaranteed: Explicitly flush
   debouncer.enqueue(notification);
   await debouncer.flush(); // Now it's sent
   ```

---

## Testing

### Unit Test Example

```typescript
import { describe, it, expect, vi } from 'vitest';
import { NotificationDebouncer, ToolNotification } from '@joinerytech/mcp';

describe('Tool Notification Debouncer', () => {
  it('should batch tool notifications by size', async () => {
    const batchHandler = vi.fn();
    const debouncer = new NotificationDebouncer<ToolNotification>({
      maxBatchSize: 10,
      maxDelayMs: 1000,
      onBatch: batchHandler,
    });

    // Enqueue 25 notifications → should trigger 2 batches (10 + 10), 5 pending
    for (let i = 0; i < 25; i++) {
      await debouncer.enqueue({
        type: 'tool_invoked',
        timestamp: Date.now(),
        data: { tool_name: `tool-${i}`, execution_id: `exec-${i}` },
      });
    }

    expect(batchHandler).toHaveBeenCalledTimes(2);
    expect(batchHandler.mock.calls[0][0]).toHaveLength(10);
    expect(batchHandler.mock.calls[1][0]).toHaveLength(10);

    // Flush remaining
    await debouncer.flush();
    expect(batchHandler).toHaveBeenCalledTimes(3);
    expect(batchHandler.mock.calls[2][0]).toHaveLength(5);
  });
});
```

---

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| High memory usage | `maxBatchSize` too large | Reduce `maxBatchSize` to 50–100 |
| Notifications not being sent | Haven't called `flush()` | Explicitly call `await debouncer.flush()` |
| Batch handler errors ignored | No `onError` handler | Add `onError: (e) => logger.error(e)` |
| Notifications arriving late | `maxDelayMs` is high | Reduce delay for real-time requirements |
| Performance degradation | Too many debouncers | Consolidate to single debouncer per category |

---

## API Reference

### NotificationDebouncer<T>

```typescript
class NotificationDebouncer<T extends INotification> {
  constructor(options: NotificationDebouncerOptions<T>);

  // Enqueue a single notification
  enqueue(notification: T): Promise<void>;

  // Send all queued notifications immediately
  flush(): Promise<void>;

  // Get performance metrics
  getStats(): NotificationDebouncerStats;

  // Gracefully shutdown
  destroy(): Promise<void>;
}
```

### Types

```typescript
interface INotification {
  type: string;
  timestamp: number;
  data: Record<string, any>;
}

interface NotificationDebouncerOptions<T extends INotification> {
  maxBatchSize?: number; // Default: 10
  maxDelayMs?: number; // Default: 100
  onBatch: (notifications: T[]) => Promise<void>; // Required
  onError?: (error: Error) => void; // Optional
  name?: string; // Default: 'NotificationDebouncer'
}

interface NotificationDebouncerStats {
  queued: number;
  processed: number;
  batches: number;
  avgBatchSize: number;
  totalTimeMs: number;
  uptime: string;
  savingsEstimate: string;
}
```

---

## See Also

- [TASK-14-10: Notification Debouncer (Acceptance Criteria)](TASK-14-10-DEBOUNCING.md)
- [Memory Plugin Module (TASK-14-06)](TASK-14-06-memory-plugin-module.md)
- [MCP Server Architecture](../MCP_Server_Architecture.md)
