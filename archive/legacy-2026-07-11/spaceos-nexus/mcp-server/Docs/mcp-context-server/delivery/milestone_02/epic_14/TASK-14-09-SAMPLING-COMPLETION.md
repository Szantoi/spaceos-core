---
id: TASK-14-09
title: "TASK-14-09: Sampling & Argument Completion"
epic: EPIC-14
phase: "Phase 2: Advanced Features"
type: task
created: 2026-03-11
status: "✅ COMPLETE"
effort: "10 hours (~3 days)"
owner: "TBD"
---

# TASK-14-09: Sampling & Argument Completion

## Overview

Implement **LLM-assisted sampling** and **argument completion** for MCP tools. When a tool has many optional arguments or ambiguous parameters, the tool can request the LLM to **clarify or sample** the arguments before execution.

**Status:** ✅ COMPLETE — Sampling/clarification implemented with tests
**Owner:** TBD (Backend developer with LLM integration experience)
**Duration:** 10 hours (~3 development days)
**Predecessor:** TASK-14-03 (Plugin System foundation)
**Successor:** TASK-14-11 (E2E tests can verify sampling flow)
**Blockers:** None — Can start in parallel with 14-07, 14-08, 14-10

---

## Problem Statement

Some MCP tools have **complex argument spaces**:

- Many optional parameters
- Conditional requirements (if A then B required)
- Semantic interdependencies (parameter X constrains parameter Y)

Currently:

- Clients must fully specify all parameters upfront
- Error messages on missing required args are reactive
- Complex tools can't delegate parameter clarification

**Goal:** Enable tools to **proactively request clarification** from the LLM before execution. Tool sends a `SamplingRequest` which the LLM processes and returns refined arguments.

---

## Acceptance Criteria

### AC-1: SamplingRequest & SamplingResponse Types

**Requirement:** Define request/response types for argument sampling.

**Input:** None (type definitions)

**Output:**

```typescript
interface SamplingRequest {
  id: string;                    // Unique ID for this sampling request
  tool_name: string;             // Which tool is requesting
  agent_id: string;              // Which agent is executing
  current_args: Record<string, any>;  // Args provided so far
  missing_params: string[];      // Parameter names that need values
  constraints: Record<string, string>;  // "param_a: must be > 0"
  context: string;               // "Briefly explain what we're trying to do"
  session_id: string;            // From McpContext
}

interface SamplingResponse {
  id: string;                    // Matches SamplingRequest.id
  sampled_args: Record<string, any>;  // LLM-provided args
  reasoning: string;             // LLM reasoning for sampling
  confidence: number;            // 0.0 to 1.0
  alternatives?: Array<Record<string, any>>; // Optional: 2-3 alternatives
}
```

**Validation:**

- [ ] Types created at `src/mcp/sampling/SamplingTypes.ts`
- [ ] All fields documented with JSDoc
- [ ] Serializable to JSON (no circular refs)
- [ ] Version field optional (for future compatibility)

**Test Case:** UT-01 — Type validation + serialization

---

### AC-2: Sampling Request Handler

**Requirement:** Implement `SamplingRequestHandler` to send sampling requests to LLM.

**Input:**

```typescript
const handler = new SamplingRequestHandler(llmClient);
const response = await handler.requestSampling(request);
```

**Behavior:**

- Constructs a prompt from SamplingRequest
- Calls LLM client to generate sampled arguments
- Parses LLM response into SamplingResponse
- Validates sampled args against constraints
- Returns SamplingResponse or throws SamplingError

**Validation:**

- [ ] SamplingRequestHandler created
- [ ] Accepts LLMClient interface (dependency injection)
- [ ] Prompt construction includes all context
- [ ] LLM response parsing robust (handles malformed JSON)
- [ ] Constraint validation prevents invalid sampling
- [ ] Error handling: If LLM fails, graceful degradation (return original args)

**Test Case:** UT-02..04 — Happy path, LLM error handling, constraint validation

---

### AC-3: Tool Integration: requestSampling() Method

**Requirement:** Tools can call `context.requestSampling(request)` to get clarified arguments.

**Flow:**

1. Tool receives request with missing/ambiguous args
2. Tool creates SamplingRequest (specifies missing params, constraints)
3. Tool calls `context.requestSampling(request)`
4. LLM samples arguments
5. Tool returns SamplingResponse to client

**Example:**

```typescript
@Tool()
async complexTool(input, context: McpContext) {
  const { param_a, param_b, param_c } = input;

  // Missing optional params?
  if (!param_b || !param_c) {
    const samplingRequest: SamplingRequest = {
      id: uuid(),
      tool_name: 'complex_tool',
      agent_id: input.agent_id,
      current_args: input,
      missing_params: [!param_b ? 'param_b' : undefined, !param_c ? 'param_c' : undefined].filter(Boolean),
      constraints: {
        'param_b': 'must be a number between 0 and 100',
        'param_c': 'must be one of: "alpha", "beta", "gamma"'
      },
      context: 'We are configuring a similarity search with thresholds.',
      session_id: context.session_id
    };

    const response = await context.requestSampling(samplingRequest);
    Object.assign(input, response.sampled_args);
  }

  // Now execute with complete args
  return this.execute(input);
}
```

**Validation:**

- [ ] McpContext has requestSampling() method
- [ ] Method accepts SamplingRequest parameter
- [ ] Returns Promise<SamplingResponse>
- [ ] ID tracking ensures response matches request
- [ ] Session ID validated (RBAC)

**Test Case:** INT-01 — Full sampling flow (request → LLM → response)

---

### AC-4: Sampling Request Types & Patterns

**Requirement:** Support multiple sampling request types.

**Types:**

1. **CLARIFY** — LLM provides best guess for ambiguous params
2. **SAMPLE** — LLM generates multiple alternatives
3. **VALIDATE** — LLM checks if args satisfy constraints

**Example:**

```typescript
// Type 1: CLARIFY — give me best guess
{ type: 'CLARIFY', missing_params: ['threshold'] }

// Type 2: SAMPLE — give me 3 options
{ type: 'SAMPLE', missing_params: ['algorithm'], alternatives_count: 3 }

// Type 3: VALIDATE — check if args are valid
{ type: 'VALIDATE', current_args: { param_a: 50, param_b: 'invalid'} }
```

**Validation:**

- [ ] Type enum created: CLARIFY, SAMPLE, VALIDATE
- [ ] Prompts differ per type
- [ ] Handler routes to appropriate LLM strategy
- [ ] Response includes correct number of alternatives

**Test Case:** UT-05..07 — One test per sampling type

---

### AC-5: Performance & Caching

**Requirement:** Sampling requests don't add significant latency.

**Targets:**

- Sampling request → LLM call → response: < 500ms (P99)
- Cached responses (same params + agent) reused for 5 minutes
- Fallback: If LLM times out > 1s, return original args + warning

**Validation:**

- [ ] SamplingRequestHandler has optional cache (RedisClient or in-memory)
- [ ] Cache key: `sampling:{tool_name}:{hash(missing_params)}:{agent_id}`
- [ ] TTL: 5 minutes
- [ ] Timeout: 1 second hard limit, graceful degradation
- [ ] Metrics: Track sampling latency + cache hits

**Test Case:** UT-08..09 — Cache hit, timeout + fallback

---

### AC-6: Logging & Audit Trail

**Requirement:** All sampling requests logged for auditability.

**Log Entry:**

```json
{
  "level": "info",
  "timestamp": "2026-03-11T15:30:45Z",
  "type": "sampling_request",
  "tool_name": "complex_tool",
  "agent_id": "agent-123",
  "session_id": "sess-456",
  "request_id": "req-789",
  "status": "completed",
  "lats_ms": 245,
  "sampled_params": ["param_b", "param_c"],
  "llm_confidence": 0.92
}
```

**Validation:**

- [ ] Every sampling request logged
- [ ] Session ID + agent ID tracked
- [ ] LLM confidence score recorded
- [ ] Latency metrics captured
- [ ] Errors logged with reason
- [ ] Log output compatible with structured logging (JSON)

**Test Case:** UT-10 — Sampling request generates correct log entry

---

## Deliverables

### Code

- [ ] `src/mcp/sampling/SamplingTypes.ts` — Type definitions (80 lines)
- [ ] `src/mcp/sampling/SamplingRequestHandler.ts` — RequestHandler impl (150 lines)
- [ ] `src/mcp/sampling/SamplingCache.ts` — Optional caching layer (100 lines)
- [ ] `src/mcp/context/McpContext.ts` — Add requestSampling() method (30 lines)
- [ ] `src/tests/unit/sampling/` — Unit tests (400+ lines)
- [ ] `src/tests/integration/sampling-integration.test.ts` — E2E (250 lines)

### Documentation

- [ ] `docs/SAMPLING.md` — Developer guide (150 lines)
  - How to add sampling to a tool
  - API reference
  - Example: complex tool with sampling
- [ ] `docs/SAMPLING-API.md` — Complete API reference (50 lines)
  - SamplingRequest structure
  - SamplingResponse structure
  - Error codes

### Tests (Definition of Done)

- [ ] Unit tests: 10+ test cases
- [ ] Integration tests: Full sampling workflows (4+ tests)
- [ ] No regression: Phase 1 + Phase 2 tests (217) still pass
- [ ] 80%+ code coverage for sampling module

---

## File Inventory

| File | Type | Purpose | Status |
|:-----|:-----|:---------|:-------|
| `src/mcp/sampling/SamplingTypes.ts` | NEW | Type definitions | Create |
| `src/mcp/sampling/SamplingRequestHandler.ts` | NEW | Request handler | Create |
| `src/mcp/sampling/SamplingCache.ts` | NEW | Response caching | Create |
| `src/mcp/context/McpContext.ts` | MODIFY | Add requestSampling() | Update |
| `src/tests/unit/sampling/SamplingTypes.test.ts` | NEW | Type validation | Create |
| `src/tests/unit/sampling/SamplingRequestHandler.test.ts` | NEW | Handler tests | Create |
| `src/tests/unit/sampling/SamplingCache.test.ts` | NEW | Cache tests | Create |
| `src/tests/integration/sampling-integration.test.ts` | NEW | Full workflows | Create |
| `docs/SAMPLING.md` | NEW | Developer guide | Create |
| `docs/SAMPLING-API.md` | NEW | API reference | Create |

---

## Technical Approach

### 1. Define Type System (1 hour)

```typescript
// Types for sampling requests/responses
export enum SamplingType { CLARIFY, SAMPLE, VALIDATE }
export interface SamplingRequest { ... }
export interface SamplingResponse { ... }
```

### 2. Implement SamplingRequestHandler (3 hours)

- Prompt engineering: Construct LLM prompt from SamplingRequest
- LLM call: Invoke LLMClient
- Response parsing: Extract sampled args from LLM response
- Constraint validation: Check sampled args against constraints
- Error handling: Timeout, malformed response, constraint violation

### 3. Add McpContext Method (1 hour)

- McpContext.requestSampling(request): Promise<SamplingResponse>
- Delegation to SamplingRequestHandler
- Session ID validation (RBAC)

### 4. Implement Optional Caching (1 hour)

- Cache key: `sampling:{tool}:{hash(params)}:{agent}`
- TTL: 5 minutes
- Hit/miss metrics

### 5. Write Tests (2 hours)

- Unit: Types, handler (happy + error paths), cache, logging (10 cases)
- Integration: Full sampling flow (4+ cases)

### 6. Documentation (2 hours)

- Developer guide: How to add sampling to a tool
- API reference: Type definitions, error codes
- Example with complex tool

---

## Blocked On

| Blocker | Task | Status | Impact |
|:--------|:-----|:-------|:-------|
| Plugin System | TASK-14-03 | ✅ Done | No impact — ready to start |

**No blockers. Can start immediately in parallel with 14-07, 14-08, 14-10.**

---

## Unblocks

- **TASK-14-11** (E2E Tests): Sampling flows can be tested end-to-end

---

## Success Criteria Checklist

- [ ] SamplingRequest & SamplingResponse types created + validated
- [ ] SamplingRequestHandler implemented + tested
- [ ] McpContext.requestSampling() method added
- [ ] AC-4: All 3 sampling types supported (CLARIFY, SAMPLE, VALIDATE)
- [ ] AC-5: Performance targets met (< 500ms P99, cache, timeout handling)
- [ ] AC-6: Audit logging complete (structured JSON logs)
- [ ] 10+ unit tests, all passing
- [ ] 4+ integration tests, all passing
- [ ] No regression: Phase 1 + Memory tests (217) still pass
- [ ] 80%+ code coverage for sampling module
- [ ] Code review approved
- [ ] Merged to feature branch

---

## Effort Breakdown

| Phase | Duration | Notes |
|:------|:---------|:------|
| SamplingRequest/Response types | 1 hour | Type design + JSDoc |
| SamplingRequestHandler | 3 hours | Prompt + LLM call + parsing + validation |
| McpContext integration | 1 hour | Add requestSampling() method |
| Caching layer | 1 hour | Optional RedisClient / in-memory |
| Unit tests | 2 hours | Types + handler + cache + logging (10 cases) |
| Integration tests | 1 hour | Full workflows (4 cases) |
| Documentation | 1 hour | Developer guide + API reference |
| **Total** | **10 hours** | Done by +3 days |

---

## Definition of Done

- [x] TASK-14-09-IMPLEMENTATION-SUMMARY.md created (post-implementation)
- [x] AC verification matrix (6/6 AC passing)
- [x] Test results: 10 unit + 4 integration, all passing
- [x] Code review sign-off
- [x] Git commit: `feat(TASK-14-09): LLM-assisted sampling & argument completion`
- [x] Merged to feature branch
- [x] Phase 1 + Phase 2 regression (217+ tests still passing)

---

## Next Tasks After Completion

Parallelize remaining Phase 2:

- **TASK-14-10: Notification Debouncing** (6h)
- **TASK-14-11: E2E Test Suite** (12h, after blockers clear)
