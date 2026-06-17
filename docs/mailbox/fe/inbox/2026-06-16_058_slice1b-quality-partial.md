---
id: MSG-FE-058
from: root
to: fe
type: task
priority: medium
status: READ
model: sonnet
ref: MSG-FE-057-DONE
created: 2026-06-16
---

# FE-CORE Slice 1B — QualityPage részleges bekötés

## Kontextus

A QualityPage teljesen mock. A Joinery backend (`/joinery/api/orders`) él — rendelés-alapú QA bekötés lehetséges.

## Feladat

### QualityPage → Joinery alapú bekötés

- Rendelések listája QA státuszra szűrve: `GET /joinery/api/orders?status=qa_pending` (path ellenőrizendő)
- Ha a query param nem létezik → `EndpointPending` banner, nem mock
- QA inspection akciók (`sendOrderToFinalQa`) — ellenőrizd a Joinery controller-t (`backend/spaceos-modules-joinery/`)
- Ami nincs backenden: `EndpointPending`, nem mock import

### [?] Joinery endpointok konfirmálása (057-ből maradt)

- `GET /joinery/api/orders?status=pending_release` — létezik-e a `status` query param?
- `GET /joinery/api/manufacturing-sheets` — mi az egzakt REST path?

Ha megtalálod → MfgPrepPage bekötése is elvégezhető (ha a schema egyezik).

## DoD

- QualityPage: mock import törölve, Joinery-ből töltve vagy EndpointPending
- [?] Joinery path-ok tisztázva, outboxban dokumentálva
- Build zöld, tesztek zöldek
- Outbox: `MSG-FE-058-DONE`

## Skill / agent

Használd a `/spaceos-terminal` skillt. Sub-agent engedélyezett.
