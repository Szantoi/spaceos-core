# Security Report — SpaceOS.Modules.Joinery
Date: 2026-04-15
Auditor: kernel-security-scanner

---

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 0 |
| MEDIUM   | 3 |
| LOW      | 4 |
| OK       | many |

**Verdict: PASS** — No CRITICAL findings. All MEDIUM findings require developer review before production deployment.

---

## Area 1: Authentication & Authorization

**[OK]** All 13 endpoints (including `/api/orders/{id}/sheet`, `/api/orders/{id}/snapshots`, `/api/orders/{id}/revert`, `/api/orders/internal/results`) are covered by the `ManufacturerOnly` route group policy applied at line 23 of `DoorOrderEndpoints.cs`.

**[OK]** `TryGetTenantId()` guards every handler body: returns `null` → `401` if the `tenant_id` claim is absent, empty, or `Guid.Empty`. Cross-tenant access is structurally impossible at the endpoint layer.

**[OK]** JWT Authority is sourced from `IConfiguration["Jwt:Authority"]` or environment variable `JWT_AUTHORITY`. No hardcoded value present.

**[MEDIUM]** `ValidateAudience = false` in `Program.cs:31`.
- File: `SpaceOS.Modules.Joinery.Api/Program.cs:31`
- Risk: A token issued for a different service in the same Keycloak realm is accepted as long as it is signed correctly and contains a `Manufacturer` tenant_type claim. If any other SpaceOS microservice issues such tokens, a token meant for that service can be replayed against the Joinery API.
- Recommendation: Set `ValidateAudience = true` and set `opts.Audience = jwtAudience` (`kernel-api` is already configured). If the Kernel currently omits the audience claim in issued tokens, track this as a backlog item with a decision date.

**[MEDIUM]** `opts.Authority` can be null at startup (if both `Jwt:Authority` and `JWT_AUTHORITY` are unset). The framework will skip OIDC discovery / public key fetch silently rather than failing fast.
- File: `SpaceOS.Modules.Joinery.Api/Program.cs:27-28`
- Risk: In a misconfigured deployment the Authority is null, causing `JwtBearerHandler` to fail token validation at runtime. However it fails closed (rejects all tokens) so there is no authentication bypass. The risk is operational: silent misconfiguration is harder to detect than a startup crash.
- Recommendation: Add a fail-fast guard analogous to the connection string guard on line 13: `if (string.IsNullOrEmpty(jwtAuthority)) throw new InvalidOperationException("JWT Authority not configured")`. Exclude `Development` / `Testing` environments where `Authority` is intentionally blank.

**[OK]** `ManufacturerOnly` policy correctly requires `tenant_type == "Manufacturer"`. Non-manufacturer tokens receive `403 Forbidden`. Covered by `AuthApiTests.ProtectedEndpoints_NonManufacturerToken_Return403`.

---

## Area 2: Row-Level Security (RLS)

**[OK]** Migration `0001_InitialSchema`:
- `DoorOrders`: `ENABLE ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY` + policy `tenant_isolation` using `current_setting('app.tenant_id', true)::uuid`. Correct.
- `DoorItems`: Same, using a subquery into `DoorOrders`. Correct.

**[OK]** Migration `J0002_V2_CuttingListSnapshot`:
- `CuttingListSnapshots`: RLS FORCE + policy. Correct.
- `CuttingListLines`: RLS FORCE + policy (TenantId column). Correct.
- `CncInstructions`: RLS FORCE + policy. Correct.
- `ProcessSteps`: RLS FORCE + policy. Correct.
- `ProductionSheetCache`: RLS FORCE + policy. Correct.
- `JoineryOutboxEntries`: RLS FORCE + policy. Correct.

All seven tenant-relevant tables are fully covered. Config tables (`DoorTypeRules`, `PartDimensionRules`, `ProcessTaskTemplates`, `GlobalConstants`) are correctly excluded from RLS — they are global reference data.

**[OK]** `TenantSessionInterceptor` sets `app.tenant_id` via a fully parameterized `set_config(@key, @value, false)` call on every opened connection and resets it to empty on connection close/return-to-pool. Injection-safe, correctly implemented.

**[LOW]** `JoineryOutboxWorker` uses `FromSqlRaw` without a schema qualifier.
- File: `SpaceOS.Modules.Joinery.Infrastructure/Outbox/JoineryOutboxWorker.cs:44`
- Risk: The query `SELECT * FROM "JoineryOutboxEntries"` relies on the EF Core default schema (`spaceos_joinery`) being in the `search_path` of the worker's database connection. The `TenantSessionInterceptor` sets `app.tenant_id` but does not set the schema. If `search_path` is ever changed or the connection is obtained outside the normal interceptor scope, the query may fail with a table-not-found error. This is an operational/availability risk, not a security bypass — the `FROM UPDATE SKIP LOCKED` query still runs through the same connection pool that has RLS active.
- Recommendation: Qualify the table name: `SELECT * FROM spaceos_joinery."JoineryOutboxEntries"`.

**[LOW]** `JoineryOutboxCleanupJob` has the same schema-qualification gap.
- File: `SpaceOS.Modules.Joinery.Infrastructure/Outbox/JoineryOutboxCleanupJob.cs:37`
- Risk: Same as above. `DELETE FROM "JoineryOutboxEntries"` should be `DELETE FROM spaceos_joinery."JoineryOutboxEntries"`.
- Recommendation: Add schema prefix.

---

## Area 3: PDF Generation Security

**[OK]** QuestPDF renders all user-supplied strings (`ClientName`, `ProjectId`, component names) as PDF text strings — not HTML. QuestPDF has no HTML rendering path. HTML injection / script injection is not possible. Confirmed by reading all `.Text(...)` call sites in `ProductionSheetGenerator.cs`.

**[OK]** ComponentName is truncated to 100 characters (SEC-08) at `ProductionSheetGenerator.cs:143,187`. Both CuttingListLines and CncInstructions tables truncate. Covered by `ProductionSheetGeneratorTests.Generate_WithLongComponentName_TruncatesToHundredChars`.

**[OK]** PDF file path construction in `GetProductionSheetQueryHandler.cs:86-89`:
```
tenantDir = Path.Combine(basePath, order.TenantId.ToString("N"))
fileName  = $"{query.OrderId:N}_{hash}.pdf"
filePath  = Path.Combine(tenantDir, fileName)
```
Both the tenant directory segment and the file name are derived from `Guid.ToString("N")` (32 hex chars, no path separators possible) and the SHA-256 hash (64 hex chars). Path traversal is structurally impossible: neither `TenantId.ToString("N")` nor `OrderId.ToString("N")` can produce `..` or `/`.

**[OK]** `ProductionSheetCache.CK_ProductionSheetCache_FilePath` check constraint `NOT LIKE '%..%'` provides a secondary database-level guard. File path validation at the application layer and DB constraint both present.

**[OK]** `DoorOrderRevertedEventHandler` uses `entry.FilePath` sourced from the database (not user input) before calling `File.Delete`. Since `FilePath` was written by `GetProductionSheetQueryHandler` using only guid/hash components, and the DB constraint blocks `..`, there is no path traversal vector in the delete path.

**[LOW]** `GetProductionSheetQueryHandler.cs:74` logs `cache.FilePath` at `Debug` level. The path contains `TenantId` (as a directory component). `TenantId` is acceptable in logs per project policy, but logging absolute file paths at Debug may expose server directory structure in verbose logging scenarios.
- File: `SpaceOS.Modules.Joinery.Infrastructure/Handlers/GetProductionSheetQueryHandler.cs:74,111`
- Risk: LOW — no credential or PII exposure. Server path is not a secret but is unnecessary in the log message.
- Recommendation: Consider logging only `OrderId` and omit the full file path, or redact the base path prefix.

---

## Area 4: Input Validation

**[OK]** `CreateDoorOrderCommandValidator`: `FlowEpicId != Guid.Empty`, `ProjectId` not empty + max 30 chars, `ProjectName` not empty + max 200 chars. Wired in `Application.DependencyInjection.AddApplication()` via `AddValidatorsFromAssembly`.

**[OK]** `AddDoorItemCommandValidator`: `Quantity > 0`, `Sorszam` not empty + max 5 chars, `DoorType` must parse as valid enum. Dimensions are validated downstream in `DoorDimensions.Create()`.

**[OK]** `DoorDimensions.Create()`: Guards `doorWidth <= 0`, `doorHeight <= 0`, `doorWidth > 2600`, `doorHeight > 3000`, `doorWidth > wallOpeningWidth`, `doorHeight > wallOpeningHeight`. All required dimension bounds enforced at the domain layer. Covered by `DoorDimensionValidationTests`.

**[OK]** `DoorOrder.AddItem()`: MaxItems = 500 enforced at domain level (SEC-07).

**[OK]** EF Core `FromSqlRaw` usage in `JoineryOutboxWorker` contains no user-controlled input — the SQL is a static, hardcoded polling query. Not an injection risk.

**[OK]** `ExecuteSqlRawAsync` in `JoineryOutboxCleanupJob` is a static, parameterless delete. Not an injection risk.

**[OK]** `ExecuteSqlInterpolatedAsync` in `DoorRulesDataSeeder` uses C# string interpolation interpreted as parameterized SQL by EF Core — values are passed as `DbParameter` objects, not concatenated into the query string. Not an injection risk.

**[MEDIUM]** `ListDoorOrdersQuery` accepts user-supplied `page` and `pageSize` query parameters with no upper bound on `pageSize`.
- File: `SpaceOS.Modules.Joinery.Api/Endpoints/DoorOrderEndpoints.cs:114`, `SpaceOS.Modules.Joinery.Application/Orders/Queries/ListDoorOrders/ListDoorOrdersQuery.cs:7`
- Risk: A Manufacturer-authenticated user can request `pageSize=1000000`, causing EF Core to issue `TAKE 1000000` and potentially return a very large result set, exhausting memory or starving the database. This is an authenticated DoS / resource exhaustion issue.
- Recommendation: Clamp `pageSize` in the endpoint or query handler: `pageSize = Math.Clamp(pageSize, 1, 100)`. A reasonable maximum is 100–200 rows per page.

**[OK]** `SubmitDoorOrder`, `CalculateDoorOrder`, `RevertDoorOrder`, `GetProductionSheet`, `GetSnapshots` commands/queries have no validator classes. However all their ID inputs are typed as `Guid` (parsed by route binder, invalid GUIDs yield 400 at the framework level) and the handlers perform explicit `TenantId` checks. No free-form string inputs exist on these commands that would require validators.

---

## Area 5: Sensitive Data Exposure

**[OK]** `appsettings.json`: Connection string is empty (`""`). Populated from environment at runtime. No hardcoded credentials.
**[OK]** `appsettings.Development.json`: Contains only logging levels. No credentials.
**[OK]** `publish/appsettings.json`: Identical to source — empty connection string and authority. No hardcoded credentials.

**[OK]** No stack traces appear in API responses. No `Results.Problem(..., detail: ex.StackTrace, ...)` pattern found in the codebase. Exception messages are not passed through to the HTTP response layer; handlers use `Result.Error(ex.Message)` only in the outbox worker (internal, not exposed to API callers).

**[OK]** Log statements reviewed. No PII (ClientName, ClientAddress, ClientPhone) is logged. `TenantId` and `OrderId` appear in logs — both are permitted per project policy.

**[OK]** `CalculationError` on `DoorOrder` is truncated to 2000 characters. This field is an internal state, not served directly to API clients in the current implementation.

---

## Area 6: OrchestratorClient

**[OK]** Per-attempt timeout is `TimeSpan.FromSeconds(10)` via a linked `CancellationTokenSource` (`OrchestratorClient.cs:42`). Correctly applied per attempt, not as a total budget.

**[OK]** `HttpStatusCode.BadRequest` (400) and `HttpStatusCode.NotFound` (404) return immediately without retry (`OrchestratorClient.cs:52-53`). Correct non-retryable 4xx handling.

**[OK]** 5xx responses are retried with delays of 0s, 2s, 5s (3 attempts total). `OrchestratorClient.cs:17-18`. Correct exponential-like backoff.

**[OK]** Orchestrator base URL is read from `IConfiguration["Orchestrator:BaseUrl"]` with a localhost fallback. Not user-controlled. No SSRF risk: the URL is a server-configured backend address, not derived from request input.

**[OK]** All retry/timeout/error scenarios are covered by `OrchestratorClientTests` (6 tests).

---

## Area 7: OWASP Top 10

**[OK] A1 — Broken Access Control:** Cross-tenant reads return `NotFound` (not `Forbidden`). `GetDoorOrder`, `GetCuttingList`, `GetProcessPlan`, `GetHardwareList`, `GetMaterialRequirements`, `GetProductionSheet` all filter by `TenantId` at the query/handler level. Cross-tenant access returns the same response shape as a missing resource, preventing tenant enumeration. Covered by `RlsTenantIsolationTests`.

**[OK] A3 — Injection:** No raw SQL with user input. `FromSqlRaw` usage is static polling SQL with no parameters. `ExecuteSqlInterpolatedAsync` uses EF Core parameterization. All EF LINQ queries use typed predicates.

**[OK] A4 — Insecure Design:** Calculation data (CuttingList, ProcessPlan, HardwareList, MaterialRequirements, ProductionSheet) is behind `ManufacturerOnly` policy. `Cache-Control: no-store` header on cutting-list and production sheet endpoints.

**[OK] A5 — Security Misconfiguration:** No hardcoded secrets in appsettings. `RequireHttpsMetadata = false` is present in `Program.cs:28` — this is acceptable for a VPS deployment behind a TLS-terminating reverse proxy, consistent with project convention (`RequireHttpsMetadata=false` documented in prior Kernel findings as intentional).

**[OK] A7 — Identification and Authentication Failures:** JWT signature validation is active (`ValidateIssuerSigningKey = true` is implied by Authority-based OIDC configuration in production). `ValidateLifetime` defaults to `true`. `ValidateIssuer` defaults to `true` when Authority is set.

**[LOW]** `Database.MigrateAsync()` is called at startup in `Program.cs:66`.
- File: `SpaceOS.Modules.Joinery.Api/Program.cs:66`
- Risk: Per project rule I8 (inherited from Kernel CLAUDE.md), auto-migration at startup is forbidden in production. In the Joinery module's CLAUDE.md the rule is not explicitly repeated, but the Kernel-wide convention applies. If the migration fails mid-deployment (e.g., partial apply), the application starts in an inconsistent schema state.
- Recommendation: Gate the `MigrateAsync()` call on `!app.Environment.IsProduction()`, or replace with a pre-deployment migration step (e.g., EF migrations bundled in CI/CD). The `if (db.Database.IsRelational())` guard is present, which correctly skips in-memory test contexts, but does not protect against production auto-migration.

---

## Supply Chain Findings

| Package | Version | On Approved List | CVE | Notes |
|---------|---------|-----------------|-----|-------|
| QuestPDF | 2024.12.* | No | None known | Infrastructure-only; PDF generation. Not on CLAUDE.md approved list. Flag until approved. |
| PdfPig | 0.1.9 | No | None known | Test-only; PDF text extraction for assertions. No production risk. |
| coverlet.collector | 6.0.0 | No | None known | Test-only coverage tooling. Standard .NET test infrastructure. |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.11 | No | None known | Previously flagged in Kernel (project_jwt_packages_unapproved.md). Same status here. |
| Ardalis.Specification.EntityFrameworkCore | 8.0.0 | Extension of approved Ardalis.Specification | None known | Variant of approved package. Acceptable. |

**[LOW]** QuestPDF 2024.12.* uses a wildcard minor version pin.
- Risk: Wildcard pinning means a patch release (e.g., 2024.12.1 → 2024.12.9) may be pulled automatically during restore without a code review. QuestPDF processes untrusted content (user-supplied strings rendered to PDF). A supply chain compromise in any 2024.12.x patch is automatically included.
- Recommendation: Pin to an exact version (e.g., `2024.12.0`). Review and update manually on each patch.

---

## Verdict

**PASS**

No CRITICAL findings. The module demonstrates strong security posture:
- All endpoints are correctly authorization-gated.
- RLS is applied to all 7 tenant-data tables with FORCE.
- Tenant session variable is set/cleared via parameterized SQL on every connection.
- No hardcoded secrets anywhere in the codebase.
- Path traversal is structurally impossible in PDF file handling.
- No stack traces exposed in API responses.
- OrchestratorClient has correct per-attempt timeouts and 4xx/5xx differentiation.

Required developer action before production:
1. **MEDIUM** — Clamp `pageSize` in `ListDoorOrders` to prevent authenticated resource exhaustion.
2. **MEDIUM** — Evaluate enabling `ValidateAudience = true` (coordinate with Kernel JWT issuance).
3. **MEDIUM** — Add fail-fast guard for null JWT Authority in non-Development environments.
4. **LOW** — Gate `Database.MigrateAsync()` to non-production environments.
5. **LOW** — Pin QuestPDF to an exact version rather than a wildcard.
6. **LOW** — Qualify schema in `FromSqlRaw` and `ExecuteSqlRawAsync` queries in OutboxWorker and CleanupJob.
