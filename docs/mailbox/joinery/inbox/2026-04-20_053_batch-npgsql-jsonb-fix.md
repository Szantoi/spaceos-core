---
id: MSG-JOINERY-053
from: root
to: joinery
type: task
priority: high
status: READ
ref: MSG-TESTER-037-BLOCKED
created: 2026-04-20
---

# JOINERY-053 — Batch JSONB Npgsql 8 fix + Testcontainers integration teszt

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Timeline:** ~30 perc
> **Blokkoló:** TESTER-037 azonosította (Npgsql 8.0.x JSONB deserialization)

## Root Cause

Npgsql 8.0.x-től a `jsonb` oszlopból való olvasás komplex CLR típusokra (`List<Guid>`) explicit konfigurációt igényel. `UsePropertyAccessMode` megoldotta az EF hozzáférést, de a Npgsql driver a JSONB tartalmat nem tudja `List<Guid>`-vá alakítani konfiguráció nélkül.

**Miért nem fogták meg a tesztek:** `JoineryWebFactory.cs` — `UseInMemoryDatabase()` van, nem Npgsql/PostgreSQL → driver nem fut.

## Javítás 1 — HasConversion (Opció B)

**Fájl:** `Infrastructure/Persistence/Configurations/GyartasilapBatchConfiguration.cs`

```csharp
using System.Text.Json;

builder.Property<List<Guid>>("_gyartasilapIds")
    .HasColumnName("GyartasilapIds")
    .HasColumnType("jsonb")
    .IsRequired()
    .UsePropertyAccessMode(PropertyAccessMode.Field)
    .HasConversion(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null)!
    );
```

**Miért B és nem A (EnableDynamicJson):** A `HasConversion` megközelítés explicitebb, lokálisabb hatókörű, nem változtat globális Npgsql beállításokon, és tesztelhető Npgsql nélkül is.

## Javítás 2 — Testcontainers integration teszt (KÖTELEZŐ)

Ez a 3. egymást követő "tesztek zöldek, prod elromlik" hiba ugyanazon okból. **Az InMemoryDatabase integration tesztek nem fedik a valódi PostgreSQL/Npgsql viselkedést.**

**Fájl:** `Tests/Integration/GyartasilapBatchIntegrationTests.cs` (új fájl)

Legalább 1 Testcontainers teszt ami valódi PostgreSQL-en ellenőrzi a JSONB round-trip-et:

```csharp
// Testcontainers PostgreSQL-lel:
// 1. GyartasilapBatch.Create(..., new List<Guid> { Guid.NewGuid() })
// 2. Repository.Save(batch)
// 3. Repository.GetById(batch.Id)
// 4. Assert: betöltött entity.GyartasilapIds.Count == 1
```

A Testcontainers már be van vezetve a projektbe (Phase 1-ben), csak alkalmazni kell.

## Elvárások

- `dotnet build` — 0 error, 0 warning
- `dotnet test` — **≥385 pass** (384 + legalább 1 új Testcontainers integration teszt)
- A batch JSONB round-trip tesztje valódi PostgreSQL-en fut és PASS

## Definition of Done

- [ ] `HasConversion` hozzáadva `GyartasilapBatchConfiguration.cs`-ben
- [ ] Legalább 1 Testcontainers integration teszt JSONB round-trip-re
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥385 pass
- [ ] Outbox DONE üzenet küldve
