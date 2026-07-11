// src/interpreter/kernel.action.ts
// Translates LLM tool_call names → C# Kernel API calls.
// This is the ONLY file that knows the Kernel's URL structure.

import axios from 'axios';
import { env } from '../config/env';
import { kernelClient, setKernelJwt, KernelClientError } from '../kernel/kernelClient';
import type { ToolCall } from '../types/llm.types';

const kernel = axios.create({
  baseURL: env.KERNEL_BASE_URL,
  timeout: 10_000,
});

/** Forward the JWT token from the original request to the Kernel (axios + KernelClient). */
export function setKernelAuthToken(token: string): void {
  kernel.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  setKernelJwt(token);
}

/** Forward the X-SpaceOS-Brand header from the original request to the Kernel. */
export function setKernelBrand(brand: string | undefined): void {
  if (brand) {
    kernel.defaults.headers.common['X-SpaceOS-Brand'] = brand;
  } else {
    delete kernel.defaults.headers.common['X-SpaceOS-Brand'];
  }
}

/**
 * Executes a single tool call against the C# Kernel.
 * Returns a string result that will be fed back to the LLM as tool_result.
 */
export async function executeToolCall(call: ToolCall): Promise<string> {
  const i = call.input as Record<string, string>;

  try {
    const result = await dispatch(call.name, i);
    return JSON.stringify(result);
  } catch (err: unknown) {
    // KernelClientError: re-throw — handled in interpreter.service.ts (agentic loop continues)
    if (err instanceof KernelClientError) {
      throw err;
    }
    if (axios.isAxiosError(err)) {
      const status = err.response?.status;
      const detail = err.response?.data;
      return JSON.stringify({ error: true, status, detail });
    }
    return JSON.stringify({ error: true, message: String(err) });
  }
}

// ─── Dispatch table ──────────────────────────────────────────────────────────

async function dispatch(
  toolName: string,
  i: Record<string, string>,
): Promise<unknown> {
  switch (toolName) {
    // ── Tenant ────────────────────────────────────────────────────────────
    case 'get_all_tenants':
      return (await kernel.get('/api/tenants')).data;

    case 'create_tenant':
      return (await kernel.post('/api/tenants', { name: i.name })).data;

    // ── Facility ──────────────────────────────────────────────────────────
    case 'get_facilities_by_tenant':
      return (await kernel.get(`/api/facilities/by-tenant/${i.tenantId}`)).data;

    case 'create_facility':
      return (await kernel.post('/api/facilities', { tenantId: i.tenantId, name: i.name })).data;

    // ── WorkStation ───────────────────────────────────────────────────────
    case 'get_workstations_by_facility':
      return (await kernel.get(`/api/work-stations/by-facility/${i.facilityId}`)).data;

    case 'register_workstation':
      return (await kernel.post('/api/work-stations', {
        facilityId: i.facilityId,
        name: i.name,
        type: i.type,
      })).data;

    case 'update_workstation_status':
      return (await kernel.put(`/api/work-stations/${i.workStationId}/status`, {
        status: i.status,
      })).data;

    // ── SpaceLayer ────────────────────────────────────────────────────────
    case 'get_spacelayers_by_facility':
      return (await kernel.get(`/api/space-layers/by-facility/${i.facilityId}`)).data;

    case 'register_spacelayer':
      return (await kernel.post('/api/space-layers', {
        facilityId:        i.facilityId,
        tradeType:         i.tradeType,
        isExternalNode:    i.isExternalNode === 'true',
        externalSourceUrl: i.externalSourceUrl ?? null,
        intentDataJson:    i.intentDataJson ?? null,
      })).data;

    case 'update_spacelayer_intent':
      return (await kernel.put(`/api/space-layers/${i.spaceLayerId}/intent`, {
        intentDataJson: i.intentDataJson,
      })).data;

    // ── FlowEpic ──────────────────────────────────────────────────────────
    case 'get_flowepics_by_facility':
      return (await kernel.get(`/api/flow-epics/by-facility/${i.facilityId}`)).data;

    case 'create_flowepic':
      return (await kernel.post('/api/flow-epics', {
        facilityId:    i.facilityId,
        workStationId: i.workStationId,
        title:         i.title,
      })).data;

    case 'start_flowepic_execution':
      return (await kernel.put(`/api/flow-epics/${i.epicId}/start`)).data;

    case 'delegate_flowepic':
      return (await kernel.put(`/api/flow-epics/${i.epicId}/delegate`, {
        guestTenantId: i.guestTenantId,
      })).data;

    // ── Phase 2 query tools (KernelClient — typed errors, native fetch) ──────
    // Endpoints: GET /api/tools/* — TenantId sourced from JWT claim (not query param).

    case 'list_flow_epics':
      return kernelClient.get('/api/tools/flow-epics', {
        page:     i.page     ?? '1',
        pageSize: i.pageSize ?? '20',
      });

    case 'list_facilities':
      return kernelClient.get('/api/tools/facilities', {
        page:     i.page     ?? '1',
        pageSize: i.pageSize ?? '20',
      });

    case 'get_workstation_summary':
      return kernelClient.get('/api/tools/workstations', {
        page:     i.page     ?? '1',
        pageSize: i.pageSize ?? '50',
      });

    case 'get_tenant_summary':
      return kernelClient.get('/api/tools/summary');

    default:
      return { error: true, message: `Unknown tool: ${toolName}` };
  }
}
