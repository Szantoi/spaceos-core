// src/interpreter/kernel.action.test.ts
import { vi, describe, it, expect, beforeEach } from 'vitest';

// Use vi.hoisted so the mock instance is available before module evaluation.
// kernel.action.ts calls axios.create() at module scope — the factory mock
// must be registered before that happens.
const mockKernel = vi.hoisted(() => ({
  get:      vi.fn(),
  post:     vi.fn(),
  put:      vi.fn(),
  defaults: { headers: { common: {} as Record<string, string> } },
}));

vi.mock('axios', async (importActual) => {
  const actual = await importActual<typeof import('axios')>();
  return {
    ...actual,
    default: {
      ...actual.default,
      create: vi.fn(() => mockKernel),
      isAxiosError: vi.fn((err: unknown) =>
        !!(err && typeof err === 'object' && (err as Record<string, unknown>).isAxiosError === true),
      ),
    },
  };
});

import { executeToolCall, setKernelAuthToken } from './kernel.action';

// Helper: build a minimal ToolCall
function call(name: string, input: Record<string, string> = {}) {
  return { id: 'test-id', name, input };
}

describe('setKernelAuthToken', () => {
  it('sets the Authorization header on the kernel instance', () => {
    setKernelAuthToken('my-jwt-token');
    expect(mockKernel.defaults.headers.common['Authorization']).toBe('Bearer my-jwt-token');
  });
});

describe('executeToolCall — dispatch table', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  // ── Tenant ────────────────────────────────────────────────────────────

  it('get_all_tenants → GET /api/tenants', async () => {
    const tenants = [{ id: '1', name: 'Acme' }];
    mockKernel.get.mockResolvedValueOnce({ data: tenants });

    const result = await executeToolCall(call('get_all_tenants'));

    expect(mockKernel.get).toHaveBeenCalledWith('/api/tenants');
    expect(JSON.parse(result)).toEqual(tenants);
  });

  it('create_tenant → POST /api/tenants with { name }', async () => {
    const created = { id: '99', name: 'NewCo' };
    mockKernel.post.mockResolvedValueOnce({ data: created });

    const result = await executeToolCall(call('create_tenant', { name: 'NewCo' }));

    expect(mockKernel.post).toHaveBeenCalledWith('/api/tenants', { name: 'NewCo' });
    expect(JSON.parse(result)).toEqual(created);
  });

  // ── Facility ──────────────────────────────────────────────────────────

  it('get_facilities_by_tenant → GET /api/facilities/by-tenant/:tenantId', async () => {
    const facilities = [{ id: 'f1', tenantId: 't1' }];
    mockKernel.get.mockResolvedValueOnce({ data: facilities });

    const result = await executeToolCall(call('get_facilities_by_tenant', { tenantId: 't1' }));

    expect(mockKernel.get).toHaveBeenCalledWith('/api/facilities/by-tenant/t1');
    expect(JSON.parse(result)).toEqual(facilities);
  });

  it('create_facility → POST /api/facilities with { tenantId, name }', async () => {
    const created = { id: 'f2', tenantId: 't1', name: 'Workshop A' };
    mockKernel.post.mockResolvedValueOnce({ data: created });

    const result = await executeToolCall(
      call('create_facility', { tenantId: 't1', name: 'Workshop A' }),
    );

    expect(mockKernel.post).toHaveBeenCalledWith('/api/facilities', {
      tenantId: 't1',
      name: 'Workshop A',
    });
    expect(JSON.parse(result)).toEqual(created);
  });

  // ── WorkStation ───────────────────────────────────────────────────────

  it('get_workstations_by_facility → GET /api/work-stations/by-facility/:facilityId', async () => {
    const workstations = [{ id: 'ws1', facilityId: 'f1' }];
    mockKernel.get.mockResolvedValueOnce({ data: workstations });

    const result = await executeToolCall(
      call('get_workstations_by_facility', { facilityId: 'f1' }),
    );

    expect(mockKernel.get).toHaveBeenCalledWith('/api/work-stations/by-facility/f1');
    expect(JSON.parse(result)).toEqual(workstations);
  });

  it('register_workstation → POST /api/work-stations with { facilityId, name, type }', async () => {
    const created = { id: 'ws2', facilityId: 'f1', name: 'CNC-01', type: 'CNC' };
    mockKernel.post.mockResolvedValueOnce({ data: created });

    const result = await executeToolCall(
      call('register_workstation', { facilityId: 'f1', name: 'CNC-01', type: 'CNC' }),
    );

    expect(mockKernel.post).toHaveBeenCalledWith('/api/work-stations', {
      facilityId: 'f1',
      name: 'CNC-01',
      type: 'CNC',
    });
    expect(JSON.parse(result)).toEqual(created);
  });

  it('update_workstation_status → PUT /api/work-stations/:id/status', async () => {
    const updated = { id: 'ws1', status: 'Active' };
    mockKernel.put.mockResolvedValueOnce({ data: updated });

    const result = await executeToolCall(
      call('update_workstation_status', { workStationId: 'ws1', status: 'Active' }),
    );

    expect(mockKernel.put).toHaveBeenCalledWith('/api/work-stations/ws1/status', {
      status: 'Active',
    });
    expect(JSON.parse(result)).toEqual(updated);
  });

  // ── SpaceLayer ────────────────────────────────────────────────────────

  it('get_spacelayers_by_facility → GET /api/space-layers/by-facility/:facilityId', async () => {
    const layers = [{ id: 'sl1', facilityId: 'f1' }];
    mockKernel.get.mockResolvedValueOnce({ data: layers });

    const result = await executeToolCall(
      call('get_spacelayers_by_facility', { facilityId: 'f1' }),
    );

    expect(mockKernel.get).toHaveBeenCalledWith('/api/space-layers/by-facility/f1');
    expect(JSON.parse(result)).toEqual(layers);
  });

  it('register_spacelayer → POST /api/space-layers with correct body', async () => {
    const created = { id: 'sl2' };
    mockKernel.post.mockResolvedValueOnce({ data: created });

    const result = await executeToolCall(
      call('register_spacelayer', {
        facilityId: 'f1',
        tradeType: 'Joinery',
        isExternalNode: 'false',
        intentDataJson: '{}',
      }),
    );

    expect(mockKernel.post).toHaveBeenCalledWith('/api/space-layers', {
      facilityId: 'f1',
      tradeType: 'Joinery',
      isExternalNode: false,
      externalSourceUrl: null,
      intentDataJson: '{}',
    });
    expect(JSON.parse(result)).toEqual(created);
  });

  it('update_spacelayer_intent → PUT /api/space-layers/:id/intent', async () => {
    const updated = { id: 'sl1' };
    mockKernel.put.mockResolvedValueOnce({ data: updated });

    const result = await executeToolCall(
      call('update_spacelayer_intent', {
        spaceLayerId: 'sl1',
        intentDataJson: '{"key":"value"}',
      }),
    );

    expect(mockKernel.put).toHaveBeenCalledWith('/api/space-layers/sl1/intent', {
      intentDataJson: '{"key":"value"}',
    });
    expect(JSON.parse(result)).toEqual(updated);
  });

  // ── FlowEpic ──────────────────────────────────────────────────────────

  it('get_flowepics_by_facility → GET /api/flow-epics/by-facility/:facilityId', async () => {
    const epics = [{ id: 'e1', facilityId: 'f1' }];
    mockKernel.get.mockResolvedValueOnce({ data: epics });

    const result = await executeToolCall(
      call('get_flowepics_by_facility', { facilityId: 'f1' }),
    );

    expect(mockKernel.get).toHaveBeenCalledWith('/api/flow-epics/by-facility/f1');
    expect(JSON.parse(result)).toEqual(epics);
  });

  it('create_flowepic → POST /api/flow-epics with { facilityId, workStationId, title }', async () => {
    const created = { id: 'e2' };
    mockKernel.post.mockResolvedValueOnce({ data: created });

    const result = await executeToolCall(
      call('create_flowepic', {
        facilityId: 'f1',
        workStationId: 'ws1',
        title: 'Build Cabinet',
      }),
    );

    expect(mockKernel.post).toHaveBeenCalledWith('/api/flow-epics', {
      facilityId: 'f1',
      workStationId: 'ws1',
      title: 'Build Cabinet',
    });
    expect(JSON.parse(result)).toEqual(created);
  });

  it('start_flowepic_execution → PUT /api/flow-epics/:id/start', async () => {
    const updated = { id: 'e1', status: 'IN_DEV' };
    mockKernel.put.mockResolvedValueOnce({ data: updated });

    const result = await executeToolCall(
      call('start_flowepic_execution', { epicId: 'e1' }),
    );

    expect(mockKernel.put).toHaveBeenCalledWith('/api/flow-epics/e1/start');
    expect(JSON.parse(result)).toEqual(updated);
  });

  it('delegate_flowepic → PUT /api/flow-epics/:id/delegate with { guestTenantId }', async () => {
    const updated = { id: 'e1', delegatedTo: 'gt1' };
    mockKernel.put.mockResolvedValueOnce({ data: updated });

    const result = await executeToolCall(
      call('delegate_flowepic', { epicId: 'e1', guestTenantId: 'gt1' }),
    );

    expect(mockKernel.put).toHaveBeenCalledWith('/api/flow-epics/e1/delegate', {
      guestTenantId: 'gt1',
    });
    expect(JSON.parse(result)).toEqual(updated);
  });

  // ── Error handling ────────────────────────────────────────────────────

  it('unknown tool name → returns { error: true, message: "Unknown tool: ..." }', async () => {
    const result = await executeToolCall(call('non_existent_tool'));

    const parsed = JSON.parse(result);
    expect(parsed.error).toBe(true);
    expect(parsed.message).toBe('Unknown tool: non_existent_tool');
    expect(mockKernel.get).not.toHaveBeenCalled();
    expect(mockKernel.post).not.toHaveBeenCalled();
    expect(mockKernel.put).not.toHaveBeenCalled();
  });

  it('axios 4xx error → returns { error: true, status: 404 }', async () => {
    const axiosError = {
      isAxiosError: true,
      response: { status: 404, data: { message: 'Tenant not found' } },
      message: 'Not Found',
    };
    mockKernel.get.mockRejectedValueOnce(axiosError);

    const result = await executeToolCall(call('get_all_tenants'));

    const parsed = JSON.parse(result);
    expect(parsed.error).toBe(true);
    expect(parsed.status).toBe(404);
  });

  it('axios 5xx error → returns { error: true, status: 500 }', async () => {
    const axiosError = {
      isAxiosError: true,
      response: { status: 500, data: { message: 'Kernel crashed' } },
      message: 'Internal Server Error',
    };
    mockKernel.get.mockRejectedValueOnce(axiosError);

    const result = await executeToolCall(call('get_all_tenants'));

    const parsed = JSON.parse(result);
    expect(parsed.error).toBe(true);
    expect(parsed.status).toBe(500);
  });
});
