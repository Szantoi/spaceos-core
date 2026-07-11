---
id: TASK-14-03
title: "TASK-14-03 — Plugin System: Dynamic Module Loading + Dependency Resolution"
epic: EPIC-14
milestone: M02
phase: "Phase 1 (Foundation)"
created: 2026-03-09
type: "task-assignment"
status: "✅ READY (Refinement validated 2026-03-09)"
assignee: "Dev C"
effort: "26 hours (22h base + 4h QA improvement)"
duration: "3 calendar days (2026-03-19 Thu → 2026-03-21 Sat EOD)"
blocker: "Depends on TASK-14-01 (Dev A, Transport Abstraction) — delivers Transport error enum"
blocked_by: []
ac_count: 24
priority: "P0 (Enterprise architecture)"
---

# TASK-14-03: Plugin System — Dynamic Module Loading + Dependency Resolution

## 📋 Problem Statement

The MCP server currently has tool modules that are manually imported and registered. To support modular architecture and future scaling (multi-agent, distributed deployment), we need:

1. **Dynamic module loading** — Load plugins at runtime from configurable paths
2. **Dependency resolution** — Plugin A can declare dependencies on Plugin B (circular dependency detection)
3. **Lifecycle hooks** — Plugins can initialize/cleanup resources (DB connections, file handles)
4. **Error recovery** — Optional plugins fail gracefully; critical plugins block server startup
5. **Health status tracking** — Know which plugins loaded successfully and why failures occurred

**This enables:**

- Modular tool organization (bootstrap, context, discovery, memory, legacy plugins)
- Cleaner separation of concerns (each plugin manages its own lifecycle)
- Production-grade reliability (proper error handling + recovery)

---

## ✅ Acceptance Criteria

### Base AC-1 through AC-20: Plugin System Architecture

**AC-1: Plugin Manifest Definition**

- Given: A plugin module with metadata (name, version, dependencies, optional flag)
- When: PluginManifest interface is defined
- Then: Plugin can declare dependencies and criticality (required vs optional)
- *Validation:* PluginTypes.ts has PluginManifest interface with all required fields

**AC-2: Plugin Lifecycle States**

- Given: Plugin state machine (NOT_LOADED → LOADING → LOADED/FAILED → UNLOADING)
- When: Plugin transitions between states
- Then: Only valid transitions allowed; terminal states (LOADED, FAILED) cannot transition again
- *Validation:* PluginStatus enum defined; state machine tested in unit tests

**AC-3: Plugin Registry**

- Given: PluginManager instance
- When: Plugins load successfully
- Then: All loaded plugins registered in pluginRegistry (Map<name, plugin>)
- *Validation:* getLoadedPlugins() returns only LOADED plugins

**AC-4: Plugin Loading Sequential**

- Given: 10 plugins with declared dependencies
- When: loadPlugin() called for each plugin
- Then: Dependencies loaded first, before dependent plugin
- *Validation:* Load order deterministic; unit tests verify sequence

**AC-5: Optional Plugin Error Recovery**

- Given: Plugin marked as optional=true in manifest
- When: Plugin fails during onInit()
- Then: Failure logged and plugin marked FAILED; server continues starting
- *Validation:* failedPlugins list contains the plugin; no exception thrown

**AC-6: Critical Plugin Failure Blocks Startup**

- Given: Plugin marked as optional=false (critical=true)
- When: Plugin fails during onInit()
- Then: Exception thrown; server startup blocked
- *Validation:* PluginLoadError thrown; startServer() fails

**AC-7: Lifecycle Hook — onInit Before Registration**

- Given: Plugin implements lifecycle.onInit()
- When: loadPlugin() called
- Then: onInit() executed BEFORE plugin is added to registry
- Then: If onInit() throws, plugin not registered
- *Validation:* Registry is empty if onInit throws; unit tests verify order

**AC-8: Lifecycle Hook — onDestroy Called**

- Given: Loaded plugin has lifecycle.onDestroy()
- When: Server shuts down or unloadPlugin() called
- Then: onDestroy() executed
- Then: Plugin removed from registry
- *Validation:* Plugin cleanup verified; resources freed

**AC-9: Lifecycle Hook — onError Called**

- Given: Plugin lifecycle fails at any step
- When: Error occurs (onInit failed, dependency missing, etc.)
- Then: onError() hook called with error details
- *Validation:* Error handler triggered; error data passed correctly

**AC-10: Plugin Dependency Declaration**

- Given: PluginManifest with dependencies array
- When: Plugin declares { dependencies: ["bootstrap", "context"] }
- Then: Bootstrap and context must load before this plugin
- *Validation:* Manifest parsing correct; dependencies extracted

**AC-11: Circular Dependency Detection — A→B→A**

- Given: Plugin A depends on B, Plugin B depends on A
- When: Attempt to resolve dependencies
- Then: CircularDependencyError thrown (not silent failure)
- *Validation:* DFS cycle detection algorithm; test with A→B→A cycle

**AC-12: Circular Dependency Detection — Deep Cycle A→B→C→B**

- Given: Plugin A depends on B, B depends on C, C depends on B
- When: Attempt to resolve dependencies
- Then: CircularDependencyError thrown with cycle path (B→C→B)
- *Validation:* Algorithm detects all cycle types; error message includes path

**AC-13: Dependency Resolution Algorithm — Topological Sort**

- Given: Plugins with dependencies forming a DAG (directed acyclic graph)
- When: resolveDependencies() called
- Then: Returns plugin names in topological order (dependencies first)
- Then: Complexity O(V + E) where V=plugins, E=dependency edges
- *Validation:* Algorithm verified correct; performance tested

**AC-14: Missing Dependency Detection**

- Given: Plugin A depends on "missing-plugin" (not declared)
- When: Attempt to load Plugin A
- Then: MissingDependencyError thrown
- Then: Error message includes missing plugin name
- *Validation:* Dependency validation; error details correct

**AC-15: Plugin Health Status**

- Given: Some plugins loaded, some failed, some not loaded
- When: getPluginStatus() called
- Then: Returns { loaded: [...], failed: [...], notLoaded: [...] }
- Then: Each entry includes plugin name + failure reason (if applicable)
- *Validation:* Status report accurate; all categories included

**AC-16: Plugin Unload Cleanup**

- Given: Loaded plugin with onDestroy() hook
- When: unloadPlugin("plugin-name") called
- Then: onDestroy() executed
- Then: Plugin removed from pluginRegistry
- Then: Plugin state → NOT_LOADED
- *Validation:* Cleanup verified; plugin no longer accessible

**AC-17: Reentrancy Guard — No Concurrent Loads**

- Given: Multiple concurrent loadPlugin() calls for same plugin
- When: Two calls race to load "memory" plugin
- Then: Only one load attempt succeeds; second returns existing plugin
- *Validation:* Mutex/lock prevents double-load; unit tests verify

**AC-18: Plugin Exports IToolModule Interface**

- Given: Plugin module exports { tools: {...}, lifecycle: {...}, manifest: {...} }
- When: Plugin loaded dynamically
- Then: Module conforms to IToolModule interface
- Then: Type validation enforces interface
- *Validation:* TypeScript types; compilation fails if interface violated

**AC-19: Dynamic Import with Windows Compatibility**

- Given: Plugin path contains backslashes or drive letters (Windows)
- When: pluginManager.loadPlugin() calls import()
- Then: pathToFileURL() converts path correctly
- Then: Plugin loads in Windows + Linux + macOS
- *Validation:* Cross-platform test; pathToFileURL used

**AC-20: Plugin Registry Immutability**

- Given: Plugin is loaded in registry
- When: External code attempts registry.set(plugin, new_value)
- Then: Set operation denied (registry is read-only to external callers)
- *Validation:* getPluginRegistry() returns ReadonlyMap; mutations impossible

---

### New AC-21 through AC-24: P1 High-Priority Requirements (Refinement Study)

**AC-21: Plugin Dependency Declaration (NEW AC-31 from Refinement)**

- Given: Plugin manifest defines {name: "memory", dependencies: ["bootstrap", "context"]}
- When: loadPlugin("memory") called
- Then: bootstrap + context loaded first (dependencies satisfied)
- Then: memory plugin loaded only after dependencies ready
- *Validation:* DependencyResolver ensures order; unit tests verify

**AC-22: Circular Dependency Detection (NEW AC-32 from Refinement)**

- Given: Plugin A depends on B, Plugin B depends on A
- When: Attempt to load either plugin
- Then: CircularDependencyError thrown (not silent failure)
- Then: Error includes cycle path (A→B→A)
- *Validation:* DFS-based cycle detection; test matrix includes all cycle types

**AC-23: Plugin Lifecycle Hooks (NEW AC-33 from Refinement)**

- Given: Plugin implements {lifecycle: {onInit, onDestroy, onError}}
- When: pluginManager.loadPlugin()
- Then: lifecycle.onInit() called BEFORE registration (AC-23a)
- Then: lifecycle.onDestroy() called during unload (AC-23b)
- Then: lifecycle.onError() called if any step fails (AC-23c)
- *Validation:* Hook execution order tested; lifecycle verified

**AC-24: Optional Plugin Error Recovery (NEW AC-34 from Refinement)**

- Given: Optional plugin fails during load (e.g., DB migration fails)
- When: Server continues starting
- Then: Plugin marked in failedPlugins list (AC-24a)
- Then: Server proceeds without error (AC-24b)
- Then: Critical plugins still block (AC-24c)
- *Validation:* Error recovery tested; server startup verified

---

## 📂 Deliverables

### Code Files to Create/Modify

| File | Type | Purpose | Status |
|:-----|:-----|:--------|:-------|
| `src/plugins/PluginManager.ts` | ✅ **DONE** | Orchestrates plugin lifecycle (load/unload) | Exists + tested |
| `src/plugins/PluginDependencyResolver.ts` | ✅ **DONE** | Topological sort + cycle detection | Exists + tested |
| `src/plugins/PluginDecorators.ts` | ✅ **DONE** | @Plugin, @Tool metadata decorators | Exists + tested |
| `src/plugins/PluginTypes.ts` | ✅ **DONE** | PluginManifest, PluginStatus, IToolModule interfaces | Exists |
| `src/plugins/index.ts` | ✅ **DONE** | Public exports | Exists |
| `src/plugins/plugins/` | ✅ **DONE** | Refactored plugins (bootstrap, context, discovery) | All migrated |
| `src/tests/unit/PluginManager.test.ts` | ✅ **DONE** | AC-1 through AC-20 coverage (80%+) | Exists, 10+ tests |
| `src/tests/integration/plugin-system.test.ts` | ✅ **DONE** | AC-21 through AC-24 validation (cycle detection, lifecycle) | Exists, 5+ tests |
| `src/tests/e2e/plugin-integration.e2e.test.ts` | ✅ **DONE** | Full workflow (load all plugins → verify tools) | Exists |

### Documentation Files

| File | Type | Purpose |
|:-----|:-----|:--------|
| `IMPLEMENTATION-SUMMARY.md` | Doc | Dev C completion report (fill after implementing) |
| `TASK-14-03-KICKOFF.md` | Doc | Implementation roadmap (see companion file) |

---

## 🔗 Dependencies & Blockers

### Depends On (Blocking Tasks)

| Task | Component | Reason | Expected Completion |
|:-----|:----------|:-----:|:-----|
| **TASK-14-01** | Transport error enum | Plugin system needs error types for lifecycle failures | 2026-03-19 EOD (Dev A) |

### Blocked By (None)

No external blockers. Can start 2026-03-19 as soon as TASK-14-01 delivers Transport error types.

---

## 🧪 Testing Strategy

### Unit Tests (80%+ coverage required)

| Test | AC | Coverage | Mock Strategy |
|:-----|:---|:---------|:------|
| `test_plugin_manifest_parsing` | AC-1, AC-10 | PluginManifest parsing | No mocks |
| `test_plugin_state_transitions` | AC-2 | State machine validity | FSM verification |
| `test_circular_dependency_detection_simple` | AC-11 | A→B→A cycle | DFS graph |
| `test_circular_dependency_detection_deep` | AC-12 | A→B→C→B cycle | DFS graph |
| `test_dependency_resolution_topological_sort` | AC-13 | DAG → topological order | Graph library |
| `test_missing_dependency_error` | AC-14 | Dependency validation | Error check |
| `test_lifecycle_hooks_onInit_order` | AC-7, AC-23 | onInit before registration | Mock lifecycle |
| `test_plugin_registry_immutability` | AC-20 | ReadonlyMap enforcement | Type check |
| `test_windows_path_compatibility` | AC-19 | pathToFileURL() | Windows paths |

### Integration Tests (Plugin interactions)

| Test | AC | Scenario |
|:-----|:---|:----------|
| `test_plugin_dependency_chain` | AC-4, AC-21 | A → B → C dependency chain loads in order |
| `test_optional_plugin_failure` | AC-5, AC-24 | Optional plugin fails; server continues |
| `test_critical_plugin_failure_blocks` | AC-6 | Critical plugin fails; server blocked |
| `test_plugin_lifecycle_full` | AC-7, AC-8, AC-23 | onInit → register → unload → onDestroy |

### E2E Tests (Full workflow)

| Test | AC | Workflow |
|:-----|:---|----------|
| `test_load_all_plugins` | All | Start PluginManager → load all plugins → verify health status |
| `test_plugin_tools_accessible` | AC-3, AC-18 | Verify all plugin tools registered in MCP tool registry |

---

## 🎯 Success Criteria (Definition of Done)

- [ ] **All 24 AC implemented** (AC-1 through AC-24)
- [ ] **Unit test coverage ≥ 80%** — Tests for all AC groups
- [ ] **Integration tests passing** — Dependency resolution + lifecycle hooks working
- [ ] **E2E tests passing** — Full plugin load workflow end-to-end
- [ ] **Windows cross-platform verified** — pathToFileURL() tested on Windows paths
- [ ] **Code merged to main** — PR reviewed + approved
- [ ] **Implementation Summary written** — Dev C documents what was built + decisions

---

## 📊 Effort Breakdown (26 hours)

| Phase | Hours | Dates |
|:------|:---:|:-----:|
| **Implementation (AC-1 through AC-20)** | 22h | 2026-03-19 (full day) → 2026-03-21 morning |
| **QA Improvement (AC-21 through AC-24)** | 4h | 2026-03-21 afternoon (refine tests + docs) |
| **Total** | **26h** | **2026-03-19 Thu → 2026-03-21 Sat EOD** |

---

## 🎓 Refinement Study Evidence

**This task has been thoroughly validated via refinement study (completed 2026-03-09):**

✅ [EPIC-14-REFINEMENT-STUDY-T14-03-DESIGN.md](../../../devs/dev-c/EPIC-14-REFINEMENT-STUDY-T14-03-DESIGN.md) — Architecture + plugin lifecycle validated

✅ [EPIC-14-REFINEMENT-STUDY-T14-03-QA-MAPPING.md](../../../devs/dev-c/EPIC-14-REFINEMENT-STUDY-T14-03-QA-MAPPING.md) — All AC-31 through AC-35 traced to implementation + tests

✅ [EPIC-14-REFINEMENT-STUDY-T14-03-RISKS.md](../../../devs/dev-c/EPIC-14-REFINEMENT-STUDY-T14-03-RISKS.md) — 10 identified risks with mitigations; all LOW after risk assessment

---

## 📝 Notes

- **Tech Lead Decision:** Option A (FULL EPIC-14) approved 2026-03-09 09:30 UTC
- **All code exists:** Plugin system fully implemented + tested in src/plugins/
- **Task is validation + commit:** Dev C commits existing code to formal structure
- **No breaking changes:** Backward-compatible with existing tool API
