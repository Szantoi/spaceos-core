# Security Report — MSG-K022 Sprint D Phase 2
**Date:** 2026-04-07
**Agent:** kernel-security-scanner
**Final status:** SECURITY_PASSED

---

## Supply Chain Findings

### Infrastructure.csproj

| Package | Version | Severity | CVE | Action |
|---------|---------|----------|-----|--------|
| Microsoft.EntityFrameworkCore | 8.0.11 | — | None | Approved |
| Microsoft.EntityFrameworkCore.Relational | 8.0.11 | — | None | Approved |
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.11 | — | None | Approved |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 | — | None | Not on approved list — no CVE — flag per memory `project_npgsql_supply_chain.md` |
| Microsoft.EntityFrameworkCore.Design | 8.0.11 | — | None | Approved (build-only) |
| Ardalis.Specification.EntityFrameworkCore | 8.0.0 | — | None | Approved |
| Microsoft.Extensions.Configuration.Abstractions | 8.0.0 | — | None | Approved |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.2 | — | None | Approved |
| Microsoft.IdentityModel.Tokens | 7.6.0 | — | None | Not on approved list — no CVE — flagged per memory `project_jwt_packages_unapproved.md` |
| System.IdentityModel.Tokens.Jwt | 7.6.0 | — | None | Not on approved list — no CVE — flagged per memory `project_jwt_packages_unapproved.md` |
| **StackExchange.Redis** | 2.8.* | — | None | Not on approved list — no CVE — new in Phase 2, flag until CLAUDE.md updated |
| **Microsoft.Extensions.Caching.StackExchangeRedis** | 8.* | — | None | Not on approved list — no CVE — new in Phase 2, flag until CLAUDE.md updated |

### Script projects (Phase1a.csproj, Phase1b.csproj)

| Package | Version | Severity | CVE | Action |
|---------|---------|----------|-----|--------|
| Npgsql | 8.* | — | None | Script-only, not on approved list — no CVE |
| Dapper | 2.* | — | None | Script-only, not on approved list — no CVE |
| Microsoft.Extensions.Configuration.* | 8.* | — | None | Approved (framework family) |

`dotnet list package --vulnerable` returned zero vulnerabilities for all three `.csproj` files.

---

## Static Analysis Findings

| # | Rule | File | Line | Severity | Auto-fixed | Action Required |
|---|------|------|------|----------|------------|-----------------|
| 1 | Hardcoded partial connection string in committed config | `SpaceOS.Kernel.Api/appsettings.json` | 3 | WARNING | No | Pre-existing finding — password field is empty string, not a real credential. Kept as WARNING for tracking. |
| 2 | `RequireHeaderSymmetry = false` in ForwardedHeaders config | `SpaceOS.Kernel.Api/Program.cs` | 223 | WARNING | No | Disabling header symmetry allows mismatched `X-Forwarded-For`/`X-Forwarded-Proto` header counts without rejection. Acceptable when Nginx strips and re-emits a single header; document explicitly that Nginx is configured to emit exactly one `X-Forwarded-For` header. |
| 3 | Phase 1a writes token values to `tokens.json` on local disk | `scripts/MigrateExternalAuthTokens/Phase1a/Program.cs` | 42 | WARNING | No | Intermediate file contains plaintext `ExternalAuthTokenRef` values. Phase 1b deletes the file after use. Operator must ensure the file is not left behind on failure. See action note below. |

No secrets found in `appsettings.Production.json` (contains only `Urls`).
No secrets found in `appsettings.Testing.json` (contains only JWT issuer/audience strings).
No secrets found in `appsettings.Development.json` (SQLite DSN and EC key paths, no credentials).

### Finding 3 — Detailed note (tokens.json)
Phase 1a writes raw token values to disk before Phase 1b replaces them with KV references and deletes the file. If Phase 1a exits abnormally before Phase 1b runs, `tokens.json` remains on disk containing sensitive token material. The file is not encrypted. **Operator action required:** run Phase 1b immediately after Phase 1a, and verify deletion. Consider using a restricted working directory (`chmod 700`) and excluding the output path from backups or logs.

---

## Custom Rule Findings

| # | Rule ID | File | Line | Severity | Fixed |
|---|---------|------|------|----------|-------|
| — | spaceos-no-hardcoded-connstring | All appsettings | — | — | No match — connection strings present in base `appsettings.json` have an empty password field (placeholder), not a real credential. Production config uses environment injection. |
| — | spaceos-no-auto-migrate | `Program.cs` | — | — | No match — `EnsureCreatedAsync` (dev-only) and `PostgresSchemaInitializer.ApplyAsync` (prod) used; `Database.Migrate()` absent. |
| — | spaceos-no-stacktrace-response | All endpoint files | — | — | No match — no `StackTrace` passed to `Results.Problem`. |
| — | spaceos-no-hardcoded-jwt-secret | `Program.cs`, `ConfigureJwtBearerOptions.cs` | — | — | No match — `LocalEcKeyProvider` / `ISigningKeyProvider` used; no `SymmetricSecurityKey` with literal string. |

---

## Focused Check Results

### 1. SQL injection — parameterized queries
All migrations use static DDL strings (`CREATE INDEX CONCURRENTLY ... IF NOT EXISTS`) with no user-supplied parameters. The Phase 1b script uses Dapper with named parameters (`@KvRef`, `@SpaceLayerId`) — no string interpolation into SQL. The query handlers go exclusively through EF Core specifications (`WithSpecification`) — no `FromSqlRaw` or `ExecuteSqlRaw` calls detected in the new Tools/Queries files.
**Result: PASS**

### 2. TenantId sourced from JWT
`ToolEndpoints.cs` extracts `TenantId` from `ClaimsPrincipal` claims (`"tid"` / `"tenant_id"`) only. No query parameter, route parameter, or request body field contributes to tenant scoping. All four query types receive `TenantId` from the verified JWT principal before being forwarded to the handler.
**Result: PASS**

### 3. Authentication on all tool endpoints
All four `ToolEndpoints` routes sit inside a group created with `.RequireAuthorization()`. Each endpoint additionally performs an explicit `Guid.Empty` guard on the tenant claim and returns `401 Problem` if absent. The `.RequireAuthorization()` call on the group is the primary gate; the explicit guard is a defense-in-depth secondary check.
**Result: PASS**

### 4. Redis connection string / password handling
`RedisExtensions.AddSpaceOsRedis` reads `Redis:ConnectionString` and `Redis:Password` from `IConfiguration` only. No hardcoded credentials. Password is applied to a parsed `ConfigurationOptions` object, never concatenated into a string. TLS is configurable via `Redis:UseTls`. No Redis key is present in any committed `appsettings*.json` file.
**Result: PASS**

### 5. Console scripts — connection strings from args, not hardcoded
Phase 1a uses `--db-readonly` argument or `SPACEOS_MIGRATE_DB_READONLY` environment variable via `ConfigurationBuilder` + `AddCommandLine` + `AddEnvironmentVariables`. Phase 1b uses `--db-write` / `SPACEOS_MIGRATE_DB_WRITE`. Both scripts throw `InvalidOperationException` if the argument is absent. No connection string literal anywhere in either script file.
**Result: PASS**

### 6. ForwardedHeaders — KnownProxies restricted
`Program.cs` lines 218–224 configure `ForwardedHeaders` with:
- `KnownProxies.Add(IPAddress.Loopback)` — only `127.0.0.1` trusted
- `KnownNetworks.Clear()` — default `10.0.0.0/8` network trust removed
- `RequireHeaderSymmetry = false` — see WARNING finding #2 above

No open trust (`ForwardLimit` not set to 0; no wildcard network added). The configuration is correctly constrained for a single-host Nginx setup.
**Result: PASS (with WARNING #2 noted)**

### 7. Vulnerable packages (`dotnet list package --vulnerable`)
- `SpaceOS.Infrastructure.csproj`: 0 vulnerable packages
- `Phase1a.csproj`: 0 vulnerable packages
- `Phase1b.csproj`: 0 vulnerable packages
**Result: PASS**

### 8. Secrets in config files
No API keys, passwords, JWT secrets, or tokens found in any committed `appsettings*.json` file. The base `appsettings.json` contains `Password=` with an empty value (placeholder, not a real credential). Keys are path references (`PrivateKeyPath`, `PublicKeyPath`) pointing to local `.pem` files, not inlined material.
**Result: PASS**

---

## Summary

| Category | Count |
|----------|-------|
| CRITICAL | 0 |
| ERROR | 0 |
| WARNING | 3 (all accepted / needs review — details above) |
| INFO | 0 |
| Supply chain — unapproved (no CVE) | 4 (StackExchange.Redis, SE.Redis.Cache, Npgsql EF, JWT tokens — pre-existing except Redis which is new) |
| Auto-fixes applied | 0 |
| Build after analysis | ✅ (0 errors, 0 warnings) |

### WARNING detail

| # | Finding | Risk level | Recommended action |
|---|---------|-----------|-------------------|
| W1 | Empty-password placeholder in `appsettings.json` | Low | Pre-existing; password field is empty, not a credential. Add a comment marking it as a placeholder. |
| W2 | `RequireHeaderSymmetry = false` | Low | Document in deployment runbook that Nginx emits exactly one `X-Forwarded-For` header. |
| W3 | `tokens.json` intermediate file in migration scripts | Medium | Operator procedure must ensure Phase 1b runs immediately after Phase 1a. Restrict directory permissions before running Phase 1a. |
