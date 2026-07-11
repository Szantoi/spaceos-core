---
id: DEV-B-PHASE-2-TASK-ASSIGNMENT
title: "Dev B — Phase 2 Task Assignment (TASK-14-08 & TASK-14-09)"
date: 2026-03-11
developer: Dev B
epic: EPIC-14
phase: Phase 2 (Advanced Features)
status: "✅ COMPLETE — Phase 2 implementation delivered and validated"
---

# Dev B — Phase 2 Task Assignment

## 🎯 Executive Overview

**Welcome to Phase 2, Dev B!** 🚀

Phase 1 is complete (3 days early!), and you're perfectly positioned to lead Phase 2 advanced feature work. Your two recommended tasks leverage your transport expertise and build on HTTP/Stdio foundation you just completed.

**Assigned Tasks:**

1. **TASK-14-08: Resource Template Support** (10h, 6 AC)
2. **TASK-14-09: Sampling & Argument Completion** (10h, 5 AC)

**Total Phase 2 Effort:** 20 hours
**Estimated Timeline:** 2026-03-12 → 2026-03-17 (5 calendar days, parallel with other Phase 2 devs)
**Status:** Ready to start immediately

---

## 📋 TASK-14-08: Resource Template Support

### What You're Building

A resource template system that enables dynamic URI-based access to role definitions, workflows, discovery templates, and task contexts — all without hardcoding file paths into responses.

### Your Challenge

**Problem:** Currently, role definitions and workflow templates live in `database/roles/`, but there's no clean way for agents to discover and access them through the MCP protocol.

**Solution:** Implement resource templates that support URI patterns like:

- `resource://role/{domain}/{role}` → returns role definition
- `resource://workflow/{type}` → returns workflow
- `resource://template/{category}` → returns template
- `resource://discovery/{phase}` → returns discovery workflow

### Acceptance Criteria (6 AC)

```
AC-1: ResourceTemplate base class
  Given: Template system initialized
  When: Developer extends ResourceTemplate
  Then: Can define URI patterns and resolver functions

AC-2: URI pattern matching (e.g., `resource://role/{domain}/{role}`)
  Given: ResourceTemplate with pattern `resource://role/{domain}/{role}`
  When: Resource URI `resource://role/engineering/backend_developer` requested
  Then: Pattern matches and extracts domain=engineering, role=backend_developer

AC-3: Dynamic resolver functions
  Given: Template pattern matched
  When: Resolver function called with extracted parameters
  Then: Returns resource content (JSON) or 404 if not found

AC-4: Resource listing (server.listResources)
  Given: Multiple resource templates registered
  When: Client calls `server.listResources()`
  Then: Returns all available resource URIs with descriptions

AC-5: No file paths in URIs
  Given: Resource template resolver
  When: Template resolves to file content
  Then: File paths never exposed in URI or metadata

AC-6: Error handling (404 for missing resources)
  Given: Resource URI with parameters that don't exist
  When: Resource requested
  Then: Returns 404 error with helpful message
```

### Key Implementation Areas

**1. ResourceTemplate Base Class (~2-3h)**

```typescript
// src/mcp/resources/resourceTemplates.ts

abstract class ResourceTemplate {
  abstract uriPattern: string;           // e.g., "resource://role/{domain}/{role}"
  abstract description: string;

  abstract matchUri(uri: string): MatchResult;
  abstract resolve(params: Record<string, string>): Promise<ResourceContent>;
}

interface ResourceContent {
  uri: string;
  name: string;
  description?: string;
  mimeType: string;
  contents: string;  // JSON stringified
}
```

**2. Resource Template Implementations (~3-4h)**

```typescript
// Register templates for:
- RoleTemplates: resource://role/{domain}/{role}
- WorkflowTemplates: resource://workflow/{type}
- TemplateLibrary: resource://template/{category}
- DiscoveryTemplates: resource://discovery/{phase}
- TaskContextTemplates: resource://task/{task_id}
```

**3. Server Integration (~2h)**

- Modify `server.registerResource(template)`
- Implement `server.listResources()` to discover all templates
- Both transports (stdio + HTTP) support resource URIs

**4. Tests (~2-3h)**

- Unit: URI pattern matching
- Unit: Parameter extraction
- Unit: Resource resolution
- Integration: Both transports support resources

### Why This Matters

- **Agents discover resources** without hardcoding paths
- **Dynamic URIs** scale to new roles, workflows, templates without code changes
- **Transport-agnostic** — works via stdio or HTTP
- **Foundation for TASK-14-11 E2E tests** — resources verified cross-transport

---

## 📋 TASK-14-09: Sampling & Argument Completion

### What You're Building

A protocol for tools to ask LLMs for clarification when arguments are ambiguous — enabling complex tools that delegate decision-making back to the LLM agent.

### Your Challenge

**Problem:** Some tools have optional arguments with ambiguous meanings. E.g., `request_context` tool has filters like "by-role", "by-phase", "by-status" — but the agent might say "get context by everything" without specifying which.

**Solution:** Tool can call `context.requestSampling()` to ask the LLM agent: "Which filters did you mean?" and the LLM responds with clarified arguments.

### Acceptance Criteria (5 AC)

```
AC-1: Tool can request sampling (clarification from LLM)
  Given: Tool executing with ambiguous arguments
  When: Tool calls context.requestSampling({ prompt, options })
  Then: Request is queued and sent to LLM agent

AC-2: Sampling response includes arguments
  Given: LLM agent receives sampling request
  When: Agent responds with clarification
  Then: Tool receives JSON response with selected option(s)

AC-3: Error response marks "needs_clarification"
  Given: Tool detects ambiguity in arguments
  When: Sampling fails or times out
  Then: Tool returns error with "needs_clarification" flag

AC-4: Sampling timeout (5s default)
  Given: Sampling request sent
  When: 5 seconds elapse without response
  Then: Request times out, tool gets error

AC-5: LLM delegation example (request_context tool)
  Given: request_context tool with ambiguous filters
  When: Tool calls requestSampling with filter options
  Then: Returns refined filter set, tool continues
```

### Key Implementation Areas

**1. RequestContext Enhancement (~2-3h)**

```typescript
// In tool request handler:
if (args.filters.isAmbiguous) {
  const clarification = await context.requestSampling({
    prompt: "Which filters did you mean?",
    options: [
      { label: "by-role", value: "role" },
      { label: "by-phase", value: "phase" },
      { label: "by-status", value: "status" }
    ],
    timeout: 5000
  });

  if (clarification.error) {
    return {
      status: "error",
      message: "Clarification needed",
      needsClarification: true
    };
  }

  args.filters = clarification.selected;
}
```

**2. Sampling Protocol (~2-3h)**

- Add `RequestContext.requestSampling()` method
- Implement message routing between tool and LLM agent
- Queue sampling requests in session state
- Match responses by request ID

**3. Both Transports Support (~2h)**

- Stdio: Sampling requests as JSON lines
- HTTP: Sampling as async endpoint
- Session state persistence across transport calls

**4. Tests (~2-3h)**

- Unit: Sampling request/response protocol
- Unit: Timeout handling
- Integration: request_context tool with ambiguous filters
- E2E: Sampling flow across both transports

### Why This Matters

- **Complex tools can delegate** uncertain decisions to LLM
- **Better UX** — agent can clarify rather than guess
- **Enables intelligent defaults** — tool suggests options
- **Foundation for advanced workflows** — TASK-14-11 E2E testing includes sampling scenarios

---

## 📊 Timeline & Blockage Analysis

### Start Condition

- ✅ Phase 1 complete (all transports, plugins ready)
- ✅ TASK-14-02 (HTTP transport) done
- ✅ TASK-14-03 (Plugin system) ready
- ✅ **NO BLOCKERS** — You can start immediately

### Dependency Chain

```
Phase 1 (Complete) ✅
  ├─ TASK-14-01 ✅
  ├─ TASK-14-02 ✅ (your HTTP transport)
  ├─ TASK-14-03 ✅ (plugin system)
  └─ TASK-14-04/05 ✅ (tool modules)

Phase 2 (Start Now)
  ├─ TASK-14-08 (Your task) 🟢 START NOW
  │  └─ Unblocks: TASK-14-11 (E2E tests)
  ├─ TASK-14-09 (Your task) 🟢 START NOW
  │  └─ Unblocks: TASK-14-11 (E2E tests)
  └─ Other Phase 2: Parallel (14-06, 14-07, 14-10)
```

### Estimated Completion

- **Start Date:** 2026-03-12 (tomorrow)
- **TASK-14-08 Complete:** 2026-03-14 EOD (2.5 days)
- **TASK-14-09 Complete:** 2026-03-17 EOD (3 days parallel)
- **Ready for TASK-14-11 E2E Integration:** 2026-03-17+

**Calendar view:**

```
Mon 03-11: Phase 1 signup + this assignment
Tue 03-12: TASK-14-08 Day 1 + TASK-14-09 kickoff
Wed 03-13: TASK-14-08 Day 2 (midway)
Thu 03-14: TASK-14-08 complete + TASK-14-09 Day 2
Fri 03-15: TASK-14-09 Day 3 (testing phase)
Mon 03-17: Both tasks ready for E2E integration
```

---

## 🚀 Getting Started (RIGHT NOW)

### Step 1: Read Full Context (30 min)

```
1. EPIC-14-TASK-MATRIX.md (this file covered Phase 2)
2. EPIC-14-PHASE-1-COMPLETION.md → understand what Phase 1 built
3. PLUGIN-SYSTEM-API-REFERENCE.md → see how plugins work
4. src/mcp/transports/HTTPTransport.ts → review your HTTP transport
```

### Step 2: Design TASK-14-08 (2 hours)

- [ ] Sketch URI pattern matching algorithm
- [ ] Design ResourceTemplate interface
- [ ] Plan role/workflow/template resolver implementations
- [ ] Identify edge cases (invalid URIs, missing resources, circular refs)
- [ ] Create `TASK-14-08-DESIGN.md` with state diagram

### Step 3: Implement TASK-14-08 Core (6 hours)

- [ ] Create `src/mcp/resources/resourceTemplates.ts` base class
- [ ] Implement URI pattern matcher
- [ ] Implement role template resolver
- [ ] Integrate `server.registerResource()`
- [ ] Full unit test suite

### Step 4: Review & Complete (2 hours)

- [ ] Run full test suite: `npm test -- --match "*resource*"`
- [ ] Verify both transports can access resources
- [ ] Document AC completion in `TASK-14-08-IMPLEMENTATION-BRIEF.md`
- [ ] Ready for PR

### Step 5: Start TASK-14-09 (Begin Wed 03-13)

- [ ] Design sampling protocol
- [ ] Implement `RequestContext.requestSampling()`
- [ ] Enhance `request_context` tool as example
- [ ] Build tests + E2E scenarios
- [ ] Document in implementation brief

---

## 🎯 Success Criteria

### TASK-14-08 Done When

- [ ] All 6 AC implemented and tested
- [ ] URI pattern matching works (both static + dynamic patterns)
- [ ] Both stdio + HTTP transports support resource URIs
- [ ] No file paths exposed in responses
- [ ] 100% test coverage (unit + integration)
- [ ] Implementation brief completed
- [ ] Ready for TASK-14-11 E2E integration

### TASK-14-09 Done When

- [ ] All 5 AC implemented and tested
- [ ] Sampling protocol works (request → timeout/response)
- [ ] `request_context` tool uses sampling for ambiguous filters
- [ ] 5s timeout enforced
- [ ] Both transports support sampling
- [ ] 100% test coverage
- [ ] Implementation brief completed

---

## 📞 Support & Questions

**Tech Lead (Checkpoints):**

- 2026-03-12 09:00: Kickoff + TASK-14-08 design review
- 2026-03-13 15:00: TASK-14-08 mid-point check
- 2026-03-14 EOD: TASK-14-08 sign-off + TASK-14-09 kickoff
- 2026-03-17 EOD: Both tasks complete + ready for E2E

**Blockers?**

- Ping Tech Lead in Slack immediately
- Escalation: Tech Lead → Backend Developer Agent

**File Locations:**

- Task doc: `Docs/mcp-context-server/delivery/milestone_02/epic_14/EPIC-14-TASK-MATRIX.md`
- Phase 1 summary: `Docs/mcp-context-server/delivery/milestone_02/epic_14/EPIC-14-PHASE-1-COMPLETION.md`
- Your Phase 2 updates: `Docs/mcp-context-server/delivery/mcp-maintenance/devs/dev-b/TASK-14-08/` + `TASK-14-09/`

---

## ✅ Acceptance Sign-Off

| Role | Sign-Off | Date |
|------|----------|------|
| **Tech Lead** | Assignment approved, Dev B ready to start | 2026-03-11 |
| **Dev B** | Understood, starting TASK-14-08 tomorrow | — |
| **QA Lead** | (Will integrate into E2E suite on 03-17) | — |

---

**Next: Create your task day-1 working docs in `dev-b/TASK-14-08/` and `dev-b/TASK-14-09/` folders.**

🚀 **Go ahead and build Phase 2!**
