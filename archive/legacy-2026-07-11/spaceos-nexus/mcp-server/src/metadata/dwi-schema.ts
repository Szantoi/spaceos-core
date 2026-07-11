/**
 * src/metadata/schema.ts — DWI Schema Types
 *
 * Zod runtime schemas for DWI data structures.
 * Standards: database/standards/01-discovery/discovery.work-item.standard.md
 */

import { z } from 'zod';

// ===========================================================================
// DWI Work Item Schema
// ===========================================================================

export const DiscoveryWorkItemSchema = z.object({
  id: z.string().regex(/^dwi-[a-z0-9-]+$/, 'Must be dwi-<kebab-case>'),
  topic: z.string().min(1).max(200),
  status: z.enum(['open', 'in_progress', 'concluded', 'archived']),
  current_phase: z.number().int().min(0).max(4),
  next_action: z.string().min(10).max(500),
  verdict: z.enum(['validated', 'invalidated', 'pivoted']).nullable(),
  hypothesis_count: z.number().int().min(0),
  validated_count: z.number().int().min(0),
  created_at: z.string().datetime(),
  updated_at: z.string().datetime(),
  created_by: z.string().optional(),
  updated_by: z.string().optional(),
});

export type DiscoveryWorkItem = z.infer<typeof DiscoveryWorkItemSchema>;

// ===========================================================================
// DWI Phase Gate Schema
// ===========================================================================

export const DwiPhaseGateSchema = z.object({
  id: z.number().int().optional(),
  dwi_id: z.string().regex(/^dwi-[a-z0-9-]+$/),
  phase: z.number().int().min(0).max(4),
  gate_crossed: z.boolean(),
  gate_crossed_date: z.string().datetime().nullable(),
  notes: z.string().max(500).optional(),
  created_at: z.string().datetime(),
});

export type DwiPhaseGate = z.infer<typeof DwiPhaseGateSchema>;

// ===========================================================================
// DWI Hypothesis Schema
// ===========================================================================

export const DwiHypothesisSchema = z.object({
  id: z.string().regex(/^hyp-\d{3,}$/, 'Must be hyp-NNN'),
  dwi_id: z.string().regex(/^dwi-[a-z0-9-]+$/),
  statement: z.string().min(20).max(1000),
  status: z.enum(['open', 'testing', 'validated', 'invalidated']),
  phase: z.number().int().min(1).max(4),
  artifact_path: z.string().optional(),
  created_at: z.string().datetime(),
  closed_at: z.string().datetime().nullable(),
  created_by: z.string().optional(),
  updated_by: z.string().optional(),
});

export type DwiHypothesis = z.infer<typeof DwiHypothesisSchema>;

// ===========================================================================
// Input Validation Schemas (for mutations)
// ===========================================================================

export const CreateDiscoveryWorkItemSchema = DiscoveryWorkItemSchema.omit({
  id: true,
  created_at: true,
  updated_at: true,
}).extend({
  id: z.string().regex(/^dwi-[a-z0-9-]+$/),
});

export type CreateDiscoveryWorkItem = z.infer<typeof CreateDiscoveryWorkItemSchema>;

export const UpdateDiscoveryWorkItemSchema = DiscoveryWorkItemSchema.partial().extend({
  id: z.string().regex(/^dwi-[a-z0-9-]+$/),
});

export type UpdateDiscoveryWorkItem = z.infer<typeof UpdateDiscoveryWorkItemSchema>;

// ===========================================================================
// Query Response Schemas
// ===========================================================================

export const DwiDashboardResponseSchema = z.object({
  dwi_id: z.string(),
  topic: z.string(),
  status: z.enum(['open', 'in_progress', 'concluded', 'archived']),
  current_phase: z.number().int(),
  next_action: z.string(),
  verdict: z.enum(['validated', 'invalidated', 'pivoted']).nullable(),
  hypothesis_count: z.number(),
  validated_count: z.number(),
  updated_at: z.string().datetime(),
});

export type DwiDashboardResponse = z.infer<typeof DwiDashboardResponseSchema>;
