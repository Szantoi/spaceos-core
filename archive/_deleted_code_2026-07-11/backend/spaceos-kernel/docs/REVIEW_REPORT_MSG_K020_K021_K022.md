# Review Report — MSG-K020, MSG-K021, MSG-K022 (Sprint C Phase 1–3)
**Date:** 2026-04-04
**Agent:** kernel-review-enforcer
**Final status:** REVIEW_FAILED

---

## Violations Found & Fixed

| # | Rule | File | Violation | Fix Applied |
|---|------|------|-----------|-------------|
| 1 | I-config: `internal sealed` | `SpaceOS.Infrastructure/Data/Configurations/FlowEpicConfiguration.cs` | Class declared `public class` — infrastructure configurations must be `internal sealed` to prevent leakage outside the assembly | Changed to `internal sealed class FlowEpicConfiguration` |
| 2 | I-config: `sealed` DbContext | `SpaceOS.Modules.FlowManagement/Infrastructure/ModulesDbContext.cs` | Class declared `public class` — DbContext must be `sealed` to prevent inheritance-based lifetime bugs; no derivation is needed | Changed to `public sealed class ModulesDbContext` |

---

## Unfixable Violations (requires developer decision)

| # | Rule | File | Issue | Why unfixable |
|---|------|------|-------|---------------|
| 1 | D4 — every mutation raises a domain event | `SpaceOS.Kernel.Domain/Federation/NodeManifest.cs` | `NodeManifest.Create()` creates a new aggregate root but never calls `AddDomainEvent(...)`. A `NodeManifestRegisteredEvent` (or equivalent) is required. `UpdateHeartbeat()` mutates state without an event (`NodeHeartbeatReceivedEvent` is needed). | Requires new `readonly record struct` domain events, companion `INotificationHandler` in Application, and a decision on whether downstream side-effects (e.g. audit, federation sync) should be triggered. Architectural decision — cannot fix without developer sign-off. |
| 2 | D4 — every mutation raises a domain event | `SpaceOS.Kernel.Domain/Sync/SyncSignal.cs` | `SyncSignal.Create()` and `SyncSignal.MarkSynced()` both mutate aggregate state with no `AddDomainEvent(...)` call. `SyncSignalCreatedEvent` and `SyncSignalAcknowledgedEvent` are missing. | Same reasoning as #1. These are new aggregate root mutations and require new event types, handler stubs, and downstream wiring decisions. |

---

## Additional Observations (no rule violation — informational)

These are not CLAUDE.md violations. Recorded for developer awareness:

| # | Area | File | Note |
|---|------|------|------|
| 1 | Duplicate constant | `SpaceOS.Kernel.Domain/Sync/SyncConstants.GenesisHash = "GENESIS"` vs `SpaceOS.Modules.Abstractions/Sync/SyncConstants.GenesisHash = "000...0"` | The two constants diverge in value. The Domain uses `"GENESIS"` (a human-readable sentinel); Abstractions uses the 64-zero hex string (a hash-chain convention). If `SyncSignalRepository.GetLastHashAsync` is meant to interoperate with the audit-chain hash verifier, these must agree. Confirm intentional divergence or unify. |
| 2 | `ITransactionManager` in Domain | `SpaceOS.Kernel.Domain/Repositories/ITransactionManager.cs` | Interface lives in `Domain.Repositories` but depends on no domain type — it is a pure infrastructure protocol. The domain should not own infrastructure transaction semantics. Consider moving to `Application` layer. Not a blocking violation because the domain project has no EF/infrastructure NuGet dep and the interface is pure C#. |
| 3 | `ISyncSignalWriteLock` in Application | `SpaceOS.Kernel.Application/Sync/ISyncSignalWriteLock.cs` | `AcquireAsync` accepts a raw `Guid tenantId` parameter instead of `TenantId` (the domain value object). This breaks VO encapsulation at the Application/Infrastructure boundary. Not a D1 violation but weakens the type system. |
| 4 | Production write lock | `SpaceOS.Infrastructure/DependencyInjection.cs` line 117 | `ISyncSignalWriteLock` is registered as `InProcessSyncSignalWriteLock` in **both** Development and Production (`else` branch). The class comment explicitly flags this as single-instance only. For multi-instance production a distributed lock (PostgreSQL advisory lock) is required. This is a deployment risk, not a code rule violation. |
| 5 | `NodeManifest.TenantId` is `init` not `private set` | `SpaceOS.Kernel.Domain/Federation/NodeManifest.cs` line 17 | `TenantId { get; init; }` exposes an `init` setter. While this is safe for object initialisers at construction time, it is inconsistent with the domain rule that only explicit mutating methods change aggregate state. `EpicId` on `SyncSignal` (line 19) has the same pattern. These properties are set only in the static factory, so the risk is low, but the convention should be `private set` for consistency. |

---

## Build & Test Result
- Build: 0 errors, 0 warnings (`-warnaserror` flag active)
- Tests: 590 passing (452 unit + 46 API + 92 integration), 0 failed

---

## Scope Reviewed

| Phase | Area | Files | Status |
|-------|------|-------|--------|
| 1 | `SpaceOS.Modules.Abstractions/` — 25 files | All interfaces, enums, VOs, constants | PASS — zero external deps confirmed, XML docs present, no public setters |
| 2 | Domain: `NodeManifest`, `SyncSignal`, `B2BHandshake`, `ITransactionManager`, `ISyncSignalWriteLock` | 7 files | REVIEW_FAILED (D4 — no domain events) |
| 2 | Infrastructure: `NodeManifestConfiguration`, `SyncSignalConfiguration`, `NodeManifestRepository`, `SyncSignalRepository`, `InProcessSyncSignalWriteLock`, `EfTransactionManager`, `DependencyInjection`, `FlowEpicConfiguration` | 8 files | PASS after fix #1 |
| 3 | `Modules.FlowManagement/` — 10 files | Domain entities, `ModulesDbContext`, 5 configs | PASS after fix #2 |
| 3 | `PostgresSchemaInitializer`, `ITenantConnectionResolver`, `SharedTenantConnectionResolver`, `Program.cs` | 4 files | PASS |
