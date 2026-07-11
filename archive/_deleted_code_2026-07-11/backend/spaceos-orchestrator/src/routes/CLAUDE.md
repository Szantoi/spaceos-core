# CLAUDE.md — src/routes/

**Module:** Express route handlers — thin layer only
**Rule:** Routes validate input and delegate. No business logic here.

---

## What lives here

| File | Purpose |
|---|---|
| `chat.route.ts` | `POST /bff/chat` — Zod validate → `interpret()` → JSON response |
| `auth.route.ts` | `POST /bff/auth/logout` (proxy), `GET /bff/auth/me` (identity from JWT claims) |
| `health.route.ts` | `GET /bff/health` — orchestrator + Kernel liveness check |

## Rules

- Every handler: `try { ... } catch (err) { next(err); }` — no unhandled rejections
- Input validated with Zod `safeParse()` before touching any data
- On `422`: return `parsed.error.flatten()` — never let invalid data reach the interpreter
- No `await` on anything that isn't wrapped in try/catch
- Route files export a `Router` — they never call `app.listen()`
- No `res.send()` — always `res.json()` or `res.status(N).json()`

## Adding a new route

1. Create `[name].route.ts` exporting a `Router`
2. Define a Zod schema for the request body
3. Apply `requireAuth` if the route needs authentication
4. Mount in `index.ts` under `/bff/[name]`
5. Document in `docs/epics/` if it's part of a new epic

## Test pattern

```typescript
// Use supertest against the Express app
import request from 'supertest';
import app from '../../index';

it('POST /bff/chat → 422 on empty messages', async () => {
  const res = await request(app)
    .post('/bff/chat')
    .set('Authorization', 'Bearer valid.jwt.here')
    .send({ messages: [] });
  expect(res.status).toBe(422);
});
```
