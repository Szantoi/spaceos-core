# Phase 3C+ — Migration 0025 + 0026 + B2BHandshake — DONE

**Date:** 2026-04-08  
**From:** SpaceOS.Kernel agent  
**Re:** Phase 3C+ scope completion — Migrations 0025/0026, JWT claims, HandshakeEndpoints, test fixes

---

## Build Result

```
dotnet build SpaceOS.Kerner.sln
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Test Result

```
SpaceOS.Kernel.Tests          — Passed: 746 / 746  (0 fail)
SpaceOS.Kernel.IntegrationTests — Passed: 101 / 101  (0 fail)
SpaceOS.Kernel.Api.Tests      — Passed:  68 /  68  (0 fail)
─────────────────────────────────────────────────────────────
TOTAL                           Passed: 915 / 915  (0 fail)
```

---

## What Was Fixed

### Root Cause: `MigrateAsync` on SQLite in Testing Environment

The recent Phase 3C+ commit added `await db.Database.MigrateAsync()` to the non-Development startup path in `Program.cs`. This caused all API and Integration tests to fail because:

1. Both test factories (`ApiFactory`, `SpaceOsApiFactory`) set `UseEnvironment("Testing")`.
2. The `Testing` environment falls into the `else` branch (non-Development), which now called `MigrateAsync`.
3. Migration `SprintC_SchemaUpdate` uses `migrationBuilder.RenameColumn(name: "ExternalAuthToken" → "ExternalAuthTokenRef")` — this is not supported by SQLite's ALTER TABLE.
4. Result: `SQLite Error 1: 'no such column: "ExternalAuthToken"'` on every test that starts the app host.

**Fix applied to `SpaceOS.Kernel.Api/Program.cs`:**

- Added a new `else if (app.Environment.IsEnvironment("Testing"))` branch.
- In this branch, `EnsureCreatedAsync()` is called (not `MigrateAsync`) — SQLite test DB schema is built fresh from the live EF model.
- The call is wrapped in a `try/catch` to tolerate deliberately broken DB connections used by the `HealthEndpoint_WhenDbUnavailable_StillReturns200` test.
- `ModulesDbContext.EnsureCreatedAsync()` is intentionally skipped in `Testing` — it would attempt a Npgsql connection, and the `ModulesDbContext` is not used in API tests.

### Secondary Fixes: Test IntentDataJson Payloads

Four tests were sending IntentDataJson that failed the `IntentDataSchemaValidator` per-trade structural checks:

| Test | TradeType | Old JSON | Problem | Fixed JSON |
|---|---|---|---|---|
| `Api.Tests` `RegisterSpaceLayer_ValidRequest_Returns201` | Architecture | `{"walls":[]}` | Missing `floorPlan` | `{"floorPlan":"A-101"}` |
| `IntegrationTests` `RegisterSpaceLayer_LocalLayer_Returns201WithLocationHeader` | Architecture | `{"floor":1}` | Missing `floorPlan` (had number, not string) | `{"floorPlan":"A-101"}` |
| `IntegrationTests` `RegisterSpaceLayer_LocalLayer_ReturnsNonEmptyId` | Electrical | `{}` | Missing `voltage` + `circuitCount` | `{"voltage":230,"circuitCount":8}` |
| `IntegrationTests` `RegisterSpaceLayer_ValidRequest_DispatchesSpaceLayerRegisteredEvent` | Joinery | `{"zone":"A"}` | Missing `material` + `dimensions` | `{"material":"oak","dimensions":{"width":90,"height":210}}` |

These tests were passing before the Phase 3C+ changes only because the DB schema error caused them to fail early (before reaching validation). Once the schema issue was resolved, the latent validation failures became visible.

---

## Already Implemented (Phase 3C+ scope)

| Item | Status | Details |
|---|---|---|
| Migration 0025 — `Tenants.EnabledModules` | ✅ Done | `varchar(32)[]` on PostgreSQL, JSON ValueConverter for SQLite via `TenantConfiguration` |
| Migration 0026 — `TenantHandshakeAllowlist` | ✅ Done | Composite PK, FK to Tenants, CHECK constraints, RLS policy |
| `TenantHandshakeAllowlistConfiguration` | ✅ Done | `_allowedTradeTypes` shadow property with JSON converter for SQLite |
| `TenantHandshakeAllowlist` domain entity | ✅ Done | `Create()` factory, invariant guards (no self-link, ≥1 trade type, valid values) |
| `ITenantHandshakeAllowlistRepository` | ✅ Done | Interface in Domain |
| `TenantHandshakeAllowlistRepository` | ✅ Done | EF Core implementation in Infrastructure |
| JWT claim `enabled_modules` | ✅ Done | Serialized as JSON array in `JwtAccessTokenIssuer` |
| JWT claim `allowed_hosts` | ✅ Done | Serialized as JSON array, max 20 entries (SEC-P3CP-08) |
| `HandshakeEndpoints.cs` | ✅ Done | `GET /api/handshakes/allowed-hosts` — requires ReadPolicy, rate-limited |
| `GetAllowedHostsQuery` + handler | ✅ Done | Returns `IReadOnlyList<AllowedHostDto>` |
| `AllowedHostDto` | ✅ Done | DTO in Domain |
| `AppDbContext.OnModelCreating` Npgsql override | ✅ Done | Removes JSON converter, sets `varchar(32)[]` column type for both `Tenants._enabledModules` and `TenantHandshakeAllowlist._allowedTradeTypes` |

---

## B2BHandshake Endpoint Status

`GET /api/handshakes/allowed-hosts` is registered at line 349 of `Program.cs` via `app.MapHandshakeEndpoints()`.

The endpoint returns the list of host tenants for which the currently authenticated tenant appears as a guest in `TenantHandshakeAllowlist`, limited to 20 entries (SEC-P3CP-08).
