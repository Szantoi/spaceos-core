# Security Report — MSG-K021 / Sprint D Phase 1.5
**Date:** 2026-04-06
**Agent:** kernel-security-scanner
**Final status:** SECURITY_PASSED (with logged WARNINGs)

---

## Scope

| Task | Summary |
|------|---------|
| T-01 | Race condition load test + `SaveChangesAsync` moved inside advisory lock scope |
| T-02 | `AuditDbContext` separation, RLS SQL scripts, table ownership |
| T-03 | JWT ES256 (`ISigningKeyProvider`, `LocalEcKeyProvider`, `ConfigureJwtBearerOptions`), `RefreshToken` CQRS vertikum (refresh/logout), JWKS endpoint + OutputCache, `RefreshTokenService` (timing-safe) |
| T-04 | `TenantSessionInterceptor` (`set_config is_local=false`, parameterized SQL) |
| T-05 | `HashSinkDbContext` + `DbContextFactory`, `PostgresHashSink`, `ConnectionStringOptions` `ValidateOnStart` |

---

## Supply Chain Findings

### Changed .csproj files audited
- `SpaceOS.Kernel.Application/SpaceOS.Kernel.Application.csproj`
- `SpaceOS.Infrastructure/SpaceOS.Infrastructure.csproj`
- `SpaceOS.Kernel.Api/SpaceOS.Kernel.Api.csproj`
- `SpaceOS.Kernel.Tests/SpaceOS.Kernel.Tests.csproj`
- `SpaceOS.Kernel.IntegrationTests/SpaceOS.Kernel.IntegrationTests.csproj`

### New packages introduced in this sprint

| Package | Version | Project | On approved list | Severity | CVE | Action |
|---------|---------|---------|-----------------|----------|-----|--------|
| `Microsoft.IdentityModel.Tokens` | 7.6.0 | Infrastructure | No | WARNING | None known | Flag until added to approved list or reviewed |
| `System.IdentityModel.Tokens.Jwt` | 7.6.0 | Infrastructure | No | WARNING | None known | Flag until added to approved list or reviewed |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.11 | Api | No | WARNING | None known | Flag until added to approved list or reviewed |
| `Microsoft.Data.Sqlite` | 8.0.11 | Kernel.Tests | No | WARNING | None known | Carry-over from prior scans — test-only, no CVE |
| `Microsoft.Data.Sqlite` | 8.0.11 | IntegrationTests | No | WARNING | None known | Carry-over from prior scans — test-only, no CVE |

**Notes:**
- `Microsoft.IdentityModel.Tokens` 7.6.0 and `System.IdentityModel.Tokens.Jwt` 7.6.0 are the Microsoft Identity Model packages required for ES256 JWT issuance and validation. They are the correct packages for this use case and have no known CVEs at 7.6.0. They are not on the approved list in `CLAUDE.md`; an explicit update to the approved list is recommended.
- `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.11 is a first-party ASP.NET Core package, covered by the `FrameworkReference Microsoft.AspNetCore.App` umbrella. Listing it explicitly is appropriate; the approved list should be updated to include it.
- `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.11 carry-over flag from E6/T1 — not re-filed here.

---

## Static Analysis Findings

### Rule set: `p/csharp`, `p/secrets`, `p/owasp-top-ten`

| # | Rule | File | Line | Severity | Auto-fixed | Action Required |
|---|------|------|------|----------|------------|-----------------|
| SA-01 | `p/secrets` — partial connection string with empty Password field | `SpaceOS.Kernel.Api/appsettings.json` | 3 | WARNING | No | See note below |
| SA-02 | `p/csharp` — `ExecuteSqlRawAsync` with static string (no user input; pre-existing) | `SpaceOS.Infrastructure/Data/PostgresSchemaInitializer.cs` | 30, 38 | WARNING | No | Pre-existing; static DDL only — no user-controlled input |
| SA-03 | `p/csharp` — `SqlQueryRaw` with parameterized `{0}` (advisory lock key) | `SpaceOS.Infrastructure/AuditLog/PostgresAdvisoryAuditWriteLock.cs` | 94 | WARNING | No | Parameterized via `{0}` placeholder with a `long` derived from `GetHashCode`; not user-controlled — accepted |
| SA-04 | `p/csharp` — `DomainException.Message` forwarded to HTTP 400 response body | `SpaceOS.Kernel.Api/Middleware/ExceptionHandlingMiddleware.cs` | 36 | WARNING | No | See note below |
| SA-05 | `p/secrets` — placeholder password literal `'CHANGE_ME_IN_VAULT'` in SQL init script | `scripts/db/init-audit-sink-roles.sql` | 32, 60 | WARNING | No | Placeholder is intentional; must never be committed with a real password — operator procedure |

**SA-01 detail:** `appsettings.json` contains `"Password="` (empty value) in the base `DefaultConnection` string. This is a structural placeholder — the actual password is injected at runtime via environment variables or Docker secrets. The empty `Password=` token itself is not a leaked credential. Severity: WARNING (not ERROR) because the value is empty.

**SA-04 detail:** `ExceptionHandlingMiddleware` forwards `DomainException.Message` as the `detail` field of a 400 Problem Details response. `DomainException` messages are authored by domain code and do not contain infrastructure secrets or stack traces. However, message content could theoretically aid enumeration (e.g., "Tenant name cannot exceed 100 characters"). This is a pre-existing pattern, not introduced in this sprint. Severity: WARNING.

**SA-02 detail (pre-existing):** `PostgresSchemaInitializer.ExecuteSqlRawAsync` uses static DDL strings with no user-controlled input. This is not SQL injection risk but semgrep flags any `ExecuteSqlRaw` call. Pre-existing finding, not introduced in this sprint.

---

## Custom Rule Findings

| # | Rule ID | File | Line | Severity | Fixed | Notes |
|---|---------|------|------|----------|-------|-------|
| CR-01 | `spaceos-no-hardcoded-connstring` | `SpaceOS.Kernel.Api/appsettings.json` | 3 | WARNING | No | Empty `Password=` — not a real credential, see SA-01 |
| CR-02 | `spaceos-no-hardcoded-jwt-secret` | All scanned files | — | PASS | N/A | No `SymmetricSecurityKey` or hardcoded JWT secret found anywhere |
| CR-03 | `spaceos-no-auto-migrate` | All scanned files | — | PASS | N/A | No `Database.Migrate()` call found in production paths |
| CR-04 | `spaceos-no-stacktrace-response` | All scanned files | — | PASS | N/A | `ExceptionHandlingMiddleware` never exposes `StackTrace` |
| CR-05 | `spaceos-no-hardcoded-jwt-secret` (node auth variant) | `SpaceOS.Infrastructure/Auth/NodeAuthService.cs` | 19–20 | WARNING | No | See note below |

**CR-05 detail:** `NodeAuthService` has hardcoded string constants `"spaceos-kernel"` and `"spaceos-sip"` for `Issuer` and `Audience`. These are not secrets, but the `DevRsaKeyManager` backing this service generates and persists an RSA private key to `keys/dev-private-key.pem` on disk. This is a known carry-over finding (see memory: `project_config_keyvault_unconstrained.md`). The file is dev-only; no production credential is hardcoded. Severity: WARNING (carry-over, developer action still outstanding).

---

## Detailed Security Analysis — New Patterns

### T-03: JWT ES256 Implementation

| Check | Result |
|-------|--------|
| Algorithm | ES256 (ECDSA P-256) — correct; asymmetric |
| Hardcoded key | None — `LocalEcKeyProvider` reads from config paths; ephemeral fallback in dev only |
| `ValidateIssuerSigningKey` | `true` |
| `ValidateIssuer` | `true` |
| `ValidateAudience` | `true` |
| `ValidateLifetime` | `true` |
| `ClockSkew` | `TimeSpan.Zero` — no grace window (SEC-P15-04) |
| `ValidAlgorithms` | `["ES256"]` — algorithm pinned, prevents algorithm confusion attacks |
| Token lifetime (access) | 15 minutes (SEC-P15-05) |
| Token lifetime (refresh) | 8 hours — acceptable |
| Key disposal | `LocalEcKeyProvider.Dispose()` clears `ECDsa` instances from memory |
| `BuildServiceProvider()` | Absent — `IConfigureNamedOptions<JwtBearerOptions>` pattern used correctly (BE-P15-01) |

**PASS** — No issues found in the ES256 JWT implementation.

### T-03: Refresh Token Implementation

| Check | Result |
|-------|--------|
| Token entropy | 256-bit CSPRNG (`RandomNumberGenerator.Fill`) — correct |
| Token length | 43 characters Base64Url — acceptable |
| Storage | SHA-256 hex digest only — raw token never persisted |
| Timing-safe comparison | `CryptographicOperations.FixedTimeEquals` — correct (BE-P15-12) |
| Duplication (`RefreshTokenHasher` vs `RefreshTokenService`) | Both exist — see WARNING below |
| Token rotation | Old token revoked before new token issued — correct |
| Idempotent logout | `RevokeTokenCommandHandler` is 200 OK on missing/already-revoked token (BE-P15-11) |
| Input validation | `RefreshTokenCommandValidator` / `RevokeTokenCommandValidator` enforce length=43 |

**WARNING (W-01):** Two near-identical implementations of `GenerateOpaqueToken` / `HashToken` / `VerifyToken` exist:
- `SpaceOS.Kernel.Application/Auth/RefreshTokenHasher.cs`
- `SpaceOS.Infrastructure/Auth/RefreshTokenService.cs`

The `Application` layer version (`RefreshTokenHasher`) is used by CQRS handlers. The `Infrastructure` version (`RefreshTokenService`) appears unused in production paths after this sprint. Duplicate implementations risk divergence. Developer should remove or internalize the Infrastructure duplicate.

**WARNING (W-02) — Role not preserved across token rotation:**
`RefreshTokenCommandHandler.cs` line 61: `_tokenIssuer.GenerateAccessToken(stored.UserId, "User", Guid.Empty)`. The new access token issues with a hardcoded `"User"` role and `Guid.Empty` for `tenantId`. If the original token carried an `"Admin"` or `"Designer"` role, the rotated access token silently downgrades permissions. This is a privilege degradation bug with security implications. Developer action required.

### T-04: TenantSessionInterceptor

| Check | Result |
|-------|--------|
| SQL injection via claim | `set_config` called with `$1`, `$2` positional parameters — parameterized, no injection risk |
| Claim validation | Only well-formed, non-empty GUIDs forwarded; arbitrary claim values rejected |
| Session leak on pool return | `ConnectionClosingAsync` resets variable to `string.Empty` — correct |
| `is_local=false` | Session-level scope — survives cross-context transaction (BE-P15-03 fix) |
| SQLite guard | Interceptor only registered when `!environment.IsDevelopment()` |

**PASS** — Implementation is correct and the SQL injection concern is properly addressed.

### T-02: AuditDbContext Separation + RLS Scripts

| Check | Result |
|-------|--------|
| `AuditDbContext` isolation | Separate context, separate connection string, separate PG role |
| `FORCE ROW LEVEL SECURITY` | Applied in `fix-audit-ownership.sql` after NOLOGIN owner transfer |
| `COALESCE` nil-UUID fallback | RLS policy falls back to `'00000000-...'` when GUC absent — safe |
| `UPDATE`/`DELETE` revoked | REVOKE applied for `spaceos_app`, `spaceos_audit_writer`, `PUBLIC` |
| `spaceos_audit_writer` insert bypass | `WITH CHECK (true)` policy limited to `FOR INSERT TO spaceos_audit_writer` — acceptable |

**WARNING (W-03) — `init-audit-sink-roles.sql` placeholder passwords:**
`scripts/db/init-audit-sink-roles.sql` lines 32 and 60 create `spaceos_sink_writer_user` and `spaceos_sink_verifier_user` with `PASSWORD 'CHANGE_ME_IN_VAULT'`. This is an intentional placeholder with an inline operational comment but the literal string is committed to source control. Operator procedure must ensure these roles are created with real credentials injected from a vault before the script is run against any non-development instance. Carry-forward of the `project_db_init_sql_hardcoded_password.md` pattern.

### T-05: HashSinkDbContext + PostgresHashSink

| Check | Result |
|-------|--------|
| Context isolation | `AddDbContextFactory` used — short-lived contexts, no Scoped-lifetime risk |
| Retry on failure | `EnableRetryOnFailure(3, 5s)` configured |
| Sink failure isolation | All exceptions caught and counted; never rethrown (Golden Rule #12) |
| `EventId` deduplication | SHA-256 derived deterministic UUID — prevents duplicate sink rows on retry |
| `DeriveEventId` UUID version bits | Version nibble set to `0x40` (version 4 marker) but this is a derived/deterministic UUID; functionally acceptable as a deduplication key |
| `ConnectionStringOptions.ValidateOnStart` | `[Required]` on `DefaultConnection`; `AuditSink` optional — correct |

**PASS** — No blocking issues found.

### T-01: Advisory Lock Fix (SaveChangesAsync inside lock scope)

| Check | Result |
|-------|--------|
| Lock scope | `SaveChangesAsync` now inside `await using (lock.AcquireAsync(...))` block |
| Hash chain integrity | Lock held for the full read-tail → append → save transaction |
| `pg_try_advisory_xact_lock` SQL | Parameterized via `{0}` with a `long` cast from `GetHashCode` — not user-controlled |

**PASS** — Race condition fix is correct.

---

## Forwarded Headers Finding (carry-over)

`HttpContextCurrentRequestContext.SourceIp` reads `X-Forwarded-For` directly without `ForwardedHeadersMiddleware` being registered in `Program.cs`. This is a carry-over finding from the full re-scan (memory: `project_forwarded_headers_missing.md`). Not introduced in this sprint. Re-flagged: WARNING.

---

## Summary

| Category | Count |
|----------|-------|
| CRITICAL | 0 |
| ERROR | 0 |
| WARNING — supply chain (unapproved packages, no CVE) | 5 |
| WARNING — static analysis | 5 (SA-01 through SA-05) |
| WARNING — custom rules | 2 (CR-01, CR-05) |
| WARNING — design/logic | 3 (W-01, W-02, W-03) |
| INFO | 0 |
| Auto-fixes applied | 0 |
| Build after fixes | N/A — no auto-fixes needed |
| Build status | `0 errors, 27 xUnit1051 analyzer warnings` (pre-existing, non-security) |

### Blocking issues: NONE

### Developer action items

| # | Severity | Item |
|---|----------|------|
| W-01 | WARNING | Remove or internalize `SpaceOS.Infrastructure/Auth/RefreshTokenService.cs` — duplicates `RefreshTokenHasher` in Application |
| W-02 | WARNING | `RefreshTokenCommandHandler` line 61: preserve original role and `tenantId` when issuing rotated access token — current code hardcodes `"User"` role and `Guid.Empty` tenantId |
| W-03 | WARNING | `scripts/db/init-audit-sink-roles.sql` placeholder passwords `'CHANGE_ME_IN_VAULT'` must be injected from vault before running against any non-dev instance — operator runbook required |
| SC-1 | WARNING | Add `Microsoft.IdentityModel.Tokens 7.6.0`, `System.IdentityModel.Tokens.Jwt 7.6.0`, and `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11` to the approved package list in `CLAUDE.md` |
| FH-1 | WARNING | Register `UseForwardedHeaders` with a trusted proxy allowlist for production — carry-over from prior scan |
| DEV-1 | WARNING | `DevRsaKeyManager` writes RSA private key to `keys/dev-private-key.pem` relative to the working directory — ensure `keys/` is in `.gitignore` and `.dockerignore` |
