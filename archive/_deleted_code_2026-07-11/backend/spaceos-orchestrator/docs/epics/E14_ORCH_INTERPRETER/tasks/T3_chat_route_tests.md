# T3 — Chat Route Tests

**Epic:** E14 — Interpreter Service (Agentic Loop)
**Status:** `CLOSED_DONE`
**File:** `src/routes/chat.route.test.ts`

---

## Spec

Tests for `POST /bff/chat` using supertest against a minimal Express app.
The `interpret()` function and the full `index.ts` bootstrap are both mocked to isolate the route layer.

---

## Test Cases

### 1. No Authorization header → 401

**Input:** `POST /bff/chat` with no `Authorization` header, valid body
**Expected:** HTTP 401, JSON body `{ error: 'Missing or malformed Authorization header.' }`
**Purpose:** Verifies `requireAuth` rejects requests without a Bearer token before the route handler runs.

---

### 2. Invalid JWT → 401

**Input:** `POST /bff/chat` with `Authorization: Bearer invalid.jwt.token`, valid body
**Expected:** HTTP 401, JSON body `{ error: 'Invalid or expired JWT token.' }`
**Purpose:** Verifies `requireAuth` rejects tokens that fail `jwt.verify()`.

---

### 3. Valid JWT + empty messages array → 422

**Input:** `POST /bff/chat` with valid Bearer JWT, body `{ messages: [] }`
**Expected:** HTTP 422
**Purpose:** Verifies Zod schema rejects `messages` arrays with fewer than 1 element before `interpret()` is called.

---

### 4. Valid JWT + valid body → 200 with reply field

**Input:** `POST /bff/chat` with valid Bearer JWT, body `{ messages: [{ role: 'user', content: 'Hello' }] }`
**Expected:** HTTP 200, JSON body containing `{ reply: 'Hello', toolsUsed: [], iterations: 1 }`
**Purpose:** Happy path — verifies the route calls `interpret()` and returns its result as JSON.

---

## Mock Strategy

```typescript
vi.mock('../interpreter/interpreter.service', () => ({
  interpret: vi.fn().mockResolvedValue({ reply: 'Hello', toolsUsed: [], iterations: 1 }),
}));
```

JWT generation for test cases 3 and 4:
```typescript
import jwt from 'jsonwebtoken';
const TEST_KEY = 'test-secret-key-minimum-32-characters-long';
const validToken = jwt.sign({ sub: 'test-user' }, TEST_KEY);
```

The signing key matches `JWT_SIGNING_KEY` set in `vitest.config.ts` test env.

---

## App Setup

A minimal Express app is constructed in the test file — `index.ts` is never imported to avoid server startup side-effects:

```typescript
const app = express();
app.use(express.json());
app.use('/bff/chat', chatRouter);
```

---

## Definition of Done

- [x] All 4 tests pass in `npm test`
- [x] `interpret()` never called on 401 or 422 paths
- [x] No real JWT signing key used (test env key only)
- [x] No TODO/FIXME in test file
