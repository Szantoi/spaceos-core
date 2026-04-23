---
id: MSG-JOINERY-052
from: root
to: joinery
type: task
priority: high
status: READ
ref: MSG-TESTER-036-BLOCKED
created: 2026-04-20
---

# JOINERY-052 — GyartasilapBatch EF Core backing field fix

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Timeline:** ~15 perc
> **Blokkoló:** TESTER-036 azonosította (8/11 pass, batch 500)

## Root Cause

`GyartasilapBatchConfiguration.cs` — `_gyartasilapIds` backing field EF Core-ban `PropertyAccessMode.Field` nélkül van konfigurálva. Enélkül EF Core nem tud a private field-en keresztül olvasni/írni → INSERT-nél null értéket persist-el, olvasásnál deszirializációs hiba → 500.

**Érintett tesztek:** A JOINERY Phase 2 DONE-ban (384/384) ez a hiba nem jelent meg mert az in-memory/Testcontainers tesztek nem fedik le ezt az EF Core mapping edge case-t JSONB-vel.

## Javítás

**Fájl:** `Infrastructure/Persistence/Configurations/GyartasilapBatchConfiguration.cs`

```csharp
// ELŐTTE (hibás):
builder.Property<List<Guid>>("_gyartasilapIds")
    .HasColumnName("GyartasilapIds")
    .HasColumnType("jsonb")
    .IsRequired();

// UTÁNA (javított):
builder.Property<List<Guid>>("_gyartasilapIds")
    .HasColumnName("GyartasilapIds")
    .HasColumnType("jsonb")
    .IsRequired()
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

## Elvárások

- `dotnet build` — 0 error, 0 warning
- `dotnet test` — 384/384 pass (változatlan, a meglévő tesztek nem törnek)
- **Opcionális (erősen ajánlott):** Adj hozzá 1 Testcontainers integrációs tesztet ami valódi PostgreSQL-en ellenőrzi a `_gyartasilapIds` JSONB round-trip-et (INSERT + SELECT), hogy ez a hiba ne tudjon visszajönni.

## Deploy

A konfig változtatás nem igényel migration-t (csak EF Core mapping, nem DB séma változás).

Commit + outbox DONE után INFRA redeploy következik.

## Definition of Done

- [ ] `UsePropertyAccessMode(PropertyAccessMode.Field)` hozzáadva
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥384 pass
- [ ] Outbox DONE üzenet küldve
