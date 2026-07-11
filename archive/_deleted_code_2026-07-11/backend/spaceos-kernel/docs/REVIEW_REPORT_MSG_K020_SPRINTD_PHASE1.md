# Review Report — MSG-K020 Sprint D Phase 1

**Date:** 2026-04-06
**Agent:** kernel-review-enforcer
**Final status:** REVIEW_FAILED

---

## Scope

| Task | Files Reviewed |
|------|---------------|
| T-08 (loopback binding) | `SpaceOS.Kernel.Api/appsettings.Production.json` |
| T-07 (SourceBrand feature) | `AuditEvent.cs`, `ICurrentRequestContext.cs`, `AuditEventDispatcher.cs`, `AuditEventDto.cs`, `GetAuditEventsQueryHandler.cs`, `HttpContextCurrentRequestContext.cs`, `AuditEventConfiguration.cs`, `20260406090000_AddSourceBrandToAuditEvents.cs`, `AppDbContextModelSnapshot.cs` |
| T-07 Tests | `AuditEventDispatcherTests.cs`, `HttpContextCurrentRequestContextTests.cs` |
| T-03 | `spaceos-kernel/spaceos-kernel.service` |
| T-04 | `.github/workflows/ci.yml` |

---

## External Documentation Consulted

| Topic | Source | Finding |
|-------|--------|---------|
| EF Core `suppressTransaction` | context7 `/dotnet/entityframework.docs` | `suppressTransaction: true` on `migrationBuilder.Sql()` is the correct pattern for `CREATE INDEX CONCURRENTLY` — PostgreSQL cannot execute this inside a transaction. The migration is correct. |
| ASP.NET Core `Urls` binding | context7 `/dotnet/aspnetcore.docs` | `"Urls": "http://127.0.0.1:5000"` in `appsettings.Production.json` is the documented loopback binding mechanism for Kestrel. Correct. |
| .NET 8 systemd `Type=notify` | (validated against known systemd SD_NOTIFY spec) | `Type=notify` requires the .NET host to call `sd_notify(READY=1)` — the .NET 8 generic host does this automatically via `Microsoft.Extensions.Hosting.Systemd`. Correct usage. |

---

## Violations Found & Fixed

| # | Rule | File | Violation | Fix Applied |
|---|------|------|-----------|-------------|
| 1 | G6 — No pragma suppression | `SpaceOS.Kernel.Application/AuditLog/AuditEventDispatcher.cs` | `#pragma warning disable CS4014` / `#pragma warning restore CS4014` wrapping the fire-and-forget sink call. Pragma suppression is forbidden by CLAUDE.md. | Replaced with `_ = _sink.WriteAsync(...).ContinueWith(...)` — task discarded via discard variable, no pragma needed. |

---

## Unfixable Violations (requires developer decision)

| # | Rule | File | Issue | Why Unfixable |
|---|------|------|-------|---------------|
| 1 | I6 — Infrastructure visibility | `SpaceOS.Infrastructure/Data/Repositories/AuditEventRepository.cs` | Class is `public sealed class` instead of `internal sealed class`. CLAUDE.md (memory: `feedback_infra_config_visibility.md`) requires all repository implementations to be `internal sealed`. | Integration test `AuditEventRepositoryTests.cs` directly instantiates the concrete class rather than using its interface, so making it `internal` produces CS0122 build errors. Fix requires either adding `InternalsVisibleTo` for the integration test project, or refactoring the integration test to inject via the interface. Architectural decision required. |
| 2 | I7 — Snapshot provider mismatch | `SpaceOS.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` | Snapshot was generated with the SQLite provider — all column types are `TEXT` / `INTEGER` instead of PostgreSQL-native `uuid`, `character varying`, `timestamptz`, `bigint`. The `SourceBrand` column type is recorded as `TEXT` instead of `character varying(50)`. | Regeneration requires running `dotnet ef migrations add` against the PostgreSQL Npgsql provider. Cannot be fixed in place — requires the full EF CLI toolchain pointing at a live PostgreSQL instance. Developer must run: `dotnet ef migrations add RegenerateSnapshot --context AppDbContext` (after removing this migration) or switch `AppDbContext` to use Npgsql by default in the dev tooling profile. |
| 3 | Logic bug (out of rule scope, flagged for awareness) | `SpaceOS.Kernel.Application/AuditLog/AuditEventDispatcher.cs` line 159 | `FireAndForgetSink` is called with `previousHash` as the 4th argument, but `previousHash` has already been advanced to `stateHash` at line 154. The sink receives `(stateHash, stateHash)` — both hashes identical — when the intent is `(stateHash, priorPreviousHash)`. | Requires understanding the external sink contract to determine correct argument order. Architectural/semantic decision required before applying fix. |

---

## Per-Rule Audit Results

### Domain (D1–D11)

| Rule | Check | Result |
|------|-------|--------|
| D1 — No public setters | `AuditEvent.cs` — all properties `{ get; private set; }` | PASS |
| D4 — Mutations raise domain event | `AuditEvent.Create()` calls `AddDomainEvent(new AuditEventCreatedEvent(...))` | PASS |
| D7 — No `with` expression bypass | No `with {` patterns in Domain files | PASS |
| D11 — Zero external NuGet in Domain | `SpaceOS.Kernel.Domain.csproj` contains only `MediatR.Contracts` + `Ardalis.Specification` — both approved | PASS |
| D2 — Private constructor + static factory | `AuditEvent` has `private AuditEvent()` (EF ctor) + `private AuditEvent(...)` + `public static Create(...)` | PASS |
| D8 — XML docs on public types | All public properties and the `Create` factory method have `<summary>` docs | PASS |

### Application (A1–A12)

| Rule | Check | Result |
|------|-------|--------|
| A2 — `ConfigureAwait(false)` | All `await` calls in `AuditEventDispatcher.cs` and `GetAuditEventsQueryHandler.cs` use `.ConfigureAwait(false)` | PASS |
| A3 — `CancellationToken ct` naming | `GetAuditEventsQueryHandler.Handle(GetAuditEventsQuery request, CancellationToken ct)` — correct | PASS |
| A5 — `PopDomainEvents` + `DispatchAsync` | Not applicable to `AuditEventDispatcher` (it IS the dispatcher). `GetAuditEventsQueryHandler` is read-only — no mutation. | N/A |
| A9 — Handler has companion test | `AuditEventDispatcherTests.cs` exists with 11 tests (6 pre-existing + 5 new SourceBrand tests) | PASS |
| A10 — XML docs | `AuditEventDto` record has inline XML docs on all parameters; `GetAuditEventsQueryHandler` has class + method docs | PASS |
| A11 — No EF Core in Application | No EF Core imports in Application files | PASS |
| A12 — No HTTP in Application | `ICurrentRequestContext` abstraction used — no HTTP dependency | PASS |

### Infrastructure (I1–I9)

| Rule | Check | Result |
|------|-------|--------|
| I1 — `AsNoTracking` on reads | All read methods in `AuditEventRepository` (`ListAsync`, `CountAsync`, `GetLastHashAsync`, `GetChainAsync`) use `.AsNoTracking()` | PASS |
| I3 — `WithSpecification` on list queries | `ListAsync` and `CountAsync` use `.WithSpecification(specification)` | PASS |
| I5 — `builder.Ignore(DomainEvents)` | Per project memory `project_i5_domain_events_design.md`: `AggregateRoot` uses a private field, no `DomainEvents` property — I5 is PASS-BY-DESIGN | PASS |
| I6 — `internal sealed` on configurations | `AuditEventConfiguration` is correctly `internal sealed class` | PASS |
| I6 — Repository visibility | `AuditEventRepository` is `public sealed class` — should be `internal sealed` | UNFIXABLE (see above) |
| I7 — Snapshot provider | Snapshot column types are SQLite (`TEXT`/`INTEGER`) not PostgreSQL | UNFIXABLE (see above) |
| I8 — No auto-migrate at startup | `Database.Migrate()` absent; `EnsureCreatedAsync()` is dev-only; prod path calls `PostgresSchemaInitializer` only | PASS |
| I9 — Migration correctness | `suppressTransaction: true` on `CREATE INDEX CONCURRENTLY` is correct per EF Core docs; partial index `WHERE "SourceBrand" IS NOT NULL` is valid PostgreSQL syntax | PASS |

### API (P1–P8)

| Rule | Check | Result |
|------|-------|--------|
| P1 — No `ControllerBase` | No controllers found in `SpaceOS.Kernel.Api` | PASS |
| P2 — ProblemDetails responses | `AuditEventEndpoints.cs` uses `Results.ValidationProblem(...)` for validation errors; `result.ToApiResult()` extension for handler results | PASS |
| P4 — `IResult` return type | All endpoint lambdas return `IResult` via `result.ToApiResult()` or `Results.ValidationProblem(...)` | PASS |
| P5 — No business logic in endpoints | Endpoints contain only `mediator.Send(...)` + minimal input parsing (enum parse, null check for required param) | PASS |

### General (G1–G6)

| Rule | Check | Result |
|------|-------|--------|
| G1 — No TODO/FIXME | No `TODO` or `FIXME` in any scoped `.cs` file | PASS |
| G2 — PascalCase public types | All new types follow PascalCase | PASS |
| G3 — `_camelCase` private fields | `_httpContextAccessor`, `_repository`, etc. — all correct | PASS |
| G4 — Approved NuGet only | No unapproved packages introduced | PASS |
| G5 — File name = class name | All files 1:1 with class names | PASS |
| G6 — No pragma suppression | `#pragma warning disable CS4014` introduced in `AuditEventDispatcher.cs` | **FIXED** (violation #1) |

---

## T-08: appsettings.Production.json Review

```json
{ "Urls": "http://127.0.0.1:5000" }
```

Correct loopback binding. The `Urls` key is the documented ASP.NET Core configuration key for Kestrel endpoint binding. `127.0.0.1` (IPv4 loopback) is the correct value for a VPS deployment behind a reverse proxy (nginx/caddy handles TLS termination). No wildcard binding (`0.0.0.0` or `*`) is present — this is correct security posture.

**Observation:** IPv6 loopback (`[::1]:5000`) is not bound. If the production host has IPv6 enabled and the reverse proxy uses IPv6 to communicate with the backend, the proxy will fail to connect. Low risk for a single-VPS deployment but worth documenting.

---

## T-03: systemd Service File Review

```
Type=notify
```

Correct — .NET 8 generic host emits `sd_notify READY=1` automatically. The hardening flags (`NoNewPrivileges`, `ProtectSystem=strict`, `CapabilityBoundingSet=`, `SystemCallFilter=@system-service`, `RestrictNamespaces`) are production-grade and comply with Security Rule #11.

`ReadWritePaths=/opt/spaceos/spaceos-kernel/publish /var/log/spaceos` — correct pair for the working directory and log output path.

---

## T-04: CI Workflow Review

Actions are pinned to full commit SHAs — this satisfies the security scanner's action-pinning requirement.

**Observation (not a CLAUDE.md violation):** The `deploy` job re-runs `dotnet publish` on the VPS server after a separate `dotnet publish` on the CI runner. The runner-built artifact is discarded. This means the production binary was not built from the artifact that passed tests. This issue was flagged by the security scanner in `project_cicd_double_build.md` and remains unresolved.

---

## Build & Test Result

- Build: green — 0 errors, 9 pre-existing warnings (all in `SipVersionMiddlewareTests.cs`, out of scope)
- Tests: 541 passing, 0 failed (SpaceOS.Kernel.Tests)

---

## Summary

| Category | Fixed | Unfixable | Pass |
|----------|-------|-----------|------|
| Domain (D) | 0 | 0 | 6 |
| Application (A) | 1 | 0 | 7 |
| Infrastructure (I) | 0 | 2 | 7 |
| API (P) | 0 | 0 | 4 |
| General (G) | 1 | 0 | 5 |
| **Total** | **1** | **2** | **29** |

**Status is REVIEW_FAILED** due to 2 unfixable violations (I6 repository visibility blocked by integration test direct instantiation; I7 snapshot provider mismatch). Both require developer action before the sprint can be closed.
