// src/routes/test.route.test.ts
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';

vi.mock('axios');

import axios from 'axios';
import request from 'supertest';
import express from 'express';
import { testRouter } from './test.route';

const VALID_SECRET = 'test-secret-abc123';
const VALID_TENANT = '2c84d541-4ccf-4b3a-a932-aca21c43a99e';
const OTHER_TENANT = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';

const app = express();
app.use(express.json());
app.use('/bff/test', testRouter);

describe('POST /bff/test/tenants/:tenantId/reset', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER', 'true');
    vi.stubEnv('TEST_SEEDER_SECRET', VALID_SECRET);
    vi.stubEnv('TEST_TENANT_ALLOWLIST', VALID_TENANT);

    // Default: all module delete calls succeed
    vi.mocked(axios.delete).mockResolvedValue({ data: { deletedCounts: { total: 3 } } });
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  // ── Test 1: feature flag disabled → 404 ────────────────────────────────────
  it('feature flag disabled → 404', async () => {
    vi.stubEnv('SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER', 'false');

    const res = await request(app)
      .post(`/bff/test/tenants/${VALID_TENANT}/reset`)
      .set('x-test-seeder-secret', VALID_SECRET)
      .send({ seedProfile: 'empty-v1' });

    expect(res.status).toBe(404);
    expect(res.body.error).toBe('Not found');
  });

  // ── Test 2: wrong secret → 403 ─────────────────────────────────────────────
  it('invalid x-test-seeder-secret → 403', async () => {
    const res = await request(app)
      .post(`/bff/test/tenants/${VALID_TENANT}/reset`)
      .set('x-test-seeder-secret', 'wrong-secret')
      .send({ seedProfile: 'empty-v1' });

    expect(res.status).toBe(403);
    expect(res.body.error).toBe('Forbidden');
    expect(res.body.message).toContain('seeder secret');
  });

  // ── Test 3: missing secret header → 403 ───────────────────────────────────
  it('missing x-test-seeder-secret header → 403', async () => {
    const res = await request(app)
      .post(`/bff/test/tenants/${VALID_TENANT}/reset`)
      .send({ seedProfile: 'empty-v1' });

    expect(res.status).toBe(403);
    expect(res.body.error).toBe('Forbidden');
  });

  // ── Test 4: tenant not in allowlist → 403 ─────────────────────────────────
  it('tenant not in allowlist → 403', async () => {
    const res = await request(app)
      .post(`/bff/test/tenants/${OTHER_TENANT}/reset`)
      .set('x-test-seeder-secret', VALID_SECRET)
      .send({ seedProfile: 'empty-v1' });

    expect(res.status).toBe(403);
    expect(res.body.message).toContain('allowlist');
  });

  // ── Test 5: unknown seedProfile → 400 with available list ────────────────
  it('unknown seedProfile → 400 with available list', async () => {
    const res = await request(app)
      .post(`/bff/test/tenants/${VALID_TENANT}/reset`)
      .set('x-test-seeder-secret', VALID_SECRET)
      .send({ seedProfile: 'nonexistent-profile' });

    expect(res.status).toBe(400);
    expect(res.body.error).toBe('Bad request');
    expect(res.body.message).toContain('nonexistent-profile');
    expect(res.body.available).toContain('empty-v1');
  });

  // ── Test 6: successful reset → 200 with expected shape ────────────────────
  it('valid request → 200 with tenantId, seedProfile, resetAt, deletedCounts, seededEntities', async () => {
    const res = await request(app)
      .post(`/bff/test/tenants/${VALID_TENANT}/reset`)
      .set('x-test-seeder-secret', VALID_SECRET)
      .send({ seedProfile: 'empty-v1' });

    expect(res.status).toBe(200);
    expect(res.body.tenantId).toBe(VALID_TENANT);
    expect(res.body.seedProfile).toBe('empty-v1');
    expect(res.body.resetAt).toMatch(/^\d{4}-\d{2}-\d{2}T/);
    expect(res.body.deletedCounts).toBeDefined();
    expect(res.body.seededEntities).toBeDefined();
  });

  // ── Test 7 (NEW): empty-v1 → seededEntities all zeros, no axios.post ──────
  it('empty-v1 → seededEntities all zeros, no seed HTTP calls', async () => {
    const res = await request(app)
      .post(`/bff/test/tenants/${VALID_TENANT}/reset`)
      .set('x-test-seeder-secret', VALID_SECRET)
      .send({ seedProfile: 'empty-v1' });

    expect(res.status).toBe(200);
    expect(res.body.seededEntities).toEqual({ orders: 0, cuttingSheets: 0, panelStocks: 0, suppliers: 0 });
    expect(vi.mocked(axios.post)).not.toHaveBeenCalled();
  });

  // ── Test 8 (NEW): doorstar-smoke-v1 → seededEntities.orders === 1 ─────────
  it('doorstar-smoke-v1 → seededEntities.orders === 1, 4 axios.post calls', async () => {
    vi.mocked(axios.post)
      .mockResolvedValueOnce({ data: { access_token: 'test-kc-token' } })   // KC token
      .mockResolvedValueOnce({ data: { id: 'facility-uuid-0001' } })         // Facility
      .mockResolvedValueOnce({ data: { id: 'epic-uuid-1234' } })             // FlowEpic
      .mockResolvedValueOnce({ data: { id: 'order-uuid-5678' } });           // DoorOrder

    const res = await request(app)
      .post(`/bff/test/tenants/${VALID_TENANT}/reset`)
      .set('x-test-seeder-secret', VALID_SECRET)
      .send({ seedProfile: 'doorstar-smoke-v1' });

    expect(res.status).toBe(200);
    expect(res.body.seededEntities).toEqual({ orders: 1, cuttingSheets: 0, panelStocks: 0, suppliers: 0 });
    expect(vi.mocked(axios.post)).toHaveBeenCalledTimes(4);
  });

  // ── Test 11 (NEW): doorstar-cutting-ready-v1 → full seed, 12 axios.post calls ─
  it('doorstar-cutting-ready-v1 → orders=1, cuttingSheets=1, panelStocks=5, suppliers=1', async () => {
    vi.mocked(axios.post)
      .mockResolvedValueOnce({ data: { access_token: 'test-kc-token' } })   // KC token
      .mockResolvedValueOnce({ data: { id: 'facility-uuid-0001' } })         // Facility
      .mockResolvedValueOnce({ data: { id: 'epic-uuid-1234' } })             // FlowEpic
      .mockResolvedValueOnce({ data: { id: 'order-uuid-5678' } })            // DoorOrder
      .mockResolvedValueOnce({ data: { id: 'item-uuid-0001' } })             // AddDoorItem (③b)
      .mockResolvedValueOnce({ data: {} })                                    // submit DoorOrder
      .mockResolvedValueOnce({ data: { id: 'sheet-uuid-9999' } })            // CuttingSheet
      .mockResolvedValue({ data: {} });                                       // 5x PanelStock + 1 Supplier

    const res = await request(app)
      .post(`/bff/test/tenants/${VALID_TENANT}/reset`)
      .set('x-test-seeder-secret', VALID_SECRET)
      .send({ seedProfile: 'doorstar-cutting-ready-v1' });

    expect(res.status).toBe(200);
    expect(res.body.seededEntities).toEqual({
      orders: 1,
      cuttingSheets: 1,
      panelStocks: 5,
      suppliers: 1,
    });
    // 1 KC + 1 Facility + 1 FlowEpic + 1 DoorOrder + 1 AddDoorItem + 1 submit + 1 CuttingSheet + 5 PanelStock + 1 Supplier = 13
    expect(vi.mocked(axios.post)).toHaveBeenCalledTimes(13);
  });

  // ── Test 9 (NEW): seed step failure → 502 ─────────────────────────────────
  it('seed step failure → 502', async () => {
    const seedErr = Object.assign(new Error('Joinery down'), {
      response: { status: 503 },
    });
    vi.mocked(axios.post)
      .mockResolvedValueOnce({ data: { access_token: 'test-kc-token' } })   // KC token
      .mockResolvedValueOnce({ data: { id: 'facility-uuid-0001' } })         // Facility
      .mockResolvedValueOnce({ data: { id: 'epic-uuid-1234' } })             // FlowEpic
      .mockRejectedValueOnce(seedErr);                                        // DoorOrder fails
    vi.spyOn(axios, 'isAxiosError').mockReturnValue(true);

    const res = await request(app)
      .post(`/bff/test/tenants/${VALID_TENANT}/reset`)
      .set('x-test-seeder-secret', VALID_SECRET)
      .send({ seedProfile: 'doorstar-smoke-v1' });

    expect(res.status).toBe(502);
    expect(res.body.error).toBe('Seed failed');
  });

  // ── Test 10: module endpoint missing (graceful degradation) → still 200 ───
  it('module delete endpoint returns 404 → graceful degradation, still 200', async () => {
    const axiosError = Object.assign(new Error('Not Found'), {
      response: { status: 404 },
    });
    vi.mocked(axios.delete).mockRejectedValue(axiosError);
    vi.spyOn(axios, 'isAxiosError').mockReturnValue(true);
    // Seed post calls must succeed for the test to reach 200
    vi.mocked(axios.post)
      .mockResolvedValueOnce({ data: { access_token: 'test-kc-token' } })
      .mockResolvedValueOnce({ data: { id: 'facility-uuid-0001' } })
      .mockResolvedValueOnce({ data: { id: 'epic-uuid-1234' } })
      .mockResolvedValueOnce({ data: { id: 'order-uuid-5678' } })
      .mockResolvedValueOnce({ data: {} })
      .mockResolvedValueOnce({ data: { id: 'sheet-uuid-9999' } })
      .mockResolvedValue({ data: {} });

    const res = await request(app)
      .post(`/bff/test/tenants/${VALID_TENANT}/reset`)
      .set('x-test-seeder-secret', VALID_SECRET)
      .send({ seedProfile: 'doorstar-cutting-ready-v1' });

    expect(res.status).toBe(200);
    expect(res.body.deletedCounts.joinery).toMatchObject({ error: expect.stringContaining('404') });
  });
});
