---
id: MSG-KERNEL-093
from: root
to: kernel
type: task
priority: critical
status: READ
ref: MSG-INFRA-018b-BLOCKED
created: 2026-04-18
---

# KERNEL-093 — 4. transaction call site: TenantEndpoints.cs line ~104

## Szimptóma

KERNEL-091 fix (6e7b87b) deployolva, `/healthz` 200, de `POST /api/tenants` **még mindig 500**:

```
System.InvalidOperationException: The configured execution strategy
'NpgsqlRetryingExecutionStrategy' does not support user-initiated transactions.
   at TenantEndpoints.cs:line 104
```

## Root cause

A KERNEL-091 3 call site-ot fedett le, de **legalább egy 4. hely** maradt. A stack trace `TenantEndpoints.cs:line 104`-ra mutat.

## Teendő

### 1. Keress meg minden explicit transaction hívást

```bash
grep -rn "BeginTransaction" src/ --include="*.cs"
grep -rn "ExecuteStrategy\|CreateExecutionStrategy" src/ --include="*.cs"
```

### 2. TenantEndpoints.cs line ~104

Valószínűleg a tenant create handler belső LINQ/EF query fut explicit transaction scope-ban (pl. `SingleAsync`, `FirstOrDefaultAsync`, stb. egy még nyitott TX-en belül, ami nem ExecutionStrategy-ba van csomagolva).

**Fix:** az érintett tenant create flow teljes body-ját csomagold be:

```csharp
var strategy = _context.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    await using var tx = await _context.Database.BeginTransactionAsync(ct);
    try
    {
        // ... teljes tenant create logika ide ...
        await tx.CommitAsync(ct);
    }
    catch
    {
        await tx.RollbackAsync(ct);
        throw;
    }
});
```

### 3. Teljes audit — ne maradjon be nem csomagolt hívás

Az összes `BeginTransactionAsync` hívást nézd meg a codebase-ben és győződj meg mindegyik `CreateExecutionStrategy().ExecuteAsync()` blokkban van.

## DoD

- [ ] `POST /api/tenants` → **201** VPS-en
- [ ] `dotnet test` → legalább 1138 zöld
- [ ] INFRA redeploy szükséges (jelezd DONE-ban)

---

*Skill: `/spaceos-terminal`*
