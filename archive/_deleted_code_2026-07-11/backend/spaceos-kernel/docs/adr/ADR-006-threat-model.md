---
id: ADR-006
title: Threat Model v1.0 — STRIDE for Sprint D Phase 2
status: Accepted
date: 2026-04-07
deciders: architect, kernel-team
---

# ADR-006: Threat Model v1.0

## Context

Following Sprint D Phase 2 security review (8 findings: 2 CRITICAL + 3 HIGH + 3 MEDIUM),
a formal STRIDE threat model was required to document the security posture of the system
and validate that all findings have mitigations in place.

## Decision

Produced `docs/security/THREAT_MODEL.md` covering all 5 components in scope:
- Nginx (reverse proxy)
- Orchestrator (Node.js LLM agent)
- Kernel API (ASP.NET Core 8)
- PostgreSQL (primary data store)
- Redis (distributed cache / RL backing)

All BE-P2-01..BE-P2-08 findings are mapped to their mitigations. Residual risks are documented
with owner and target phase for resolution.

## Consequences

**Positive:**
- Security posture is formally documented and reviewable
- Each Sprint D finding has a traceable mitigation status
- Residual risks (HA, backup encryption, P1-3/P1-4/P1-8) are clearly owned and phased

**Negative:**
- Threat model must be updated with each sprint that introduces new components or attack surface
- Current model covers single-VPS deployment — multi-instance will require re-review (ADR-007)

## Trigger for Update

Update `THREAT_MODEL.md` and this ADR when:
1. New component added to the system (e.g. Joinery Module, Escrow)
2. Horizontal scaling introduced (invalidates single-VPS assumptions)
3. Any P1-3/P1-4/P1-8 finding is implemented
4. Annual security review

## References

- `docs/security/THREAT_MODEL.md` — full STRIDE matrix
- `docs/adr/ADR-007-rl-backing-store.md` — RL backing store decision
- Sprint D Phase 2 review findings (BE-P2-01..BE-P2-08)
