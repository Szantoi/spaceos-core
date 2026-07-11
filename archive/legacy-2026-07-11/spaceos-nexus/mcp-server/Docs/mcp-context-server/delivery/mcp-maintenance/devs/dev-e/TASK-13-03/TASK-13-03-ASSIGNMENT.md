---
title: "Dev E — TASK-13-03 Assignment Sheet"
subtitle: "Discovery Tools Implementation — request_context, reference_prior_discovery"
created: 2026-03-08
updated: 2026-03-09
assigned_to: "Dev E"
priority: "P0"
epic: "EPIC-13"
phase: "M02 — Phase 1: Discovery Track Setup"
status: "✅ READY (after TASK-13-02)"
effort_estimate: "16 hours"
ac_count: 3
---

# 🚀 Dev E — TASK-13-03 Assignment

**Task:** TASK-13-03 (Discovery Tools Implementation)
**Epic:** EPIC-13 (Discovery Track Tools)
**Priority:** P0
**Effort Estimate:** 16 hours (~2 days)

---

## 🎯 Your Mission

Implement the **core discovery MCP tools**:

1. `request_context(phase)` → Returns phase-specific workflow + artifact templates
2. `reference_prior_discovery(search_text, phase)` → Semantic search over discovery episodes (EPIC-12 integration)

These tools enable discovery agents to get contextual guidance and learn from past discoveries.

---

## 📋 Acceptance Criteria (3 AC)

### AC-1: request_context() Tool ✅

Tool Schema:

```
Input: phase (enum: ideation, validation, iteration, delivery_handoff)
Output: { workflow_template, artifact_templates, phase_checklist, available_tools }
```

- [ ] Returns correct templates for requested phase
- [ ] Enforces phase access control (based on role)
- [ ] Error handling for invalid phases

### AC-2: reference_prior_discovery() Tool ✅

Tool Schema:

```
Input: search_text, phase (optional)
Output: { episodes: [{ id, phase, summary, artifacts }], total_found }
```

- [ ] Uses EPIC-12 ChromaDB semantic search
- [ ] Filters episodes by track="discovery"
- [ ] Returns top 5 similar episodes
- [ ] Performance: < 200ms

### AC-3: Tool Integration ✅

- [ ] Both tools registered in MCP server
- [ ] RBAC enforcement: discovery-only access
- [ ] E2E test: call both tools in discovery workflow
- [ ] 10+ unit tests

---

## 🛠️ Implementation Details

### Tool 1: request_context

```typescript
export async function request_context(input: {
  phase: "ideation" | "validation" | "iteration" | "delivery_handoff"
}): Promise<{
  workflow_template: string;
  artifact_templates: Array<{ name: string; content: string }>;
  phase_checklist: string[];
  available_tools: string[];
}> {
  // 1. Load phase template from database/roles/discovery/*/workflows/
  // 2. Check user role has access to phase
  // 3. Return all templates + checklist + tools for phase
  // 4. Error handling: invalid phase, access denied
}
```

### Tool 2: reference_prior_discovery

```typescript
export async function reference_prior_discovery(input: {
  search_text: string;
  phase?: "ideation" | "validation" | "iteration";
  limit?: number;
}): Promise<Array<{
  episode_id: string;
  phase: string;
  summary: string;
  artifacts: Array<{ title: string; type: string }>;
  similarity_score: number;
}>> {
  // 1. Use EPIC-12 ChromaDB semantic search
  // 2. Filter: track="discovery" only
  // 3. Optional phase filter
  // 4. Return top N (default 5)
  // 5. Performance: < 200ms
}
```

---

## 📁 Files to Create

- `src/mcp/tools/discovery.ts` — Tool handlers
- `src/mcp/tools/discoveryTypes.ts` — TypeScript types
- `src/tests/unit/DiscoveryTools.test.ts` — 10+ unit tests
- `src/tests/integration/DiscoveryTools.integration.test.ts` — E2E tests

---

## 📞 Definition of Done

- [ ] All 3 AC passing
- [ ] 10+ unit tests green
- [ ] Tools registered in MCP server
- [ ] Performance benchmarks met
- [ ] Ready for peer review
