# Review Report — MSG-K022 Sprint D Phase 2

**Date:** 2026-04-07
**Agent:** kernel-review-enforcer
**Final status:** CLOSED_DONE

---

## Scope

Tasks reviewed: T-07 (Redis RL hardening), T-05 (ExternalAuthToken KV), T-01 (Query Endpoints), T-06 (IntentDataJson), T-08 (Threat Model).

Layers affected: Application, Infrastructure, API, Tests.

---

## Violations Found & Fixed

| # | Rule | File | Violation | Fix Applied |
|---|------|------|-----------|-------------|
| 1 | P2 | `SpaceOS.Kernel.Api/Endpoints/ToolEndpoints.cs:34` | `Results.Unauthorized()` used — plain 401 with no ProblemDetails body | Replaced with `Results.Problem(title: "Unauthorized", statusCode: 401, type: "https://tools.ietf.org/html/rfc7235#section-3.1")` |
| 2 | P2 | `SpaceOS.Kernel.Api/Endpoints/ToolEndpoints.cs:56` | Same as #1 — `/workstations` endpoint | Same fix |
| 3 | P2 | `SpaceOS.Kernel.Api/Endpoints/ToolEndpoints.cs:78` | Same as #1 — `/facilities` endpoint | Same fix |
| 4 | P2 | `SpaceOS.Kernel.Api/Endpoints/ToolEndpoints.cs:98` | Same as #1 — `/summary` endpoint | Same fix |
| 5 | A10 | `SpaceOS.Kernel.Application/Tools/Queries/` — `ListFlowEpicsQuery` | No companion validator (every query requires one per Application CLAUDE.md) | Created `ListFlowEpicsQueryValidator.cs` — validates TenantId not-empty, Page >= 1, PageSize 1–50 |
| 6 | A10 | `SpaceOS.Kernel.Application/Tools/Queries/` — `ListWorkStationsQuery` | No companion validator | Created `ListWorkStationsQueryValidator.cs` |
| 7 | A10 | `SpaceOS.Kernel.Application/Tools/Queries/` — `ListFacilitiesQuery` | No companion validator | Created `ListFacilitiesQueryValidator.cs` |
| 8 | A10 | `SpaceOS.Kernel.Application/Tools/Queries/` — `GetTenantSummaryQuery` | No companion validator | Created `GetTenantSummaryQueryValidator.cs` — validates TenantId not-empty |

---

## Unfixable Violations (requires developer decision)

None.

---

## Rule-by-Rule Audit Summary

### Domain (D1–D11) — SpaceOS.Kernel.Domain/
| Rule | Status | Notes |
|------|--------|-------|
| D1 — no public setters | PASS | No public setters in Domain source files (CLAUDE.md examples excluded) |
| D4 — mutations raise domain events | PASS | No new aggregates introduced this sprint |
| D7 — no with-expression bypass | PASS | No `with {}` in Domain |
| D11 — no external NuGet in Domain | PASS | Domain csproj unchanged |

### Application (A1–A12) — SpaceOS.Kernel.Application/
| Rule | Status | Notes |
|------|--------|-------|
| A2 — ConfigureAwait(false) | PASS | All awaits in new handlers use `.ConfigureAwait(false)` (multiline verified) |
| A3 — CancellationToken named `ct` | PASS | All 4 handlers use `CancellationToken ct` |
| A5 — PopDomainEvents + DispatchAsync | N/A | Query handlers only — no domain mutations |
| A9 — handler has companion test | PASS | All 4 handlers have companion tests in SpaceOS.Kernel.Tests/Application/ |
| A10 — handler has companion validator | FIXED (#5–#8) | 4 validators created |
| A-rules visibility | PASS | All 4 handlers are `internal sealed` |

### Infrastructure (I1–I9) — SpaceOS.Infrastructure/
| Rule | Status | Notes |
|------|--------|-------|
| I1 — AsNoTracking on reads | PASS | AuditEventRepository: ListAsync, CountAsync, GetLastHashAsync, GetChainAsync all use AsNoTracking |
| I3 — ToListAsync via WithSpecification | PASS | GetLastHashAsync and GetChainAsync use explicit projections; count uses WithSpecification |
| I8 — no auto-migrate at startup | PASS | Only `EnsureCreatedAsync` in Development, `Database.Migrate()` absent from all production paths |
| I-rules visibility | PASS | AuditEventRepository is `internal sealed` |

### API (P1–P8) — SpaceOS.Kernel.Api/
| Rule | Status | Notes |
|------|--------|-------|
| P1 — no ControllerBase | PASS | No controllers; all Minimal API |
| P2 — ProblemDetails error responses | FIXED (#1–#4) | 4 occurrences of `Results.Unauthorized()` replaced |
| P4 — IResult return type | PASS | All endpoints return `IResult` via `ToApiResult()` or `Results.*` |
| P5 — no business logic in endpoints | PASS | ToolEndpoints only calls `sender.Send(...)` after tenant claim extraction |
| P8 — authorization required | PASS | All ToolEndpoints group uses `.RequireAuthorization()` |
| RequestBodySizeLimitFilter | PASS | `internal sealed`, returns `Results.Problem(statusCode: 413)` |

### General (G1–G6) — all layers
| Rule | Status | Notes |
|------|--------|-------|
| G1 — no TODO/FIXME | PASS | Zero hits across all 4 layers |
| G4 — approved NuGet only | INFO | `StackExchange.Redis 2.8.*` and `Microsoft.Extensions.Caching.StackExchangeRedis 8.*` added by T-07. Not on the pre-sprint approved list. These are intentional sprint additions for Redis RL hardening — flagged for developer acknowledgement to update CLAUDE.md approved list. |
| G5 — no pragma warning disable | PASS | `#pragma warning disable 612, 618` present only in EF Core tooling-generated Designer.cs and Snapshot files (exempt) |
| XML docs | PASS | All new public types and methods have `<summary>` docs |
| Private fields `_camelCase` | PASS | All new private fields use `_camelCase` |
| xUnit1051 | PASS (sprint scope) | New test files (AuthEndpointTests, TenantClaimSecurityTests, SpaceLayerEndpointTests) all use `TestContext.Current.CancellationToken`. Pre-existing violations in SipVersionMiddlewareTests.cs are out of scope. |

---

## Build & Test Result

- Build: **0 errors, 14 warnings** (all 14 warnings are pre-existing baseline — xUnit1051 in SipVersionMiddlewareTests.cs and CS8764/CS8765/CS8602 in TenantSessionInterceptorTests.cs; none introduced by this review)
- Tests: **777 passing, 0 failed** (608 unit + 101 integration + 68 API)
