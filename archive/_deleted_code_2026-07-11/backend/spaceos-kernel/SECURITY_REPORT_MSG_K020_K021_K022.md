# Security Report — MSG-K020 / K021 / K022 (Sprint C Phase 1-3)
**Date:** 2026-04-04
**Agent:** kernel-security-scanner
**Scope:** Sprint C Phase 1-3 changes — Modules.Abstractions, Domain federation/sync/auth, Infrastructure persistence/sync/auth, FlowManagement module, API registration
**Final status:** SECURITY_FAILED

---

## Supply Chain Findings

All new `.csproj` files introduced in Sprint C Phase 1-3 audited against the approved package list.

| Package | Project | Version | On Approved List | CVE | Severity | Action |
|---------|---------|---------|-----------------|-----|----------|--------|
| `Microsoft.EntityFrameworkCore` | SpaceOS.Modules.FlowManagement | 8.0.11 | Yes (EF Core 8) | None | PASS | Approved |
| `Microsoft.EntityFrameworkCore.Relational` | SpaceOS.Modules.FlowManagement | 8.0.11 | Yes (EF Core 8) | None | PASS | Approved |
| `SpaceOS.Modules.Abstractions` (project ref) | SpaceOS.Modules.FlowManagement | — | N/A | — | PASS | Zero external NuGet packages in Abstractions — clean |
| `Microsoft.Data.Sqlite` (carry-forward) | Api.Tests, IntegrationTests | 8.0.11 | No | None | WARNING | Test-only; re-flag until approved or removed |
| `Npgsql.EntityFrameworkCore.PostgreSQL` (carry-forward) | SpaceOS.Infrastructure | 8.0.11 | No (original list) | None | WARNING | Production driver; re-flag until REVIEW_CHECKLIST.md updated |

**No new packages outside the approved set were introduced in this sprint.**
`SpaceOS.Modules.Abstractions.csproj` has zero `<PackageReference>` entries — domain contract layer maintains the zero-external-dependency rule.

---

## Static Analysis Findings

| # | Rule | File | Line | Severity | Auto-fixed | Action Required |
|---|------|------|------|----------|------------|-----------------|
| S1 | `spaceos-no-hardcoded-connstring` / `p/secrets` | `scripts/db-init.sql` | 8 | ERROR | No | **Carry-forward — unresolved.** Hardcoded password `'changeme_in_production'` for `spaceos_audit_writer` PostgreSQL role. Replace with environment-injected credential at container startup. |
| S2 | `p/secrets` — `Password=` key in committed appsettings | `SpaceOS.Kernel.Api/appsettings.json` | 3 | WARNING | No | Value is empty — no credential leak. However the `Password=` key in a committed base appsettings exposes connection string structure. Recommend removing `Password=` from the template and requiring `ConnectionStrings__DefaultConnection` env var entirely at startup. |
| S3 | `p/csharp` — `ExecuteSqlRawAsync` | `SpaceOS.Infrastructure/Data/PostgresSchemaInitializer.cs` | 30, 38 | INFO | N/A | Both calls use fully static DDL string literals — no user input is interpolated. `CREATE OR REPLACE VIEW` DDL is idempotent. No SQL injection risk. Clear. |

---

## Custom Rule Findings

| # | Rule ID | File | Line | Severity | Fixed |
|---|---------|------|------|----------|-------|
| C1 | `spaceos-no-auto-migrate` — EnsureCreated scope | `SpaceOS.Kernel.Api/Program.cs` | 199, 202 | INFO | N/A |
| C2 | `spaceos-ssrf-url-validator-not-implemented` | `SpaceOS.Modules.Abstractions/Actors/INodeUrlValidator.cs` + entire solution | — | ERROR | No |
| C3 | `spaceos-inprocess-lock-in-production` | `SpaceOS.Infrastructure/DependencyInjection.cs` | 117 | WARNING | No |
| C4 | `spaceos-offline-queue-payload-unbounded` | `SpaceOS.Modules.FlowManagement/Infrastructure/Configurations/OfflineSyncQueueItemConfiguration.cs` | 23 | WARNING | No |

---

## Finding Detail

### S1 — Hardcoded password in db-init.sql (ERROR / BLOCKER — carry-forward)

`scripts/db-init.sql` line 8:
```sql
CREATE ROLE spaceos_audit_writer LOGIN PASSWORD 'changeme_in_production';
```

Credential committed to source control. This finding has been open since the first security scan and is re-flagged per scanner memory policy. Must be resolved before any production PostgreSQL container is provisioned.

**Required action:** Replace with an environment-variable-driven credential. Options:
- Pass the password via `POSTGRES_PASSWORD` / a secrets manager at container startup using `psql -c "CREATE ROLE ... PASSWORD '$AUDIT_WRITER_PASSWORD'"`
- Use Docker Secrets or Kubernetes Secrets to mount the credential and reference it in the init script

### S2 — `Password=` key in appsettings.json (WARNING)

`SpaceOS.Kernel.Api/appsettings.json` contains:
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=spaceos;Username=spaceos;Password="
```

The `Password=` key with an empty value is not a credential leak but violates the policy that connection strings must come from environment variables, not from committed appsettings. The presence of this key trains developers to assume a base-template connection string exists in source.

**Required action (recommended):** Remove the `Password=` key from the committed template. The `SharedTenantConnectionResolver` and `DependencyInjection.cs` already guard with `?? throw new InvalidOperationException(...)`, so startup will fail fast if the env var is missing — which is the correct behaviour.

### C1 — EnsureCreated in Program.cs (INFO / clean)

`db.Database.EnsureCreatedAsync()` at lines 199 and 202 is correctly guarded by `if (app.Environment.IsDevelopment())`. The production `else` branch calls only `PostgresSchemaInitializer.ApplyAsync()`, which applies idempotent DDL views and does not auto-migrate. The custom rule `spaceos-no-auto-migrate` targets `Database.Migrate()` — not `EnsureCreated()` in a development-only guard. **No action required.**

### C2 — INodeUrlValidator has no concrete implementation — SSRF gap (ERROR / BLOCKER)

`INodeUrlValidator` is declared in `SpaceOS.Modules.Abstractions/Actors/INodeUrlValidator.cs` and is the explicit SSRF-prevention contract for all outbound federation URLs. The `NodeManifest.Create()` factory comment delegates URL validation to the Application layer via this interface.

**Search result:** No concrete implementation of `INodeUrlValidator` exists anywhere in the solution. The interface is not registered in `SpaceOS.Infrastructure/DependencyInjection.cs` or anywhere else. Any command handler that calls `NodeManifest.Create(tenantId, serverUrl)` with an API-supplied `serverUrl` currently has no SSRF guard.

Cross-reference: scanner memory entry `project_spacelayer_ssrf_risk.md` tracks the federation SSRF surface. `NodeManifest.ServerUrl` is a second, structurally identical risk: it is a user-supplied URL stored in the database that will be used as the target of outbound HTTP calls by the federation execution layer.

**Required action (blocking):** Before any node-registration API endpoint is wired up and deployed:
1. Implement `INodeUrlValidator` in `SpaceOS.Infrastructure` or `SpaceOS.Kernel.Application`. The implementation must:
   - Require HTTPS scheme
   - Reject RFC-1918 ranges (10.x, 172.16–31.x, 192.168.x)
   - Reject loopback (127.x, `::1`)
   - Reject link-local (169.254.x)
   - Reject non-absolute URIs
2. Register the implementation in `DependencyInjection.cs`
3. All `NodeManifest` creation/update command validators must call `INodeUrlValidator.Validate()` before invoking `NodeManifest.Create()`

### C3 — InProcessSyncSignalWriteLock registered in production branch (WARNING)

`SpaceOS.Infrastructure/DependencyInjection.cs` line 117 (non-development branch):
```csharp
services.AddScoped<ISyncSignalWriteLock, InProcessSyncSignalWriteLock>();
```

The class doc states: "For multi-instance production deployments, replace with a distributed lock (e.g., PostgreSQL advisory lock)."

In a single-instance deployment this is safe. In any horizontally-scaled deployment, concurrent writes from different instances can corrupt the `SyncSignal` hash chain. The `PreviousHash` / `StateHash` tamper-detection guarantee breaks silently — two instances can race to read the same "last hash" and produce a forked chain.

Compare: `IAuditWriteLock` correctly uses `PostgresAdvisoryAuditWriteLock` in production. `ISyncSignalWriteLock` does not have an equivalent.

**Required action:** Before multi-instance production deployment, implement `PostgresAdvisorySyncSignalWriteLock` following the existing advisory lock pattern and register it in the production branch.

### C4 — OfflineSyncQueueItem.Payload has no column size limit (WARNING)

`OfflineSyncQueueItemConfiguration` configures `Payload` as:
```csharp
builder.Property(q => q.Payload).IsRequired();
```

No `HasMaxLength()` is applied. The domain factory `OfflineSyncQueueItem.Create()` validates only that `payload` is not null or whitespace — no upper-bound check. An authenticated caller can submit arbitrarily large payloads, causing unbounded row sizes in the `OfflineSyncQueue` table and potential memory pressure in any background consumer that loads queue items.

**Required action:** Add `HasMaxLength(65535)` (or a named domain constant) to the EF Core configuration and enforce the same bound in `OfflineSyncQueueItem.Create()` with an `ArgumentOutOfRangeException`.

---

## Architecture Observations (not security findings)

| Observation | File | Notes |
|-------------|------|-------|
| `ModulesDbContext` is not `sealed` | `SpaceOS.Modules.FlowManagement/Infrastructure/ModulesDbContext.cs` | All other DbContext types in the solution are sealed. Deviation from convention — not a security issue. |
| `SyncSignalConfiguration.StateHash` maxlength is 64 | `SyncSignalConfiguration.cs:38` | Correct for HMAC-SHA256 hex output. |
| `SyncSignalConfiguration.PreviousHash` maxlength is 64 | `SyncSignalConfiguration.cs:43` | Correct. `SyncConstants.GenesisHash = "GENESIS"` (7 chars) fits within 64. |
| `NodeManifestConfiguration` enforces unique index on `TenantId` | `NodeManifestConfiguration.cs:28` | Correct — one manifest per tenant enforced at DB level. |
| `B2BHandshake` anchor fields stored as raw JSON strings | `Domain/ValueObjects/B2BHandshake.cs` | Avoids Domain dependency on Abstractions. Deserialization at Application/Infrastructure layer is correct and the comment is explicit. No injection risk in this layer. |
| `SharedTenantConnectionResolver` reads connection string once at construction | `SharedTenantConnectionResolver.cs:26` | Connection string is cached as a private `readonly` field. Never logged. Clean. |
| `EfTransactionManager.TransactionRollbackGuard` disposes but does not auto-rollback | `EfTransactionManager.cs:62` | The guard disposes the transaction on exception paths, but disposal of a `IDbContextTransaction` without prior `Commit()` triggers implicit rollback in EF Core. Safe. |

---

## Carry-Forward Findings (from prior scans — still unresolved)

| Finding | File | Severity | Status |
|---------|------|----------|--------|
| Hardcoded password `changeme_in_production` | `scripts/db-init.sql:8` | ERROR | Unresolved — re-flagged as S1 |
| `Microsoft.Data.Sqlite` not on approved package list | `*.Tests.csproj` | WARNING | Unresolved — test-only |
| `Npgsql.EntityFrameworkCore.PostgreSQL` not on original approved list | `SpaceOS.Infrastructure.csproj` | WARNING | Unresolved |

---

## Summary

| Severity | Count | Disposition |
|----------|-------|-------------|
| CRITICAL | 0 | — |
| ERROR | 2 | S1: db-init.sql hardcoded password (carry-forward); C2: INodeUrlValidator not implemented — SSRF gap |
| WARNING | 4 | S2: appsettings.json Password= key; C3: InProcessLock in production; C4: Payload unbounded; supply-chain carry-forwards (×2) |
| INFO | 2 | S3: ExecuteSqlRawAsync static DDL — clean; C1: EnsureCreated dev-only — clean |
| Auto-fixes applied | 0 | No semgrep auto-fix candidates in this scope |
| Build after fixes | N/A | No auto-fixes applied |

**Blocking issues preventing SECURITY_PASSED:**

1. **S1** — `scripts/db-init.sql:8` — hardcoded credential `'changeme_in_production'`. Persistent carry-forward. Blocked until removed.
2. **C2** — `INodeUrlValidator` has no concrete implementation. SSRF guard contract exists in Abstractions but is not fulfilled. Must be implemented and registered before any node-registration endpoint is reachable in any non-test environment.
