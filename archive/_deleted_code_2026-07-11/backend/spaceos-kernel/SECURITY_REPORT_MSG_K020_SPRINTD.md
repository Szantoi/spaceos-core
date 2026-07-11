# Security Report — MSG-K020 (Sprint D Phase 1)
**Date:** 2026-04-06
**Agent:** kernel-security-scanner
**Scope:** T-08 (appsettings.Production.json), T-07 (SourceBrand feature), T-03 (systemd service), T-04 (CI/CD pipeline)
**Final status:** SECURITY_PASSED (with WARNING-level findings requiring developer review)

---

## Supply Chain Findings

All `.csproj` files in scope were audited against the approved package list.

| Package | Version | Project | Severity | CVE | Action |
|---------|---------|---------|----------|-----|--------|
| `Microsoft.Data.Sqlite` | 8.0.11 | Tests (x2) | WARNING | None known | Recurring — not on approved list; test-only; flag until approved or removed |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.11 | Infrastructure | WARNING | None known | Recurring — not on original approved list; no CVE; flag until `REVIEW_CHECKLIST.md` updated |

No new packages were introduced by MSG-K020 changes. No CRITICAL or ERROR supply chain findings.

---

## Static Analysis Findings

### Secrets

| # | Rule | File | Line | Severity | Auto-fixed | Action Required |
|---|------|------|------|----------|------------|-----------------|
| SA-01 | `p/secrets` — empty password in connection string | `SpaceOS.Kernel.Api/appsettings.json` | 4 | WARNING | No | Password field is empty string `""` — not a secret leak, but confirms the baseline config file carries a DSN template with `Password=`. This file IS git-tracked. The empty password is intentional (populated from environment at runtime via `IConfiguration`). No credentials present; accepted per prior project pattern. |

No hardcoded JWT secrets found. `Program.cs` reads the RSA public key PEM from `IConfiguration["Jwt:RsaPublicKeyPem"]` and falls back to `DevRsaKeyManager` only in development. `appsettings.Production.json` contains only `Urls` — no secrets.

### Injection / Raw SQL

| # | Rule | File | Line | Severity | Auto-fixed | Action Required |
|---|------|------|------|----------|------------|-----------------|
| SA-02 | Raw SQL — `SqlQuery<T>` with interpolated string | `SpaceOS.Infrastructure/Data/Queries/DashboardStatsQuery.cs` | 29 | INFO | No | Uses C# raw string literal interpolation (`$"""`). EF Core's `SqlQuery<T>` with a C# interpolated string is treated as a `FormattableString` — EF translates this to a parameterized query. No user input enters the string; all values are hardcoded SQL aggregate expressions. No injection risk. No action required. |
| SA-03 | Raw SQL — `SqlQueryRaw<bool>` with positional parameter | `SpaceOS.Infrastructure/Sync/PostgresAdvisorySyncSignalWriteLock.cs` | 49 | INFO | No | Uses `{0}` positional parameter — correctly parameterized. No injection risk. |
| SA-04 | Raw SQL — `SqlQueryRaw<bool>` with positional parameter | `SpaceOS.Infrastructure/AuditLog/PostgresAdvisoryAuditWriteLock.cs` | 37 | INFO | No | Same pattern as SA-03. No injection risk. |
| SA-05 | Raw SQL — `ExecuteSqlRawAsync` with literal DDL | `SpaceOS.Infrastructure/Data/PostgresSchemaInitializer.cs` | 30, 38 | INFO | No | DDL statements for `security_barrier` views. No user input. No injection risk. |

### Insecure Deserialization

No uses of `JsonSerializer` with untrusted, externally-sourced input found in the changed files. `AuditEventDispatcher` serializes internal domain events (developer-controlled types) — not deserializing inbound payloads.

### Path Traversal

No file path construction from user-supplied input found in the changed files.

### SSRF

| # | Rule | File | Line | Severity | Auto-fixed | Action Required |
|---|------|------|------|----------|------------|-----------------|
| SA-06 | Outbound HTTP — webhook URL from `IConfiguration` | `SpaceOS.Infrastructure/Alerting/WebhookAlertService.cs` | 63 | WARNING | No | `_webhookUrl` is read from `Alerting:WebhookUrl` in configuration — not from any request or user input. An operator misconfiguring this to an internal address would create an SSRF-equivalent risk at the infrastructure level. URL is not validated against an allowlist or SSRF guard before use. Developer action: add scheme + host validation (HTTPS-only, no RFC1918 targets) before the `PostAsJsonAsync` call in a future sprint. |

`ExternalSourceUrl` on `SpaceLayer` is stored read-only; no sprint D code makes outbound HTTP calls using it. SSRF risk from that field remains deferred (tracked in `project_spacelayer_ssrf_risk.md`).

---

## Custom Rule Findings

| # | Rule ID | File | Line | Severity | Fixed | Notes |
|---|---------|------|------|----------|-------|-------|
| CR-01 | `spaceos-no-hardcoded-connstring` | `SpaceOS.Kernel.Api/appsettings.json` | 4 | PASS | N/A | Connection string present but `Password=` is empty; populated from environment. Compliant. |
| CR-02 | `spaceos-no-auto-migrate` | All `.cs` files | — | PASS | N/A | No `Database.Migrate()` call found anywhere in the codebase. |
| CR-03 | `spaceos-no-stacktrace-response` | `SpaceOS.Kernel.Api/Program.cs` | — | PASS | N/A | No `ex.StackTrace` passed to `Results.Problem`. ExceptionHandlingMiddleware does not expose stack traces. |
| CR-04 | `spaceos-no-hardcoded-jwt-secret` | `SpaceOS.Kernel.Api/Program.cs` | 77–86 | PASS | N/A | `IssuerSigningKey` is constructed from `IConfiguration["Jwt:RsaPublicKeyPem"]`, not from a hardcoded string. Dev fallback uses asymmetric `DevRsaKeyManager` — symmetric HS256 is never used. |

---

## SourceBrand Feature — Focused Analysis (T-07)

### Header injection risk

`HttpContextCurrentRequestContext.SourceBrand` (Infrastructure/Auth/HttpContextCurrentRequestContext.cs):

```csharp
var raw = ctx.Request.Headers["X-SpaceOS-Brand"]
    .FirstOrDefault()?.Trim().ToLowerInvariant();
return raw is not null && AllowedBrands.Contains(raw) ? raw : null;
```

**Finding: CLEAN.** The implementation normalizes to lowercase and validates against a static `HashSet<string>` allowlist (`joinerytech`, `asztalostech`) before any use. Non-matching values are silently discarded (return `null`). The raw header value is never stored, logged, or returned to callers. No injection path exists.

**Allowlist hardcoded in source:** The two brand values are literals in a private static field. This is intentional for the current two-tenant deployment. If brand count grows, this will require a config-driven allowlist — flag for future sprint when onboarding a third brand.

### Hash chain integrity

`AuditEventDispatcher.cs` line 134:

```csharp
var chainInput = $"{previousHash}:{json}:{occurredAt:O}:{sourceBrand ?? ""}";
```

**Finding: WARNING (CR-05).** See dedicated finding below.

### SourceBrand in AuditEventDto — information disclosure

`AuditEventDto` includes `SourceBrand` as a returned field. `SourceBrand` values are confined to the allowlist (`joinerytech`, `asztalostech`) and carry no secrets. The field is appropriate audit metadata. No information disclosure risk.

---

## Hash Chain Integrity Finding

| # | Rule ID | File | Line | Severity | Fixed |
|---|---------|------|------|----------|-------|
| CR-05 | `spaceos-hash-chain-separator-collision` | `SpaceOS.Kernel.Application/AuditLog/AuditEventDispatcher.cs` | 134 | WARNING | No |

**Description:** The hash chain input uses `:` as a field separator:

```
{previousHash}:{json}:{occurredAt:O}:{sourceBrand ?? ""}
```

`previousHash` and `stateHash` are fixed-length SHA-256 hex strings (64 characters) — no collision risk from those fields. `occurredAt:O` (ISO 8601 round-trip) is also fixed-format. However, `json` (the serialized domain event payload) can contain the `:` separator character, which is expected and harmless here because the delimiter structure is positionally unambiguous: `previousHash` is always exactly 64 hex chars, `occurredAt:O` is always a known-width ISO timestamp, and `sourceBrand` is post-final-colon.

The actual risk is subtler: `sourceBrand ?? ""` means that when `sourceBrand` is `null`, the chain input ends with a trailing `:`. When `sourceBrand` is `"joinerytech"`, it ends with `:joinerytech`. This is non-ambiguous for a hash input — the hash function processes the full byte string and both produce distinct digests. **No length-extension or collision risk exists for SHA-256 in this usage pattern.**

**Residual concern:** If a future brand value were allowed to contain `:` (which the current allowlist prevents), separator collision would become theoretically constructable. The current allowlist (`joinerytech`, `asztalostech`) contains no colons, so this is not exploitable today.

**Action required:** Add a code comment in `AuditEventDispatcher.cs` documenting that any future addition to `AllowedBrands` must not contain the `:` character, to protect hash chain integrity. No code change needed now. Developer review item.

---

## Migration Safety Finding (T-07)

| # | Rule | File | Line | Severity | Fixed |
|---|------|------|------|----------|-------|
| MS-01 | `CREATE INDEX CONCURRENTLY` with `suppressTransaction: true` | `SpaceOS.Infrastructure/Migrations/20260406090000_AddSourceBrandToAuditEvents.cs` | 22–28 | INFO | N/A |

**Description:** `suppressTransaction: true` is correctly required for `CREATE INDEX CONCURRENTLY` in PostgreSQL — this index build runs outside any transaction, which is exactly what `CONCURRENTLY` demands. The `AddColumn` operation on line 15 is in the default transaction wrapper (correct). The `CONCURRENTLY` index creation follows as a separate statement outside any transaction (correct).

**Partial index** (`WHERE "SourceBrand" IS NOT NULL`) is appropriate — it reduces index size for the nullable column. No issues.

**`Down()` migration** mirrors this correctly with `DROP INDEX CONCURRENTLY IF EXISTS` under `suppressTransaction: true`.

**Finding: CLEAN.** No action required.

---

## Systemd Service Hardening Analysis (T-03)

File: `spaceos-kernel/spaceos-kernel.service`

| Directive | Present | Assessment |
|-----------|---------|------------|
| `User=spaceos` | Yes | Correct — dedicated non-root service account |
| `NoNewPrivileges=true` | Yes | Prevents privilege escalation via setuid/setcap |
| `ProtectSystem=strict` | Yes | Mounts `/`, `/usr`, `/boot` read-only |
| `ProtectHome=true` | Yes | Blocks access to `/home`, `/root`, `/run/user` |
| `PrivateTmp=true` | Yes | Isolated `/tmp` and `/var/tmp` namespaces |
| `ReadWritePaths=` | Yes | Scoped write access to publish dir and log dir |
| `EnvironmentFile=/etc/spaceos/kernel.env` | Yes | Secrets loaded from protected env file |
| `Restart=always` | Yes | Appropriate for a production daemon |

**Missing hardening directives — WARNING (SY-01):**

The following systemd security directives are absent and would be appropriate for this workload:

| Missing Directive | Risk if Absent | Recommended Value |
|-------------------|---------------|-------------------|
| `CapabilityBoundingSet=` | Service retains default Linux capabilities | `CapabilityBoundingSet=` (empty — drops all) |
| `AmbientCapabilities=` | Without explicit drop, capabilities are inherited | `AmbientCapabilities=` (empty) |
| `ProtectKernelTunables=true` | Kernel sysctl writes possible | Add |
| `ProtectKernelModules=true` | Kernel module loading possible | Add |
| `ProtectControlGroups=true` | cgroup filesystem writable | Add |
| `RestrictNamespaces=true` | Namespace creation unrestricted | Add |
| `LockPersonality=true` | Personality syscall unrestricted | Add |
| `SystemCallFilter=@system-service` | No syscall allowlist enforced | Add `SystemCallFilter=@system-service` |

**Severity: WARNING.** The current file provides solid baseline hardening (`NoNewPrivileges`, `ProtectSystem=strict`, `PrivateTmp`). The missing directives are defense-in-depth — they do not represent an exploitable gap given the loopback-only binding and the `spaceos` non-root user. Developer action: add the missing directives in a follow-up hardening pass.

---

## CI/CD Pipeline Analysis (T-04)

File: `.github/workflows/ci.yml`

### Secrets handling

| Finding | Severity | Notes |
|---------|----------|-------|
| `VPS_HOST`, `VPS_DEPLOY_USER`, `VPS_DEPLOY_KEY` are passed via `${{ secrets.* }}` | PASS | GitHub Actions encrypted secrets — correct pattern |
| SSH private key passed as `key:` to `appleboy/ssh-action@v1` | INFO | Key never appears in logs when passed via secrets; the action handles it correctly |

### Action pinning — WARNING (CI-01)

| Action | Current Reference | Risk |
|--------|------------------|------|
| `actions/checkout@v4` | Mutable tag | Tag can be moved to a different commit |
| `actions/setup-dotnet@v4` | Mutable tag | Same |
| `appleboy/ssh-action@v1` | Mutable tag | Third-party action — higher supply chain risk |

**Finding: WARNING.** GitHub Actions referenced by mutable version tags (`@v4`, `@v1`) rather than pinned SHA digests. A compromised or force-pushed tag could substitute malicious action code. `appleboy/ssh-action` is a third-party action with SSH key access — this represents the highest-risk unpinned reference.

**Action required:** Pin all actions to their full commit SHA, especially `appleboy/ssh-action`. Example:
```yaml
uses: appleboy/ssh-action@7eaf76671a0d7eec5d98ee897acda4f968e77e48  # v1.0.3
```

### Double-build in deploy job — WARNING (CI-02)

```yaml
deploy:
  steps:
    - run: dotnet publish -c Release -o ./publish   # builds from fresh checkout
    - Deploy to VPS:
        script: |
          cd /opt/spaceos/spaceos-kernel
          git pull
          dotnet publish -c Release -o ./publish    # builds again on the VPS
```

**Finding: WARNING.** The deploy job publishes the artifact on the runner, but the VPS deployment script does a `git pull` and then runs `dotnet publish` again on the server. The artifact built in the runner is never transferred to the VPS — the runner's `./publish` output is discarded. The effective deployment is the server-side build, which:
1. Means the artifact deployed to production was **not** the artifact tested in CI (different environment, different inputs).
2. Requires `dotnet` SDK on the production VPS (not just the runtime).
3. Creates a window where `git pull` fetches code that was not signed off by the CI test run (e.g., a commit pushed between the CI `test` job and the deploy step could bypass tests on the next deploy).

**Action required:** Either transfer the runner-built artifact via `scp`/`rsync` and restart the service, or remove the runner `dotnet publish` step and document the server-side build as intentional. This is a deployment integrity issue, not a secret-exposure issue.

### Vulnerability scan step — PASS (CI-03)

```yaml
- name: Vulnerability scan (SEC-07)
  run: dotnet list package --vulnerable --include-transitive 2>&1 | tee vuln.txt && ! grep -q "has the following vulnerable packages" vuln.txt
```

The step correctly fails the job when vulnerable packages are detected. The `!` negation of `grep` exit code and `tee` for log capture are both correct. The `2>&1` redirect ensures SDK errors are also captured. No issues.

---

## X-Forwarded-For — Ongoing Finding

| # | Rule | File | Severity | Status |
|---|------|------|----------|--------|
| FF-01 | `X-Forwarded-For` read without `UseForwardedHeaders` middleware | `SpaceOS.Infrastructure/Auth/HttpContextCurrentRequestContext.cs` line 42 | INFO | Persisting from prior scan |

`Program.cs` does not call `app.UseForwardedHeaders()` or configure `ForwardedHeadersOptions` with a trusted proxy list. `SourceIp` derived from `X-Forwarded-For` is therefore spoofable by any caller. Since `SourceIp` is audit metadata only (not used for authorization or rate limiting), this is an audit log accuracy issue, not a security bypass. The production binding is loopback-only (`127.0.0.1:5000`) — a reverse proxy sits in front, making this relevant for deployment. Finding persists from full re-scan (2026-04-05). No new sprint D code worsens this.

---

## Summary

| Category | CRITICAL | ERROR | WARNING | INFO |
|----------|----------|-------|---------|------|
| Supply chain | 0 | 0 | 2 | 0 |
| Static analysis — secrets | 0 | 0 | 0 | 1 |
| Static analysis — injection/SQL | 0 | 0 | 0 | 4 |
| Static analysis — SSRF | 0 | 0 | 1 | 0 |
| Custom rules | 0 | 0 | 1 | 0 |
| Migration safety | 0 | 0 | 0 | 1 |
| Systemd hardening | 0 | 0 | 1 | 0 |
| CI/CD pipeline | 0 | 0 | 2 | 1 |
| Forwarded headers (ongoing) | 0 | 0 | 0 | 1 |
| **Total** | **0** | **0** | **7** | **7** |

- CRITICAL: 0
- ERROR: 0
- WARNING: 7 (all require developer review — see individual findings)
- INFO: 7 (no action required)
- Auto-fixes applied: 0 (no ERROR/CRITICAL findings eligible)
- Build after fixes: N/A (no code changes made)

### Warning index for developer action

| ID | Finding | File | Owner |
|----|---------|------|-------|
| SA-06 | Webhook URL not SSRF-validated | `WebhookAlertService.cs` | Infra |
| CR-05 | Hash chain separator comment missing | `AuditEventDispatcher.cs` line 134 | Application |
| SY-01 | systemd: missing `CapabilityBoundingSet`, `SystemCallFilter`, 6 others | `spaceos-kernel.service` | DevOps |
| CI-01 | GitHub Actions not pinned to commit SHA | `.github/workflows/ci.yml` | DevOps |
| CI-02 | Double-build: runner artifact discarded, VPS re-builds from git | `.github/workflows/ci.yml` | DevOps |
| SC-01 | `Microsoft.Data.Sqlite` not on approved list (recurring) | `*.Tests.csproj` (x2) | Team |
| SC-02 | `Npgsql.EFCore.PostgreSQL` not on approved list (recurring) | `SpaceOS.Infrastructure.csproj` | Team |
