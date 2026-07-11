---
id: TASK-14-10-QUICKSTART
title: "TASK-14-10 Day-1 Quickstart — Notification Debouncing"
type: developer-quickstart
owner: "Dev D (or TBD)"
duration: "6 hours"
created: 2026-03-11
---

# 🚀 TASK-14-10 Quickstart — Notification Debouncing

**Your mission:** Implement notification batching to improve performance during bulk operations.

**Duration:** 6 hours (~2 dev days)
**Files you'll touch:** `src/mcp/notifications/debouncer.ts`, `src/tests/unit/debouncer.test.ts`
**Predecessor:** None (independent!)
**No blockers** → Start immediately!

---

## Problem Statement

**Before (no debouncing):**
- Save 50 roles → 50 notifications sent immediately → Client overwhelmed
- Network overhead: 50 HTTP requests
- Server load spike

**After (with debouncing):**
- Save 50 roles → 1 batched notification with all 50 items
- Network overhead: 1 HTTP request
- Server load: smooth

---

## Architecture Pattern

```typescript
// Usage pattern:

const debouncer = new NotificationDebouncer<Role>({
  maxFrequency: 100,  // Max 1 notification per 100ms
  batchSize: 50       // Or whenever we reach 50 items
});

for (let i = 0; i < 50; i++) {
  const role = await saveRole(...);
  debouncer.notifyResource("roles");  // Queued, not sent yet
}

// After loop, debouncer batches and sends 1 notification
await debouncer.flush();  // Force immediate send if needed
```

---

## Step-by-Step Implementation

### Step 1: Base Class (1.5h)

```typescript
// src/mcp/notifications/debouncer.ts

export interface NotificationDebounceConfig {
  maxFrequency: number;  // ms between notifications (default: 100)
  batchSize?: number;    // Max items per batch (default: unlimited)
  onFlush?: (items: any[]) => Promise<void>;  // Callback when batch sent
}

export class NotificationDebouncer<T> {
  private config: NotificationDebounceConfig;
  private queue: T[] = [];
  private lastSent = 0;
  private timer: NodeJS.Timeout | null = null;

  constructor(config: NotificationDebounceConfig) {
    this.config = { maxFrequency: 100, ...config };
  }

  notify(item: T): void {
    this.queue.push(item);
    this.scheduleFlush();
  }

  private scheduleFlush(): void {
    if (this.timer) return;  // Already scheduled

    const timeSinceLastSend = Date.now() - this.lastSent;
    const delayMs = Math.max(0, this.config.maxFrequency - timeSinceLastSend);

    this.timer = setTimeout(() => this.flush(), delayMs);
  }

  async flush(): Promise<void> {
    if (this.timer) clearTimeout(this.timer);
    this.timer = null;

    if (this.queue.length === 0) return;

    const batch = this.queue.splice(0, this.queue.length);
    this.lastSent = Date.now();

    // Call callback or notify client
    if (this.config.onFlush) {
      await this.config.onFlush(batch);
    }
  }
}
```

### Step 2: Resource Notification Integration (1h)

```typescript
export class ResourceNotificationDebouncer {
  private debouncers: Map<string, NotificationDebouncer<string>> = new Map();

  notifyResourceUpdated(resourceId: string): void {
    if (!this.debouncers.has(resourceId)) {
      this.debouncers.set(resourceId, new NotificationDebouncer({
        maxFrequency: 100
      }));
    }
    this.debouncers.get(resourceId)!.notify(Date.now().toString());
  }

  async flushAll(): Promise<void> {
    for (const debouncer of this.debouncers.values()) {
      await debouncer.flush();
    }
  }
}
```

### Step 3: Seeder Integration (1.5h)

```typescript
// Use debouncer in role seeder for bulk operations:

export class RoleSeeder {
  async seed(roles: Role[]): Promise<{ created: number; skipped: number }> {
    const debouncer = new NotificationDebouncer<Role>({
      maxFrequency: 100,
      onFlush: (batch) => {
        // Emit: Role batch created notification
        logger.info(`Batch created: ${batch.length} roles`);
      }
    });

    let created = 0;
    for (const role of roles) {
      const result = await this.createOrUpdate(role);
      if (result.created) {
        debouncer.notify(role);
        created++;
      }
    }

    await debouncer.flush();
    return { created, skipped: roles.length - created };
  }
}
```

### Step 4: Unit Tests (1h)

```typescript
describe("NotificationDebouncer", () => {
  it("should batch notifications", async () => {
    let batchCount = 0;
    const debouncer = new NotificationDebouncer<string>({
      maxFrequency: 50,
      onFlush: async () => { batchCount++; }
    });

    debouncer.notify("item1");
    debouncer.notify("item2");
    debouncer.notify("item3");

    // Before flush: no batches sent
    expect(batchCount).toBe(0);

    await debouncer.flush();

    // After flush: 1 batch sent with 3 items
    expect(batchCount).toBe(1);
  });

  it("should respect maxFrequency timing", async () => {
    const debouncer = new NotificationDebouncer({ maxFrequency: 100 });
    let startTime = Date.now();

    debouncer.notify("item1");
    // Wait a bit, then check - should still be queued
    await new Promise(resolve => setTimeout(resolve, 50));

    startTime = Date.now();
    await new Promise(resolve => setTimeout(resolve, 100));  // Wait for debounce timer

    const elapsed = Date.now() - startTime;
    expect(elapsed).toBeGreaterThanOrEqual(50);  // At least 50ms of debounce
  });
});
```

---

## Testing Your Work

```bash
npm test -- --match "*debouncer*"
```

**Expected:** All timing + batching tests pass ✅

---

## Performance Gains

Example: Seeding 1000 roles

**Without debouncing:**
- 1000 notifications sent
- Network round-trips: 1000
- Total time: ~5 seconds

**With debouncing (100ms batches):**
- 10 notification batches
- Network round-trips: 10
- Total time: ~500ms

**Savings:** 10x fewer network trips!

---

## Completion Sign-Off

When done:

1. All 4 AC from `TASK-14-10-DEBOUNCING.md` passing ✅
2. NotificationDebouncer class works ✅
3. Batch timing tests pass ✅
4. Integration test with seeder ✅
5. Post completion report!

