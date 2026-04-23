---
id: MSG-JOINERY-053-DONE
from: joinery
to: root
type: done
priority: high
status: READ
ref: MSG-JOINERY-053
created: 2026-04-20
---

## Összefoglaló

JOINERY-053 — Batch JSONB Npgsql 8 fix + Testcontainers integration teszt teljes.

**Commit:** `680ca91`

**Módosított/létrehozott fájlok:**

- `Infrastructure/Persistence/Configurations/GyartasilapBatchConfiguration.cs`
  - `HasConversion(JsonSerializer.Serialize, JsonSerializer.Deserialize<List<Guid>>)` hozzáadva
  - `UsePropertyAccessMode(PropertyAccessMode.Field)` megmarad (együtt szükséges)

- `Tests/Integration/GyartasilapBatchIntegrationTests.cs` (új fájl)
  - 3 Testcontainers teszt valódi PostgreSQL 16-on:
    1. `GyartasilapBatch_JsonbRoundTrip_SavesAndLoadsIds` — 2 elem round-trip
    2. `GyartasilapBatch_JsonbRoundTrip_SingleId_SavesAndLoads` — 1 elem round-trip
    3. `GyartasilapBatch_StatusTransition_PersistedCorrectly` — státuszváltás + JSONB együtt

- `Tests/SpaceOS.Modules.Joinery.Tests.csproj`
  - `Testcontainers.PostgreSql 3.10.0` package hozzáadva

## Tesztek

**387/387 pass** (384 → +3 új Testcontainers integration teszt)

A 3 új teszt valódi Docker PostgreSQL 16-on fut — lefedi az Npgsql JSONB deserialization útvonalat, amit az InMemoryDatabase nem fedett.

Utolsó sor: `Passed! - Failed: 0, Passed: 387, Skipped: 0, Total: 387, Duration: 19 s`

## Security review

- ✅ Nem érint auth/RLS logikát
- ✅ Testcontainers teszt izolált (minden tesztnél új container, `IAsyncLifetime`)
- ✅ Nincs TODO/FIXME

## Kockázatok / kérdések

Nincsenek. INFRA redeploy elvégezhető — ez volt az utolsó blocker a batch endpoint éles működéséhez.
