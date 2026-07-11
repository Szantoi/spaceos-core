---
id: dwi-mcp-rbac
type: discovery_work_item
topic: "MCP RBAC Schema & Enforcement Strategy"
status: in_progress
current_phase: 3
next_action: "Experimenter: execute exp-001 spike on RBAC permission resolution performance under load (100 concurrent requests, 15 role profiles)"
verdict: null
hypothesis_count: 3
validated_count: 2
created: 2026-02-15
updated: 2026-03-03
---

# DWI: MCP RBAC Schema & Enforcement Strategy

## Active Phase

**Phase 3 — Prototype** (Experimenter phase — execute and measure MVE)

Previous phase closed on 2026-02-28. Currently executing feasibility experiment to validate RBAC permission resolution can scale to production needs.

## Next Action

> **Experimenter: execute exp-001 spike on RBAC permission resolution performance under load.** Run 100 concurrent requests across all 15 role profiles, measure query latency (p50/p95/p99), identify any serialization bottlenecks in permission caching layer.

## Hypothesis Summary

| ID | Statement (short) | Status | Notes |
|:---|:-----------------|:-------|:------|
| hyp-001 | YAML schema can represent 15 distinct role profiles with clear permission inheritance | validated | Schema completed, tested with all 15 roles Jan 2026 |
| hyp-002 | Application can enforce RBAC gate checks on every MCP tool invocation with <50ms latency | testing | Spike exp-001 running now (target: 10k RPM throughput) |
| hyp-003 | Role binding state can be persisted efficiently in SQLite without schema bloat | open | Deferred to Phase 4 if hyp-002 validates |

## Phase Gate History

| Phase | Gate crossed | Date | Notes |
|:------|:------------|:-----|:------|
| 0 — Discover | ✅ | 2026-02-01 | Threat surface documented in obs-001, obs-002, obs-003 (authorization gaps in DocumentServer) |
| 1 — Define | ✅ | 2026-02-10 | Scope locked: RBAC must support 15 roles, <50ms gate checks, retroactive binding on session resume |
| 2 — Ideate | ✅ | 2026-02-25 | ADR-001 approved: YAML schema over JSON for human readability; ADR-002 approved: eager caching in memory with SQLite audit log |
| 3 — Prototype | 🔄 | 2026-03-03 | Experimenter running exp-001 (performance spike); results due end of week |
| 4 — Test & Learn | ⬜ | — | Blocked on exp-001 results |

## Linked Artifacts

### Observations (Phase 0)

- `00_discovery/obs-001.md` — Current DocumentServer has no MCP tool-level permission checks
- `00_discovery/obs-002.md` — Session handoff does not preserve role context
- `00_discovery/obs-003.md` — 15 distinct role use cases identified across McpServer stakeholders

### Hypotheses (Phase 1)

- `01_define/hyp-001.md` — YAML role schema feasibility
- `01_define/hyp-002.md` — RbacFilter performance under load
- `01_define/hyp-003.md` — Persistence efficiency (deferred)

### Architecture Decisions (Phase 2)

- `02_ideate/adrs/ADR-001-rbac-schema-format.md` — YAML (approved 2026-02-20)
- `02_ideate/adrs/ADR-002-rbac-caching-strategy.md` — Eager memory cache + audit (approved 2026-02-25)
- `02_ideate/adrs/ADR-003-role-binding-lifecycle.md` — Draft pending hyp-002 results

### Experiments (Phase 3)

- `03_prototype/exp-001-rbac-performance-spike.md` — Load test on RbacFilter query latency (in progress)

### Learnings (Phase 4)

- (pending)

## Success Criteria for Phase 4 Closure

For this DWI to conclude with `verdict: validated`, the following must be true:

1. **exp-001 results**: Permission gate check latency ≤50ms at p95 under 100 concurrent requests, 15 roles
2. **ADR-003 approved**: Role binding lifecycle decision document signed off by Architect
3. **DoR package assembled**:
   - All 3 hypotheses have terminal status (validated or invalidated)
   - Architect + Product Owner have reviewed and approved the RBAC strategy
   - Epic-level scope for Delivery track documented

## Risk Register

| Risk | Probability | Impact | Mitigation |
|:-----|:------------|:--------|:-----------|
| YAML schema too verbose for 15 roles | Low | High | ADR-001 already approved (tested with draft schemas) |
| RbacFilter query latency >100ms | Medium | High | exp-001 designed to measure exact latency; early pivot to caching if needed |
| Role binding state corruption on session resume | Low | Critical | Session manager tests + audit log tracing (Phase 4) |

---

## Phase Notes

### Phase 0 (Completed 2026-02-01)

Explorer gathered 3 observation files on current authorization gaps. Problem space sealed: RBAC is missing from MCP layer.

### Phase 1 (Completed 2026-02-10)

Framer locked scope with Architect: need 15 distinct roles, sub-50ms gate checks, retroactive role binding on session resume. Success criteria finalized.

### Phase 2 (Completed 2026-02-25)

Designer drafted 3 ADRs. ADR-001 (YAML schema) and ADR-002 (memory cache + audit) approved by Architect on 2026-02-20. ADR-003 (role binding lifecycle) deferred pending Phase 3 data.

### Phase 3 (In Progress)

Experimenter running exp-001: load test on RbacFilter latency. Target: 100 concurrent requests, measure p50/p95/p99 latency, identify serialization bottlenecks. Results due 2026-03-08.

**Next Phase 4 gate criterion**: If exp-001 validates hyp-002 (latency ≤50ms), proceed to Integrator. Else, pivot caching strategy and re-spike.
