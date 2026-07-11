# Review Report — MSG-K023 / K024 / K025 / K026
**Date:** 2026-04-05
**Agent:** kernel-review-enforcer
**Final status:** REVIEW_FAILED

---

## Scope

Sprint C Phases 4–7: Security infrastructure, Node/Sync API + SIP middleware, FlowManagement module, CLAUDE.md Golden Rules.

Previous run applied two I1 fixes (AsNoTracking on FlowNodeResolver and OfflineQueueService.GetPendingAsync). This run picks up from that baseline.

---

## Violations Found & Fixed

| # | Rule | File | Violation | Fix Applied |
|---|------|------|-----------|-------------|
| 1 | I1 (previous run) | `SpaceOS.Modules.FlowManagement/Services/FlowNodeResolver.cs` | `AnyAsync` calls had no `AsNoTracking()` — read-only probe queries tracked by EF | Added `AsNoTracking()` to all 4 `AnyAsync` chains |
| 2 | I1 (previous run) | `SpaceOS.Modules.FlowManagement/Services/OfflineQueueService.cs` | `GetPendingAsync` queried without `AsNoTracking()` | Added `AsNoTracking()` to the `Where/OrderBy/Take/ToListAsync` chain |

No additional fixable violations were found in this run. All other in-scope files are compliant.

---

## Unfixable Violations (requires developer decision)

| # | Rule | File | Issue | Why unfixable |
|---|------|------|-------|---------------|
| 1 | D4 | `SpaceOS.Kernel.Domain/Federation/NodeManifest.cs` | `NodeManifest.Create()` does not call `AddDomainEvent(...)`. The Domain CLAUDE.md states "every aggregate mutation → one domain event minimum". Node registration is a significant lifecycle event with no corresponding domain event. | Requires defining a new `NodeRegisteredEvent` readonly record struct, adding it to the Domain project, and calling `AddDomainEvent` in `Create()`. Architectural decision: confirm event name and payload with the team before introducing. |
| 2 | D4 | `SpaceOS.Kernel.Domain/Federation/NodeManifest.cs` | `NodeManifest.UpdateHeartbeat()` mutates state (LastHeartbeatAt, UpdatedAt, Version) with no domain event raised. | Requires a new `NodeHeartbeatRecordedEvent`. High-frequency heartbeat events may be intentionally excluded from the domain event bus for performance reasons — developer must decide. |
| 3 | D4 | `SpaceOS.Kernel.Domain/Sync/SyncSignal.cs` | `SyncSignal.Create()` does not call `AddDomainEvent(...)`. A sync signal append is an important domain event (chain integrity, audit trail). | Requires a new `SyncSignalReceivedEvent`. This is an architectural decision: the handler currently logs the chain-append via the transaction but never dispatches a domain event side-effect. |
| 4 | A5 | `SpaceOS.Kernel.Application/Nodes/Commands/RegisterNode/RegisterNodeCommandHandler.cs` | No `PopDomainEvents()` + `DispatchAsync()` call after `SaveChangesAsync`. The Application CLAUDE.md checklist item "PopDomainEvents() + DispatchAsync() at the end, after persist" is absent. | Directly caused by D4 #1 — there are no domain events to pop because `NodeManifest.Create()` raises none. Fix is blocked on D4 #1. Once D4 #1 is resolved, `IDomainEventDispatcher` must be injected and the pop/dispatch pattern added. |
| 5 | A5 | `SpaceOS.Kernel.Application/Nodes/Commands/Heartbeat/HeartbeatCommandHandler.cs` | No `PopDomainEvents()` + `DispatchAsync()` call after `SaveChangesAsync`. | Directly caused by D4 #2. Fix is blocked on D4 #2. |
| 6 | A5 | `SpaceOS.Kernel.Application/Sync/Commands/ReceiveSignal/ReceiveSyncSignalCommandHandler.cs` | No `PopDomainEvents()` + `DispatchAsync()` call after the transaction commit. Golden Rule #12 states "PopDomainEvents() only after successful commit" — the commit exists but the pop/dispatch is absent. | Directly caused by D4 #3. Fix is blocked on D4 #3. |

---

## Files Audited — Compliance Summary

### Phase 4 — Security Infrastructure

| File | G1 | A2 | A3 | D1 | D4 | I1 | XML | Result |
|------|----|----|----|----|----|----|-----|--------|
| `Infrastructure/Validation/NodeUrlValidator.cs` | PASS | n/a | n/a | n/a | n/a | n/a | PASS | PASS |
| `Infrastructure/Crypto/AesGcmColumnEncryptionService.cs` | PASS | n/a | n/a | n/a | n/a | n/a | PASS | PASS |
| `Infrastructure/Crypto/ConfigKeyVaultService.cs` | PASS | n/a | n/a | n/a | n/a | n/a | PASS | PASS |
| `Infrastructure/Crypto/SyncSignalHasher.cs` | PASS | n/a | n/a | n/a | n/a | n/a | PASS | PASS |
| `Infrastructure/Data/TenantContextMiddleware.cs` | PASS | PASS | n/a | n/a | n/a | n/a | PASS | PASS |
| `Infrastructure/Auth/NodeAuthService.cs` | PASS | n/a | PASS | n/a | n/a | n/a | PASS | PASS |
| `Infrastructure/Sync/OfflineQueuePurgeWorker.cs` | PASS | PASS | PASS | n/a | n/a | PASS | PASS | PASS |

### Phase 5 — Application Handlers + API

| File | G1 | A2 | A3 | A5 | A9 | P1 | P2 | P5 | XML | Result |
|------|----|----|----|----|----|----|----|----|-----|--------|
| `Application/Nodes/Commands/RegisterNode/RegisterNodeCommandHandler.cs` | PASS | PASS | PASS | FAIL | PASS | n/a | n/a | n/a | PASS | FAIL |
| `Application/Nodes/Commands/Heartbeat/HeartbeatCommandHandler.cs` | PASS | PASS | PASS | FAIL | PASS | n/a | n/a | n/a | PASS | FAIL |
| `Application/Nodes/Queries/GetManifestQueryHandler.cs` | PASS | PASS | PASS | n/a | PASS | n/a | n/a | n/a | PASS | PASS |
| `Application/Sync/Commands/ReceiveSignal/ReceiveSyncSignalCommandHandler.cs` | PASS | PASS | PASS | FAIL | PASS | n/a | n/a | n/a | PASS | FAIL |
| `Application/Sync/ISyncSignalHasher.cs` | PASS | n/a | n/a | n/a | n/a | n/a | n/a | n/a | PASS | PASS |
| `Api/Endpoints/NodeEndpoints.cs` | PASS | PASS | PASS | n/a | n/a | PASS | PASS | PASS | PASS | PASS |
| `Api/Endpoints/SyncEndpoints.cs` | PASS | PASS | PASS | n/a | n/a | PASS | PASS | PASS | PASS | PASS |
| `Api/Middleware/SipVersionMiddleware.cs` | PASS | PASS | n/a | n/a | n/a | n/a | n/a | n/a | PASS | PASS |

### Phase 6 — FlowManagement Module

| File | Module isolation (no Kernel.Domain ref) | G1 | A2/A3 | D1 | D4 note | I1 | XML | Result |
|------|----------------------------------------|----|-------|----|---------|----|-----|--------|
| `Modules.FlowManagement/Domain/FlowTask.cs` | PASS (Abstractions only) | PASS | n/a | PASS | n/a (not AggregateRoot) | n/a | PASS | PASS |
| `Modules.FlowManagement/Domain/FlowMilestone.cs` | PASS | PASS | n/a | PASS | n/a | n/a | PASS | PASS |
| `Modules.FlowManagement/Domain/FlowProject.cs` | PASS | PASS | n/a | PASS | n/a | n/a | PASS | PASS |
| `Modules.FlowManagement/Domain/FlowProgram.cs` | PASS | PASS | n/a | PASS | n/a | n/a | PASS | PASS |
| `Modules.FlowManagement/Domain/Interfaces/*.cs` | PASS | PASS | PASS | n/a | n/a | n/a | PASS | PASS |
| `Modules.FlowManagement/Services/FlowNodeResolver.cs` | PASS | PASS | PASS | n/a | n/a | FIXED | PASS | PASS |
| `Modules.FlowManagement/Services/OfflineQueueService.cs` | PASS | PASS | PASS | n/a | n/a | FIXED | PASS | PASS |

### Phase 7 — CLAUDE.md Golden Rules

| Rule | Status | Notes |
|------|--------|-------|
| #9 Data Sovereignty | PASS | NodeManifest stores only URL + heartbeat. SyncSignal stores only HMAC hash and state string. No content data reaches the Kernel. |
| #10 Offline First | PASS | OfflineQueueService + OfflineQueuePurgeWorker implemented. TTL-based expiry via SyncConstants.OfflineQueueTtlDays. |
| #11 Security by Default | PASS | NodeUrlValidator enforces HTTPS + port + SSRF blocks. NodeAuthService issues RS256 JWTs. AesGcmColumnEncryptionService implements AES-256-GCM. TenantContextMiddleware sets RLS session var via parameterised set_config. |
| #12 Transactional Integrity | PARTIAL | ReceiveSyncSignalCommandHandler uses ITransactionManager.BeginTransactionAsync + CommitAsync correctly. However PopDomainEvents() is absent (A5 unfixable #6 above) — domain events are not dispatched post-commit. |

---

## Approved Package Compliance

| Layer | Unapproved packages | Newtonsoft.Json | Result |
|-------|---------------------|-----------------|--------|
| SpaceOS.Modules.FlowManagement | None | None | PASS |
| SpaceOS.Modules.Abstractions | None | None | PASS |
| All other in-scope projects | None | None | PASS |

---

## Build & Test Result

- Build: 0 errors, 0 warnings
- Tests: 629 passing (491 unit + 92 integration + 46 API), 0 failed

---

## Developer Action Required

The three D4 violations (NodeManifest, SyncSignal) and their three dependent A5 violations in the command handlers are the only outstanding blockers. All require coordinated architectural decisions:

1. **Decide** whether heartbeat mutations warrant a domain event (high-frequency, fire-and-forget vs. bus dispatch).
2. **Define** `NodeRegisteredEvent`, `NodeHeartbeatRecordedEvent`, and `SyncSignalReceivedEvent` in Domain.
3. **Add** `AddDomainEvent(...)` calls in the respective `Create()` / `UpdateHeartbeat()` / `SyncSignal.Create()` methods.
4. **Inject** `IDomainEventDispatcher` into the three command handlers and add `PopDomainEvents()` + `DispatchAsync()` after each successful commit.
5. **Add** companion unit tests for the new event assertions.

Until these decisions are made, status remains **REVIEW_FAILED**.
