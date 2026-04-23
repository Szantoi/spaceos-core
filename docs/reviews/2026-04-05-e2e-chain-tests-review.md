# Code Review: E2E Chain Tests (01-09)
**Date**: 2026-04-05
**Ready for Production**: No
**Critical Issues**: 5
**High Issues**: 8
**Medium Issues**: 7
**Low Issues**: 4

---

## Priority 1 (Must Fix)

### 1. CRITICAL -- Silent test skips disguised as passes (all files 03-09)

Every CRUD and lifecycle test uses the `if (!chainWorks) return;` or `if (!tenantId) return;` pattern. When a test early-returns, Vitest marks it as **PASSED** (green). This means the entire test suite can report 100% pass rate while testing literally nothing.

**Files/Lines**:
- `03-tenant-crud.chain.test.ts` lines 24, 40, 54, 65, 77, 90
- `04-facility-crud.chain.test.ts` lines 27, 38, 49, 57, 65
- `05-flowepic-lifecycle.chain.test.ts` lines 35, 47, 58, 70
- `06-audit-trail.chain.test.ts` lines 25, 36, 56, 68, 80
- `07-role-based-access.chain.test.ts` lines 11, 27
- `08-workstation-spacelayer.chain.test.ts` lines 33, 43, 50, 60, 69, 81, 88
- `09-dashboard.chain.test.ts` line 16

**Fix**: Use `it.skipIf(!chainWorks)(...)` (Vitest built-in) or call `test.skip()` / `context.skip()` inside the test body so the runner reports them as SKIPPED, not PASSED.

### 2. CRITICAL -- Test 02-auth accepts 401 as valid for end-to-end proxy (02-auth.chain.test.ts, line 42)

```typescript
expect([200, 401]).toContain(res.status);
```

This test is titled "authenticated request to BFF proxy reaches Kernel" but it passes when auth is completely broken (401). A test that passes whether the feature works or not provides zero signal. The comment says "known issue" but that means this test can never catch a regression.

### 3. CRITICAL -- RBAC test accepts 429 as substitute for 403 (07-role-based-access.chain.test.ts, line 36)

```typescript
expect([403, 429]).toContain(res.status);
```

The test "Designer cannot create a tenant (403)" will pass if the designer gets 429 (rate limited) instead of 403. A rate-limited request that was never evaluated for authorization is not the same as a denied request. If the RBAC policy is removed entirely, this test still passes as long as rate limiting fires first.

### 4. CRITICAL -- FlowEpic close accepts 404/422 as valid outcomes (05-flowepic-lifecycle.chain.test.ts, line 78)

```typescript
expect([200, 404, 422]).toContain(res.status);
```

The FSM transition test "Delivery -> ClosedDone" passes when the endpoint returns 404 (not found) or 422 (validation error). This defeats the purpose of testing the FSM lifecycle -- a broken endpoint is indistinguishable from a working one.

### 5. CRITICAL -- Cascading silent skips from shared mutable state (03, 04, 05, 08)

Tests within each `describe` block depend on IDs set by prior `it` blocks via module-level `let` variables (`tenantId`, `facilityId`, `epicId`, etc.). If the first test fails or is rate-limited, every subsequent test early-returns silently. The entire file appears green.

**Example in 05-flowepic-lifecycle.chain.test.ts**:
- `beforeAll` creates tenant and facility, storing IDs
- If `POST /tenants` returns 429, `tenantId` is never set
- `POST /facilities` is skipped, `facilityId` is never set
- All 4 `it` blocks early-return, all report PASSED

---

## Priority 2 (Should Fix)

### 6. HIGH -- No cleanup / teardown of test data (all files 03-09)

Files 03-08 create tenants, facilities, workstations, space layers, and flow epics. There is no `afterAll` or `afterEach` to delete/archive this data. Repeated test runs pollute the database with orphan records. File 03 does delete its tenant, but files 04, 05, 06, and 08 create parent tenants in `beforeAll` and never clean them up.

### 7. HIGH -- Unsafe type casting hides response format mismatches (03-tenant-crud.chain.test.ts, line 31; 04, 05, 08)

```typescript
const data = res.data as string | { id: string };
tenantId = typeof data === 'string' ? data.replace(/"/g, '') : data.id;
```

The `as` cast in TypeScript is erased at runtime and provides no validation. If the API returns `{ data: { id: "..." } }` (wrapped) or `null`, the cast silently produces `undefined` which then causes cascading silent skips. The `.replace(/"/g, '')` on a string response is a code smell -- it suggests the response format is not well-understood.

### 8. HIGH -- Verify-chain test accepts 400/404 (06-audit-trail.chain.test.ts, line 85)

```typescript
expect([200, 400, 404]).toContain(res.status);
```

The audit chain integrity test passes when the verify endpoint does not exist (404) or rejects the request (400). This is a security-critical feature (tamper-evident audit log) that should have a strict assertion.

### 9. HIGH -- WorkStation status update accepts 400 (08-workstation-spacelayer.chain.test.ts, line 57)

```typescript
expect([200, 400]).toContain(res.status);
```

Comment says "or 400 if enum mismatch." If the enum value `'Active'` is wrong, the test should fail, not silently accept the error. The test value should match the API contract.

### 10. HIGH -- Facility DELETE accepts 429 (04-facility-crud.chain.test.ts, line 70)

```typescript
expect([204, 429]).toContain(res.status);
```

If the facility was not actually deleted due to rate limiting, the test passes and the facility is left as an orphan. No retry, no skip, no warning.

### 11. HIGH -- Tenant CREATE accepts 429 as success (03-tenant-crud.chain.test.ts, line 28; 07 line 19)

```typescript
expect([201, 429]).toContain(res.status);
```

In file 03, if tenant creation is rate-limited, the test passes but `tenantId` is never set. The test itself passes, but all downstream tests silently skip. In file 07, the admin RBAC test passes even if the tenant was never created, undermining the RBAC validation.

### 12. HIGH -- SpaceLayer DELETE accepts 429 (08-workstation-spacelayer.chain.test.ts, line 93)

Same pattern as issue 10. Test passes without the resource being cleaned up.

### 13. HIGH -- Hardcoded test password in helpers (helpers.ts, line 17)

```typescript
body: JSON.stringify({ username, password: 'test' }),
```

The password `'test'` is hardcoded. If the test environment uses a different password, auth fails and `chainWorks` becomes false, silently skipping all tests. This should be an environment variable.

---

## Priority 3 (Recommended)

### 14. MEDIUM -- Dashboard stats test has no meaningful assertion (09-dashboard.chain.test.ts, lines 21-23)

```typescript
const body = res.data as Record<string, unknown>;
expect(body).toBeDefined();
```

`expect(body).toBeDefined()` only checks that the response is not `undefined`. An empty object `{}`, an error message object, or any non-undefined value passes this test. There is no validation of expected stats fields (tenant count, facility count, etc.).

### 15. MEDIUM -- Audit events filter test passes on empty results (06-audit-trail.chain.test.ts, lines 56-65)

```typescript
const body = res.data as { items: Array<{ eventType: string }> };
for (const event of body.items) {
  expect(event.eventType).toBe('TenantCreated');
}
```

If `body.items` is an empty array, the `for` loop executes zero iterations and the test passes. This means a broken filter that returns no results is indistinguishable from a correct filter. Should assert `body.items.length > 0` first.

### 16. MEDIUM -- Audit date range test has no meaningful assertion (06-audit-trail.chain.test.ts, lines 68-77)

The test sends a date-range filter and only checks that `body.items` is defined. It does not verify that the returned events actually fall within the date range, nor that the list is non-empty.

### 17. MEDIUM -- No timeout on BFF requests (helpers.ts, bff function)

The `bff()` helper does not set `AbortSignal.timeout()`. If the BFF hangs, the entire test suite hangs indefinitely. The `kernel()` helper correctly uses `AbortSignal.timeout(3000)`, but `bff()` does not.

### 18. MEDIUM -- Test execution order dependency not enforced (all describe blocks)

Vitest does not guarantee test execution order within a `describe` by default. Tests like "GET tenant by id" depend on "POST tenant" having run first. While Vitest currently runs tests sequentially within a file by default, this is an implementation detail, not a contract. The `--sequence` flag or `concurrent` could break these tests. The files should either use `test.sequential` or be restructured.

### 19. MEDIUM -- FlowEpic phase assertion uses overly broad matching (05-flowepic-lifecycle.chain.test.ts, lines 55, 67, 83)

```typescript
expect(['Discovery', 'discovery', 1]).toContain(body.phase);
```

Accepting three different representations (PascalCase string, lowercase string, integer) indicates the test authors do not know the actual API contract. If the API changes from returning `1` to returning `"Active"`, this assertion would still pass for the first phase. The test should assert the exact expected value.

### 20. MEDIUM -- Token cache is module-level singleton shared across all tests (helpers.ts, line 8)

```typescript
const tokenCache: Record<string, string> = {};
```

If a token expires mid-run (especially likely in long CI pipelines), all subsequent tests using that cached token will fail with 401, and the `chainWorks` guard will cause silent skips. There is no TTL or expiry check.

---

## Priority 4 (Minor / Informational)

### 21. LOW -- `kernel()` swallows all errors (helpers.ts, lines 74-76)

```typescript
} catch {
  return { status: 0, data: null };
}
```

Network errors, DNS failures, and timeouts are all collapsed into `{ status: 0, data: null }`. The health test in `01-health.chain.test.ts` then asserts `expect(res.status).toBe(200)` which will fail with a confusing message: "expected 0 to be 200". The error context is lost.

### 22. LOW -- Console.warn for known issues creates noise without action (02, 03, 05, 07)

Multiple tests use `console.warn` with emoji to flag known issues. These warnings are easily lost in CI output and are not machine-parseable. Consider using Vitest's `test.todo()` or `test.fails()` for known broken tests.

### 23. LOW -- Inconsistent response parsing between files (03 vs 04/05/08)

File 03 line 31 handles `string | { id: string }`:
```typescript
const data = res.data as string | { id: string };
tenantId = typeof data === 'string' ? data.replace(/"/g, '') : data.id;
```

Files 04, 05, 08 always assume string:
```typescript
tenantId = (tRes.data as string).replace(/"/g, '');
```

If the API returns an object, `.replace()` will throw a runtime error in 04/05/08 but be handled in 03.

### 24. LOW -- `uniqueName` uses `Date.now()` which is not monotonic under mocking (helpers.ts, line 81)

If tests mock time (e.g., for date-range assertions), `uniqueName` could produce collisions. Minor but worth noting.

---

## Summary of Patterns

| Pattern | Count | Severity |
|---------|-------|----------|
| Silent pass-on-skip (no `.skip()`) | 24 instances | CRITICAL |
| Overly broad status arrays `[200, 4xx]` | 9 instances | CRITICAL-HIGH |
| Missing cleanup (no `afterAll`) | 6 files | HIGH |
| Unsafe `as` type casts | 15+ instances | HIGH |
| Vacuous assertions (empty loop, `.toBeDefined()`) | 3 instances | MEDIUM |
| Missing request timeout | 1 instance | MEDIUM |

## Recommended Actions

1. Replace all `if (!condition) return` with `test.skipIf` or `ctx.skip()` -- this is the single highest-impact fix.
2. Remove 429 from all assertion arrays. Instead, add retry logic in the helper or fail explicitly so rate limiting is diagnosed.
3. Remove multi-status assertions like `[200, 401]` and `[200, 404, 422]`. If a test cannot assert a single expected status, it should be marked as `.todo()` or `.fails()`.
4. Add `afterAll` cleanup blocks to delete seed data.
5. Add runtime response validation (e.g., zod schema) instead of `as` casts.
6. Add `AbortSignal.timeout()` to the `bff()` helper.
7. Add `expect(items.length).toBeGreaterThan(0)` before iterating filter results.
