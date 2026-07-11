---
id: TASK-14-09-QUICKSTART
title: "TASK-14-09 Day-1 Quickstart — Sampling & Argument Completion"
type: developer-quickstart
owner: "Dev B (or TBD)"
duration: "10 hours"
created: 2026-03-11
---

# 🚀 TASK-14-09 Quickstart — Sampling & Argument Completion

**Your mission:** Enable tools to request LLM clarification for ambiguous parameters.

**Duration:** 10 hours (~3 dev days)
**Files you'll touch:** `src/mcp/sampling/samplingUtil.ts`, `src/tests/unit/sampling.test.ts`
**Predecessor:** TASK-14-03 ✅
**No blockers** → Can run in parallel!

---

## Acceptance Criteria Overview

- [ ] AC-1: SamplingRequest & SamplingResponse types
- [ ] AC-2: Tool can call context.requestSampling()
- [ ] AC-3: McpContext integration
- [ ] AC-4: Sampling timeout (5s default)
- [ ] AC-5: Example tool (request_context with sampling)

---

## Architecture Pattern

```typescript
// When tool has ambiguous args, it can ask LLM to clarify:

if (args.filters.isAmbiguous) {
  // Step 1: Request clarification from LLM
  const response = await context.requestSampling({
    tool_name: "request_context",
    missing_params: ["filter_type"],
    constraints: { "filter_type": "one of: by-role, by-phase, by-status" },
    context: "User wants to see context but didn't specify which filters"
  });

  // Step 2: LLM provides sampled args
  if (response.status === 'success') {
    args.filters = response.sampled_args.filters;
  }
}

// Step 3: Continue with resolved args
return { ... };
```

---

## Step-by-Step Implementation

### Day 1: Types & Interface (3h)

Create `src/mcp/sampling/samplingTypes.ts`:

```typescript
export interface SamplingRequest {
  id: string;                         // Unique ID
  tool_name: string;                  // Which tool
  agent_id: string;                   // Which agent
  current_args: Record<string, any>;  // What we have so far
  missing_params: string[];           // What's missing
  constraints: Record<string, string>; // "param_a: must be > 0"
  context: string;                    // "We're trying to..."
  session_id: string;                 // From McpContext
}

export interface SamplingResponse {
  id: string;                         // Matches request.id
  status: 'success' | 'error' | 'timeout';
  sampled_args?: Record<string, any>; // LLM-provided args
  reasoning?: string;                 // LLM reasoning
  confidence?: number;                // 0.0 to 1.0
  alternatives?: Array<Record<string, any>>; // Optional: 2-3 alternatives
  error?: string;                     // If error
}
```

Then implement `context.requestSampling()` method in McpContext.

### Day 2: Sampling Handler (3h)

Create `src/mcp/sampling/samplingHandler.ts`:

```typescript
export class SamplingHandler {
  private timeoutMs = 5000;  // 5s default

  async processRequest(req: SamplingRequest): Promise<SamplingResponse> {
    // 1. Validate request
    // 2. Call LLM with formatted prompt
    // 3. Parse LLM response into SamplingResponse
    // 4. Return with timeout protection

    try {
      const promise = this.callLlm(req);
      return await Promise.race([
        promise,
        this.timeoutPromise()
      ]);
    } catch (err) {
      return { id: req.id, status: 'error', error: String(err) };
    }
  }

  private async callLlm(req: SamplingRequest): Promise<SamplingResponse> {
    // Format prompt for LLM
    const prompt = this.formatPrompt(req);

    // Call your LLM (mock for testing)
    const llmResponse = await llmClient.complete(prompt);

    // Parse response into SamplingResponse
    return this.parseLlmResponse(llmResponse, req.id);
  }

  private formatPrompt(req: SamplingRequest): string {
    return `
      Tool: ${req.tool_name}
      Current args: ${JSON.stringify(req.current_args)}
      Missing: ${req.missing_params.join(', ')}
      Constraints: ${JSON.stringify(req.constraints)}
      Context: ${req.context}

      Please provide sampled values for missing parameters.
      Response format: JSON { sampled_args: {...} }
    `;
  }
}
```

### Day 3: Integration & Tests (4h)

1. **Add to McpContext (1h30m):**

```typescript
export class McpContext {
  async requestSampling(req: SamplingRequest): Promise<SamplingResponse> {
    const handler = new SamplingHandler();
    return handler.processRequest(req);
  }
}
```

2. **Example Tool — request_context with sampling (1h):**

```typescript
@Tool({
  name: "request_context",
  description: "Request agent context with optional sampling"
})
async requestContext(
  args: { domain: string; role: string; filters?: any },
  context: McpContext
) {
  // If filters ambiguous, ask for clarification
  if (args.filters && Array.isArray(args.filters) && args.filters.length === 0) {
    const sampling = await context.requestSampling({
      tool_name: "request_context",
      missing_params: ["filter_type"],
      constraints: { "filter_type": "by-role | by-phase | by-status" },
      context: "User wants context but didn't specify filters"
    });

    if (sampling.status === 'success') {
      args.filters = sampling.sampled_args.filters;
    }
  }

  // Get context with filters
  return { ... };
}
```

3. **Unit Tests (1h30m):**

```typescript
describe("SamplingHandler", () => {
  it("should format prompt correctly", () => {
    const req: SamplingRequest = { ... };
    const prompt = handler.formatPrompt(req);
    expect(prompt).toContain(req.tool_name);
    expect(prompt).toContain(req.missing_params[0]);
  });

  it("should timeout after 5s", async () => {
    const hangingLlm = new Promise(resolve => setTimeout(resolve, 10000));
    const response = await handler.processRequest(req);
    expect(response.status).toBe('timeout');
  });

  it("should parse LLM response", async () => {
    const response = await handler.processRequest(mockRequest);
    expect(response.status).toBe('success');
    expect(response.sampled_args).toBeDefined();
  });
});
```

---

## Testing Your Work

```bash
npm test -- --match "*sampling*"
```

**Expected:** Timeout + parsing tests pass ✅

---

## Gotchas & Tips

**Gotcha 1:** LLM response parsing fails?
→ Add error handling + return `{ status: 'error', error: '...' }`

**Gotcha 2:** Timeout too short?
→ Increase from 5s to 10s if LLM is slow (but update AC!)

**Tip:** Mock the LLM for unit tests - don't call real API!

---

## Completion Sign-Off

When done:

1. All 5 AC passing ✅
2. SamplingRequest/Response types defined ✅
3. Example tool (request_context) implemented ✅
4. Timeout test passing ✅
5. Post completion report!

