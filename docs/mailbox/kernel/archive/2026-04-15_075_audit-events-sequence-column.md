---
id: MSG-KERNEL-075
from: root
to: kernel
type: task
priority: high
status: READ
ref: MSG-KERNEL-074-DONE
created: 2026-04-15
---

# MSG-KERNEL-075 — AuditEvents sequence column (OccurredAt tiebreaker)

## Háttér

A KERNEL-074-DONE (R-15) vizsgálat azonosította: `GetChainAsync` client-side OccurredAt
szerint rendez. Azonos clock tick-en belüli eventek nem-determinisztikus sorrendben
kerülnek vissza → chain verification false positive high-concurrency alatt.

**Jelenlegi mitigáció:** az AuditChainIntegrityTest 1ms delay-t alkalmaz — ez nem
elegendő Doorstar production terhelés esetén (bulk import, B2B handshake burst).

## Feladat

### 1. Migration 0030 — sequence column

```sql
ALTER TABLE "AuditEvents"
  ADD COLUMN "sequence" BIGINT GENERATED ALWAYS AS IDENTITY;
```

EF Core Migration generálása tooling-gal:
```bash
dotnet ef migrations add AddAuditEventSequence \
  --project SpaceOS.Infrastructure \
  --startup-project SpaceOS.Kernel.Api
```

### 2. GetChainAsync rendezés frissítése

```csharp
// Előtte:
.OrderBy(e => e.OccurredAt)

// Utána:
.OrderBy(e => e.OccurredAt).ThenBy(e => e.Sequence)
```

### 3. AuditEvent entitás + DbContext

- `AuditEvent.cs`: `public long Sequence { get; private set; }` property (DB-generated, ne legyen setter az alkalmazásban)
- `AppDbContext`: EF config → `ValueGeneratedOnAdd()`, nem kell explicit set

### 4. Tesztek

- `AuditChain_NEvents_ChainIsConsistent` → 1ms delay eltávolítható (de maradhat, nem baj)
- Új teszt (opcionális, de ajánlott): `AuditChain_SameClockTick_SequencePreservesOrder` — 2 event azonos OccurredAt-tel, sequence alapján helyes sorrend

## Elfogadási kritériumok (DoD)

- [ ] Migration 0030 generálva (`dotnet ef migrations add`)
- [ ] `AuditEvent.Sequence` property EF-konfigurált (`ValueGeneratedOnAdd`)
- [ ] `GetChainAsync` OccurredAt + Sequence tiebreaker
- [ ] Összes teszt zöld (1110+ pass)
- [ ] Nincs breaking change a chain integritás logikájában

## Megjegyzés

A Migration 0030 production-safe: az új `sequence` column IDENTITY, minden existing row
automatikusan kap értéket INSERT sorrendben. Az `__EFMigrationsHistory` entry normálisan kerül be.
