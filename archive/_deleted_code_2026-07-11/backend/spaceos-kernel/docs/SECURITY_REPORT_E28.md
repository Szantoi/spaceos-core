# Security Report — E28 Dashboard Stats Endpoint
**Date:** 2026-03-31
**Agent:** kernel-security-scanner
**Scope:** 9 changed files + all .csproj files (supply chain audit)
**Final status:** SECURITY_FAILED

---

## Note on Semgrep MCP Tooling

The configured semgrep MCP server (`mcp__semgrep__*`) is deprecated and returned no functional scan results. This report is based on exhaustive manual static analysis applying all checks from the Execution Protocol. The deprecated MCP should be updated to `semgrep mcp` per the deprecation notice — update `.claude/mcp.json` as follows:

```json
{
  "mcpServers": {
    "semgrep": {
      "command": "semgrep",
      "args": ["mcp"]
    }
  }
}
```

---

## Supply Chain Findings

No new NuGet packages were introduced by E28. The following pre-existing non-approved packages are re-flagged per standing policy (first noted in prior epic scans):

| Package | Version | Project | Severity | CVE | Action |
|---------|---------|---------|----------|-----|--------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.11 | SpaceOS.Infrastructure | WARNING | None known | Not on original approved list; flag until REVIEW_CHECKLIST.md updated — no change from prior scans |
| `Microsoft.Data.Sqlite` | 8.0.11 | SpaceOS.Kernel.Tests, SpaceOS.Kernel.IntegrationTests, SpaceOS.Kernel.Api.Tests | WARNING | None known | Test-only, not on approved list; flag until approved or removed — no change from prior scans |
| `Microsoft.EntityFrameworkCore.Sqlite` | 8.0.11 | SpaceOS.Kernel.Tests, SpaceOS.Kernel.IntegrationTests, SpaceOS.Kernel.Api.Tests | WARNING | None known | Test-only, not on approved list; flag until approved or removed — no change from prior scans |

No new supply chain risk introduced by E28.

---

## Static Analysis Findings

| # | Rule | File | Line | Severity | Auto-fixed | Action Required |
|---|------|------|------|----------|------------|-----------------|
| SA-1 | `p/secrets` — hardcoded JWT signing key | `SpaceOS.Kernel.Api/appsettings.Development.json` | 6 | **CRITICAL** | No | See detail below |
| SA-2 | `p/secrets` — JWT signing key in test config | `SpaceOS.Kernel.Api/appsettings.Testing.json` | 4 | WARNING | No | See detail below |
| SA-3 | `p/secrets` — partial connection string with empty password placeholder | `SpaceOS.Kernel.Api/appsettings.json` | 3 | WARNING | No | See detail below |
| SA-4 | `p/csharp` — unparameterized `Database.SqlQuery` via FormattableString | `SpaceOS.Infrastructure/Data/Queries/DashboardStatsQuery.cs` | 28–40 | INFO | N/A | Verified safe — see SQL injection analysis |

### SA-1 Detail — CRITICAL: Hardcoded JWT Signing Key in appsettings.Development.json

`appsettings.Development.json` line 6:
```json
"SigningKey": "AIzaSyAmVVu7TvjuZiAII2kYZkgVKhGHmhUH1Xk"
```

This is a 39-character base64-style string consistent with a real API key or a generated signing secret. It is committed to the repository (`appsettings.Development.json` is not listed in `.gitignore`). Any developer who clones the repo obtains this key and can forge valid JWTs for the development environment.

**Risk:** Token forgery — an attacker or malicious insider can sign arbitrary JWT claims (including `Admin` role) accepted by any environment that reuses this key.

**Required action:** Developer must:
1. Rotate this key immediately if it has ever been used in a shared or deployed environment.
2. Remove the value from the file — replace with an empty string or a clearly fake placeholder (`"SigningKey": ""`).
3. Add `appsettings.Development.json` to `.gitignore` **or** ensure it never contains real key material (use `dotnet user-secrets` for local development keys instead).

**This finding blocks SECURITY_PASSED.**

### SA-2 Detail — WARNING: JWT Key in appsettings.Testing.json

`appsettings.Testing.json` line 4:
```json
"SigningKey": "test-signing-key-for-integration-tests-min32chars!!"
```

This is a human-readable, obviously synthetic test key. It is already excluded from the Docker build context via `.dockerignore` (`**/*Testing*`), confirmed by the E7/T1 resolution in project memory.

**Risk:** Low. The key is clearly test-only and has no production value. However, if `appsettings.Testing.json` is committed to source control (it is not in `.gitignore`), it is visible to all repo collaborators.

**Required action (developer review):** Confirm this file is excluded from source control or accept as low-risk test secret. Consider adding `appsettings.Testing.json` to `.gitignore` for defence in depth.

### SA-3 Detail — WARNING: Partial Connection String in appsettings.json

`appsettings.json` line 3:
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=spaceos;Username=spaceos;Password="
```

The password field is empty (`Password=`). This is a template/placeholder, not a live credential. The connection string is read at runtime from `IConfiguration` in `DependencyInjection.cs` (line 30) — correct pattern. No real password is hardcoded.

**Risk:** Informational. An empty-password template committed to source control is acceptable as a dev default, provided production credentials always come from environment variables.

**Required action:** No fix required. Developer should confirm production deploys always inject `ConnectionStrings__DefaultConnection` via environment variable, never rely on this file.

---

## Custom Rule Findings

| # | Rule ID | File | Line | Severity | Fixed |
|---|---------|------|------|----------|-------|
| CR-1 | `spaceos-no-hardcoded-jwt-secret` | `SpaceOS.Kernel.Api/appsettings.Development.json` | 6 | **CRITICAL** | No |
| CR-2 | `spaceos-no-hardcoded-connstring` | `SpaceOS.Kernel.Api/appsettings.json` | 3 | WARNING | No — empty password, acceptable pattern |
| CR-3 | `spaceos-no-auto-migrate` | All Infrastructure files | — | PASS | N/A |
| CR-4 | `spaceos-no-stacktrace-response` | `ExceptionHandlingMiddleware.cs`, `ResultExtensions.cs` | — | PASS | N/A |

---

## SQL Injection Analysis — DashboardStatsQuery.cs (SA-4)

`Database.SqlQuery<T>()` accepts a `FormattableString` (the `$"""..."""` raw string literal). EF Core treats `FormattableString` arguments specially: interpolated holes become parameterized SQL parameters, while the static template becomes the query text.

**In this specific query there are zero interpolated holes.** The entire SQL is a compile-time constant:

```csharp
$"""
SELECT
    CAST((SELECT COUNT(*) FROM "Tenants") AS INTEGER)   AS "TenantCount",
    ...
"""
```

- No user-supplied input reaches this query.
- No runtime-constructed identifiers (table names, column names).
- No `string.Format`, `+` concatenation, or `FromSqlRaw` with non-parameterized values.
- The `'Active'` status literal is a hardcoded string, not user input.

**Verdict: No SQL injection risk.** The use of `FormattableString` here is the correct EF Core pattern and provides defence in depth even though it is not strictly required for this static query.

---

## Authorization Analysis — DashboardEndpoints.cs

| Check | Finding | Verdict |
|-------|---------|---------|
| Endpoint has `RequireAuthorization` | Yes — `"ReadPolicy"` | PASS |
| `ReadPolicy` defined in Program.cs | Yes — line 153: Joiner, Designer, Admin | PASS |
| Authentication middleware registered before authorization | Yes — lines 165–166 | PASS |
| Rate limiting applied | Yes — `"fixed"` (100 req/60s) | PASS |
| Health endpoint bypass scope | `/healthz` only — `.DisableRateLimiting()`, no auth | PASS |
| No route parameter accepted from user | Parameterless query — no injection surface | PASS |
| No tenant-scoped filter needed | Dashboard is system-wide aggregate, intentionally cross-tenant | PASS |

No authorization bypass risk found.

---

## Information Disclosure Analysis

| Check | File | Finding | Verdict |
|-------|------|---------|---------|
| Stack trace in API response | `ExceptionHandlingMiddleware.cs` | Generic "An unexpected error occurred." for 500s | PASS |
| Stack trace in API response | `ResultExtensions.cs` | No exception detail ever surfaced | PASS |
| `DashboardStatsDto` field exposure | Returns only aggregate integer counts | No PII, no internal IDs, no tenant names | PASS |
| Logging sensitive data | `GetDashboardStatsQueryHandler.cs` | No logging present — handler is read-only query | PASS |
| `AuditEventCount` disclosure | Returns a total count only | No event content, no event IDs returned | PASS |

No information disclosure risk found.

---

## SSRF Analysis

The dashboard endpoint issues no outbound HTTP calls. `IDashboardStatsQuery` / `DashboardStatsQuery` only reads from the local PostgreSQL database. The SSRF surface (federation `ExternalSourceUrl` on `SpaceLayer`) is not exercised by this endpoint.

**Verdict: No SSRF risk.**

---

## Architecture / Layer Dependency Analysis

| Check | Finding | Verdict |
|-------|---------|---------|
| Domain layer has zero external NuGet packages | `SpaceOS.Kernel.Domain.csproj` — only `MediatR.Contracts`, `Ardalis.Specification` | PASS |
| `IDashboardStatsQuery` interface in Domain | Correct — `/Domain/Dashboard/IDashboardStatsQuery.cs` | PASS |
| `DashboardStatsQuery` implementation in Infrastructure | Correct — `/Infrastructure/Data/Queries/DashboardStatsQuery.cs` | PASS |
| Application layer has no EF Core reference | No `Microsoft.EntityFrameworkCore` in `Application.csproj` | PASS |
| Handler references only Domain interface | `GetDashboardStatsQueryHandler` depends on `IDashboardStatsQuery` only | PASS |
| `Database.Migrate()` at startup | Not present in `DependencyInjection.cs` or `Program.cs` | PASS |

---

## Summary

| Category | Count |
|----------|-------|
| CRITICAL (unresolved) | 1 |
| ERROR (unresolved) | 0 |
| WARNING (needs developer review) | 4 |
| INFO | 1 (SA-4 — SQL injection verified safe) |
| Auto-fixes applied | 0 |
| Build after fixes | N/A — no auto-fixes applied |

### CRITICAL unresolved
- **SA-1 / CR-1:** Hardcoded JWT signing key in `appsettings.Development.json` — key `"AIzaSyAmVVu7TvjuZiAII2kYZkgVKhGHmhUH1Xk"` must be removed and rotated.

### WARNING — developer review required
- **SA-2:** Test JWT key in `appsettings.Testing.json` — low risk, confirm gitignore status.
- **SA-3 / CR-2:** Empty-password connection string template in `appsettings.json` — confirm production always overrides via environment variable.
- **Supply chain:** `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.Data.Sqlite`, `Microsoft.EntityFrameworkCore.Sqlite` remain unapproved — pre-existing, no change from E6.

### Clean checks (no findings)
- SQL injection (DashboardStatsQuery — static query, zero interpolation holes)
- Authorization bypass
- Information disclosure / stack trace leakage
- SSRF
- Auto-migration at startup
- Layer dependency violations
- New package additions (none introduced by E28)
