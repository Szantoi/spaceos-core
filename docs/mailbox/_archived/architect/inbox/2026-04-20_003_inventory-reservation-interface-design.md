---
id: MSG-ARCH-003
from: root
to: architect
type: question
priority: high
status: READ
ref: MSG-CUTTING-038
created: 2026-04-20
---

# ARCH-003 — Inventory Reservation interfész design kérdés

> **Kereshet weben is** — ha releváns mintát talál modular monolith cross-module reservation design-ra.

---

## Kontextus

A CUTTING-038 (PanelReservation aggregate) implementálása során a terminál azt találta, hogy az `IInventoryProvider` NuGet csomag (v1.1.0) **nem tartalmazza** a tervezett `ReserveAsync()` metódust. Az arch spec (ARCH-002, OQ-4) szinkron HTTP call-t írt elő ezen a metóduson keresztül.

A terminál megoldása: saját `IInventoryReservationAdapter` interfészt hozott létre a Cutting modulon belül, HTTP adapter implementációval (`POST /api/inventory/reservations` + `DELETE` rollback). Ez jelenleg működőképes, de az `/api/inventory/reservations` endpoint az Inventory modulban **még nem létezik**.

---

## Jelenlegi állapot

```
Cutting modul (belső):
  IInventoryReservationAdapter
    → InventoryReservationHttpAdapter
        → POST http://inventory/api/inventory/reservations   ← 404 (endpoint hiányzik)
        → DELETE http://inventory/api/inventory/reservations/{id}  ← 404

IInventoryProvider v1.1.0 (NuGet, shared):
  - GetStockAsync()
  - ReserveStockAsync() ← más célra, nem panel reservation
  - nincs ReserveAsync() panel reservation célra
```

---

## Nyitott kérdések

### Q1 — Hol éljen a reservation contract?

**A opció:** `IInventoryProvider` NuGet frissítés (v1.2.0) — `ReserveAsync(PanelReservationRequest)` + `ReleaseAsync(Guid)` bekerül a shared contracts-ba. Cutting és bármely más modul ebből dolgozik.

**B opció:** Cutting belső `IInventoryReservationAdapter` megmarad saját absztrakcióként, az Inventory modul csak egy HTTP endpoint-ot implementál (nem NuGet contract). A két modul csak HTTP-n érintkezik, nincs shared interfész.

**C opció:** Új dedikált `IInventoryReservationProvider` interfész a NuGet-ben — szeparált a stock management-től.

### Q2 — Inventory modul task

Bármelyik opciónál az Inventory modulban implementálni kell:
- `POST /api/inventory/reservations` — panel foglalás létrehozása
- `DELETE /api/inventory/reservations/{id}` — foglalás törlése (rollback)

Ez egy önálló Inventory task lesz. Milyen adatmodell javasolt? (Inventory oldalon `PanelReservation` entitás szükséges-e, vagy elég egy egyszerű `ReservationRecord`?)

### Q3 — Rollback stratégia

A CUTTING-038 implementáció "best-effort rollback"-ot csinál — ha a reservation sikertelen, a már létrehozott rezervációkat Release-eli. Ez helyes minta modular monolith-ban, vagy 2-phase commit / saga pattern-t kellene itt alkalmazni?

---

## Kért output

1. Q1 döntési javaslat (A/B/C) indoklással
2. Q2: Inventory modul adatmodell irányvonal
3. Q3: Rollback stratégia megerősítése vagy alternatíva
4. Ha talál weben releváns mintát (pl. modular monolith cross-module reservation) — idézze be

Nincs időkritikus blokkolt task, de CUTTING-040 (IPlanningStrategy refactor) után jön az Integration tests (CUTTING-041), ahol ez a kérdés releváns lesz.
