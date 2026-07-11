---
id: TASK-14-10
title: "TASK-14-10: Notification Debouncing"
epic: EPIC-14
phase: "Phase 2: Advanced Features"
type: task
created: 2026-03-11
status: "✅ COMPLETE"
effort: "6 hours (~2 days)"
owner: "TBD"
---

# TASK-14-10: Notification Debouncing

## Overview

Implement a **notification debouncer** to improve performance during bulk operations. Instead of sending individual tool notifications for each operation, the debouncer **batches notifications** and sends them at controlled intervals (max 1 per 100ms).

**Status:** ✅ COMPLETE — NotificationDebouncer implemented with unit tests
**Owner:** TBD (Backend developer)
**Duration:** 6 hours (~2 development days)
**Predecessor:** TASK-14-03 (Plugin System foundation)
**Successor:** TASK-14-11 (E2E tests can verify debouncing)
**Blockers:** None — Can start in parallel with 14-07, 14-08, 14-09

---

## Problem Statement

When an agent performs **bulk operations** (e.g., processing 100 items, database migration, batch workflow):

- Many tool invocations generate notifications
- Each notification is sent immediately
- Network overhead + MCP server load increases significantly
- Client may be overwhelmed with individual updates

**Goal:** Provide a `NotificationDebouncer` utility that:

1. Batches notifications together
2. Sends batch every 100ms max (configurable)
3. Allows immediate flush for important notifications
4. Tracks batch stats (items debounced, time saved)

---

## Acceptance Criteria

### AC-1: NotificationDebouncer Base Class

**Requirement:** Implement `NotificationDebouncer<T>` generic class for batching any notification type.

**Input:** None (class interface)

**Output:**

```typescript
interface INotification {
  type: string;     // 'tool_invoked', 'resource_fetched', etc.
  timestamp: number;
  data: Record<string, any>;
}

export class NotificationDebouncer<T extends INotification> {
  constructor(options: {
    maxBatchSize?: number;      // Default: 10
    maxDelayMs?: number;        // Default: 100ms
    onBatch: (notifications: T[]) => Promise<void>;  // Handler
  });

  async enqueue(notification: T): Promise<void>;
  async flush(): Promise<void>;
  getStats(): { queued: number; processed: number; batches: number };
}
```

**Validation:**

- [ ] Generic class created at `src/mcp/notifications/NotificationDebouncer.ts`
- [ ] Accepts configurable maxBatchSize + maxDelayMs
- [ ] Requires onBatch handler (dependency injection)
- [ ] getStats() returns debouncer metrics
- [ ] Thread-safe (or async-safe for Node.js)

**Test Case:** UT-01 — Debouncer creation + interface validation

---

### AC-2: Batching Logic

**Requirement:** Notifications batched and sent when batch is full OR time limit exceeded.

**Behavior:**

1. enqueue(notification) adds to queue
2. If queue.length >= maxBatchSize → Send batch immediately
3. If no batch sent for maxDelayMs → Send batch automatically (even if < maxBatchSize)
4. Batch handler called with array of notifications

**Example Flow:**

```
t=0ms:    enqueue(notif1) → queue=[notif1]
t=10ms:   enqueue(notif2) → queue=[notif1, notif2]
t=20ms:   enqueue(notif3) → queue=[notif1, notif2, notif3]
          ... enqueue until queue.length=10 (maxBatchSize)
t=50ms:   enqueue(notif10) → queue.length=10 → SEND BATCH [notif1..notif10]
          → onBatch([notif1..notif10]) called
          → queue reset
t=100ms:  timeout expires, queue has remaining items → SEND BATCH [notif11..]
```

**Validation:**

- [ ] Batch sent immediately when size limit hit
- [ ] Batch sent after delay if no size limit hit
- [ ] No batches sent when no notifications queued
- [ ] Timing accurate (± 50ms tolerance for debounce window)
- [ ] Queue properly reset after batch

**Test Case:** UT-02..05 — Various batching scenarios

---

### AC-3: Immediate Flush

**Requirement:** Implement `flush()` method to immediately send pending notifications.

**Behavior:**

- flush() sends any queued notifications immediately (no wait)
- If queue is empty, flush() is no-op
- Useful for critical operations that can't wait (e.g., shutdown, error conditions)

**Example:**

```typescript
debouncer.enqueue(notification1);
debouncer.enqueue(notification2);
await debouncer.flush(); // Send [notification1, notification2] immediately
```

**Validation:**

- [ ] flush() method exists
- [ ] Sends all queued notifications immediately
- [ ] Waits for onBatch handler to complete
- [ ] Returns Promise
- [ ] No-op if queue empty

**Test Case:** UT-06 — Immediate flush

---

### AC-4: Integration with Tool Notifications

**Requirement:** Tool handlers can use debouncer for batch notifications.

**Example:**

```typescript
// Create debouncer for tool notifications
const toolDebouncer = new NotificationDebouncer({
  maxBatchSize: 50,
  maxDelayMs: 100,
  onBatch: async (notifications) => {
    await mcpServer.sendNotifications(notifications);
  }
});

// In tool handler
@Tool()
async bulkProcessTool(input, context: McpContext) {
  for (let i = 0; i < 1000; i++) {
    const result = await processItem(i);

    await toolDebouncer.enqueue({
      type: 'item_processed',
      timestamp: Date.now(),
      data: { item_id: i, result }
    });
  }

  // Ensure all notifications sent before tool returns
  await toolDebouncer.flush();

  return { processed: 1000, notifications_batched: true };
}
```

**Validation:**

- [ ] Debouncer usable in tool handlers
- [ ] Tool can await flush() for ordering guarantees
- [ ] Notification types are simple (type + data)
- [ ] Optional integration with MCP server

**Test Case:** INT-01 — Tool handler with debouncer workflow

---

### AC-5: Performance & Metrics

**Requirement:** Debouncer tracks performance + provides metrics.

**Metrics:**

```typescript
interface DebounceStats {
  queued: number;      // Total notifications queued
  processed: number;   // Total notifications sent in batches
  batches: number;     // Number of batches sent
  avgBatchSize: number;
  totalTimeMs: number; // Time since debouncer created
  savingsEstimate: string; // e.g., "Batched 1000 notifications into 20 batches (95% reduction)"
}
```

**Targets:**

- Batching 1000 notifications into ~20 batches (95% reduction)
- No latency overhead per enqueue (<1ms)
- flush() completes within onBatch handler time + <10ms overhead

**Validation:**

- [ ] getStats() returns accurate counts
- [ ] Batch math correct (savings = (queued - batches) / queued * 100)
- [ ] Timing measured accurately
- [ ] Stats available mid-operation

**Test Case:** UT-07..08 — Metrics tracking + performance validation

---

### AC-6: Configuration & Error Handling

**Requirement:** Debouncer handles configuration + errors gracefully.

**Error Scenarios:**

1. onBatch handler rejects → Log error, continue queuing
2. Debouncer destroyed (in-progress) → Graceful shutdown
3. Invalid notification (missing required fields) → Skip or throw (configurable)

**Configuration:**

```typescript
interface NotificationDebouncerOptions<T> {
  maxBatchSize?: number;      // Default: 10
  maxDelayMs?: number;        // Default: 100
  onBatch: (notifications: T[]) => Promise<void>;
  onError?: (error: Error) => void;  // Optional error handler
  name?: string;              // For logging
}
```

**Validation:**

- [ ] Error handler optional (logs to console if missing)
- [ ] Debouncer continues operating if onBatch fails
- [ ] Configuration validation (no negative delays, etc.)
- [ ] Graceful shutdown (drain queue on destroy)
- [ ] Clear error messages

**Test Case:** UT-09..11 — Error scenarios + configuration

---

## Deliverables

### Code

- [ ] `src/mcp/notifications/NotificationDebouncer.ts` — Debouncer class (200 lines)
- [ ] `src/mcp/notifications/NotificationTypes.ts` — INotification interface (30 lines)
- [ ] `src/tests/unit/notifications/` — Unit tests (350+ lines)
- [ ] `src/tests/integration/debouncer-integration.test.ts` — E2E (200 lines)
- [ ] Update to `src/mcp/index.ts` to export NotificationDebouncer

### Documentation

- [ ] `docs/NOTIFICATION-DEBOUNCING.md` — Usage guide (100 lines)
  - When to use debouncer
  - Configuration options
  - Example: Tool with bulk operations
  - Performance expectations

### Tests (Definition of Done)

- [ ] Unit tests: 11+ test cases (all AC covered)
- [ ] Integration tests: Full debouncer workflows (4+ tests)
- [ ] No regression: Phase 1 + Phase 2 tests (217+) still pass
- [ ] 85%+ code coverage for debouncer module

---

## File Inventory

| File | Type | Purpose | Status |
|:-----|:-----|:---------|:-------|
| `src/mcp/notifications/NotificationDebouncer.ts` | NEW | Debouncer class | Create |
| `src/mcp/notifications/NotificationTypes.ts` | NEW | INotification interface | Create |
| `src/tests/unit/notifications/NotificationDebouncer.test.ts` | NEW | Debouncer unit tests | Create |
| `src/tests/integration/debouncer-integration.test.ts` | NEW | Full workflow tests | Create |
| `docs/NOTIFICATION-DEBOUNCING.md` | NEW | Usage guide | Create |
| `src/mcp/index.ts` | MODIFY | Export NotificationDebouncer | Update |

---

## Technical Approach

### 1. Design Debouncer Algorithm (0.5 hours)

- Queue-based buffering
- Timeout-based flushing (setInterval or setTimeout)
- Size-based flushing
- Two timers: One for overall maxDelay, one for batch processing

### 2. Implement NotificationDebouncer (2 hours)

```typescript
export class NotificationDebouncer<T extends INotification> {
  private queue: T[] = [];
  private timer: NodeJS.Timeout | null = null;
  private stats = { queued: 0, processed: 0, batches: 0 };

  constructor(private options: NotificationDebouncerOptions<T>) {}

  async enqueue(notification: T): Promise<void> {
    this.queue.push(notification);
    this.stats.queued++;

    if (this.queue.length >= this.options.maxBatchSize) {
      await this.sendBatch();
    } else if (!this.timer) {
      this.timer = setTimeout(() => this.sendBatch(), this.options.maxDelayMs);
    }
  }

  private async sendBatch(): Promise<void> {
    const batch = this.queue.splice(0);
    if (batch.length === 0) return;

    clearTimeout(this.timer);
    this.timer = null;

    await this.options.onBatch(batch);
    this.stats.batches++;
    this.stats.processed += batch.length;
  }

  async flush(): Promise<void> {
    await this.sendBatch();
  }
}
```

### 3. Write Tests (2.5 hours)

- Unit: Batching (3), size/delay limits (2), flush (1), metrics (2), errors (3)
- Integration: Full tool workflow (2+)

### 4. Documentation (1 hour)

- Usage guide: When + how to use debouncer
- Configuration reference
- Example: Bulk processing tool
- Performance expectations

---

## Blocked On

| Blocker | Task | Status | Impact |
|:--------|:-----|:-------|:-------|
| Plugin System | TASK-14-03 | ✅ Done | No impact — ready to start |

**No blockers. Can start immediately in parallel with 14-07, 14-08, 14-09.**

---

## Unblocks

- **TASK-14-11** (E2E Tests): Debouncer behavior can be tested end-to-end

---

## Success Criteria Checklist

- [ ] NotificationDebouncer generic class created
- [ ] AC-1: Interface validation passing
- [ ] AC-2: Batching logic tested (size + delay limits)
- [ ] AC-3: flush() method working
- [ ] AC-4: Integration with tool handlers verified
- [ ] AC-5: Performance metrics accurate + targets met
- [ ] AC-6: Config validation + error handling
- [ ] 11+ unit tests, all passing
- [ ] 4+ integration tests, all passing
- [ ] No regression: Phase 1 + Phase 2 tests (217+) still pass
- [ ] 85%+ code coverage for debouncer module
- [ ] Code review approved
- [ ] Merged to feature branch

---

## Effort Breakdown

| Phase | Duration | Notes |
|:------|:---------|:------|
| Algorithm design | 0.5 hours | Queue, timeout, batching logic |
| NotificationDebouncer implementation | 2 hours | Core class + methods |
| Unit tests | 2 hours | 11 test cases (all AC, error paths) |
| Integration tests | 1 hour | Full workflows (4 tests) |
| Documentation | 0.5 hours | Usage guide + reference |
| **Total** | **6 hours** | Done by +2 days |

---

## Definition of Done

- [x] TASK-14-10-IMPLEMENTATION-SUMMARY.md created (post-implementation)
- [x] AC verification matrix (6/6 AC passing)
- [x] Test results: 11 unit + 4 integration, all passing
- [x] Code review sign-off
- [x] Git commit: `feat(TASK-14-10): Notification debouncer for bulk operations`
- [x] Merged to feature branch
- [x] Phase 1 + Phase 2 regression (217+ tests still passing)

---

## Next Tasks After Completion

Parallelize remaining Phase 2:

- **TASK-14-11: E2E Test Suite** (12h, after 14-02 + 14-07 complete)
- **TASK-14-12: Architecture Documentation** (8h, final sprint)
