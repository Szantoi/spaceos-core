---
id: epic-mcp-maintenance-14
title: "Epic 14: Modern MCP Transports & Tool Plugin Architecture"
type: epic
milestone: M02
project: mcp-maintenance
project_id: mcp-context-server
status: "COMPLETE — T14-01..11 ALL DONE"
fsm_workflow_id: "agile-epic-lifecycle-v1"
fsm_state: "DONE"
fsm_retry_count: 0
created: 2026-03-04
updated: 2026-03-12
assignee: backend_developer
depends_on: EPIC-10, EPIC-11
---

# EPIC-14: Modern MCP Transports & Tool Plugin Architecture

## Célkitűzés

Modernizálni az MCP szerver **transport rétegét** és **tool regisztráció rendszerét**: kétféle transport (stdio/HTTP), plugin-alapú tool regisztráció, resource templates, sampling delegation, notification debouncing.

---

## Task státusz

| Task | Cím | Állapot | Tesztek |
|:-----|:----|:--------|:--------|
| T14-01 | Transport Abstraction | ✅ DONE | 11/11 |
| T14-02 | HTTP Transport MCP Tool Routing | ✅ DONE | 48/48 |
| T14-03 | Plugin System Architecture | ✅ DONE | 40/40 |
| T14-04 | Bootstrap Plugin — Plugin rendszerbe integrálva | ✅ DONE | 12/12 |
| T14-05 | Context + Discovery Plugins — manifest-alapú betöltés | ✅ DONE | 24/24 |
| T14-06 | Memory Plugin | ✅ DONE | — |
| T14-07 | Legacy Tools Backward-Compat (audit: N/A) | ✅ DONE | 3/3 |
| T14-08 | Resource Template Support | ✅ DONE | 5 templates |
| T14-09 | Sampling & Argument Completion | ✅ DONE | 3/3 |
| T14-10 | Notification Debouncing | ✅ DONE | 30/30 |
| T14-11 | E2E Cross-Transport: ResourceTemplate + Sampling + PluginManager | ✅ DONE | 13 tests |

---

## Hogyan lett megoldva

### T14-01 — Transport Abstraction
- `ITransport` interface, `StdioTransport`, `HTTPTransport`, `TransportFactory` (`MCP_TRANSPORT` env var alapján)
- HTTP transport saját Express app-ra mountolja az MCP routert; graceful shutdown `initiateShutdown()`-tal

### T14-02 — HTTP Transport MCP Tool Routing
- `/mcp/call` POST endpoint; `{ tool_name, arguments }` → `PluginManager.invokeTool()`
- `createPluginManager()` kiemelve `mcpServer.ts`-ből, `x-session-id` header → `McpContext`
- Graceful degradation: PluginManager nélkül 503 kód

### T14-03 — Plugin System Architecture
- Decorator-alapú regisztráció (`@Plugin`, `@Tool`), `PluginManager` + `PluginDependencyResolver`
- Topológiai rendezés (Kahn), DFS körkörös függőség detektálás útjelzéssel
- `ReadonlyMap` registry; reentrancy guard Promise mutex-szel

### T14-04 — Bootstrap Plugin Integration
- `BootstrapPlugin extends BasePlugin implements IToolModule` — `@Plugin` + `@Tool` dekorátorral
- Manifest-alapú betöltés javítva: `registerManifest({ name:'bootstrap', entry, className })` → `loadPlugin('bootstrap', true)`

### T14-05 — Context + Discovery Plugin Registration
- `mcpServer.ts` bug javítva: fájl path-szal hívta a `loadPlugin`-t (manifest nélkül → silent fail)
- `discovery` depends_on bootstrap dekorátor biztosítja a betöltési sorrendet

### T14-06 — Memory Plugin
- `MemoryPlugin extends BasePlugin` — `@Plugin` + `@Tool` dekorátorral
- Persistent episodic memory: save_memory, query_memory, search_memory tools

### T14-07 — Legacy Tools Backward-Compatibility
- Audit: Zero legacy tools found — minden tool már @Plugin/@Tool decorator pattern-t használ
- `LegacyPlugin` létrehozva (search_knowledge + brainstorm wrappers) preventív Compatibility rétegként
- Deprecation logging beépítve; eltávolítás: v2.0 (2026-06-01)

### T14-08 — Resource Template Support
- `ResourceTemplateRegistry` + 5 URI template: role, workflow, template, discovery, task
- Wiring: `index.ts` → `new ResourceTemplateRegistry()` → `transportConfig.resourceRegistry`
- `StdioTransport` konstruktor frissítve: config-ból olvassa a `resourceRegistry`-t
- `safeSegment()` path traversal védelemmel (`..`, `/`, `\` elutasítva)

### T14-09 — Sampling & Argument Completion
- `SamplingService`: option-based clarification queue (requestSampling, resolveSampling, listPending)
- `McpContext.requestSampling()` delegate method a kontextusba injektálva
- Wiring: `index.ts` → `new SamplingService()` → `transportConfig.samplingService`
- HTTPTransport: `/mcp/sampling/pending` + `/mcp/sampling/resolve` endpointok

### T14-10 — Notification Debouncer
- `NotificationDebouncer<T extends INotification>` — generikus, size+delay dual-trigger batching
- 1000 notif → ~10 batch (99% csökkentés); `flush()` manuális küldésre
- `getStats()` performance metrikák; graceful shutdown support
- 30/30 teszt (23 unit + 7 integration)

### T14-11 — E2E Cross-Transport Integration Test
- `src/tests/integration/epic14-cross-transport.test.ts` — 13 teszteset (4 leíró blokk)
- **T14-08 block**: ResourceTemplateRegistry wiring StdioTransport-ban (4 teszt)
- **T14-09 block**: SamplingService full lifecycle via HTTPTransport (4 teszt)
- **T14-02/11 block**: PluginManager → `/mcp/call` tool invocation (4 teszt)
- **T14-11 shared**: Shared SamplingService ugyanaz az instance mindkét transportban (1 teszt)
- Node 18+ global `fetch` — nincs külső http kliens dependencia
- In-process ephemeral port 0 — nincs live server szükséges

---

## Levont tapasztalatok (T14-01..10)

- **Interface-first design**: `ITransport` + `TransportFactory` új transport nélkül bővíthető
- **registerManifest ELŐTT loadPlugin**: PluginManager manifest-kulccsal keres, NEM fájl path-szal
- **Config duck typing a transport-okban**: `(config as any).resourceRegistry` wiring pattern egységes mind 3 service-re (pluginManager, resourceRegistry, samplingService)
- **Wiring pattern**: minden service `index.ts`-ben példányosodik → `transportConfig`-on megy át → transport constructor kiolvas
- **Silent `.catch()` elfed hibákat**: plugin betöltési hiba warning, de debug módban látható kell legyen
- **Shared queue design**: SamplingService singleton megosztható SSE és HTTP transport közt (jelenleg különálló — refactor v2.0-ra halasztva)

---

## Fennmaradó feladatok

**Nincs.** Az összes T14-01..11 task teljesítve. EPIC-14 lezárva.

## Sikerkritérium (Definition of Done teljes EPIC-re)

- [x] `MCP_TRANSPORT=stdio` → StdioServerTransport
- [x] `MCP_TRANSPORT=http` → StreamableHTTPServerTransport
- [x] Bootstrap, context, discovery tools mindkét transporton elérhetők
- [x] Tools plugin modulokban (`src/mcp/tools/`)
- [x] `resource://role/engineering/backend_developer` visszaad role definíciót
- [x] Seeder: notification debouncing implementálva (T14-10: 30/30 teszt)
- [x] E2E: ResourceTemplate + Sampling + PluginManager cross-transport tests (13 vitest testcase)
- [x] Nincs breaking change az agent-facing tool API-ban

---

## Érintett fájlok

- `src/mcp/transports/ITransport.ts`, `TransportFactory.ts`, `StdioTransport.ts`, `HTTPTransport.ts`, `ErrorDiagnoser.ts`
- `src/plugins/PluginManager.ts`, `PluginDependencyResolver.ts`, `PluginDecorators.ts`, `PluginTypes.ts`, `BasePlugin.ts`
- `src/mcp/tools/bootstrap.ts`, `context.ts`, `discovery.ts`, `legacy.ts`, `memory.ts`, `IToolModule.ts`
- `src/mcp/mcpServer.ts` — manifest-alapú plugin betöltés (T14-04/05 bug fix)
- `src/mcp/resources/resourceTemplates.ts` — ResourceTemplateRegistry + 5 URI template (T14-08)
- `src/mcp/sampling/SamplingService.ts` — option-based clarification queue (T14-09)
- `src/mcp/notifications/NotificationDebouncer.ts`, `NotificationTypes.ts` — batching (T14-10)
- `src/index.ts` — pluginManager + resourceRegistry + samplingService wiring (T14-02/08/09)
