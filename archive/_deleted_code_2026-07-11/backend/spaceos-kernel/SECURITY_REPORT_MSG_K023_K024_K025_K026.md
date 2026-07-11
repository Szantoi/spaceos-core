# Security Report — MSG-K023 / K024 / K025 / K026
**Date:** 2026-04-04
**Agent:** kernel-security-scanner
**Scope:** Sprint C Phase 4-7 — Crypto, Auth, SSRF Guard, API Endpoints, FlowManagement module
**Final status:** SECURITY_FAILED

---

## Scope — Files Reviewed

| File | Sprint |
|------|--------|
| `SpaceOS.Infrastructure/Crypto/AesGcmColumnEncryptionService.cs` | K023 |
| `SpaceOS.Infrastructure/Crypto/ConfigKeyVaultService.cs` | K023 |
| `SpaceOS.Infrastructure/Crypto/SyncSignalHasher.cs` | K023 |
| `SpaceOS.Infrastructure/Auth/NodeAuthService.cs` | K024 |
| `SpaceOS.Infrastructure/Auth/DevRsaKeyManager.cs` | K024 |
| `SpaceOS.Infrastructure/Data/TenantContextMiddleware.cs` | K024 |
| `SpaceOS.Infrastructure/Validation/NodeUrlValidator.cs` | K025 |
| `SpaceOS.Infrastructure/DependencyInjection.cs` | K023–K025 |
| `SpaceOS.Kernel.Api/Endpoints/NodeEndpoints.cs` | K026 |
| `SpaceOS.Kernel.Api/Endpoints/SyncEndpoints.cs` | K026 |
| `SpaceOS.Kernel.Api/Middleware/SipVersionMiddleware.cs` | K026 |
| `SpaceOS.Kernel.Api/Middleware/ExceptionHandlingMiddleware.cs` | K026 |
| `SpaceOS.Modules.FlowManagement/**` (all source files) | K026 |
| `scripts/db-init.sql` | persistent carry-over |
| All `*.csproj` files | supply chain |

---

## Supply Chain Findings

| # | Package | Project | Version | On Approved List | Severity | CVE | Action |
|---|---------|---------|---------|-----------------|----------|-----|--------|
| SC-1 | `Microsoft.IdentityModel.Tokens` | SpaceOS.Infrastructure | 7.6.0 | No | WARNING | None known | Flag until added to approved list or version pinned in policy |
| SC-2 | `System.IdentityModel.Tokens.Jwt` | SpaceOS.Infrastructure | 7.6.0 | No | WARNING | None known | Flag until added to approved list or version pinned in policy |
| SC-3 | `Microsoft.AspNetCore.Authentication.JwtBearer` | SpaceOS.Kernel.Api | 8.0.11 | No | WARNING | None known | Flag until added to approved list |
| SC-4 | `Npgsql.EntityFrameworkCore.PostgreSQL` | SpaceOS.Infrastructure | 8.0.11 | No (carry-over from E6/T1) | WARNING | None known | Persistent carry-over — flag every scan until approved list updated |
| SC-5 | `Microsoft.Data.Sqlite` | SpaceOS.Kernel.Tests, SpaceOS.Kernel.Api.Tests, SpaceOS.Kernel.IntegrationTests | 8.0.11 | No (carry-over) | WARNING | None known | Test-only, no CVE — persistent carry-over |
| SC-6 | `Microsoft.EntityFrameworkCore` (in FlowManagement) | SpaceOS.Modules.FlowManagement | 8.0.11 | No (new project) | WARNING | None known | New project, not explicitly on approved list — flag until confirmed |
| SC-7 | `Microsoft.EntityFrameworkCore.Relational` (in FlowManagement) | SpaceOS.Modules.FlowManagement | 8.0.11 | No (new project) | WARNING | None known | New project — flag until confirmed |

**Notes:**
- `Microsoft.IdentityModel.Tokens` 7.6.0 and `System.IdentityModel.Tokens.Jwt` 7.6.0 are the JWT stack for RS256 node auth. They are not on the original approved list from CLAUDE.md. No CVE found at audit date. Must be formally approved.
- `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.11 is part of the ASP.NET Core framework family and is effectively approved by `FrameworkReference`, but is listed explicitly — confirm intent.
- All EF Core packages in `SpaceOS.Modules.FlowManagement` are the same versions approved elsewhere but the project is new and should be reviewed.

---

## Static Analysis Findings

| # | Rule | File | Line | Severity | Auto-fixed | Action Required |
|---|------|------|------|----------|------------|-----------------|
| SA-1 | `p/secrets` — empty password in connection string | `appsettings.json` | 3 | WARNING | No | Connection string contains `Password=` (empty). Ensure environment variable injection at deploy time; verify CI/CD does not ship this file with a blank password as a valid default |
| SA-2 | `p/csharp` — sync-over-async (`GetAwaiter().GetResult()`) | `AesGcmColumnEncryptionService.cs` | 33 | WARNING | No | Blocking async call in constructor; acceptable for singleton initialisation but creates thread-pool starvation risk under high DI initialisation load |
| SA-3 | `p/csharp` — sync-over-async (`GetAwaiter().GetResult()`) | `SyncSignalHasher.cs` | 25 | WARNING | No | Same pattern as SA-2 |
| SA-4 | `p/owasp-top-ten` — insufficient SSRF hostname coverage | `NodeUrlValidator.cs` | 44–47 | ERROR | No | DNS hostname check only rejects `.local` and `.internal` suffixes; does not reject arbitrary public hostnames that could DNS-rebound to private IPs (see SSRF section) |
| SA-5 | `p/csharp` — private key written to filesystem | `DevRsaKeyManager.cs` | 31 | ERROR | No | `ExportPkcs8PrivateKeyPem()` writes the RSA private key to `keys/dev-private-key.pem` in the working directory. If this directory is within the Docker build context or a mounted volume accessible to other containers it leaks the key |
| SA-6 | `p/secrets` — hardcoded password in SQL script | `scripts/db-init.sql` | 8 | ERROR | No | `PASSWORD 'changeme_in_production'` — carry-over finding; must be removed |

---

## Custom Rule Findings

| # | Rule ID | File | Line | Severity | Fixed | Notes |
|---|---------|------|------|----------|-------|-------|
| CR-1 | `spaceos-no-hardcoded-connstring` | `appsettings.json` | 3 | WARNING | No | Connection string is present but password field is empty — not a literal credential, but the DSN structure is baked into the shipped artifact |
| CR-2 | `spaceos-no-hardcoded-jwt-secret` | `ConfigKeyVaultService.cs` | 31–33 | CRITICAL | No | Deterministic dev key derived from the well-known constant `"spaceos-dev-signing-key-2026"`. If `Crypto:SigningKey` is absent from configuration this key is used. **`ConfigKeyVaultService` is registered unconditionally** in `DependencyInjection.cs` line 115 — it is NOT gated on `environment.IsDevelopment()`. Any production deployment that omits `Crypto:SigningKey` will silently use the public constant key. |
| CR-3 | `spaceos-no-hardcoded-jwt-secret` | `ConfigKeyVaultService.cs` | 43–45 | CRITICAL | No | Same pattern for `"spaceos-dev-encryption-key-2026"`. AES column encryption falls back to a well-known deterministic key when `Crypto:EncryptionKey` is absent. Same unconditional registration risk as CR-2. |
| CR-4 | `spaceos-no-auto-migrate` | `DependencyInjection.cs` | — | PASS | N/A | No `Database.Migrate()` found |
| CR-5 | `spaceos-no-stacktrace-response` | All API files | — | PASS | N/A | `ExceptionHandlingMiddleware` correctly strips stack traces |
| CR-6 | `spaceos-no-hardcoded-jwt-secret` | `NodeAuthService.cs` | 27–29 | ERROR | No | `NodeAuthService` is registered unconditionally (line 120 DI). It uses `DevRsaKeyManager.Instance` — an in-process ephemeral key — for both issuance and validation. Production deployments will sign node JWTs with a key that is regenerated every process restart and is never persisted to Key Vault. Tokens issued before a restart will fail validation after restart. |

---

## Detailed Analysis by Focus Area

### 1. AES-GCM Nonce Reuse (K023)

**Status: PASS**

`AesGcmColumnEncryptionService.Encrypt` (line 43) calls `RandomNumberGenerator.Fill(nonce)` unconditionally on every invocation. The nonce is a stack-local 12-byte array created per call. There is no nonce counter, no global nonce state, and no nonce reuse path. The implementation is correct.

The wire format `{base64_nonce}:{base64_tag}:{base64_ciphertext}` stores the nonce with every ciphertext, enabling correct decryption without shared state.

**No finding.**

---

### 2. HMAC Key Handling (K023)

**Status: PASS with carry-over concern**

`SyncSignalHasher` stores `_key` as `private readonly byte[]` — it is not logged, not serialised, not passed to any public method, and not returned in any response. HMAC computation uses `HMACSHA256.HashData(_key, inputBytes)`, which is the correct BCL static method (no HMAC object lifetime issue).

The key comes from `IKeyVaultService.GetSigningKeyAsync()`. In production this must be bound to `AzureKeyVaultService`. The fallback in `ConfigKeyVaultService` (CR-2 above) is the CRITICAL risk here — not the hasher itself.

**No standalone finding against SyncSignalHasher. Risk flows from CR-2.**

---

### 3. Dev Key Derivation Security (K023) — CRITICAL

**Status: CRITICAL FINDING — CR-2 and CR-3**

`ConfigKeyVaultService` is registered as `IKeyVaultService` **without** any `environment.IsDevelopment()` guard (DI line 115). This means:

- In staging or production, if `Crypto:SigningKey` and `Crypto:EncryptionKey` are not set in configuration or environment, the service falls back to keys derived deterministically from the public constants `"spaceos-dev-signing-key-2026"` and `"spaceos-dev-encryption-key-2026"`.
- An attacker who knows the source code (or guesses the convention) can derive the exact signing and encryption keys.
- HMAC-signed sync signal chains become forgeable.
- AES-256-GCM encrypted column data becomes decryptable.

**Required fix:** Either:
  a. Gate `ConfigKeyVaultService` registration behind `environment.IsDevelopment()` and throw in the `else` branch if `Crypto:SigningKey`/`Crypto:EncryptionKey` are absent, or
  b. Remove the fallback constants from `ConfigKeyVaultService` and throw `InvalidOperationException` when config is absent, forcing explicit key provision in all environments.

---

### 4. `set_config` SQL Injection (K024)

**Status: PASS**

`TenantContextMiddleware` (line 41–52):
1. First validates that `tidClaim` parses as a valid `Guid` — malformed input throws `SecurityException` before the SQL call.
2. Passes `tenantGuid.ToString()` (a canonical GUID string `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`) to `ExecuteSqlInterpolatedAsync` using C# string interpolation inside `$"..."`, which EF Core translates to a parameterised query (not string concatenation in SQL).

The `ExecuteSqlInterpolatedAsync` overload takes a `FormattableString` and always uses ADO.NET parameters for interpolated values. There is no SQL injection path.

**No finding.**

---

### 5. NodeUrlValidator Bypass Vectors (K025)

**Status: ERROR — SA-4**

The validator performs a structural check (scheme, port) and then a host check. The host check has two code paths:

**Path A — IP literal:** Full private-range check (127/8, 10/8, 172.16/12, 192.168/16, 169.254/16, 0/8, ::1, fe80::/10, fc00::/7). This is thorough and correct.

**Path B — DNS hostname:** Only rejects `localhost`, `.local`, and `.internal` suffixes. It does **not** perform DNS resolution at validation time.

**DNS rebinding / TOCTOU risk:** An attacker can register `node.example.com` which resolves to a valid public IP at validation time, pass the validator, then change the DNS record to resolve to `169.254.169.254` (AWS metadata endpoint) or `10.x.x.x` by the time the federation layer makes the actual outbound HTTP call. The validator only checks what the URL looks like, not what it resolves to.

**Additional missing IPv6 ranges:**
- `::ffff:0:0/96` (IPv4-mapped IPv6 addresses) — an attacker can pass `::ffff:192.168.1.1` as an IPv6 literal and the current address-family check may misclassify it depending on the OS parser.

**Required mitigations (developer action):**
  1. At execution time (when the outbound HTTP call is made), resolve the hostname and validate the resolved IP falls outside all private ranges before connecting.
  2. Add `::ffff:0:0/96` detection to the IPv6 block (check `bytes[0] == 0 && bytes[1] == 0 && ... && bytes[10] == 0xff && bytes[11] == 0xff`).
  3. Document the TOCTOU gap in the component XML doc and link to a future DNS-validation ticket.

**Note:** This finding was raised in prior scans (memory: `project_inode_url_validator_missing.md`) as `INodeUrlValidator` having no concrete implementation. A concrete implementation now exists. The TOCTOU risk in that implementation is a new ERROR-level finding.

---

### 6. JWT Algorithm Confusion (K024)

**Status: PASS**

`NodeAuthService.ValidateNodeJwtAsync` explicitly sets:

```csharp
ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
ValidateIssuerSigningKey = true,
```

This pins the allowed algorithm to `RS256` only. An attacker cannot present a `none`-algorithm token or attempt an `HS256` confusion attack (presenting the public key as an HMAC secret) because:
- `ValidAlgorithms` is a whitelist that causes `JwtSecurityTokenHandler` to reject any token whose header `alg` differs.
- The signing key is an `RsaSecurityKey`, which the JWT library will refuse to use as an HMAC key even if the algorithm were not pinned.

**No finding.**

---

### 7. DevRsaKeyManager Private Key on Filesystem (K024)

**Status: ERROR — SA-5**

`DevRsaKeyManager` writes the private key as a PKCS#8 PEM file to `keys/dev-private-key.pem` in the working directory. Two risks:

1. **Docker build context:** If `keys/` is not in `.dockerignore`, the private key will be baked into the image layer on any machine that starts the API (the key is generated lazily on first startup, but if an image is built after a prior run the file may already exist in the build context).
2. **Volume mounting:** In `docker-compose.yml`, if the working directory is volume-mounted, the file persists across container restarts and is visible on the host filesystem.

`NodeAuthService` is registered unconditionally (not dev-only). In non-development environments, `DevRsaKeyManager.Instance` is still the signing key for inter-node JWTs unless something overrides the `INodeAuthService` binding — which the current `DependencyInjection.cs` does not do. The `else` branch registers `AzureKeyVaultRsaPublicKeyProvider` for `IRsaPublicKeyProvider` (audit signatures), but `INodeAuthService` stays bound to `NodeAuthService`/`DevRsaKeyManager` in production.

**Required fix:** Gate `NodeAuthService` registration behind `environment.IsDevelopment()`. In the `else` branch register a production implementation that uses the Key Vault RSA key for node JWT issuance.

---

### 8. `InProcessSyncSignalWriteLock` in Production Branch (K023/DI) — Carry-over

**Status: WARNING (carry-over from memory)**

`DependencyInjection.cs` line 136 registers `InProcessSyncSignalWriteLock` in the production `else` branch. This is only safe for single-instance deployments. Re-flagged per memory policy.

---

### 9. `scripts/db-init.sql` Hardcoded Password — Carry-over

**Status: ERROR (carry-over from memory)**

`scripts/db-init.sql` line 8: `PASSWORD 'changeme_in_production'`. Unchanged from prior scans. Re-flagged per memory policy.

---

### 10. SipVersionMiddleware Header Validation (K026)

**Status: PASS**

`SipVersionMiddleware` validates the `SpaceOS-SIP-Version` header using an exact allowlist (`SupportedVersions`). Case-insensitive comparison is correct. The error response does not include any user-controlled data from the request (no header value reflection). The `detail` message is a static string. The `supported` array reveals the supported version, which is by design.

**No finding.**

---

### 11. API Endpoint Security (K026)

**Status: PASS**

All three new endpoint groups (`NodeEndpoints`, `SyncEndpoints`) use `RequireAuthorization` with named policies:
- `/api/nodes/register` → `AdminPolicy` + `node-register` rate limit
- `/api/nodes/heartbeat` → `WritePolicy` + `node-heartbeat` rate limit
- `/api/nodes/{tenantId}/manifest` → `ReadPolicy` + `fixed` rate limit
- `/api/sync/signal` → `WritePolicy` + `sync-signal` rate limit

All four endpoints are covered by both auth and rate limiting. No unauthenticated surface exposed.

Route parameters are typed (`{tenantId:guid}`) — prevents non-GUID values from reaching handlers.

**No finding.**

---

### 12. FlowManagement Module Dependency Hygiene (K026)

**Status: WARNING**

`SpaceOS.Modules.FlowManagement` pulls `Microsoft.EntityFrameworkCore` 8.0.11 and `Microsoft.EntityFrameworkCore.Relational` 8.0.11 directly. These are not on the approved package list by name (EF Core 8 is approved, but the FlowManagement project is a new module and has not been formally reviewed against the approved list).

`FlowNodeResolver` and `OfflineQueueService` do not handle any secrets, do not make outbound HTTP calls, and do not process user-controlled file paths. No injection or crypto findings.

`OfflineQueueService.EnqueueAsync` passes the `payload` string directly to `OfflineSyncQueueItem.Create`. If `payload` contains structured data from the API layer, the domain object is responsible for validation. No SQL injection path — EF Core parameterises all values. No SSRF path.

**Supply chain finding only — see SC-6 / SC-7 above.**

---

## Summary

| Severity | Count | Items |
|----------|-------|-------|
| CRITICAL | 2 | CR-2 (dev signing key fallback unconstrained), CR-3 (dev encryption key fallback unconstrained) |
| ERROR | 4 | SA-4 (NodeUrlValidator TOCTOU/DNS rebinding), SA-5 (RSA private key to filesystem + unconditional prod binding), SA-6 (db-init.sql hardcoded password carry-over), CR-6 (NodeAuthService unconditional in production) |
| WARNING | 9 | SA-1 (empty password in appsettings.json), SA-2 (sync-over-async in AesGcm constructor), SA-3 (sync-over-async in SyncSignalHasher constructor), SC-1..SC-7 (supply chain unlisted packages), DI InProcessSyncSignalWriteLock in prod (carry-over) |
| Auto-fixes applied | 0 | No semgrep-autofixable patterns in this scan scope |
| Build after fixes | N/A | No auto-fixes applied; build not re-run |

---

## Blocking Items (must be resolved before SECURITY_PASSED)

| # | Severity | Item | Owner Action |
|---|----------|------|-------------|
| 1 | CRITICAL | CR-2: `ConfigKeyVaultService` fallback signing key `"spaceos-dev-signing-key-2026"` reachable in production | Gate registration behind `IsDevelopment()` OR throw when config absent |
| 2 | CRITICAL | CR-3: `ConfigKeyVaultService` fallback encryption key `"spaceos-dev-encryption-key-2026"` reachable in production | Same fix as CR-2 |
| 3 | ERROR | CR-6: `NodeAuthService` (uses `DevRsaKeyManager`) registered unconditionally | Gate behind `IsDevelopment()`; provide Key Vault-backed implementation in prod |
| 4 | ERROR | SA-4: `NodeUrlValidator` DNS hostname check has TOCTOU/DNS-rebinding gap | Add runtime DNS resolution + re-validation at HTTP call site; add IPv4-mapped IPv6 block |
| 5 | ERROR | SA-5: `DevRsaKeyManager` writes RSA private key to `keys/dev-private-key.pem` | Ensure `keys/` is in `.dockerignore`; confirm file is in `.gitignore`; verify Docker volume mapping |
| 6 | ERROR | SA-6: `scripts/db-init.sql` line 8 hardcoded password (carry-over) | Replace with env-var substitution or Docker secret injection |

---

## Accepted / Informational Items

| # | Severity | Item | Status |
|---|----------|------|--------|
| A-1 | WARNING | SA-2 / SA-3: `GetAwaiter().GetResult()` in singleton constructors | Acceptable for singleton initialisation at startup; no runtime starvation in steady state |
| A-2 | WARNING | SC-1..SC-3: JWT stack packages not on approved list | Functionally required; recommend adding to approved list in next CLAUDE.md update |
| A-3 | WARNING | `InProcessSyncSignalWriteLock` in production DI branch | Carry-over; safe for single-instance, must be resolved before multi-instance deployment |
