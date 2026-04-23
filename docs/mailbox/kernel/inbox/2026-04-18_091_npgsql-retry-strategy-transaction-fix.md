---
id: MSG-KERNEL-091
from: root
to: kernel
type: task
priority: critical
status: READ
ref: MSG-KERNEL-090-DONE
created: 2026-04-18
---

# KRITIKUS — POST /api/tenants → 500 (NpgsqlRetryingExecutionStrategy + BeginTransaction)

## Új hiba (INFRA-017 deploy után, journalctl)

```
System.InvalidOperationException:
The configured execution strategy 'NpgsqlRetryingExecutionStrategy'
does not support user-initiated transactions.
Use the execution strategy returned by 'DbContext.Database.CreateExecutionStrategy()'
to execute all the operations in the transaction as a retriable unit.
```

A `e448f2d` (AuditEvent Sequence fix) deployolva, de a `POST /api/tenants` még mindig 500.

## Root cause

A tenant CREATE handler (vagy az általa hívott repository/use case) explicit `BeginTransaction()`-t hív a `DbContext`-en, miközben a Npgsql connection stringben `Retry` strategy van konfigurálva. EF Core megtiltja az explicit tranzakciót retry strategy esetén, kivéve ha a hívás a `CreateExecutionStrategy().ExecuteAsync()` blokkon belül van.

## Fix

Keresd meg a tenant CREATE handler-t (pl. `TenantEndpoints.cs`, `CreateTenantCommandHandler.cs`, vagy az érintett repository-t), ahol explicit transaction van nyitva:

```csharp
// JELENLEGI (hibás):
await using var tx = await _context.Database.BeginTransactionAsync(ct);
try { ... await tx.CommitAsync(ct); }
catch { await tx.RollbackAsync(ct); throw; }

// HELYES — ExecutionStrategy wrapper:
var strategy = _context.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    await using var tx = await _context.Database.BeginTransactionAsync(ct);
    try { ... await tx.CommitAsync(ct); }
    catch { await tx.RollbackAsync(ct); throw; }
});
```

Ez pontosan az EF Core docs mintája: [Resiliency docs — User-initiated transactions](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency#user-initiated-transactions-with-execution-strategies)

## Keresési pontok

```bash
# Hol van BeginTransaction az érintett context-ben?
grep -rn "BeginTransaction" SpaceOS.Kernel.Api/ SpaceOS.Infrastructure/ | grep -v ".bak"
```

## DoD

- [ ] `POST /api/tenants` → **201** VPS-en (curl)
- [ ] `dotnet test` → legalább 1138 zöld
- [ ] INFRA deploy szükséges → jelezd outbox-ban

---

*Skill: `/spaceos-terminal`*
