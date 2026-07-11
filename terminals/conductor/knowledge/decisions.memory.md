# Conductor Decisions Memory

> **Architectural decisions cache** — Cold memory (365d TTL)
>
> Coordination workflow döntések.

## Workflow Decisions

### Planning Pipeline
**Decision:** 30-minute plan scan + dual debate (A/B)
**Rationale:** Consensus quality vs speed trade-off

### Task Routing
**Decision:** Epic-aware task routing (ADR-049 Phase 2)
**Rationale:** Automated context propagation

### Review Process
**Decision:** Dual terminal review (Architect + Librarian)
**Rationale:** Code quality vs knowledge consistency

## Escalation Rules

### BLOCKED Handling
**Decision:** 24h timeout → Root escalation
**Rationale:** Prevent stuck sessions

### Priority Overrides
**Decision:** Root can inject high priority tasks
**Rationale:** Strategic flexibility

---

**Last updated:** YYYY-MM-DD
