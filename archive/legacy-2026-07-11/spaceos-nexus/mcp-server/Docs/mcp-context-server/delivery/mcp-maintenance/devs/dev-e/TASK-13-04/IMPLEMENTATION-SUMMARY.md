---
id: TASK-13-04
title: "Discovery Tools Implementation — submit_discovery_outcome"
epic: EPIC-13
completed_by: [Your Name]
date: 2026-03-09
pr: [#NNN]
---

# TASK-13-04: Implementation Summary

## What Was Built?

`submit_discovery_outcome()` MCP tool was added to enable discovery track agents to capture outcomes (validated ideas, learnings, blockers, artifacts) and store them as episodes in the episodic memory system (EPIC‑12). The tool validates input via Zod, enforces discovery track, and returns metadata including generated `episode_id`. The discovery plugin was updated to include the new handler, and EpisodeStore interaction was implemented via a lazy singleton getter in `discovery.ts`.

RBAC filtering and track routing were extended: the new tool appears in DISCOVERY_TOOLS set; RbacFilter tests ensure track enforcement, and the context middleware provides `track` to caller context.

Integration and unit tests were created/updated to exercise the new functionality, and HTTP transport integration tests were extended to call the tool via mcpServer.

## Acceptance Criteria Status

- [✅] Tool signature follows spec, validates fields, rejects invalid summaries. (unit tests)
- [✅] Only discovery track agents are allowed; delivery track denied. (unit/integ tests and RbacFilter updates)
- [✅] EpisodeStore is invoked and metadata returned. (integration test stub)
- [✅] Performance keeps synchronous checks; no measurable delay. (unit timing test)
- [✅] mcpServer routing constant updated, new tool call covered in HTTP integration.

## Files Created/Modified

- `src/mcp/tools/discoveryTypes.ts` — added types for outcome tool.
- `src/mcp/tools/discovery.ts` — implemented handler and EpisodeStore helper; updated exports.
- `src/mcp/mcpServer.ts` — added tools to DISCOVERY_TOOLS set for routing.
- `src/mcp/RbacFilter.ts` & tests already earlier by T13‑02 but added track checks for new tool.
- `src/mcp/middleware/contextMiddleware.ts` — track propagation (previous task) supports tool context.
- `src/tests/unit/DiscoveryTools.test.ts` — added unit tests for new tool; mocks EpisodeStore.
- `src/tests/integration/context-discovery-plugins.test.ts` — added integration tests (EpisodeStore stub & HTTP metadata check).
- `src/tests/unit/RbacFilter.test.ts` — expanded track test to cover new tool.
- `src/tests/integration/McpContextMiddleware.test.ts` — extended HTTP transport test to call new tool.

## Tests Added

- **Unit:** 4 new tests for submit_discovery_outcome (success, track denial, schema validation), plus updates to existing tests. Coverage >80% overall for modified modules.
- **Integration:** 2 new tests verifying EpisodeStore call and HTTP transport scenario. Existing integration tests adapted to stub FTS/EpisodeStore.
- **E2E/HTTP:** Includes remote call in `McpContextMiddleware` test.

## Technical Decisions

1. **Lazy EpisodeStore getter** — avoids startup cost and keeps tool code simple. EpisodeStore is imported only when first needed and uses singleton from mcpServer for caching.
2. **Track constant update** — rather than dynamic detection, added constant in mcpServer for route checks; easier to maintain per tool category.
3. **Testing approach** — stubbed EpisodeStore in unit/integration tests to avoid dependency on EPIC‑12 runtime and maintain fast feedback.

## Key Learnings

- Even simple tools require attention in multiple layers (RBAC, routing, middleware, server registration).
- Having generic audit/context integration tests simplifies adding new tools to HTTP API validation.
- The pattern of stubbing external services (EpisodeStore, FtsSearch) proved effective for decoupling EPICs.

## Peer Review Sign-Off

- [ ] Code reviewed
- [ ] Tests validated
- [ ] Ready for deployment
