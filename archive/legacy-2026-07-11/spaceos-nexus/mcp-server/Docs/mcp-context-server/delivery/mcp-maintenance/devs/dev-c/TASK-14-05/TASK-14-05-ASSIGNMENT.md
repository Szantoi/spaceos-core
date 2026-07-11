---
id: TASK-14-05
title: "Refactor context + discovery tools as Plugins"
type: task
epic: EPIC-14
scope: mcp-context-server
status: ✅ COMPLETED
priority: P1
role: backend_developer
created: 2026-03-08
depends_on: TASK-14-04
fsm_state: "IN_PROGRESS"
assignee: dev-c
---

# TASK-14-05: Context + Discovery Tools Refaktorálása

## Description

A `context.ts` és `discovery.ts` factory függvények (`createContextToolModule`, `createDiscoveryToolModule`) refaktorálása plugin osztályokra a TASK-14-04 `BootstrapPlugin` mintájára.

## Acceptance Criteria

- [ ] `ContextPlugin extends BasePlugin` osztály `@Plugin`/`@Tool` dekorátorral
- [ ] `DiscoveryPlugin extends BasePlugin` osztály `@Plugin`/`@Tool` dekorátorral
- [ ] `AgentDb` a `SystemContext.agentDb`-n keresztül kerül DI-val injektálva
- [ ] Factory függvények (`createContextToolModule`, `createDiscoveryToolModule`) megtartva backward-compat-nak
- [ ] Integrációs tesztek: plugin osztályok + factory függvények mind zöldek
- [ ] `npx vitest run src/tests/integration/context-discovery-plugins.test.ts` → 100% pass

## Notes

- `DiscoveryPlugin` a `bootstrap` plugin-től függ (roles, workflows, templates betöltéséhez)
- `discovery_search` ChromaDB integráció PENDING — placeholder marad
