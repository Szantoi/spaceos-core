---
id: MSG-O043
from: architect
to: orchestrator
type: task
priority: P1
date: 2026-04-09
sprint: "Doorstar Onboarding — Orchestrator test coverage"
---

# Orchestrator — joinery.route.test.ts

## Kontextus

A `joinery.route.ts` elkészült (7 pass-through proxy route), de nincs mellé tesztfájl.
A Doorstar Onboarding DoD megköveteli: **≥8 új teszt** az Orchestratorban.
Jelenlegi: 168 teszt (4 új a `doorOrder.route.test.ts`-ből). Kell: 168 + ≥4 több = ≥172.

## Feladat

Hozd létre: `src/routes/joinery.route.test.ts`

### Minta (kövesd a doorOrder.route.test.ts mintáját)

```typescript
// src/routes/joinery.route.test.ts
import { describe, it, expect, vi, beforeEach } from 'vitest';
import request from 'supertest';
import express from 'express';
import jwt from 'jsonwebtoken';
import { jwtKeys } from '../config/jwt-keys';

const { mockAxiosGet, mockAxiosPost } = vi.hoisted(() => ({
  mockAxiosGet:  vi.fn(),
  mockAxiosPost: vi.fn(),
}));

vi.mock('axios', async (importOriginal) => {
  const actual = await importOriginal<typeof import('axios')>();
  return {
    ...actual,
    default: {
      ...actual.default,
      get:  mockAxiosGet,
      post: mockAxiosPost,
      isAxiosError: actual.default.isAxiosError,
    },
  };
});

import { joineryRouter } from './joinery.route';

const app = express();
app.use(express.json());
app.use('/bff/joinery', joineryRouter);

const authToken = jwt.sign(
  { sub: 'user1', roles: ['Designer'], tid: '550e8400-e29b-41d4-a716-446655440001' },
  jwtKeys.signingKey,
  { algorithm: jwtKeys.algorithm, expiresIn: '1h' },
);

const validOrderId = '550e8400-e29b-41d4-a716-446655440099';

beforeEach(() => vi.clearAllMocks());
```

### Tesztek (≥8 db)

```typescript
// 1. GET /bff/joinery/orders → 200
it('GET /orders → proxies to joinery and returns 200', async () => {
  mockAxiosGet.mockResolvedValueOnce({ status: 200, data: { items: [], total: 0 } });
  const res = await request(app)
    .get('/bff/joinery/orders')
    .set('Authorization', `Bearer ${authToken}`);
  expect(res.status).toBe(200);
  expect(mockAxiosGet).toHaveBeenCalledWith(
    expect.stringContaining('/api/orders'),
    expect.objectContaining({ timeout: 10_000 }),
  );
});

// 2. GET /bff/joinery/orders/:id (valid UUID) → 200
it('GET /orders/:id (valid UUID) → 200', async () => {
  mockAxiosGet.mockResolvedValueOnce({ status: 200, data: { id: validOrderId } });
  const res = await request(app)
    .get(`/bff/joinery/orders/${validOrderId}`)
    .set('Authorization', `Bearer ${authToken}`);
  expect(res.status).toBe(200);
});

// 3. GET /bff/joinery/orders/:id (invalid UUID) → 400
it('GET /orders/:id (invalid UUID) → 400', async () => {
  const res = await request(app)
    .get('/bff/joinery/orders/not-a-uuid')
    .set('Authorization', `Bearer ${authToken}`);
  expect(res.status).toBe(400);
  expect(mockAxiosGet).not.toHaveBeenCalled();
});

// 4. GET /bff/joinery/orders/:id/cutting-list → 200
it('GET /orders/:id/cutting-list → proxies correctly', async () => {
  mockAxiosGet.mockResolvedValueOnce({ status: 200, data: { items: [] } });
  const res = await request(app)
    .get(`/bff/joinery/orders/${validOrderId}/cutting-list`)
    .set('Authorization', `Bearer ${authToken}`);
  expect(res.status).toBe(200);
  expect(mockAxiosGet).toHaveBeenCalledWith(
    expect.stringContaining('/cutting-list'), expect.anything(),
  );
});

// 5. GET /bff/joinery/orders/:id/process-plan → 200
it('GET /orders/:id/process-plan → proxies correctly', async () => {
  mockAxiosGet.mockResolvedValueOnce({ status: 200, data: { tasks: [] } });
  const res = await request(app)
    .get(`/bff/joinery/orders/${validOrderId}/process-plan`)
    .set('Authorization', `Bearer ${authToken}`);
  expect(res.status).toBe(200);
});

// 6. POST /bff/joinery/orders/:id/items → 201
it('POST /orders/:id/items → proxies and returns 201', async () => {
  mockAxiosPost.mockResolvedValueOnce({ status: 201, data: { id: 'item-uuid' } });
  const res = await request(app)
    .post(`/bff/joinery/orders/${validOrderId}/items`)
    .set('Authorization', `Bearer ${authToken}`)
    .send({ doorType: 'Falcos', width: 900, height: 2100 });
  expect(res.status).toBe(201);
});

// 7. POST /bff/joinery/orders/:id/submit (invalid UUID) → 400
it('POST /orders/:id/submit (invalid UUID) → 400', async () => {
  const res = await request(app)
    .post('/bff/joinery/orders/bad-id/submit')
    .set('Authorization', `Bearer ${authToken}`)
    .send({});
  expect(res.status).toBe(400);
  expect(mockAxiosPost).not.toHaveBeenCalled();
});

// 8. GET /bff/joinery/orders → 401 ha nincs auth
it('GET /orders → 401 without Authorization', async () => {
  const res = await request(app)
    .get('/bff/joinery/orders');
  expect(res.status).toBe(401);
  expect(mockAxiosGet).not.toHaveBeenCalled();
});
```

## DoD

```bash
cd /opt/spaceos/spaceos.orchestrator
npm test 2>&1 | tail -5
# → ≥176 pass (168 + ≥8 új), 0 fail
```

## Output

Ha kész: `docs/mailbox/orchestrator/outbox/2026-04-09_043_joinery-route-tests-done.md`

Visszajelzés: teszt összesítő (Passed/Failed/total).
