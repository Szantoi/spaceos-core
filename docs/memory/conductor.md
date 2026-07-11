# Conductor Terminal Memory — Updated 2026-07-07

## ROLE & IDENTITY

**Primary Mission:** SpaceOS Agent Fleet Orchestrator — Daily task coordination, pipeline management, terminal dispatch

### Telegram Aliases

- **Primary:** conductor (technical)
- **Secondary:** karmester (Hungarian for "conductor")
- **Tertiary:** orchestrator (English architectural term)

### Responsibilities

1. **Session Startup:** Check planning queue, terminal status, system health
2. **Task Dispatch:** Process ROOT escalations, coordinate terminal assignments
3. **Epic Tracking:** Monitor Q3/Q4 tracks (A/B/C) progress, multi-week coordination
4. **Status Maintenance:** Update `docs/Codebase_Status.md`, focus queue management
5. **Pipeline Coordination:** Planning queue → debate → consensus → terminal dispatch

---

## COST-EFFICIENT CODEGEN PATTERN

### Haiku vs Sonnet Economics

| Model | Input $/M | Output $/M | Speed | Use Case |
|-------|-----------|------------|-------|----------|
| **Haiku** | $0.25 | $1.25 | ~2s | Repetitive codegen, template fill |
| **Sonnet** | $3.00 | $15.00 | ~5s | Complex planning, review |
| **Opus** | $15.00 | $75.00 | ~10s | Architecture, strategic decisions |

**ROI:** 5 Haiku parallel = 1 Sonnet price, but 5× faster

### Conductor Codegen Dispatcher Pattern

```typescript
// When codegen batch task arrives:
async function dispatchCodegenTask(task: CodegenBatchTask) {
  const items = task.items; // e.g., 5 CQRS handlers

  // Parallel Haiku spawn
  const workers = items.map(item => ({
    id: `codegen-${item.name}`,
    model: 'haiku',  // ALWAYS haiku for repetitive work
    prompt: generatePromptFromTemplate(task.template, item)
  }));

  await spawn_parallel_workers({
    terminal: task.targetTerminal,
    tasks: workers
  });

  // Result: 5 files in ~10 seconds, ~$0.05 cost
  // vs 1 Sonnet sequential: ~2 min, ~$0.50 cost
}
```

### Template-Based Codegen Workflow

1. **Architect (Sonnet):** Designs template structure
2. **Conductor:** Schedules batch tasks
3. **Haiku clones:** Generate files in parallel
4. **Reviewer (Haiku):** Validates output

### Available Codegen Templates

| Template | Terminal | Haiku-Optimized | Location |
|----------|----------|----------------|----------|
| `generate_hook` | frontend | ✅ Yes (simple fill) | MCP tool |
| `generate_component` | frontend | ✅ Yes | MCP tool |
| `generate_module` | backend | ⚠️ Partial (FSM logic) | MCP tool |
| CQRS Handler | backend | ✅ Yes (pure template) | Planned |
| Express Route | backend | ✅ Yes | Planned |

**Example Batch Codegen:**
```yaml
# Conductor inbox task
type: codegen_batch
template: cqrs_handler
target: backend
items:
  - name: GetPurchaseOrders (query)
  - name: CreatePurchaseOrder (command)
  - name: SubmitPurchaseOrder (command)
  - name: ApprovePurchaseOrder (command)
  - name: CancelPurchaseOrder (command)

# Conductor action:
# → 5 parallel Haiku workers
# → ~10 sec, ~$0.05
# → vs 1 Sonnet sequential: ~2 min, ~$0.50
```

---

## MODE #4: PROGRAM-AWARE ORCHESTRATION

**Status:** ✅ PRODUCTION READY (2026-07-02 implementation)

### Mode #4 Capabilities

1. **Task Awareness:** Conductor knows what terminals are working on (MCP `get_terminal_status`)
2. **Dependency Tracking:** Blocks MSG-031 on MSG-030 Phase 3 completion (Focus Queue)
3. **BLOCKED Triage:** Automatically resolves resolvable blockers (88% auto-resolution rate)

### Mode #4 Components

- **Focus Queue:** Real-time task tracking (`docs/tasks/active/`)
- **Epic-Aware Routing:** Checkpoints trigger next tasks (EPICS.yaml)
- **Monitor Coordination:** Health checks → escalations to Conductor
- **Intelligent Briefing:** Context-aware task summaries for terminals

**Production Metrics (2026-07-03):**
- 17 BLOCKED messages triaged → 15 resolved (88%)
- 0 false positives in auto-dispatch
- Average triage time: ~30 seconds

---

## JOINERYTECH DEVELOPMENT STATUS (2026-07-03)

### Code Metrics

| Component | Lines | Status |
|-----------|-------|--------|
| **Backend Week 1** | 1,109 | ✅ PostgreSQL + domain complete |
| **Frontend CRM UI Wave 1** | 1,475 | ✅ Components + hooks complete |
| **Backend Week 2** | 977 | 🔴 JWT/OAuth code done, NuGet blocked |
| **Total Code** | ~3,561 | 🟡 In progress |

### Epic Tracking (8 epics)

- **EPIC-JT-CRM:** 67% (Backend + Frontend DONE, Integration pending)
- **EPIC-JT-CTRL:** 100% ✅ (Kontrolling Dashboard complete)
- **EPIC-JT-HR:** 75%+ (Backend Week 1 complete, Frontend pending)
- **EPIC-JT-MAINT:** 67% (Backend + Frontend DONE, Integration pending)
- **EPIC-CUTTING-Q3:** 0% (Queued)
- **EPIC-GRAPH-WORKFLOW:** Pending activation

### Blockers

- **VPS Operator:** NuGet package publish blocked (Backend Week 2)
- **Infrastructure Credit:** Resolved 2026-07-07 12:19 (20 BLOCKED cleared)

---

## Q3 CUTTING EXPANSION (2026-06-29 Session)

**Decision:** MSG-CONDUCTOR-007 — Q3 Cutting Module Expansion APPROVED (2026-06-22)

### Track Status

- **Track A (Customer Portal):** MSG-BACKEND-030, MSG-BACKEND-031, MSG-FRONTEND-018 dispatched
- **Track B (Pricing):** MSG-BACKEND-032, MSG-FRONTEND-019 (pending)
- **Track C (ShopFloor):** MSG-BACKEND-033, MSG-FRONTEND-020 (pending)
- **Expected Completion:** 5.5 days from dispatch

**Track A Coordination (2026-06-29 20:25 UTC):**
- Frontend: ✅ DONE (Quote Form + Status Tracking)
- Backend MSG-030: 🟡 40% complete (Phase 1-2 done, Phase 3-5 pending)
- Backend MSG-031: 🔴 BLOCKED (depends on MSG-030 Phase 3)

**Actions Taken:**
- Created MSG-BACKEND-079 (Phase 3-5 coordination)
- Updated Focus Queue (3 items: active + queued)
- Updated Codebase_Status.md (live progress table)

---

## SESSION LEARNINGS

### 2026-07-03 Key Insights

1. **Always verify existing implementation** — Mode #4 components were already production-ready (2026-07-02)
2. **User communication excellence** — Hungarian status reports with detailed breakdowns appreciated
3. **Todo cleanup important** — Mark completed items immediately after session work
4. **BLOCKED triage effectiveness** — 88% auto-resolution rate proves system value

### 2026-06-29 Q3 Coordination Patterns

1. **Multi-track dispatch** — Parallel A/B/C tracks reduce calendar time
2. **Dependency blocking** — MSG-031 blocked on MSG-030 Phase 3 (domain event system)
3. **Phase-based coordination** — Phase 3-5 split for complex tasks (migration, testing, security)

---

**Last Updated:** 2026-07-07
**Status:** 🟢 OPERATIONAL (Mode #4 active)
**Current Focus:** JoineryTech development coordination (8 epics)
**Memory Tier:** Hot (48-hour, active orchestration)

---

_This memory is compressed from 7.5KB to ~3.5KB by removing empty template sections and consolidating 2× session narratives from 2026-06-29 into a single summary. Preserved: role definition, Haiku cost analysis, Mode #4 status, JoineryTech metrics, and coordination patterns._
