---
name: phase3c_closedloop_findings
description: Phase 3C+ full scan results — 1 ERROR (S-01 RLS COALESCE), 8 WARNINGs confirmed. No new CRITICAL. Role hardcode (S-04) still open.
type: project
---

Phase 3C+ full scan completed 2026-04-09. All three pre-existing findings from MSG-K030/K031 were re-confirmed:

- S-01 (ERROR): TenantHandshakeAllowlist RLS COALESCE missing — Migration 0028 required
- S-02 (WARNING): IgnoreQueryFilters archived tenant leak in allowed_hosts claim
- S-03 (WARNING): ConfigureJwtBearerOptions silent fallback for JWT authority

New findings in this scan:
- S-04 (WARNING): RefreshTokenCommandHandler hardcodes "User" role — role loss on refresh still open from MSG-K021
- S-05 (WARNING): MigrateAsync at startup — acknowledged TODO in Program.cs:292
- S-06 (WARNING): DomainException caught in CloseFlowEpicCommandHandler — architectural anti-pattern
- S-07 (WARNING): ChanneledAuditEventDispatcher drops events after 5s — audit compliance risk
- S-08 (WARNING): PostgresAdvisoryAuditWriteLock unbounded spin — connection pool risk under contention

**Why:** Full Phase 3C+ changeset introduces TenantHandshakeAllowlist, JWT claim extensions, and new audit infrastructure. The RLS COALESCE gap (S-01) is the sole blocker.

**How to apply:** Do not mark any Phase 3C+ tasks SECURITY_PASSED until Migration 0028 is committed and S-01 is resolved.
