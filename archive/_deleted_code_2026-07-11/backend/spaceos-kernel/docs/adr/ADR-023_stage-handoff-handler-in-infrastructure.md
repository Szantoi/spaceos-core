# ADR-023 — CreateStageHandoffCommandHandler placed in Infrastructure layer

**Date:** 2026-04-11  
**Status:** Accepted  
**Deciders:** Kernel team  
**Tags:** architecture, CQRS, advisory-lock, Infrastructure

---

## Context

The Clean Architecture rule for this solution is:

```
Domain ← Application ← Infrastructure
```

All CQRS command handlers must reside in the `Application` layer, which must not reference EF Core, `AppDbContext`, or any infrastructure-specific concern.

`CreateStageHandoffCommandHandler` requires:
1. **`pg_advisory_xact_lock`** — a PostgreSQL-specific advisory lock that serializes concurrent handoff creation for the same chain template, preventing duplicate version gaps in the handoff sequence.
2. **Explicit transaction control** — the lock must be acquired within the same transaction that inserts the new `StageHandoff` row.
3. **Direct `AppDbContext` access** — `pg_advisory_xact_lock` is issued via `context.Database.ExecuteSqlRawAsync(...)`, which requires a direct reference to `AppDbContext`. This is not expressible through the `IStageHandoffRepository` interface without leaking infrastructure concerns into the domain repository contract.

---

## Decision

`CreateStageHandoffCommandHandler` is placed in `SpaceOS.Infrastructure/StageRegistry/` instead of `SpaceOS.Kernel.Application/StageRegistry/Commands/`.

It is still registered as a MediatR `IRequestHandler` and behaves like any other command handler from the caller's perspective.

---

## Consequences

### Positive
- Advisory lock semantics are encapsulated entirely within the Infrastructure layer — no leakage into Application or Domain.
- The `IStageHandoffRepository` interface remains clean (no `BeginTransaction` or lock methods).
- The handler is co-located with other PostgreSQL-specific infrastructure code.

### Negative
- One handler violates the structural convention of handlers living in Application. This must be documented (this ADR) and reviewed at onboarding.
- The handler cannot be unit-tested with SQLite in-memory because `pg_advisory_xact_lock` is PostgreSQL-only. Four integration tests are marked `[Trait("Category", "RequiresPostgres")]` and skipped in the SQLite test suite.

### Mitigations
- The handler is `internal sealed` — it cannot be referenced from outside the Infrastructure assembly.
- The exception is singular — no other command handler may follow this pattern without a new ADR.
- A comment in the handler source (`// ADR-023`) links to this document.

---

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| Add `BeginTransactionAsync` + `AcquireAdvisoryLockAsync` to `IStageHandoffRepository` | Leaks infrastructure/PostgreSQL semantics into the domain repository interface — violates Domain purity |
| Use application-level locking (e.g. `SemaphoreSlim`) | Not effective across multiple API instances / pods |
| Remove advisory lock, accept race condition | Risk of duplicate `Version` gaps in the handoff sequence — violates DB-02 constraint |
| Use `SERIALIZABLE` isolation level for all handoff writes | Broad performance impact; advisory lock is scoped and precise |

---

## Rule

> **Except for `CreateStageHandoffCommandHandler` (this ADR), all command and query handlers MUST reside in `SpaceOS.Kernel.Application`.**
>
> Any future handler that requires direct `AppDbContext` access or PostgreSQL-specific constructs must have its own ADR approved before implementation.
