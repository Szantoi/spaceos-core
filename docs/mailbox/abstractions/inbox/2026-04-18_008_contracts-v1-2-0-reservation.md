---
id: MSG-ABSTRACTIONS-008
from: root
to: abstractions
type: task
priority: high
status: READ
created: 2026-04-18
docs:
  - docs/tasks/new/SpaceOS_Modules_Contracts_Architecture_v4_1_Amendment.md
---

# Contracts v1.2.0 — Reservation API (Track A)

## Tervdok

`docs/tasks/new/SpaceOS_Modules_Contracts_Architecture_v4_1_Amendment.md` — Section 3 (solution struktúra), Section 5 (API surface), Section 11 (DoD)

## Feladat: Track A — spaceos-modules-contracts

Implementáld a Contracts NuGet package 1.1.0 → **1.2.0** verzióbumpot az alábbi additive változásokkal:

### Új fájlok

**Inventory/Requests/**
- `ReserveStockRequest.cs` + `ReserveItemRequest.cs`

**Inventory/DTOs/**
- `ReservationDto.cs` · `ReservationItemDto.cs` · `ReservationFilter.cs`

**Inventory/Events/**
- `StockReserved.cs` · `ReservationReleased.cs` · `ReservationExpired.cs` · `ReservationConsumed.cs`
- Minden event-en SEC-03 XML doc: `Consumers MUST verify Event.TenantId matches their JWT TenantId`

**Inventory/Enums/**
- `ReservationStatus.cs` (Active=0, Released=1, Expired=2, Consumed=3)

**Inventory/Validation/**
- `ConsumerContextJsonSchema.cs` (SEC-07 — schema + XSS + PII pattern regex)

### Módosított fájlok

**`Shared/ProviderCapability.cs`**
- `InventoryReservation = 1 << 6` flag hozzáadása

**`Inventory/IInventoryProvider.cs`**
- 3 új metódus: `ReserveAsync`, `ReleaseReservationAsync`, `GetReservationsAsync`
- XML doc a tervdok Section 5.1 szerint (rate limit, idempotency, SEC policy notes)

**`SpaceOS.Modules.Contracts.csproj`**
- `<Version>1.2.0</Version>`

**`CHANGELOG.md`**
- v1.2.0 release notes

**`README.md`**
- Reservation usage + ConsumerContextJson policy

### Tesztek

`SpaceOS.Modules.Contracts.Tests/Inventory/ReservationContractTests.cs`
- ≥15 teszt: DTO invariánsok, enum értékek, ConsumerContextJsonSchema validáció, SEC-03 pattern

## DoD gate-ek (Section 11 — Contracts package DoD)

- [ ] NuGet version 1.2.0
- [ ] SemVer: MINOR bump (additive only, visszafele kompatibilis)
- [ ] ≥15 új teszt zöld
- [ ] Meglévő tesztek mind zöldek
- [ ] XML doc minden public API-n
- [ ] CHANGELOG.md + README.md frissítve
- [ ] `dotnet pack` sikeres

## Polly jóváhagyás

A `Polly` NuGet package a tervdokumentumban (Section 16 sign-off) jóváhagyott. **Track A-ban nem kell Polly** — ez INVENTORY Track C feladata. Nincs tennivalód vele.

## Sorrend és blokkoló

**Track A blokkol minden mást.** Az INVENTORY terminál (Track B+C) várakozik a kész `IInventoryProvider` interfészre és a DTO-kra.

Amint kész a pack és az internal feedre publisholva: DONE outbox → INVENTORY tud tovább menni.

## Build + test gate

```bash
dotnet build   → 0 error, 0 warning
dotnet test    → minden zöld (meglévő + ≥15 új)
dotnet pack    → SpaceOS.Modules.Contracts.1.2.0.nupkg létrejön
```

---

*Skill: `/spaceos-terminal`*
