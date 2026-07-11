---
id: DEV-C-EPIC-14-REFINEMENT-STUDY
title: "Dev C — EPIC-14 Refinement Study: Plugin System & Dependency Resolution"
created: 2026-03-08
type: "parallel-refinement-assignment"
assignee: "Dev C"
duration: "2 days (1-2h/day, non-blocking)"
phase: "Parallel Prep (while waiting for EPIC-11 blocker)"
deadline: "2026-03-10 EOD"
tech-lead-checkpoint: "2026-03-11 (go/no-go decision by 2026-03-14)"
---

# Dev C — EPIC-14 Refinement Study

## 🎯 Executive Summary

**Your Job:** Design + validate EPIC-14 **TASK-14-03** (Plugin System & Dynamic Module Loading) while waiting for Dev A's TASK-11-01 blocker to clear.

**Timeline:** Saturday 2026-03-09 → Monday 2026-03-10 EOD (2 days at 1-2h/day)

**Outcome:** Tech Lead has complete design validation + PoC to make EPIC-14 go/no-go decision by **2026-03-14 EOD** (Option A/B/C).

**Your Role:** Deep-dive the spec, build PoC skeleton, identify risks, map QA improvements.

---

## 📋 Context: Why This? Why Now?

### The Situation

1. You're currently **blocked** on Dev A completing TASK-11-01 (FSM Schema) — which unblocks your real work (TASK-11-04 Resumption Logic).
2. Dev A won't finish until **2026-03-12 EOD**, so you have **3 days of available time** (2026-03-09, 2026-03-10, starting 2026-03-11).
3. Rather than idle for 3 days, you can **parallelize** EPIC-14 refinement work — which is **independent** of EPIC-11.

### The Opportunity

- **EPIC-14 is conditional** on a Tech Lead decision gate (2026-03-14 EOD)
- If we can provide a **complete design + PoC + risk assessment by 2026-03-11**, Tech Lead can make an informed go/no-go call
- Your refinement work **directly unblocks** the decision and the O6 team's planning

### The Constraint

- This is **prep work only** — not full implementation yet
- We're funding this from "idle time" (Dev C waiting for TASK-11-01 blocker)
- If Tech Lead says "no EPIC-14" on 2026-03-14, this prep work becomes tech debt (small cost, high value if we proceed)

---

## 🔥 What You're Building (4 Deliverables)

### Deliverable 1️⃣: Design Doc — Plugin System & Dependency Resolution

**File:** `dev-c/EPIC-14-REFINEMENT-STUDY-T14-03-DESIGN.md`

**Read This First:**
- [EPIC-14-T14-03-ASSIGNMENT.md](../dev-c/EPIC-14-T14-03-ASSIGNMENT.md) — Base spec
- [EPIC-14-COORDINATION-ROUTER.md](../EPIC-14-COORDINATION-ROUTER.md) — Full epic scope
- [EPIC-14-QA-IMPROVEMENTS-INTEGRATION.md](../EPIC-14-QA-IMPROVEMENTS-INTEGRATION.md) — P0/P1/P2 issues

**Questions to Answer (500-800 words):**

1. **Plugin Lifecycle & Hooks (P1 Issue)**
   - Current spec: "Lifecycle hooks (onInit, onDestroy, onError)"
   - Question: What does each hook do? When are they called? What can they do?
   - Your answer: Document the complete lifecycle (plugin load → init → ready → shutdown → destroy)

2. **Dependency Resolution**
   - How do plugins declare dependencies? (e.g., plugin A depends on plugin B)
   - How do we resolve the dependency graph? (topological sort? cycles?)
   - What happens if a dependency is missing? (optional vs required?)
   - How do we handle version conflicts?

3. **Dynamic Module Loading**
   - How do plugins get "loaded"? (file system? remote URL? in-memory registry?)
   - Can plugins be loaded after MCP server starts? (hot-reload?)
   - How do we handle reload/update scenarios? (can old plugin shutdown cleanly?)

4. **Error Recovery & Graceful Degradation**
   - What if a plugin's `onInit` fails? (do other plugins still load?)
   - What if a plugin crashes at runtime? (can we restart it? kill other plugins?)
   - How do we communicate plugin failures to end users?

**Acceptance Criteria:**
- [ ] AC-1: 500-800 words, answers all 4 questions
- [ ] AC-2: Plugin lifecycle diagram (ASCII or text description)
- [ ] AC-3: Dependency resolution algorithm sketched (pseudocode)
- [ ] AC-4: P1 requirement (PluginDependencyResolver) clearly addressed

---

### Deliverable 2️⃣: Proof of Concept Skeleton

**File:** `src/mcp/plugins/PluginManager.ts` (skeleton, no implement)

**What to Build:**

```typescript
// src/mcp/plugins/PluginManager.ts

// Plugin interface (minimal contract)
export interface IPlugin {
  name: string;
  version: string;
  dependencies?: string[]; // e.g., ["discovery-plugin", "crud-plugin"]

  onInit(): Promise<void>;
  onDestroy(): Promise<void>;
  onError(error: Error): Promise<void>;
}

// P1 requirement: Dependency resolver
export interface IPluginDependencyResolver {
  resolve(plugins: IPlugin[]): IPlugin[]; // topologically sorted
  detectCycles(plugins: IPlugin[]): string[][] | null; // cycle detection
}

// Core plugin manager
export class PluginManager {
  private plugins: Map<string, IPlugin> = new Map();
  private dependencyResolver: IPluginDependencyResolver;

  async registerPlugin(plugin: IPlugin): Promise<void> {
    // Validate dependencies exist
    if (plugin.dependencies) {
      for (const dep of plugin.dependencies) {
        if (!this.plugins.has(dep)) {
          throw new Error(`Missing dependency: ${dep}`);
        }
      }
    }

    // Call onInit hook
    await plugin.onInit();

    // Register
    this.plugins.set(plugin.name, plugin);
  }

  async shutdown(): Promise<void> {
    // Call onDestroy in reverse order (dependency graph aware)
    const orderedShutdown = Array.from(this.plugins.values()).reverse();
    for (const plugin of orderedShutdown) {
      try {
        await plugin.onDestroy();
      } catch (err) {
        console.error(`Plugin ${plugin.name} failed during shutdown:`, err);
        await plugin.onError(err as Error);
      }
    }
  }
}

// Dependency resolver (P1)
export class PluginDependencyResolver implements IPluginDependencyResolver {
  resolve(plugins: IPlugin[]): IPlugin[] {
    // Topological sort (Kahn's algorithm or DFS)
    // TODO: implement
    return plugins; // placeholder
  }

  detectCycles(plugins: IPlugin[]): string[][] | null {
    // DFS cycle detection
    // TODO: implement
    return null; // placeholder
  }
}
```

**Acceptance Criteria:**
- [ ] AC-1: File compiles (TypeScript syntax check)
- [ ] AC-2: Plugin interface + PluginManager stubbed
- [ ] AC-3: IPluginDependencyResolver interface defined (P1)
- [ ] AC-4: Lifecycle hooks (onInit, onDestroy, onError) specified

---

### Deliverable 3️⃣: QA Improvement Mapping (P1 Requirement)

**File:** `dev-c/EPIC-14-REFINEMENT-STUDY-T14-03-QA-MAPPING.md`

**What to Map:**

Reference [EPIC-14-DECISION-GATE-SUMMARY.md](../EPIC-14-DECISION-GATE-SUMMARY.md) Table: P1 Issue = "Plugin deps: no resolution/lifecycle hooks"

Answer These:
1. **P1 Issue Description:** What exactly is missing in current spec?
   - Current spec says: "plugin architecture that supports dynamic loading"
   - Missing: explicit dependency resolution + lifecycle hook implementation

2. **PluginDependencyResolver: Where Does It Live?**
   - As a standalone service? (PluginDependencyResolver)
   - Embedded in PluginManager?
   - How does it integrate with plugin registration?

3. **How Do We Test It?**
   - Unit test: Can we resolve a simple A → B dependency chain?
   - Unit test: Do we detect A → B → A cycles?
   - Integration test: Can we load plugins with dependencies?
   - E2E test: Do lifecycle hooks fire in the right order?

4. **What's the +4h Effort?**
   - Spec says P1 requires "PluginDependencyResolver + lifecycle" (+4h)
   - What's the complexity breakdown? (resolver: 2h? lifecycle: 2h?)
   - Can we defer optional features (hot-reload? version management?) to Phase 2?

**Acceptance Criteria:**
- [ ] AC-1: P1 requirement clearly understood + mapped
- [ ] AC-2: PluginDependencyResolver design specified
- [ ] AC-3: Test strategy drafted (5-8 test cases covering cycles + ordering)
- [ ] AC-4: Effort breakdown (+4h) justified with task list

---

### Deliverable 4️⃣: Risk Assessment

**File:** `dev-c/EPIC-14-REFINEMENT-STUDY-T14-03-RISKS.md`

**Risk Matrix:**

| Risk | Probability | Impact | Mitigation |
|:-----|:----------:|:------:|:-----------|
| **Circular dependency: A depends on B, B depends on A** | 🟡 MEDIUM | 🔴 CRITICAL | Implement cycle detection (DFS-based) |
| **Plugin load ordering confusion** | 🟡 MEDIUM | 🟡 MEDIUM | Topological sort + clear documentation |
| **Plugin crash takes down whole MCP system** | 🟠 UNKNOWN | 🔴 CRITICAL | Isolate plugin errors in try-catch? |
| **How to hot-reload a plugin?** | 🟡 MEDIUM | 🟡 MEDIUM | Out of scope for MVP? Defer to Phase 2? |
| **Version conflicts: Two plugins need different versions of same dep** | 🟠 UNKNOWN | 🟡 MEDIUM | Plugin namespace isolation? Or not supported? |
| **Testing complexity: Mock plugin loading/initialization** | 🟡 MEDIUM | 🟡 MEDIUM | Use DI + interface-based mocking? |

**Acceptance Criteria:**
- [ ] AC-1: 5-8 risks identified
- [ ] AC-2: Each risk has probability + impact
- [ ] AC-3: Mitigation strategy for each
- [ ] AC-4: Phase 2 deferrals clearly marked (optional features)

---

## 📚 Reading List & References

### Core Files to Review

1. **EPIC-14 Spec:**
   - [EPIC-14-T14-03-ASSIGNMENT.md](../dev-c/EPIC-14-T14-03-ASSIGNMENT.md) — Full spec (read thoroughly)
   - [EPIC-14-COORDINATION-ROUTER.md](../EPIC-14-COORDINATION-ROUTER.md) — Epic overview

2. **QA Research:**
   - [EPIC-14-DECISION-GATE-SUMMARY.md](../EPIC-14-DECISION-GATE-SUMMARY.md) — P0/P1/P2 issues + options
   - [EPIC-14-QA-IMPROVEMENTS-INTEGRATION.md](../EPIC-14-QA-IMPROVEMENTS-INTEGRATION.md) — Detailed QA findings

3. **Context (Plugin Architecture):**
   - [EPIC-14-T14-01-ASSIGNMENT.md](../EPIC-14-T14-01-ASSIGNMENT.md) — Transport abstraction (base for plugins)
   - [EPIC-14-T14-04-ASSIGNMENT.md](../EPIC-14-T14-04-ASSIGNMENT.md) — Bootstrap plugin (example plugin)
   - `src/metadata/FsmStates.ts` — FSM context (plugin state tracking?)

### External References

- **Topological Sorting:** https://en.wikipedia.org/wiki/Topological_sorting (Kahn's algorithm)
- **Dependency Injection Patterns:** https://en.wikipedia.org/wiki/Dependency_injection
- **Node.js Dynamic Module Loading:** https://nodejs.org/api/module.html#module_require_extensions
- **Plugin Architecture Best Practices:** https://www.patterns.dev/posts/plugin-pattern/

---

## ⏱️ Timeline & Checkpoints

### Saturday 2026-03-09 (1-2h)

- [ ] Read all spec files (EPIC-14-T14-03-ASSIGNMENT + QA findings)
- [ ] Sketch plugin lifecycle diagram (textual)
- [ ] Outline dependency resolver algorithm (pseudocode thoughts)
- [ ] Outline design doc (bullet points)

### Sunday 2026-03-10 (1-2h)

- [ ] Write design doc (500-800 words)
- [ ] Create PoC skeleton (TypeScript file, compiles)
- [ ] Draft risk matrix + cycle detection strategy

### Monday 2026-03-11 EOD (checkpoint)

- [ ] All 4 deliverables complete + committed
- [ ] Tech Lead review + feedback session
- [ ] Refinement docs aggregated for decision gate (by 2026-03-14)

---

## ✅ Submission Checklist

When you're done, ensure:

- [ ] Design doc (`EPIC-14-REFINEMENT-STUDY-T14-03-DESIGN.md`) ✅ written + committed
- [ ] PoC skeleton (`src/mcp/plugins/PluginManager.ts`) ✅ compiles
- [ ] QA mapping doc (`EPIC-14-REFINEMENT-STUDY-T14-03-QA-MAPPING.md`) ✅ complete
- [ ] Risk assessment (`EPIC-14-REFINEMENT-STUDY-T14-03-RISKS.md`) ✅ detailed
- [ ] All files in `dev-c/` folder + committed to Git
- [ ] Send Tech Lead summary (email or Slack) by 2026-03-11 EOD

---

## 🎯 Why This Matters (To You & The Team)

1. **You:** Direct input on EPIC-14 design = high visibility + impact
2. **Team:** Complete design validation = faster kickoff on 2026-03-19 (if approved)
3. **Tech Lead:** Better go/no-go decision with actual PoC + risks assessed
4. **Project:** +5-7 days parallel acceleration vs sequential planning

**Tzrg:** This is "downtime leverage" — turning idle waiting time into valuable discovery work. No downside if tech lead says "no EPIC-14", high upside if they say "go"!

---

## 🔗 Coordination Note

**Dev B is doing parallel work on TASK-14-02** (HTTP Transport). Your plugin architecture needs to be compatible with their transport transport abstraction. Consider:

- How does PluginManager interact with the Transport layer?
- Are plugins modules that extend Transport? Or separate?
- Do lifecycle hooks fire at MCP startup or Transport startup?

**Suggest:** Tech Lead facilitates 30-min Dev B + C sync on 2026-03-10 to align design boundaries.

---

**Questions?** Reach out to Tech Lead for spec clarifications.

**Ready to Start?** Monday 2026-03-09 09:00, let's ship this! 🚀
