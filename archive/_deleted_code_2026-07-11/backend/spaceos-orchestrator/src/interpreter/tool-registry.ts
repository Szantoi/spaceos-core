// src/interpreter/tool-registry.ts
// Curated tool list for the DesignPortal persona.
// The LLM learns the Kernel API from THIS file — not from Swagger.
// Add/remove tools here to control exactly what the LLM can do.

import type { ToolSchema } from '../types/llm.types';

export const DESIGN_PORTAL_TOOLS: ToolSchema[] = [
  // ── Tenant ────────────────────────────────────────────────────────────────
  {
    name: 'get_all_tenants',
    description: 'List all tenants in the system.',
    input_schema: { type: 'object', properties: {}, required: [] },
  },
  {
    name: 'create_tenant',
    description: 'Create a new tenant (organization/company).',
    input_schema: {
      type: 'object',
      properties: {
        name: { type: 'string', description: 'Tenant name, max 100 characters.' },
      },
      required: ['name'],
    },
  },

  // ── Facility ──────────────────────────────────────────────────────────────
  {
    name: 'get_facilities_by_tenant',
    description: 'List all facilities belonging to a tenant.',
    input_schema: {
      type: 'object',
      properties: {
        tenantId: { type: 'string', description: 'UUID of the tenant.' },
      },
      required: ['tenantId'],
    },
  },
  {
    name: 'create_facility',
    description: 'Create a new physical facility (workshop, office, factory floor) for a tenant.',
    input_schema: {
      type: 'object',
      properties: {
        tenantId: { type: 'string', description: 'UUID of the owning tenant.' },
        name:     { type: 'string', description: 'Facility name, max 100 characters.' },
      },
      required: ['tenantId', 'name'],
    },
  },

  // ── WorkStation ───────────────────────────────────────────────────────────
  {
    name: 'get_workstations_by_facility',
    description: 'List all workstations in a facility.',
    input_schema: {
      type: 'object',
      properties: {
        facilityId: { type: 'string', description: 'UUID of the facility.' },
      },
      required: ['facilityId'],
    },
  },
  {
    name: 'register_workstation',
    description: 'Register a new workstation (CNC machine, assembly table, design PC) in a facility.',
    input_schema: {
      type: 'object',
      properties: {
        facilityId: { type: 'string', description: 'UUID of the facility.' },
        name:       { type: 'string', description: 'Workstation name.' },
        type:       { type: 'string', description: 'Type of workstation (e.g. CNC, AssemblyTable, DesignStation).' },
      },
      required: ['facilityId', 'name', 'type'],
    },
  },
  {
    name: 'update_workstation_status',
    description: 'Change the operational status of a workstation.',
    input_schema: {
      type: 'object',
      properties: {
        workStationId: { type: 'string', description: 'UUID of the workstation.' },
        status: {
          type: 'string',
          description: 'New status.',
          enum: ['Idle', 'Active', 'Maintenance', 'Offline'],
        },
      },
      required: ['workStationId', 'status'],
    },
  },

  // ── SpaceLayer ────────────────────────────────────────────────────────────
  {
    name: 'get_spacelayers_by_facility',
    description: 'List all SpaceLayers (trade layers: Joinery, MEP, etc.) for a facility.',
    input_schema: {
      type: 'object',
      properties: {
        facilityId: { type: 'string', description: 'UUID of the facility.' },
      },
      required: ['facilityId'],
    },
  },
  {
    name: 'register_spacelayer',
    description: 'Register a new SpaceLayer for a specific trade type on a facility.',
    input_schema: {
      type: 'object',
      properties: {
        facilityId:  { type: 'string', description: 'UUID of the facility.' },
        tradeType:   {
          type: 'string',
          description: 'Trade discipline.',
          enum: ['Joinery', 'MEP', 'Electrical', 'Architecture', 'Generic'],
        },
        isExternalNode:    { type: 'boolean', description: 'True if data is hosted on an external SpaceOS node.' },
        externalSourceUrl: { type: 'string',  description: 'API URL of the external node (only if isExternalNode=true).' },
        intentDataJson:    { type: 'string',  description: 'Initial JSON intent data (optional).' },
      },
      required: ['facilityId', 'tradeType'],
    },
  },
  {
    name: 'update_spacelayer_intent',
    description: 'Update the JSON intent data of a SpaceLayer.',
    input_schema: {
      type: 'object',
      properties: {
        spaceLayerId:   { type: 'string', description: 'UUID of the SpaceLayer.' },
        intentDataJson: { type: 'string', description: 'Valid JSON string representing the new intent data.' },
      },
      required: ['spaceLayerId', 'intentDataJson'],
    },
  },

  // ── FlowEpic ──────────────────────────────────────────────────────────────
  {
    name: 'get_flowepics_by_facility',
    description: 'List all FlowEpics (work packages) for a facility.',
    input_schema: {
      type: 'object',
      properties: {
        facilityId: { type: 'string', description: 'UUID of the facility.' },
      },
      required: ['facilityId'],
    },
  },
  {
    name: 'create_flowepic',
    description: 'Create a new FlowEpic (work package / manufacturing job) assigned to a workstation.',
    input_schema: {
      type: 'object',
      properties: {
        facilityId:    { type: 'string', description: 'UUID of the facility.' },
        workStationId: { type: 'string', description: 'UUID of the workstation responsible for this epic.' },
        title:         { type: 'string', description: 'Descriptive title of the epic.' },
      },
      required: ['facilityId', 'workStationId', 'title'],
    },
  },
  {
    name: 'start_flowepic_execution',
    description: 'Transition a FlowEpic from BACKLOG_READY to IN_DEV state.',
    input_schema: {
      type: 'object',
      properties: {
        epicId: { type: 'string', description: 'UUID of the FlowEpic.' },
      },
      required: ['epicId'],
    },
  },
  {
    name: 'delegate_flowepic',
    description: 'Delegate a FlowEpic to another tenant (B2B Handshake). The guest tenant will receive this epic as their own project.',
    input_schema: {
      type: 'object',
      properties: {
        epicId:        { type: 'string', description: 'UUID of the FlowEpic to delegate.' },
        guestTenantId: { type: 'string', description: 'UUID of the tenant to delegate to.' },
      },
      required: ['epicId', 'guestTenantId'],
    },
  },

  // ── Phase 2 query tools ────────────────────────────────────────────────────
  // Kernel endpoint group: GET /api/tools/* — TenantId is sourced from the JWT claim.
  // No tenantId param needed: the Kernel extracts it from the Bearer token automatically.

  {
    name: 'list_flow_epics',
    description: 'List active FlowEpics for the authenticated tenant across all facilities. Use this for a tenant-level overview of ongoing work packages.',
    input_schema: {
      type: 'object',
      properties: {
        page:     { type: 'number', description: 'Page number (default: 1).' },
        pageSize: { type: 'number', description: 'Results per page (default: 20, max: 100).' },
      },
      required: [],
    },
  },
  {
    name: 'list_facilities',
    description: 'List all facilities for the authenticated tenant. Use this for a quick overview before drilling into a specific facility.',
    input_schema: {
      type: 'object',
      properties: {
        page:     { type: 'number', description: 'Page number (default: 1).' },
        pageSize: { type: 'number', description: 'Results per page (default: 20, max: 100).' },
      },
      required: [],
    },
  },
  {
    name: 'get_workstation_summary',
    description: 'List workstations for the authenticated tenant with their current status (Idle, Active, Maintenance, Offline).',
    input_schema: {
      type: 'object',
      properties: {
        page:     { type: 'number', description: 'Page number (default: 1).' },
        pageSize: { type: 'number', description: 'Results per page (default: 50, max: 100).' },
      },
      required: [],
    },
  },
  {
    name: 'get_tenant_summary',
    description: 'Get aggregate counts for the authenticated tenant: total FlowEpics, active workstations, and pending B2B handshakes.',
    input_schema: {
      type: 'object',
      properties: {},
      required: [],
    },
  },
];
