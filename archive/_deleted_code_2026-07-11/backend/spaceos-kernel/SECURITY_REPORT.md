# Security Report — Phase 3C+ Full Scan
**Date:** 2026-04-09
**Agent:** kernel-security-scanner
**Scope:** All changed files — Phase 3C+ (EnabledModules, TenantHandshakeAllowlist, JWT claims, HandshakeEndpoints, ChanneledAuditEventDispatcher, PostgresAdvisoryAuditWriteLock, PostgresWormStorageService, AuditHashes migration)
**Final status:** SECURITY_FAILED

---

## Supply Chain Findings

| Package | Version | Project | On Approved List | Severity | CVE | Action |
|---------|---------|---------|-----------------|----------|-----|--------|
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 | SpaceOS.Infrastructure | No (known gap) | WARNING | None | Flag until CLAUDE.md updated |
| StackExchange.Redis | 2.8.* | SpaceOS.Infrastructure | No (known gap) | WARNING | None | Flag until CLAUDE.md updated |
| Microsoft.Extensions.Caching.StackExchangeRedis | 8.* | SpaceOS.Infrastructure | No (known gap) | WARNING | None | Flag until CLAUDE.md updated |
| Microsoft.IdentityModel.Tokens | 7.6.0 | SpaceOS.Infrastructure | No (known gap) | WARNING | None | Flag until CLAUDE.md updated |
| System.IdentityModel.Tokens.Jwt | 7.6.0 | SpaceOS.Infrastructure | No (known gap) | WARNING | None | Flag until CLAUDE.md updated |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.11 | SpaceOS.Kernel.Api | No (known gap) | WARNING | None | Flag until CLAUDE.md updated |
| Microsoft.Data.Sqlite | 8.0.11 | SpaceOS.Kernel.Tests, SpaceOS.Kernel.Api.Tests, SpaceOS.Kernel.IntegrationTests | No (known gap) | WARNING | None | Test-only; no CVE; flag until approved |

All packages listed above have been tracked across prior scans. No new packages were introduced in this changeset. No HIGH or CRITICAL CVEs detected.

---

## Static Analysis Findings

### S-01 — RLS Policy Missing COALESCE Sentinel (TenantHandshakeAllowlist)

| Field | Value |
|-------|-------|
| **Rule** | spaceos-rls-missing-coalesce |
| **File** | `SpaceOS.Infrastructure/Migrations/20260408110000_Migration_0026_TenantHandshakeAllowlist.cs` |
| **Line** | 39–41 |
| **Severity** | ERROR |
| **Auto-fixed** | No |
| **Status** | OPEN — tracked in agent memory as `project_allowlist_rls_missing_coalesce.md` |

**Finding:** The RLS policy `TenantHandshakeAllowlist_TenantIsolation` reads the session variable with `current_setting('app.current_tenant_id', true)::uuid`. When the session variable is absent or empty (background jobs, health checks, admin tokens), `current_setting(..., true)` returns an empty string `''`, and casting `''` to `uuid` raises a PostgreSQL runtime exception. Every other RLS policy in the codebase (e.g., `rls_audit_hashes_tenant` in Migration 0027) wraps this in `COALESCE(NULLIF(..., '')::uuid, '00000000-...'::uuid)` to provide a safe sentinel fallback.

**Vulnerable SQL (lines 39–41 of the migration):**
```sql
USING ("GuestTenantId" = current_setting('app.current_tenant_id', true)::uuid
       OR "HostTenantId" = current_setting('app.current_tenant_id', true)::uuid);
```

**Required fix — Migration 0028:**
```sql
USING (
  "GuestTenantId" = COALESCE(
    NULLIF(current_setting('app.current_tenant_id', TRUE), '')::uuid,
    '00000000-0000-0000-0000-000000000001'::uuid)
  OR
  "HostTenantId" = COALESCE(
    NULLIF(current_setting('app.current_tenant_id', TRUE), '')::uuid,
    '00000000-0000-0000-0000-000000000001'::uuid)
);
```

This blocks all access through `TenantHandshakeAllowlist` from background workers and admin operations until fixed. The existing `TenantSessionInterceptor` always sets a value (falling back to the sentinel), but any direct DB connection that bypasses the interceptor (migrations, scripts, tools) will throw.

---

### S-02 — IgnoreQueryFilters Leaks Archived Tenant Names into JWT Claims

| Field | Value |
|-------|-------|
| **Rule** | spaceos-ignorequeryfilters-archived-leak |
| **File** | `SpaceOS.Infrastructure/Data/Repositories/TenantHandshakeAllowlistRepository.cs` |
| **Line** | 26 |
| **Severity** | WARNING |
| **Auto-fixed** | No |
| **Status** | OPEN — tracked in agent memory as `project_allowlist_ignorequeryfilters_archived.md` |

**Finding:** `GetAllowedHostsAsync` calls `_context.Tenants.IgnoreQueryFilters()` to join host tenant names. This bypasses the `IsArchived = false` global query filter, meaning archived tenants will appear in the `allowed_hosts` JWT claim. A guest tenant whose host partner has been archived will continue to receive an `allowed_hosts` entry for that archived tenant in every token issued or refreshed until the allowlist entry is manually removed.

**Recommended fix:** Replace `IgnoreQueryFilters()` with an explicit `Where(t => !t.IsArchived)` predicate on the join, or remove `IgnoreQueryFilters()` entirely to let the normal filter apply.

---

### S-03 — JWT Authority/Audience Silent Dev Fallback in Production

| Field | Value |
|-------|-------|
| **Rule** | spaceos-jwt-fallback-not-fail-fast |
| **File** | `SpaceOS.Kernel.Api/ConfigureJwtBearerOptions.cs` |
| **Lines** | 44–50 |
| **Severity** | WARNING |
| **Auto-fixed** | No |
| **Status** | OPEN — tracked in agent memory as `project_jwt_authority_silent_fallback.md` |

**Finding:** When both `JWT_AUTHORITY` and `Jwt:Issuer` are absent, `ValidIssuer` silently falls back to `"https://spaceos-kernel"`. In a mis-configured production deployment, the API will accept tokens issued with the dev authority string without any startup failure. The correct posture for non-Development environments is to throw `InvalidOperationException` at startup if neither config key is present.

Additionally, `JwtAccessTokenIssuer` reads `Jwt:Issuer` (throws if absent), while `ConfigureJwtBearerOptions` reads `JWT_AUTHORITY` then `Jwt:Issuer` (falls back silently). A misconfiguration where the two paths resolve to different values causes silent 401 failures rather than a clear startup error.

**Recommended fix:**
```csharp
var authority = _config["JWT_AUTHORITY"]
             ?? _config["Jwt:Issuer"]
             ?? (env.IsDevelopment()
                 ? "https://spaceos-kernel"
                 : throw new InvalidOperationException(
                     "JWT_AUTHORITY or Jwt:Issuer must be configured in non-Development environments."));
```

---

### S-04 — RefreshTokenCommandHandler Hardcodes "User" Role on Token Rotation

| Field | Value |
|-------|-------|
| **Rule** | spaceos-role-loss-on-refresh |
| **File** | `SpaceOS.Kernel.Application/Auth/Commands/RefreshTokenCommandHandler.cs` |
| **Line** | 90 |
| **Severity** | WARNING |
| **Auto-fixed** | No |
| **Status** | OPEN — tracked in agent memory as `project_refresh_token_role_loss.md` |

**Finding:** The rotated access token is always issued with the hardcoded role `"User"` regardless of what role was in the original token. An `Admin` or `Designer` user who refreshes their session receives a downgraded `User` token. The original role must be stored on the `RefreshToken` entity or resolved via `UserProfile` and passed to `GenerateAccessToken`.

This is a partially resolved gap from MSG-K021: tenant resolution and brand skin were fixed in this changeset, but role preservation was not.

---

### S-05 — Database.MigrateAsync at Startup in Production Path

| Field | Value |
|-------|-------|
| **Rule** | spaceos-no-auto-migrate |
| **File** | `SpaceOS.Kernel.Api/Program.cs` |
| **Line** | 292 |
| **Severity** | WARNING |
| **Auto-fixed** | No |
| **Status** | OPEN (acknowledged with inline TODO comment) |

**Finding:** `db.Database.MigrateAsync()` is called in the production startup path with a comment stating it is intentional and temporary. Per Infrastructure CLAUDE.md (rule I8) and the `spaceos-no-auto-migrate` custom rule, auto-migration at startup is forbidden in production code. The code remains committed and will execute on every production restart.

**Action required:** Remove `MigrateAsync` once migrations 0015–0027 are confirmed applied on the production database.

---

### S-06 — CloseFlowEpicCommandHandler: DomainException Caught (Application Anti-Pattern)

| Field | Value |
|-------|-------|
| **Rule** | application-no-catch-domain-exception |
| **File** | `SpaceOS.Kernel.Application/FlowEpics/Commands/CloseFlowEpic/CloseFlowEpicCommandHandler.cs` |
| **Lines** | 66–70 |
| **Severity** | WARNING |
| **Auto-fixed** | No |
| **Status** | OPEN |

**Finding:** `DomainException` is caught inside the handler body and converted to `Result.Error`. The Application layer CLAUDE.md explicitly forbids this pattern ("use upfront guard instead"). The handler calls `epic.Close(...)` inside a try/catch block rather than calling guard methods first. This is not a direct security vulnerability, but it violates the architectural invariant that protects domain rules from being silently swallowed.

---

### S-07 — ChanneledAuditEventDispatcher: Audit Event Loss Path Under Sustained Sink Degradation

| Field | Value |
|-------|-------|
| **Rule** | spaceos-audit-event-loss |
| **File** | `SpaceOS.Infrastructure/AuditLog/ChanneledAuditEventDispatcher.cs` |
| **Lines** | 76–83 |
| **Severity** | WARNING |
| **Auto-fixed** | No |
| **Status** | OPEN (design trade-off, intentionally documented) |

**Finding:** When the bounded channel (capacity 512) is full for more than 5 seconds, audit event batches are dropped after a `LogCritical` entry. The `BoundedChannelFullMode.Wait` design is correct (it was the fix for the prior `Drop` design), but the 5-second timeout is the only compensating mechanism. If the downstream audit sink is degraded for longer than 5 seconds under load, events will be lost. No dead-letter queue, persistent retry, or alerting integration is present. This must be evaluated against any audit regulatory requirements and a monitoring alert added for the `LogCritical` message.

---

### S-08 — PostgresAdvisoryAuditWriteLock: Unbounded Spin Under Contention

| Field | Value |
|-------|-------|
| **Rule** | spaceos-unbounded-spin-advisory-lock |
| **File** | `SpaceOS.Infrastructure/AuditLog/PostgresAdvisoryAuditWriteLock.cs` |
| **Lines** | 94–104 |
| **Severity** | WARNING |
| **Auto-fixed** | No |
| **Status** | OPEN |

**Finding:** The advisory lock acquisition spins indefinitely with 10 ms delays, bounded only by the caller's `CancellationToken`. Under prolonged write contention on a busy tenant, every waiting API instance holds a live DB connection per spin iteration, potentially exhausting the Npgsql connection pool. A maximum wait duration (e.g., 30 seconds) should be enforced with a deadline `CancellationTokenSource` before returning an `IAuditWriteLock` error, rather than relying solely on the upstream request cancellation token.

---

### S-09 — RegisterSpaceLayerCommandValidator: URL Validation Insufficient for SSRF Prevention

| Field | Value |
|-------|-------|
| **Rule** | spaceos-ssrf-url-validation-weak |
| **File** | `SpaceOS.Kernel.Application/SpaceLayers/Commands/RegisterSpaceLayerCommandValidator.cs` |
| **Lines** | 21–27 |
| **Severity** | WARNING |
| **Auto-fixed** | No |
| **Status** | OPEN (pre-existing — tracked via `project_spacelayer_ssrf_risk.md`) |

**Finding:** `ExternalSourceUrl` is validated only as a valid absolute URI. Private addresses, loopback, and IMDS endpoints (`http://169.254.169.254/`) pass validation. `INodeUrlValidator` (registered in DI) implements SSRF checks but is not called from this validator — it appears intended for the federation execution layer which is not yet built. The risk activates only when `ExternalSourceUrl` is used for outbound HTTP calls.

**Action required (when federation execution layer is built):** Inject and call `INodeUrlValidator` in the validator.

---

## Custom Rule Findings Summary

| # | Rule ID | File | Line | Severity | Fixed |
|---|---------|------|------|----------|-------|
| 1 | spaceos-rls-missing-coalesce | Migration_0026_TenantHandshakeAllowlist.cs | 39–41 | ERROR | No |
| 2 | spaceos-no-auto-migrate | Program.cs | 292 | WARNING | No |
| 3 | spaceos-ignorequeryfilters-archived-leak | TenantHandshakeAllowlistRepository.cs | 26 | WARNING | No |
| 4 | spaceos-jwt-fallback-not-fail-fast | ConfigureJwtBearerOptions.cs | 44–50 | WARNING | No |
| 5 | spaceos-role-loss-on-refresh | RefreshTokenCommandHandler.cs | 90 | WARNING | No |

---

## Positive Security Findings

| Area | Check | Status |
|------|-------|--------|
| JWT signing algorithm | ES256 asymmetric — no symmetric secret, no hardcoded key material | PASS |
| JWT claims — allowed_hosts cap | Hard `.Take(20)` in `JwtAccessTokenIssuer.GenerateAccessToken` | PASS |
| JWT claims — enabled_modules | Sourced from `Tenant.EnabledModules` (domain-validated); serialised to JSON array | PASS |
| Connection strings | All read from `IConfiguration` / environment; `AUDIT_SINK_CONNECTION_STRING` throws if absent | PASS |
| Stack traces in API responses | `ResultExtensions` never exposes exception detail or stack trace in any branch | PASS |
| SQL injection — TenantSessionInterceptor | `set_config` uses parameterised ADO.NET command; claim validated as GUID before forwarding | PASS |
| SQL injection — PostgresAdvisoryAuditWriteLock | `SqlQueryRaw` uses typed `int64` positional parameter `{0}` — not string interpolation | PASS |
| SQL injection — PostgresWormStorageService | All values via `AddWithValue` — no string concatenation in CommandText | PASS |
| WORM role separation | `PostgresWormStorageService` exclusively uses `AUDIT_SINK_CONNECTION_STRING` (INSERT-only role) | PASS |
| RLS — AuditHashes (Migration 0027) | `COALESCE(NULLIF(...)::uuid, sentinel)` pattern — correct | PASS |
| RLS — TenantHandshakeAllowlist | `ENABLE ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY` set | PASS (COALESCE gap is separate ERROR) |
| Rate limiting — HandshakeEndpoints | `.RequireRateLimiting("fixed")` applied | PASS |
| Authorization — HandshakeEndpoints | `.RequireAuthorization("ReadPolicy")` applied | PASS |
| Self-link prevention | DB CHECK `GuestTenantId <> HostTenantId` + domain invariant in `TenantHandshakeAllowlist.Create` | PASS |
| Trade type validation | DB CHECK `AllowedTradeTypes <@ ARRAY[...]` + `cardinality > 0` + domain invariant | PASS |
| Migration DDL transactions | Both Migration 0025 and 0026 use `suppressTransaction: true` for DDL operations | PASS |
| ForwardedHeaders | `KnownProxies` = loopback only, `KnownNetworks` cleared — confirmed in Program.cs | PASS |
| Request body limit | 64 KB at Kestrel level (`MaxRequestBodySize`) | PASS |
| MD5 in advisory lock key | MD5 used for key-space distribution only (explicitly documented) — not cryptographic | PASS |
| Back-pressure design | `BoundedChannelFullMode.Wait` — no silent drop without timeout log | PASS |
| Token rotation atomicity | Revoke presented token → issue new entity → access token — all before `SaveChangesAsync` | PASS |
| TenantSessionInterceptor reset | Connection pool contamination prevented: `set_config` reset to `''` on `ConnectionClosingAsync` | PASS |

---

## Summary

| Category | Count |
|----------|-------|
| CRITICAL | 0 |
| ERROR | 1 |
| WARNING (code) | 8 |
| WARNING (supply chain, known gaps) | 7 |
| INFO | 0 |
| Auto-fixes applied | 0 |
| Build after fixes | N/A |

**SECURITY_FAILED** — 1 unresolved ERROR blocks this changeset from `SECURITY_PASSED` status.

### Blocker

**S-01 (ERROR):** `Migration_0026_TenantHandshakeAllowlist.cs` line 39–41 — RLS policy `TenantHandshakeAllowlist_TenantIsolation` casts `current_setting(...)` directly to `uuid` without `COALESCE(NULLIF(...))` sentinel. Any PostgreSQL connection where `app.current_tenant_id` is absent or empty (background jobs, health checks, `dotnet ef database update`, direct psql sessions) will receive a runtime `invalid input syntax for type uuid` exception when any query touches this table. Migration 0028 is required to DROP and recreate the policy using the established sentinel pattern from Migration 0027.

### Developer Action Items

| Priority | Finding | Action |
|----------|---------|--------|
| HIGH | S-01 — RLS COALESCE missing | Create Migration 0028 to fix the RLS policy |
| MEDIUM | S-04 — Role hardcoded "User" on refresh | Store original role on `RefreshToken` entity; pass to `GenerateAccessToken` |
| MEDIUM | S-02 — Archived tenant leak in JWT | Add `Where(t => !t.IsArchived)` predicate to `TenantHandshakeAllowlistRepository` join |
| LOW | S-03 — JWT authority silent fallback | Add fail-fast guard in `ConfigureJwtBearerOptions` for non-Development |
| LOW | S-05 — MigrateAsync at startup | Remove after prod migrations confirmed applied |
| LOW | S-06 — DomainException caught | Refactor `CloseFlowEpicCommandHandler` to use upfront guard pattern |
| LOW | S-07 — Audit event loss | Add alerting integration on `LogCritical` in `ChanneledAuditEventDispatcher` |
| LOW | S-08 — Unbounded spin | Add max-wait deadline to `PostgresAdvisoryAuditWriteLock.AcquireAsync` |
| DEFERRED | S-09 — SSRF URL validation | Wire `INodeUrlValidator` in `RegisterSpaceLayerCommandValidator` when federation execution layer is built |
