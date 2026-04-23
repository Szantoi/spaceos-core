# Production Readiness — Track B + Track C + Track A.4 — DONE

**Date:** 2026-04-08
**From:** SpaceOS.Kernel agent
**Re:** MSG-K031 Production Readiness Sprint — Kernel scope complete

---

## Build Result

```
dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Test Result

```
SpaceOS.Kernel.Tests            — Passed: 758 / 758  (0 fail)
SpaceOS.Kernel.IntegrationTests — Passed: 101 / 101  (0 fail)
SpaceOS.Kernel.Api.Tests        — Passed:  68 /  68  (0 fail)
─────────────────────────────────────────────────────────────
TOTAL                             Passed: 927 / 927  (0 fail)
```

DoD gate: ≥925 pass ✅

---

## Track B — Audit Race Fix

| Item | Status | File |
|---|---|---|
| B1 — `BoundedChannelFullMode.Wait` + `LogCritical` | ✅ Done | `SpaceOS.Infrastructure/AuditLog/ChanneledAuditEventDispatcher.cs` |
| B2 — MD5 int64 key (`pg_advisory_xact_lock(bigint)`) | ✅ Done | `SpaceOS.Infrastructure/AuditLog/PostgresAdvisoryAuditWriteLock.cs` |
| B3 — `DisposeAsync()` graceful drain 30s | ✅ Done | `SpaceOS.Infrastructure/AuditLog/ChanneledAuditEventDispatcher.cs` |

**Track B tesztek (mind meglévő):**
- `Channel_WhenFull_DoesNotDropWrite` ✅
- `DisposeAsync_DrainCompletes_Within30s` ✅
- `LockKey_DifferentTenants_DifferentInt64Keys` ✅
- `LockKey_SameTenant_SameInt64Key` ✅
- `LockKey_IsFullInt64Range_NotRestrictedToInt32` ✅

Megjegyzés: B1 — ha a `WriteAsync` nem tud írni 5s-en belül, a dispatcher `LogCritical`-t ír és kivételt dob (`OperationCanceledException`). Channel capacity=1 teszttel verifikálva, hogy `DropWrite` nem történik.

---

## Track C — PostgreSQL WORM Sink

| Item | Status | File |
|---|---|---|
| C1 — `AuditHashes` tábla + RLS + Migration 0027 | ✅ Done | `SpaceOS.Infrastructure/Migrations/20260408120000_Migration_0027_AuditHashesWorm.cs` |
| C2 — `spaceos_audit_worm` role (VPS SQL) | ✅ Done | Migration 0027 `suppressTransaction: true` |
| C3 — `PostgresWormStorageService` | ✅ Done | `SpaceOS.Infrastructure/AuditLog/PostgresWormStorageService.cs` |
| DI regisztráció | ✅ Done | `SpaceOS.Infrastructure/DependencyInjection.cs` |

**Track C tesztek (mind meglévő):**
- `Constructor_WhenConnectionStringMissing_Throws` ✅
- `Constructor_WhenConnectionStringPresent_DoesNotThrow` ✅
- `PostgresWormStorageService_Implements_IWormStorageService` ✅
- `AppendAsync_WhenHashIsEmpty_ThrowsArgumentException` ✅
- `Migration0027_ClassExists_WithCorrectName` ✅
- `Migration0027_UpSql_ContainsRlsPolicy` ✅
- `Migration0027_UpSql_ContainsSuppressTransaction` ✅

`AUDIT_SINK_CONNECTION_STRING` env varból olvas, nem `appsettings.json`-ból (SEC-07 compliant) ✅

---

## Track A.4 — JWT Authority + Audience config-driven

| Item | Status | File |
|---|---|---|
| `JWT_AUTHORITY` env var | ✅ Done | `SpaceOS.Kernel.Api/ConfigureJwtBearerOptions.cs` |
| `JWT_AUDIENCE` env var | ✅ Done | `SpaceOS.Kernel.Api/ConfigureJwtBearerOptions.cs` |
| Dev fallback (`https://spaceos-kernel`) | ✅ Done | `ConfigureJwtBearerOptions.cs` line 44-49 |

`appsettings.json` nem tartalmaz hardcoded Authority/Audience értékeket (BE-01 compliant) ✅

---

## DoD Checklist

- [x] `Channel<T>` overflow → `BoundedChannelFullMode.Wait` → `LogCritical` tesztelhető
- [x] `pg_advisory_xact_lock(bigint)` MD5 key — nem `hashtext()`
- [x] `DisposeAsync()` graceful drain 30s timeout
- [x] `AuditHashes` tábla + RLS + Migration 0027 (`suppressTransaction: true`)
- [x] `PostgresWormStorageService` implementálva
- [x] `AUDIT_SINK_CONNECTION_STRING` env varból (nem appsettings)
- [x] JWT `Authority` + `Audience` config-driven
