// src/kernel/kernelClient.test.ts
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { KernelClient, KernelClientError, KernelErrorCode } from './kernelClient';

const TEST_BASE_URL = 'http://localhost:5001';
const TEST_JWT = 'test-jwt-token';

function makeClient(): KernelClient {
  return new KernelClient(TEST_BASE_URL, () => TEST_JWT);
}

function mockFetchOk(body: unknown = {}): void {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: true,
    status: 200,
    json: () => Promise.resolve(body),
  }));
}

function mockFetchStatus(status: number): void {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: false,
    status,
    json: () => Promise.resolve({}),
  }));
}

describe('KernelClient.get()', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('200 → returns parsed JSON body', async () => {
    const data = [{ id: '1', name: 'Test Tenant' }];
    mockFetchOk(data);

    const result = await makeClient().get('/api/query/flow-epics', { tenantId: 'abc' });

    expect(result).toEqual(data);
  });

  it('401 → throws KernelClientError with AuthExpired code', async () => {
    mockFetchStatus(401);

    await expect(makeClient().get('/api/test')).rejects.toMatchObject({
      name:       'KernelClientError',
      code:       KernelErrorCode.AuthExpired,
      httpStatus: 401,
    });
  });

  it('429 → throws KernelClientError with RateLimited code', async () => {
    mockFetchStatus(429);

    await expect(makeClient().get('/api/test')).rejects.toMatchObject({
      code:       KernelErrorCode.RateLimited,
      httpStatus: 429,
    });
  });

  it('503 → throws KernelClientError with Unavailable code', async () => {
    mockFetchStatus(503);

    await expect(makeClient().get('/api/test')).rejects.toMatchObject({
      code:       KernelErrorCode.Unavailable,
      httpStatus: 503,
    });
  });

  it('400 → throws KernelClientError with BadRequest code', async () => {
    mockFetchStatus(400);

    await expect(makeClient().get('/api/test')).rejects.toMatchObject({
      code:       KernelErrorCode.BadRequest,
      httpStatus: 400,
    });
  });

  it('network error → throws KernelClientError with Unavailable code', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('ECONNREFUSED')));

    await expect(makeClient().get('/api/test')).rejects.toMatchObject({
      code:       KernelErrorCode.Unavailable,
      httpStatus: 0,
    });
  });

  it('DOMException TimeoutError → throws KernelClientError with Timeout code', async () => {
    const timeoutError = new DOMException('Signal timed out', 'TimeoutError');
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(timeoutError));

    await expect(makeClient().get('/api/test')).rejects.toMatchObject({
      code:       KernelErrorCode.Timeout,
      httpStatus: 0,
    });
  });

  it('passes query params as URL search params', async () => {
    const mockFetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve([]),
    });
    vi.stubGlobal('fetch', mockFetch);

    await makeClient().get('/api/query/tenant-summary', { tenantId: 'tenant-uuid-123', page: 1 });

    const [calledUrl] = mockFetch.mock.calls[0] as [URL, RequestInit];
    expect(calledUrl.searchParams.get('tenantId')).toBe('tenant-uuid-123');
    expect(calledUrl.searchParams.get('page')).toBe('1');
  });

  it('sends JWT in Authorization header', async () => {
    const mockFetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve({}),
    });
    vi.stubGlobal('fetch', mockFetch);

    await makeClient().get('/api/test');

    const [, options] = mockFetch.mock.calls[0] as [URL, RequestInit];
    expect((options.headers as Record<string, string>).Authorization).toBe(`Bearer ${TEST_JWT}`);
  });

  it('null params are skipped from search params', async () => {
    const mockFetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve({}),
    });
    vi.stubGlobal('fetch', mockFetch);

    await makeClient().get('/api/test', { present: 'yes', absent: null, alsoAbsent: undefined });

    const [calledUrl] = mockFetch.mock.calls[0] as [URL, RequestInit];
    expect(calledUrl.searchParams.get('present')).toBe('yes');
    expect(calledUrl.searchParams.has('absent')).toBe(false);
    expect(calledUrl.searchParams.has('alsoAbsent')).toBe(false);
  });
});
