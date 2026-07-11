# ADR-005 — Advisory Lock for Audit Event Hash Chain

| Field        | Value                                              |
|--------------|----------------------------------------------------|
| Status       | Accepted                                           |
| Date         | 2026-04-06                                         |
| Deciders     | SpaceOS Kernel team                                |
| Supersedes   | —                                                  |
| Superseded by| —                                                  |

---

## Context

The SpaceOS Kernel maintains a per-tenant SHA-256 hash chain in the `AuditEvents` table.
Every event stores:

- `StateHash` — SHA-256 of `{previousHash}:{payload}:{occurredAt}:{sourceBrand}`.
- `PreviousHash` — the `StateHash` of the immediately preceding event for the same tenant.

The chain invariant is: **`events[i].PreviousHash == events[i-1].StateHash`** for all `i > 0`.

This invariant is broken if two writers concurrently:

1. Both read the same tail hash (e.g. hash of event N).
2. Both compute a new hash using that same `previousHash`.
3. Both persist — producing two events (N+1a and N+1b) with identical `PreviousHash` values.

The `VerifyChainQueryHandler` detects such a fork immediately: the first broken link is reported
and `IsValid: false` is returned.

---

## Decision

Introduce an `IAuditWriteLock` abstraction with two environment-specific implementations:

### 1. `InProcessAuditWriteLock` — Development / SQLite

```csharp
// SpaceOS.Infrastructure/AuditLog/InProcessAuditWriteLock.cs
private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();
```

- Backed by a static `ConcurrentDictionary<Guid, SemaphoreSlim>`.
- One `SemaphoreSlim(1,1)` per tenant GUID, created lazily.
- `WaitAsync(ct)` blocks until the previous writer's `DisposeAsync()` releases the semaphore.
- **Scope:** single process only. Multiple processes each maintain independent dictionaries and
  cannot see each other's semaphores.

Registered in DI when `environment.IsDevelopment()` is true.

### 2. `PostgresAdvisoryAuditWriteLock` — Production / PostgreSQL

```sql
SELECT pg_try_advisory_xact_lock({lockKey})
```

- Uses PostgreSQL's `pg_try_advisory_xact_lock(bigint)` — a non-blocking exclusive lock
  held for the duration of the current transaction.
- Lock key: `(long)(uint)(tenantId.GetHashCode())` — folds the tenant GUID into the
  non-negative `int8` range PostgreSQL requires.
- The lock is released automatically at transaction commit or rollback — no explicit release
  call is needed.
- **Scope:** database-server level. All API processes connected to the same PostgreSQL instance
  share the same advisory lock namespace, providing true cross-process exclusion.

Registered in DI when the environment is not Development.

---

## Consequences

### Positive

- **No forked chains:** concurrent appends are serialised at the database level in production
  and at the process level in development/tests.
- **No schema changes:** advisory locks are pure in-memory PostgreSQL state — no additional
  tables or indexes are required.
- **Automatic release:** `pg_try_advisory_xact_lock` is scoped to the transaction. A crashed
  or cancelled write automatically releases the lock when the transaction rolls back.
- **Testable:** the `InProcessAuditWriteLock` is load-tested in
  `AuditEventRaceConditionTests` with 50 concurrent writers. A `PreviousHash` uniqueness
  assertion and a `VerifyChainQueryHandler` chain-integrity assertion both confirm correct
  behaviour under contention.

### Negative / Trade-offs

- **Single-instance constraint in development:** `InProcessAuditWriteLock` only serialises
  within one OS process. Running two development API processes against the same SQLite file
  would produce a forked chain. This is accepted because SQLite is a single-file database
  that does not support concurrent multi-process writes anyway.
- **Spin loop in production:** `PostgresAdvisoryAuditWriteLock` spins with a 10 ms back-off
  rather than using the blocking `pg_advisory_xact_lock`. This avoids holding a long-lived
  database wait slot but adds latency under high contention. The maximum observed contention
  is bounded by tenant write throughput, which is expected to be low.
- **Hash key collision probability:** The lock key is derived from `tenantId.GetHashCode()`,
  which has a 32-bit range. Two different tenant GUIDs with the same hash code would share
  a lock key — they would unnecessarily serialise each other but would not corrupt each
  other's chain. The probability of collision across the expected number of tenants is
  negligible.

---

## Alternatives Considered

### A — `SELECT FOR UPDATE` on the latest audit row

The dispatcher could acquire a row lock via `SELECT ... FOR UPDATE` on the most recent
`AuditEvents` row for the tenant. This avoids advisory locks entirely but requires a row
to exist (the genesis case needs special handling) and holds a table-page lock that could
delay unrelated reads.

**Rejected:** More complex, heavier locking footprint, genesis-case edge case.

### B — Serializable transaction isolation

Using `REPEATABLE READ` or `SERIALIZABLE` isolation allows PostgreSQL to detect the
write conflict and raise a serialization error. The application would then retry.

**Rejected:** Requires application-level retry logic. Serialization failures generate
additional round-trips and are difficult to distinguish from genuine errors at the
application layer.

### C — Distributed lock (Redis, etcd)

A distributed lock manager (Redis SETNX / Redlock, etcd leases) would provide cross-host
exclusion without depending on PostgreSQL.

**Rejected:** Introduces a new infrastructure dependency. PostgreSQL is already the
authoritative store; advisory locks keep the locking co-located with the data.

---

## References

- `SpaceOS.Infrastructure/AuditLog/InProcessAuditWriteLock.cs`
- `SpaceOS.Infrastructure/AuditLog/PostgresAdvisoryAuditWriteLock.cs`
- `SpaceOS.Kernel.Application/AuditLog/IAuditWriteLock.cs`
- `SpaceOS.Kernel.IntegrationTests/AuditLog/AuditEventRaceConditionTests.cs`
- PostgreSQL documentation: [Advisory Locks](https://www.postgresql.org/docs/current/explicit-locking.html#ADVISORY-LOCKS)
