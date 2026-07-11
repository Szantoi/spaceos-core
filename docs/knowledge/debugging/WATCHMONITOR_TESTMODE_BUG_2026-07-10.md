# WatchMonitor TestMode Bug Fix (2026-07-10)

**Task:** MSG-ROOT-048 (related)
**Priority:** HIGH
**Status:** FIXED
**Date:** 2026-07-10

---

## Problem Statement

**Monitor terminal inbox flooded with 469 scheduled-health-check files**, causing the monitor session to become stuck and eventually crash due to context saturation.

### Evidence

```bash
$ ls /opt/spaceos/terminals/monitor/inbox/ | wc -l
469

$ ls /opt/spaceos/terminals/monitor/inbox/ | head -5
2026-07-07_001_scheduled-health-check.md
2026-07-07_002_scheduled-health-check.md
2026-07-07_003_scheduled-health-check.md
...
```

Monitor session stuck at "Waiting..." with 11% context remaining.

---

## Root Cause

**File:** `spaceos-nexus/knowledge-service/src/pipeline/watchMonitor.ts:259`

```typescript
// TEST MODE: Run every cycle (normally every 5th = 10 min, now every 2 min)
// TODO: Revert to `cycleCount % 5 !== 0` after testing
const testMode = true; // ← BUG: Left enabled in production!
```

### Impact

| Mode | Cycle Interval | Health Checks/Hour |
|------|----------------|-------------------|
| Production (testMode=false) | Every 5th cycle (10 min) | 6/hour |
| Test (testMode=true) | Every cycle (2 min) | 30/hour |

**Result:** 5× more inbox messages than expected, overwhelming the monitor terminal.

---

## Fix Applied

```typescript
// Production mode: Run every 5th cycle (10 minutes)
// TEST MODE was causing inbox flooding - fixed 2026-07-10
const testMode = false; // Production mode
```

---

## Prevention Patterns

### 1. Never commit testMode=true

```typescript
// BAD - will cause inbox flooding
const testMode = true;

// GOOD - production default
const testMode = process.env.WATCHMONITOR_TEST_MODE === 'true';
```

### 2. Add TODO scanner warning

Add to pre-commit hook:
```bash
grep -r "testMode = true" src/ && echo "WARNING: testMode enabled!"
```

### 3. Monitor inbox count alert

Add to nightwatch health checks:
```typescript
if (monitorInboxCount > 50) {
  await sendAlert('Monitor inbox flooding detected');
}
```

---

## Related Files

- `spaceos-nexus/knowledge-service/src/pipeline/watchMonitor.ts` (fixed)
- `spaceos-nexus/knowledge-service/src/pipeline/nightwatch.ts` (calls watchMonitor)
- `terminals/monitor/inbox/` (affected directory)

---

## Cleanup Performed

```bash
# Archived 380 .md files
mv *scheduled-health-check*.md ../archive/2026-07-health-checks/

# Archived 85 .completed files
mv *.completed ../archive/2026-07-health-checks/

# Result: Empty inbox, fresh start
```

---

_Knowledge captured by: Root terminal_
_Date: 2026-07-10_
_Ref: MSG-ROOT-048, monitor terminal recovery_
