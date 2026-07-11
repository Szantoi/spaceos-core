# T2 — Middleware Unit Tests

**Epic:** E16_ORCH_TESTS
**Status:** `CLOSED_DONE`
**Last updated:** 2026-03-28

---

## Scope

Unit tests for all Express middleware. Each middleware is mounted on a minimal `express()` app and exercised via `supertest`.

---

## Files Covered

| File | Test file | Tests |
|------|-----------|-------|
| `src/middleware/auth.middleware.ts` | `src/middleware/auth.middleware.test.ts` | 9 |

---

## auth.middleware.test.ts — Coverage

### Missing or malformed Authorization header (3 tests)

| # | Scenario | Expected |
|---|----------|----------|
| 1 | No `Authorization` header at all | 401 — `Missing or malformed Authorization header.` |
| 2 | Header without `"Bearer "` prefix (e.g. `token <jwt>`) | 401 — same message |
| 3 | Header value is exactly `"Bearer"` (no space, no token) | 401 — same message |

### Invalid JWT (3 tests)

| # | Scenario | Expected |
|---|----------|----------|
| 4 | JWT signed with wrong key | 401 — `Invalid or expired JWT token.` |
| 5 | Expired JWT (`expiresIn: -1`) | 401 — same message |
| 6 | Malformed token string (not a valid JWT) | 401 — same message |

### Valid JWT (3 tests)

| # | Scenario | Expected |
|---|----------|----------|
| 7 | Valid JWT → calls `next()` | 200 |
| 8 | Valid JWT → `req.jwtToken` set to raw token string | token in response body |
| 9 | Valid JWT → `req.jwtPayload` contains `sub` and `role` claims | payload in response body |

---

## Key Implementation Details Verified

- `requireAuth` uses `jwt.verify()` — not `jwt.decode()` (signature is always checked).
- Algorithm is HS256; signing key comes from `env.JWT_SIGNING_KEY`.
- On failure the middleware responds directly with `res.status(401).json(...)` and does **not** call `next()`.
- On success `req.jwtToken` (raw string) and `req.jwtPayload` (decoded claims) are attached for downstream use.
- No internal error detail or stack trace is exposed in the 401 response body (OWASP A02).

---

## Definition of Done

- [x] `auth.middleware.test.ts` — 9 tests passing
- [x] All three failure categories covered (missing header, invalid token, expired token)
- [x] `req.jwtToken` and `req.jwtPayload` attachment verified on success path
