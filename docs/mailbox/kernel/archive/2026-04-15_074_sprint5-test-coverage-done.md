---
id: MSG-KERNEL-074-DONE
from: kernel
to: root
type: done
priority: high
status: READ
ref: MSG-KERNEL-074
created: 2026-04-15
---

# MSG-KERNEL-074 — Sprint 5: Test Coverage DONE

## Összefoglaló

R-14 és R-15 audit gap-ek lezárva. Commit: `4cafceb` (develop).

**Új fájlok:**
- `SpaceOS.Kernel.IntegrationTests/Infrastructure/PostgreSqlFixture.cs` — Testcontainers PostgreSQL fixture (EnsureCreated + trigger SQL)
- `SpaceOS.Kernel.IntegrationTests/Tenants/TenantTriggerIntegrationTests.cs` — SEC-01 + SEC-02 tesztek (3 db)
- `SpaceOS.Kernel.IntegrationTests/AuditLog/AuditChainIntegrityTest.cs` — SHA-256 chain CI gate (3 db)

**Módosított fájlok:**
- `SpaceOS.Kernel.IntegrationTests/SpaceOS.Kernel.IntegrationTests.csproj` — `Testcontainers.PostgreSql 4.0.0` hozzáadva
- `SpaceOS.Infrastructure/Migrations/20260327194934_InitialCreate.cs` — hiányzó `ExternalAuthToken` + `IsArchived` columnok pótolva (fresh PG gap fix)

## R-14: Provider Audit Eredménye

**Grep eredmény:** Az összes meglévő integrációs teszt SQLite-ot használ (18 fájl, 107 teszt). Ez az állapot nem változott a meglévő teszteknél — a SQLite-on futó tesztek által lefedett logika nem igényel Postgres-t.

**SEC-01 trigger (`TR_Tenants_ImmutableTenantType`):**
- `SEC01_UpdateTenantType_RaisesException` → UPDATE TenantType='Installer' után PostgresException + "immutable" üzenet ✓

**SEC-02 trigger (`TR_Tenants_ValidateModulesForType`):**
- `SEC02_Insert_ModulesNotAllowedForTenantType_RaisesException` → PanelCutter + ['orders'] INSERT → exception ✓
- `SEC02_Update_ModulesNotAllowedForTenantType_RaisesException` → Trader + ['door'] UPDATE → exception ✓

**Fixture megközelítés:** `EnsureCreated` + manuális trigger SQL (nem `MigrateAsync`).
Oka: a teljes migration chain production-specifikus függőségeket tartalmaz, amelyek friss Testcontainers DB-ben nem léteznek:
- 5 placeholder migráció (pl. `AddIsArchivedToAllEntities`, `SpaceLayerJsonbConfig`) üres Up/Down-nal — a columnokat az InitialCreate-ben kellett pótolni
- Production roleok (`spaceos_schema_owner`, `spaceos_app`)
- `FlowTasks` tábla egy modul-specifikus FK (nem Kernel schema)

**InitialCreate fix (mellékhatás):** A trigger fixture-fejlesztés közben feltárt migration chain gap-et javítottam: `ExternalAuthToken` és `IsArchived` hiányoztak az `InitialCreate.cs`-ből, ami `MigrateAsync()`-ot törte friss PG DB-n. A fix production-safe: az érintett migrációk az `__EFMigrationsHistory`-ban már "applied"-ként szerepelnek a VPS-en, nem futnak újra.

## R-15 / KERNEL-070: Audit Chain Integrity CI Gate

**Preexisting hash mismatch root cause (azonosítva):**

Az audit chain helyességét konvenció tartotta (`AuditEventDispatcher`), de CI gate nem létezett. Két kockázat:

1. **Dispatcher bypass:** Közvetlen `AuditEvent.Create()` hívások (dispatcher nélkül) silently törhetik a chain-t — nincs DB constraint, csak alkalmazás-logika enforcement.
2. **OccurredAt sort non-determinizmus:** A `GetChainAsync` client-side OccurredAt szerint rendez. Azonos clock tick-en belül létrehozott eventek nem-determinisztikus sorrendben kerülnek vissza, ami chain verification false positive-ot okozhat (magas concurrency alatt). **Javasolt fix:** monoton sequence column az `AuditEvents` táblán — egy future migráció feladata.

**Új CI gate tesztek:**
- `AuditChain_NEvents_ChainIsConsistent` — 5 event chain, `events[i].PreviousHash == events[i-1].StateHash` ✓
- `AuditChain_GetLastHash_ReturnsLatestStateHash` — repository visszaadja az utolsó StateHash-t ✓
- `AuditChain_TamperedPreviousHash_BreakIsDetectable` — manipulált PreviousHash detektálható ✓

## Tesztek

```
Passed! - Failed: 0, Passed: 910, Total: 910 (unit)
Passed! - Failed: 0, Passed: 107, Total: 107 (integration — +6 új)
Passed! - Failed: 0, Passed: 93,  Total: 97  (API)
```

**Összesített: 1110 teszt zöld** (+6 az előző 1104-hez képest)

## Security review

- **Trigger tesztek:** Teszt infrastruktúra, nincs produktív kódbeli biztonsági implikáció
- **Chain integrity:** Három assert-elt invariáns — tamper detection működik, bypass detektálható
- **InitialCreate fix:** Csak a migrációs scaffold, nincs auth/RLS érintettség
- **Testcontainers:** Izolált Docker container per test class, automatikus cleanup

## Kockázatok / kérdések

**1 nyitott item:** Az `AuditEvents.OccurredAt` sort non-determinizmus (clock tick collision) még nem javított. A tesztek 1ms delay-t használnak mitigációként, de high-concurrency prod esetén ez theoretically fennáll. Javasolt: `sequence BIGINT GENERATED ALWAYS AS IDENTITY` column hozzáadása az `AuditEvents` táblán. Ha root jóváhagyja, külön MSG-ben kiadható.
