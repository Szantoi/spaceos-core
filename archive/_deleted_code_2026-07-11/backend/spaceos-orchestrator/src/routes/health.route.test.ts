// src/routes/health.route.test.ts
import { vi, describe, it, expect, beforeEach } from 'vitest';

vi.mock('axios');

import axios from 'axios';
import request from 'supertest';
import express from 'express';
import { healthRouter } from './health.route';

const mockedAxios = vi.mocked(axios);

const app = express();
app.use(express.json());
app.use('/bff/health', healthRouter);

describe('GET /bff/health', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('kernel unreachable → HTTP 207, orchestrator ok, kernel unreachable, llmProvider mock', async () => {
    mockedAxios.get = vi.fn().mockRejectedValue(new Error('ECONNREFUSED'));

    const res = await request(app).get('/bff/health');

    expect(res.status).toBe(207);
    expect(res.body).toMatchObject({
      orchestrator: 'ok',
      kernel:       'unreachable',
      llmProvider:  'mock',
    });
  });

  it('kernel reachable → HTTP 200, orchestrator ok, kernel ok', async () => {
    mockedAxios.get = vi.fn().mockResolvedValue({ status: 200 });

    const res = await request(app).get('/bff/health');

    expect(res.status).toBe(200);
    expect(res.body).toMatchObject({
      orchestrator: 'ok',
      kernel:       'ok',
    });
  });

  it('timestamp present → valid ISO 8601 string', async () => {
    mockedAxios.get = vi.fn().mockResolvedValue({ status: 200 });

    const res = await request(app).get('/bff/health');

    expect(res.body.timestamp).toMatch(/^\d{4}-\d{2}-\d{2}T/);
  });

  it('llmProvider field → equals "mock"', async () => {
    mockedAxios.get = vi.fn().mockResolvedValue({ status: 200 });

    const res = await request(app).get('/bff/health');

    expect(res.body.llmProvider).toBe('mock');
  });

  it('no manufacturing field in response', async () => {
    mockedAxios.get = vi.fn().mockResolvedValue({ status: 200 });

    const res = await request(app).get('/bff/health');

    expect(res.body).not.toHaveProperty('manufacturing');
  });
});
