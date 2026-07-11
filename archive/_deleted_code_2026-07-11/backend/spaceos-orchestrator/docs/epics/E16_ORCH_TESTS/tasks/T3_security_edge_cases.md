# T3 — Security Edge Cases

**Epic:** E16_ORCH_TESTS
**Status:** `CLOSED_DONE`
**Last updated:** 2026-03-28

---

## Scope

Explicit security-oriented tests that verify the three HTTP error codes the security posture depends on: 401 (auth), 422 (validation), and 429 (rate limiting). Supplemented by a summary of all security controls verified across the test suite.

---

## 429 Rate Limit Test

**File:** `src/routes/chat.route.test.ts`

**Strategy:** Rather than sending 20 requests to trigger the production `max: 20` limiter, the test mounts a separate `limitedApp` with `max: 1`. This gives deterministic, fast 429 coverage without relying on the production app singleton.

```typescript
it('POST /bff/chat → 429 when rate limit exceeded', async () => {
  const limiter = rateLimit({ windowMs: 60_000, max: 1, standardHeaders: true, legacyHeaders: false });
  const limitedApp = express();
  limitedApp.use(express.json());
  limitedApp.use('/bff/chat', limiter, chatRouter);

  // First request exhausts the limit
  await request(limitedApp)
    .post('/bff/chat')
    .set('Authorization', `Bearer ${validToken}`)
    .send({ messages: [{ role: 'user', content: 'Hello' }] });

  // Second request must be rejected
  const res = await request(limitedApp)
    .post('/bff/chat')
    .set('Authorization', `Bearer ${validToken}`)
    .send({ messages: [{ role: 'user', content: 'Hello' }] });

  expect(res.status).toBe(429);
});
```

---

## Security Controls Verified — Full Summary

| ID | Control | Where tested | Status |
|----|---------|-------------|--------|
| S1 | Missing `Authorization` header → 401 | `auth.middleware.test.ts` | PASS |
| S2 | Malformed `Authorization` (no `Bearer` prefix) → 401 | `auth.middleware.test.ts` | PASS |
| S3 | JWT signed with wrong key → 401 | `auth.middleware.test.ts` | PASS |
| S4 | Expired JWT → 401 | `auth.middleware.test.ts` | PASS |
| S5 | Malformed token string → 401 | `auth.middleware.test.ts` | PASS |
| S6 | No error detail / stack trace in 401 response | `auth.middleware.test.ts` | PASS |
| S7 | Invalid request body (empty messages) → 422 | `chat.route.test.ts` | PASS |
| S8 | Rate limit exceeded → 429 | `chat.route.test.ts` | PASS |
| S9 | `jwt.verify()` used (not `jwt.decode()`) | `auth.middleware.ts` (code review) | PASS |
| S10 | `env.JWT_SIGNING_KEY` used (not hardcoded secret) | `config/env.ts` + Zod schema | PASS |

---

## Definition of Done

- [x] 429 test added to `chat.route.test.ts`
- [x] All 10 security controls listed and confirmed
- [x] `npm test` → 0 failed after 429 test addition
