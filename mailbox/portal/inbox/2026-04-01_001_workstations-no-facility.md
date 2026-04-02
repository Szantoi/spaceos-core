---
id: MSG-P003
from: root
to: portal
type: bug-report
priority: P1
status: DONE
created: 2026-04-01T12:00:00
---

## Tárgy

BUG — WorkStations oldal: nincs facility kiválasztó, ha direktben navigálnak

## Probléma

A WorkStationsPage a `?facilityId=` query paramből olvassa a facility-t. Ha a user a menüből navigál oda (query param nélkül), üres oldalt kap és a "Register WorkStation" gomb sem jelenik meg (az oldal facility nélkül nem tud lekérdezni).

## Elvárt megoldás

Adj hozzá tenant → facility kaszkád dropdown-ot az oldal tetejére (hasonlóan a Facilities oldalhoz):

1. Tenant dropdown (useTenants)
2. Facility dropdown (useFacilitiesByTenant, a kiválasztott tenant alapján)
3. Ha mindkettő ki van választva → listázza a workstation-öket
4. A "Register WorkStation" gomb mindig elérhető legyen
5. A form automatikusan kitöltse a facility-t ha van kiválasztott

## Pipeline

CODE → TEST. Outbox status-update.
