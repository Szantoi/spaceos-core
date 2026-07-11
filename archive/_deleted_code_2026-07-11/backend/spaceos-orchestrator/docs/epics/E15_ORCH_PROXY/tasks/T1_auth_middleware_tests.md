# T1 — Auth Middleware Unit Tests

**Epic:** E15 — Kernel Proxy & Auth Middleware
**Status:** `BACKLOG_READY`
**Test file:** `src/middleware/auth.middleware.test.ts`

---

## Objective

Verify all branches of `requireAuth` using a minimal Express app and Vitest + supertest.
No mocking of `jsonwebtoken` — tests exercise real JWT verification behavior.

---

## Test Cases

| # | Scenario | Input | Expected |
|---|----------|-------|----------|
| 1 | Missing Authorization header entirely | No `Authorization` header | `401 { error: 'Missing or malformed Authorization header.' }` |
| 2 | Header present but without `Bearer ` prefix | `Authorization: token abc` | `401 { error: 'Missing or malformed Authorization header.' }` |
| 3 | Invalid JWT (signed with wrong key) | `Bearer <jwt signed with wrong key>` | `401 { error: 'Invalid or expired JWT token.' }` |
| 4 | Expired JWT | `Bearer <jwt with expiresIn: -1>` | `401 { error: 'Invalid or expired JWT token.' }` |
| 5 | Valid JWT | `Bearer <jwt signed with TEST_KEY>` | `200`, `req.jwtToken` set, `req.jwtPayload` contains `{ sub, role }` |

---

## JWT Fixtures

```typescript
import jwt from 'jsonwebtoken';

// Must match vitest.config.ts env.JWT_SIGNING_KEY
const TEST_KEY = 'test-secret-key-minimum-32-characters-long';

const validToken   = jwt.sign({ sub: 'test-user', role: 'admin' }, TEST_KEY);
const expiredToken = jwt.sign({ sub: 'test-user' }, TEST_KEY, { expiresIn: -1 });
const badToken     = jwt.sign({ sub: 'test-user' }, 'wrong-key-32-chars-minimum-length!');
```

---

## Minimal App Fixture

```typescript
import express from 'express';
import { requireAuth } from './auth.middleware';

const app = express();
app.use(express.json());
app.get('/protected', requireAuth, (req, res) => {
  res.json({ token: (req as any).jwtToken, payload: (req as any).jwtPayload });
});
```

---

## Acceptance Criteria

- [ ] All 5 test cases pass
- [ ] `npm run build` → 0 TypeScript errors
- [ ] `npm test` → 0 failed
- [ ] No `vi.mock('jsonwebtoken')` — real JWT verification only
- [ ] No TODO/FIXME

---

## Definition of Done

All acceptance criteria checked and `npm test` green before T1 is moved to `CLOSED_DONE`.
