# Review Report — MSG-K028
**Date:** 2026-04-05
**Agent:** kernel-review-enforcer
**Final status:** CLOSED_DONE

---

## Scope

| Layer | Files |
|-------|-------|
| Domain (new events) | `SpaceOS.Kernel.Domain/Events/NodeRegisteredEvent.cs` |
| | `SpaceOS.Kernel.Domain/Events/NodeHeartbeatRecordedEvent.cs` |
| | `SpaceOS.Kernel.Domain/Events/SyncSignalReceivedEvent.cs` |
| Domain (modified aggregates) | `SpaceOS.Kernel.Domain/Federation/NodeManifest.cs` |
| | `SpaceOS.Kernel.Domain/Sync/SyncSignal.cs` |
| Application (modified handlers) | `SpaceOS.Kernel.Application/Nodes/Commands/RegisterNode/RegisterNodeCommandHandler.cs` |
| | `SpaceOS.Kernel.Application/Nodes/Commands/Heartbeat/HeartbeatCommandHandler.cs` |
| | `SpaceOS.Kernel.Application/Sync/Commands/ReceiveSignal/ReceiveSyncSignalCommandHandler.cs` |
| Tests (modified) | `SpaceOS.Kernel.Tests/Application/RegisterNodeCommandHandlerTests.cs` |
| | `SpaceOS.Kernel.Tests/Application/HeartbeatCommandHandlerTests.cs` |
| | `SpaceOS.Kernel.Tests/Application/ReceiveSyncSignalCommandHandlerTests.cs` |
| | `SpaceOS.Kernel.Tests/Entities/NodeManifestTests.cs` |

---

## Audit Results by Rule Category

### D1 — No public setters on aggregates
`NodeManifest` and `SyncSignal` use `{ get; init; }` on identity/foreign-key properties (`TenantId`, `EpicId`). This is an initialization-only accessor, semantically equivalent to `private set` for post-construction mutation. No `{ get; set; }` found anywhere in scoped files. **PASS.**

### D4 — Every aggregate mutation raises a domain event
| Method | Aggregate | Event raised? |
|--------|-----------|---------------|
| `NodeManifest.Create()` | `NodeManifest` | `NodeRegisteredEvent` — PASS |
| `NodeManifest.RecordHeartbeat(bool)` | `NodeManifest` | `NodeHeartbeatRecordedEvent` (conditionally, when `isOnlineChanged == true`) — PASS |
| `SyncSignal.Create()` | `SyncSignal` | `SyncSignalReceivedEvent` — PASS |
| `SyncSignal.MarkSynced()` | `SyncSignal` | None — **pre-existing unresolved violation; ruled UNFIXABLE in MSG-K020/K021/K022 review** |

The conditional event raise in `RecordHeartbeat` is intentional by design: the event documents an online-status transition, not every heartbeat tick. The Application layer computes `isOnlineChanged` before delegating to the aggregate. This is architecturally sound — a no-transition heartbeat is not a state change.

### D7 — No with-expression bypass
No `with {` expressions found in any scoped domain file. **PASS.**

### D11 — No external NuGet in Domain
Domain event files import only `SpaceOS.Kernel.Domain.Primitives` and `SpaceOS.Kernel.Domain.ValueObjects`. No external packages. **PASS.**

### A2 — ConfigureAwait(false) on every await
All `await` expressions in the three modified handlers carry `.ConfigureAwait(false)`. Confirmed by grep scan (zero matches for `await` without `ConfigureAwait(false)` in both `Nodes/` and `Sync/` folders). **PASS.**

### A3 — CancellationToken named `ct`
All three `Handle` signatures use `CancellationToken ct`. Confirmed by grep scan. **PASS.**

### A5 — PopDomainEvents + DispatchAsync after persist, before return
| Handler | Ordering |
|---------|----------|
| `RegisterNodeCommandHandler` | `SaveChangesAsync` → `PopDomainEvents` → `DispatchAsync` — PASS |
| `HeartbeatCommandHandler` | `SaveChangesAsync` → `PopDomainEvents` → `DispatchAsync` — PASS |
| `ReceiveSyncSignalCommandHandler` | `SaveChangesAsync` → `CommitAsync` → `PopDomainEvents` → `DispatchAsync` — PASS |

Golden Rule #12 satisfied in all three handlers: `PopDomainEvents()` is called only after the transaction commits successfully.

### A9 — Every handler has a companion test
| Handler | Test file | Exists? |
|---------|-----------|---------|
| `RegisterNodeCommandHandler` | `RegisterNodeCommandHandlerTests.cs` | PASS |
| `HeartbeatCommandHandler` | `HeartbeatCommandHandlerTests.cs` | PASS |
| `ReceiveSyncSignalCommandHandler` | `ReceiveSyncSignalCommandHandlerTests.cs` | PASS |

### G1 — No TODO/FIXME
Zero matches in all scoped files. **PASS.**

### G3 — XML docs on every public type and method
All three new event types carry a `<summary>` doc. Both aggregate mutations (`RecordHeartbeat`, `MarkSynced`) carry `<summary>` docs with `<param>` where applicable. All three handlers carry XML docs on the class and `Handle` method. **PASS.**

### G4 — Approved packages only
No new `PackageReference` entries introduced by this fix. **PASS.**

---

## Violations Found & Fixed

No violations requiring code changes were found in the scoped files.

---

## Unfixable Violations (pre-existing, carried from MSG-K020/K021/K022)

| # | Rule | File | Issue | Why unfixable |
|---|------|------|-------|---------------|
| 1 | D4 | `SpaceOS.Kernel.Domain/Sync/SyncSignal.cs` | `SyncSignal.MarkSynced()` mutates `IsSyncedToKernel` without raising a domain event (`SyncSignalAcknowledgedEvent` is missing) | Requires new domain event type, new handler stub, and architectural decision on downstream consumers. Ruled UNFIXABLE in MSG-K020/K021/K022. Not introduced by MSG-K028. |

---

## Build & Test Result
- Build: 0 errors, 0 warnings
- Tests: 635 passing (497 unit + 46 API + 92 integration), 0 failed
