---
id: TASK-14-11
title: "TASK-14-11: E2E Test Suite (Both Transports)"
epic: EPIC-14
phase: "Phase 2: Advanced Features"
type: task
created: 2026-03-11
status: "� IN PROGRESS"
effort: "12 hours"
---

# TASK-14-11: E2E Test Suite (Both Transports)

## Overview

Write comprehensive **end-to-end tests** for the complete EPIC-14 architecture, verifying:

- Transport abstraction (stdio vs HTTP work identically)
- Plugin system (registration, loading, execution)
- Resource template resolution
- Sampling/argument completion flow
- Debouncing behavior
- Legacy tools (if refactored)

**Status:** � IN PROGRESS — Added E2E resource template tests
**Owner:** TBD (QA or senior backend developer)
**Duration:** 12 hours
**Predecessor:** TASK-14-01..10 (Phase 2 features)
**Successor:** TASK-14-12 (Documentation)

---

## Acceptance Criteria

### AC-1: Scenario 1 — Tool Invocation (Both Transports)

**Requirement:** Same tool behavior via stdio and HTTP.

**Input:**

- Tool: `bootstrap_agent`
- Args: `{ domain: "engineering", role: "agent" }`

**Output:**

- Via stdio: Full context object
- Via HTTP: Same context object

**Validation:**

- [ ] Both transports return identical response
- [ ] JSON serialization/deserialization works
- [ ] Performance <200ms for both

**Test File:** `src/tests/e2e/transports.scenario.test.ts`

---

### AC-2: Scenario 2 — Plugin Registration

**Requirement:** All plugins registered and accessible via both transports.

**Input:** Server startup with all plugins loaded

**Output:**

- Server.listTools() returns all tools from:
  - BootstrapPlugin
  - ContextPlugin
  - DiscoveryPlugin
  - MemoryPlugin (TASK-14-06)
  - LegacyPlugin (TASK-14-07, if any)
  - ResourceTemplate plugin (TASK-14-08)

**Validation:**

- [ ] At least 15 tools listed (conservative estimate)
- [ ] Each tool has @Tool decorator metadata
- [ ] Tool count matches sync via stdio + HTTP
- [ ] Plugin dependency order respected

**Test File:** `src/tests/e2e/plugin-registration.test.ts`

---

### AC-3: Scenario 3 — Resource Template Resolution

**Requirement:** Dynamic resource URIs resolve correctly.

**Input:** Requests for various resource templates:

- `resource://role/engineering/agent`
- `resource://workflow/discovery`
- `resource://template/artifact-submission`

**Output:**

- Resources resolve dynamically
- No file paths exposed
- 404 for missing resources with clear error

**Validation:**

- [ ] Resource resolve() returns correct content
- [ ] Pattern matching extracts parameters correctly
- [ ] Cache invalidation works (if caching implemented)
- [ ] Works via both stdio and HTTP

**Test File:** `src/tests/e2e/resource-templates.test.ts`

---

### AC-4: Scenario 4 — Sampling/Argument Completion

**Requirement:** Complex tool with sampling works in both transports.

**Input:**

- Tool: `request_context` with ambiguous filters
- LLM clarification requested
- Sampled args returned

**Output:**

- Tool continues with sampled args
- Final result same regardless of transport

**Validation:**

- [ ] Sampling request reaches context
- [ ] LLM mock returns clarifications
- [ ] Tool receives clarified args
- [Performance <500ms total

**Test File:** `src/tests/e2e/sampling-flow.test.ts`

---

### AC-5: Scenario 5 — Notification Debouncing

**Requirement:** Bulk operations trigger only batched notifications.

**Input:**

- Save 50 roles via API
- Monitor notifications

**Output:**

- Instead of 50 notifications: 1 batch notification
- Batch contains all 50 items
- Time taken: ~100-150ms

**Validation:**

- [ ] Notification count <5 (batched)
- [ ] All items included in batch
- [ ] Flush() method forces immediate notification
- [ ] Works via both transports

**Test File:** `src/tests/e2e/debouncing.scenario.test.ts`

---

### AC-6: Scenario 6 — Graceful Shutdown

**Requirement:** HTTP server shuts down gracefully without losing requests.

**Input:**

- 10 concurrent requests in flight
- Server receives SIGTERM
- Drain timeout: 30s

**Output:**

- All in-flight requests complete
- New requests rejected (503 Service Unavailable)
- Server exits cleanly

**Validation:**

- [ ] All 10 requests return responses
- [ ] No request aborted mid-execution
- [ ] Server exits within drain timeout
- [ ] Graceful shutdown only for HTTP (stdio doesn't need it)

**Test File:** `src/tests/e2e/graceful-shutdown.test.ts`

---

## Test Coverage Target

| Component | Coverage % | Test Count |
|:----------|:-----------|:-----------|
| Transports (both) | 95% | 12+ |
| Plugins | 90% | 10+ |
| Resource templates | 85% | 8+ |
| Sampling | 80% | 5+ |
| Debouncing | 85% | 6+ |
| Shutdown | 90% | 4+ |
| **Total** | **~90%** | **45+** |

---

## Key Test Scenarios

```gherkin
Scenario: Tool works identically via stdio and HTTP
  Given stdio and HTTP servers running
  When I call bootstrap_agent via both transports
  Then both return identical responses

Scenario: Resource templates resolve dynamically
  Given resource://role/{domain}/{role} pattern
  When I request resource://role/engineering/agent
  Then response contains agent role definition

Scenario: Sampling clarifies ambiguous arguments
  Given complex tool with ambiguous filter
  When LLM provides clarification
  Then tool continues with clarified args

Scenario: Notification debouncing batches updates
  Given 50 concurrent save operations
  When debouncer is active
  Then only 1 notification batch sent
```

---

## Implementation Notes

- Use Playwright for HTTP transport testing (headless browser automation)
- Mock LLM for sampling tests
- Use `sqlite-test-utils` for seeding test data
- Share discovery workflow tests between transports
- Track performance metrics (response time, memory, CPU)
