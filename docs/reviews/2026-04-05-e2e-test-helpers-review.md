# Code Review: E2E Test Suite (helpers.ts, preflight.ts, chain tests)
**Date**: 2026-04-05
**Ready for Production**: No
**Critical Issues**: 3
**Total Issues Found**: 16

---

## Priority 1 (Must Fix)

### P1-1. Race condition in token cache (`helpers.ts:11-12`)

When multiple tests run and call `getToken('admin')` concurrently (e.g., from
`beforeAll` hooks across test suites), the check-then-set on the cache is not
atomic. Two concurrent callers can both see `tokenCache[username]` as `undefined`,
both fire a POST to `/bff/auth/token`, and both write to the cache. This wastes
auth requests and, under rate limiting (100 req/min), can cause cascading 429s
that make entire test suites silently skip.

**Fix**: Use an in-flight promise map so the second caller awaits the first
request rather than issuing a duplicate.

```typescript
const inflightTokens: Record<string, Promise<string>> = {};

export async function getToken(username = 'admin'): Promise<string> {
  if (tokenCache[username]) return tokenCache[username];
  if (inflightTokens[username]) return inflightTokens[username];

  inflightTokens[username] = (async () => {
    const res = await fetch(`${BFF}/bff/auth/token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password: 'test' }),
    });
    if (!res.ok) {
      throw new Error(`Auth failed for ${username}: ${res.status} ${await res.text()}`);
    }
    const { token } = (await res.json()) as { token: string };
    tokenCache[username] = token;
    return token;
  })();

  try {
    return await inflightTokens[username];
  } finally {
    delete inflightTokens[username];
  }
}
```

### P1-2. Hardcoded credential: password `'test'` (`helpers.ts:17`)

The password `'test'` is hardcoded in the `getToken` function. While this is a
test file, the same credential is baked into every developer's checkout and CI
pipeline. If the BFF test auth endpoint is ever accidentally exposed (e.g.,
deployed to a staging environment without stripping the test auth route), this
becomes a real credential leak.

**Fix**: Read from an environment variable with a fallback for local-only runs:

```typescript
const TEST_PASSWORD = process.env.E2E_TEST_PASSWORD ?? 'test';
```

### P1-3. Silent test skips mask real failures (all chain tests)

Nearly every chain test uses the pattern:

```typescript
if (!chainWorks) return;
if (!chainWorks || !tenantId) return;
```

When `return` is called inside an `it()` block, Vitest reports the test as
**passed**, not skipped. This means a completely broken chain will show a green
test suite with 100% pass rate. This is a **false negative** -- the most
dangerous kind of test defect.

**Fix**: Use Vitest's `it.skipIf` or call `test.skip()` explicitly:

```typescript
import { describe, it, expect, beforeAll, test } from 'vitest';

it('POST /bff/api/tenants — create tenant', async () => {
  if (!chainWorks) { test.skip(); return; }
  // ...
});
```

Or use `describe.runIf(chainWorks)` at the suite level.

---

## Priority 2 (Should Fix)

### P2-1. Unsafe type assertions without runtime validation (all chain tests)

Every chain test casts `res.data` with `as` without any runtime check:

```typescript
const body = res.data as { items: Array<{ id: string; name: string }> };
```

If the API returns a different shape (e.g., `{ data: [...] }` instead of
`{ items: [...] }`), the test will throw a confusing `TypeError: Cannot read
properties of undefined (reading 'find')` instead of a clear assertion failure.

**Fix**: Assert the structure before using it:

```typescript
const body = res.data as Record<string, unknown>;
expect(body.items).toBeDefined();
expect(Array.isArray(body.items)).toBe(true);
const items = body.items as Array<{ id: string; name: string }>;
```

### P2-2. `kernel()` always parses as JSON (`helpers.ts:72`)

The `kernel()` function calls `res.json().catch(() => null)` unconditionally. If
the kernel health endpoint returns plain text (e.g., `"Healthy"`), this silently
swallows the body and returns `null`. The caller in `01-health.chain.test.ts`
never uses the `data` field, but future callers may be surprised.

**Fix**: Check `content-type` header, same as `bff()` does:

```typescript
const ct = res.headers.get('content-type') ?? '';
const data = ct.includes('application/json')
  ? await res.json()
  : await res.text();
```

### P2-3. No timeout on `bff()` HTTP requests (`helpers.ts:47`)

The `kernel()` function uses `AbortSignal.timeout(3000)`, but the main `bff()`
helper has no timeout at all. If the BFF hangs (e.g., waiting for an LLM
response on the `/bff/chat` endpoint), the test will hang until Vitest's 15s
`testTimeout` kills it, producing an unclear timeout error.

**Fix**: Add a configurable timeout:

```typescript
const res = await fetch(`${BFF}${path}`, {
  method,
  headers,
  body: bodyStr,
  signal: AbortSignal.timeout(10_000),
});
```

### P2-4. Preflight accepts 4xx as healthy (`preflight.ts:10`)

```typescript
if (res.ok || res.status < 500) {
```

This means a 401, 403, or 404 response is considered "healthy". If the health
endpoint requires authentication and returns 401, preflight passes, but then
every subsequent test may fail with auth errors.

**Fix**: Only accept explicitly healthy status codes:

```typescript
if (res.ok) {  // 200-299 only
```

Or if the intent is to also allow 404 on specific paths, be explicit about which
codes are acceptable.

### P2-5. Typo in preflight error message (`preflight.ts:34`)

```
cd /opt/spaceos/SpaceOS.Kerner && dotnet run
```

`SpaceOS.Kerner` should be `SpaceOS.Kernel`.

### P2-6. `bff()` sends `Authorization: Bearer ` with empty string (`helpers.ts:38-39`)

When `02-auth.chain.test.ts` passes `{ token: '' }`, the `bff()` helper sends
`Authorization: Bearer ` (with a trailing space). This tests a very specific
edge case but does not test the "no Authorization header at all" scenario, which
is the more common unauthenticated request. The test comment says "Empty bearer
or no token" but only the former is tested.

### P2-7. Test `02-auth.chain.test.ts:42` accepts both 200 and 401 as passing

```typescript
expect([200, 401]).toContain(res.status);
```

This test can never fail. If the JWT algorithm mismatch is a known issue, it
should be tracked as a skip-with-reason, not a test that passes in both the
broken and working state. As written, it will silently continue passing even
after a regression removes all authentication.

### P2-8. Error response body leaked in error message (`helpers.ts:21`)

```typescript
throw new Error(`Auth failed for ${username}: ${res.status} ${await res.text()}`);
```

If the server returns sensitive data in error responses (stack traces, internal
paths, database errors), these will appear in CI logs. For a test helper this is
acceptable, but the pattern should not be copied to production code.

---

## Priority 3 (Nice to Have)

### P3-1. No test data cleanup

All chain tests create entities (tenants, facilities, epics, work-stations,
space-layers) but only some delete them. Over repeated test runs, the database
accumulates stale test data. This can cause:
- Slow list queries
- Flaky assertions on list lengths
- Disk pressure in CI environments

**Fix**: Add an `afterAll` hook that deletes created entities, or use a test
database that is reset before each run.

### P3-2. `uniqueName` collision risk (`helpers.ts:80-82`)

```typescript
return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 6)}`;
```

The random suffix is only 4 characters from base-36, yielding ~1.7M possible
values. Combined with `Date.now()` this is sufficient, but if two parallel CI
jobs start within the same millisecond, collisions are possible. Consider using
`crypto.randomUUID()` instead.

### P3-3. Inconsistent BFF URL definition

`BFF` is defined as a module-level const in `helpers.ts`, but some test files
redefine it locally:

- `14-chat.chain.test.ts:56`
- `17-gdpr.chain.test.ts:19`
- `13-flowepic-full.chain.test.ts:69`

These bypass the helper to make raw `fetch` calls. If the environment variable
name changes, these will break while the helpers continue working.

**Fix**: Export the `BFF` constant from `helpers.ts` and import it.

### P3-4. `kernel()` helper swallows all errors (`helpers.ts:74-76`)

```typescript
} catch {
  return { status: 0, data: null };
}
```

Network errors, DNS failures, and TLS errors all return `{ status: 0, data: null }`.
Callers cannot distinguish between "service is down" and "network
misconfiguration". Consider at minimum logging the error.

### P3-5. No PATCH method shorthand

The helpers export GET, POST, PUT, DELETE but not PATCH. If any future endpoint
uses PATCH (common for partial updates), tests will need to call `bff('PATCH', ...)`
directly, which is inconsistent.

### P3-6. Missing `res.body` drain on non-JSON responses in `bff()`

When `content-type` is not JSON but the response is large, `res.text()` is
called. This is fine for typical API responses, but if a test accidentally hits a
file-download endpoint, this could consume significant memory. Low risk for E2E
tests.

---

## Summary of Test Quality Concerns

| Category | Count | Severity |
|---|---|---|
| Race conditions | 1 | Critical |
| False negatives (silent skips) | 13 test files | Critical |
| Hardcoded credentials | 1 | Critical |
| Unsafe type casts | ~20 instances | Medium |
| Missing timeouts | 1 | Medium |
| Overly permissive assertions | 2 | Medium |
| No test cleanup | 13 test files | Low |

## Recommended Next Steps

1. Fix the token cache race condition (P1-1) -- this is a correctness bug.
2. Replace silent `return` with `test.skip()` (P1-3) -- otherwise the test
   suite provides no signal when things break.
3. Extract `BFF` base URL from helpers (P3-3) -- single source of truth.
4. Add `AbortSignal.timeout()` to `bff()` (P2-3) -- prevent test hangs.
5. Fix the preflight typo (P2-5) -- trivial, do it now.
