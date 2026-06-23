# Dispatch Control Testing Guide

> **Status:** TESTED (2026-06-23)
> **Test Suite:** 69 tests (57 unit + 12 integration)
> **Coverage:** Token Budget, Proposals, Windows, Alerts

## Test Structure

```
src/__tests__/
├── unit/
│   ├── tokenBudget.test.ts        # 24 tests
│   ├── dispatchProposal.test.ts   # 14 tests
│   └── scheduledWindows.test.ts   # 19 tests
└── integration/
    └── budgetAlerts.test.ts       # 12 tests
```

## Running Tests

```bash
cd spaceos-nexus/knowledge-service

# All Dispatch Control tests
npx vitest run src/__tests__/unit/tokenBudget.test.ts \
  src/__tests__/unit/dispatchProposal.test.ts \
  src/__tests__/unit/scheduledWindows.test.ts \
  src/__tests__/integration/budgetAlerts.test.ts

# Individual test suites
npx vitest run src/__tests__/unit/tokenBudget.test.ts
npx vitest run src/__tests__/integration/budgetAlerts.test.ts
```

## Test Results Summary

| Test File | Tests | Duration |
|-----------|-------|----------|
| tokenBudget.test.ts | 24 | ~50ms |
| dispatchProposal.test.ts | 14 | ~30ms |
| scheduledWindows.test.ts | 19 | ~40ms |
| budgetAlerts.test.ts | 12 | ~44ms |
| **Total** | **69** | **~165ms** |

---

## Unit Tests

### 1. Token Budget (`tokenBudget.test.ts`)

**24 tests** covering:

| Category | Tests | What's Tested |
|----------|-------|---------------|
| Dispatch Mode | 2 | Default mode, mode switching |
| Token Usage Recording | 3 | Single record, accumulation, by-model tracking |
| Budget Status | 4 | Status queries, threshold transitions |
| Daily Summary | 1 | Multi-terminal aggregation |
| Dispatch Check | 5 | Mode enforcement, budget limits, priority reserve |
| Budget Configuration | 2 | Set/update terminal limits |
| Dispatch Queue | 5 | Queue CRUD, priority ordering |
| Usage Statistics | 2 | Global and per-terminal stats |

**Key Test Cases:**

```typescript
// Budget status transitions
it('should show critical status at 90%', () => {
  recordUsage('backend', 9000);
  expect(getBudgetStatus('backend').status).toBe('critical');
});

// Priority reserve for critical tasks
it('should allow critical priority to use reserve', () => {
  recordUsage('backend', 10000); // depleted
  const check = checkCanDispatch('backend', 1000, 'critical');
  expect(check.allowed).toBe(true);
  expect(check.reason).toContain('priority reserve');
});
```

### 2. Dispatch Proposals (`dispatchProposal.test.ts`)

**14 tests** covering:

| Category | Tests | What's Tested |
|----------|-------|---------------|
| Proposal Creation | 2 | ID generation, required fields |
| Proposal Queries | 2 | Get by ID, list pending |
| Proposal Decisions | 4 | Approve, reject, status updates |
| Bulk Operations | 2 | Approve all, expire old |
| Statistics | 2 | Counts, rates |
| Edge Cases | 2 | Double approval, invalid IDs |

**Key Test Cases:**

```typescript
// Approval workflow
it('should approve proposal', async () => {
  createProposal({ terminal: 'backend', taskId: 'MSG-001' });
  const result = await decideProposal({ proposalId, decision: 'approve' });
  expect(result.status).toBe('approved');
});

// Bulk operations
it('should approve all pending proposals', () => {
  createProposal({ terminal: 'backend', taskId: 'MSG-001' });
  createProposal({ terminal: 'frontend', taskId: 'MSG-002' });
  const { approved } = approveAllPending('root');
  expect(approved).toBe(2);
});
```

### 3. Scheduled Windows (`scheduledWindows.test.ts`)

**19 tests** covering:

| Category | Tests | What's Tested |
|----------|-------|---------------|
| Window Management | 4 | Add, remove, update windows |
| Default Mode | 2 | Manual/auto fallback |
| Window Detection | 4 | Current window, multiple windows, day-of-week |
| Terminal Permissions | 4 | Allowed list, max sessions |
| Session Tracking | 3 | Register, end, multi-terminal |
| Time Edge Cases | 2 | Boundaries, overnight windows |

**Key Test Cases:**

```typescript
// Terminal permission check
it('should reject terminal not in allowed list', () => {
  addWindow({
    name: 'Dev Session',
    allowedTerminals: ['backend', 'frontend'],
    ...
  });
  const check = checkWindowForTerminal('conductor');
  expect(check.terminalAllowed).toBe(false);
});

// Overnight window handling
it('should handle overnight windows', () => {
  addWindow({ startTime: '22:00', endTime: '02:00', ... });
  expect(isTimeInRange('23:00', '22:00', '02:00')).toBe(true);
  expect(isTimeInRange('01:00', '22:00', '02:00')).toBe(true);
  expect(isTimeInRange('03:00', '22:00', '02:00')).toBe(false);
});
```

---

## Integration Tests

### Budget Alerts (`budgetAlerts.test.ts`)

**12 tests** covering the complete alert workflow:

| Category | Tests | What's Tested |
|----------|-------|---------------|
| Threshold Detection | 4 | No alert <80%, alerts at 80/90/100% |
| Alert Deduplication | 2 | No duplicates, progressive alerts |
| Telegram Notification | 1 | Mock telegram calls |
| Content Validation | 2 | Terminal name, token counts |
| Edge Cases | 3 | Exact boundaries, >100%, threshold jumping |

**Key Test Cases:**

```typescript
// Progressive threshold alerts
it('should progress through thresholds correctly', () => {
  recordUsage('test-terminal', 8000);  // 80%
  checkAndCreateAlert('test-terminal');

  recordUsage('test-terminal', 1000);  // 90%
  checkAndCreateAlert('test-terminal');

  recordUsage('test-terminal', 1000);  // 100%
  checkAndCreateAlert('test-terminal');

  const alerts = getAlerts('test-terminal');
  expect(alerts[0].alert_type).toBe('threshold_80');
  expect(alerts[1].alert_type).toBe('threshold_90');
  expect(alerts[2].alert_type).toBe('budget_depleted');
});

// Telegram notification verification
it('should send telegram notification for each threshold', () => {
  recordUsage('test-terminal', 8500);
  checkAndCreateAlert('test-terminal');
  expect(mockTelegram).toHaveBeenCalledWith(
    expect.stringContaining('📊 80%')
  );
});
```

---

## Manual API Testing

### 1. Service Health Check

```bash
curl localhost:3456/health
# Expected: {"status":"ok",...}
```

### 2. Budget Threshold Test

```bash
# Check initial state
curl localhost:3456/api/control/budget/architect

# Record 80% usage → warning alert
curl -X POST localhost:3456/api/control/usage \
  -H "Content-Type: application/json" \
  -d '{"terminal":"architect","tokensUsed":8000}'

# Verify alert in database
sqlite3 data/dispatch.db "SELECT * FROM budget_alerts WHERE terminal='architect'"
```

### 3. Proposal Workflow Test

```bash
# Create proposal (as conductor)
curl -X POST localhost:3456/api/control/proposals \
  -H "Authorization: Bearer dev-token-conductor-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","taskId":"MSG-001","reason":"UNREAD inbox"}'

# Approve (as root)
curl -X POST localhost:3456/api/control/proposals/PROP-xxx/approve \
  -H "Authorization: Bearer dev-token-root-2026"

# Verify queue
curl localhost:3456/api/control/queue
```

### 4. Scheduled Windows Test

```bash
# Load default windows
curl -X POST localhost:3456/api/control/windows/load-defaults

# Check current window
curl localhost:3456/api/control/windows

# Check terminal permission
curl localhost:3456/api/control/windows/check/explorer
```

---

## Test Database

All tests use **in-memory SQLite** for isolation:

```typescript
let testDb: Database.Database;

function resetTestDb() {
  testDb = new Database(':memory:');
  testDb.exec(`CREATE TABLE ...`);
}
```

Production database: `data/dispatch.db`

---

## Verification Checklist

After changes, verify:

- [ ] All 69 tests pass (`npx vitest run`)
- [ ] Service starts without errors
- [ ] Budget thresholds trigger alerts
- [ ] Proposals require correct auth tokens
- [ ] Windows filter terminals correctly
- [ ] Nightwatch respects dispatch mode

---

## Related Documentation

- Implementation: `DISPATCH_CONTROL_IMPLEMENTATION.md`
- Design: `DISPATCH_CONTROL_DESIGN.md`
- API Reference: See implementation guide
