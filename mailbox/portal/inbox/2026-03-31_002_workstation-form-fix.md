---
id: MSG-P002
from: root
to: portal
type: bug-report
priority: P1
status: DONE
created: 2026-03-31T14:00:00
---

## Tárgy

BUG — WorkStation regisztrálás nem működik: hiányzó Type mező + UX probléma

## Problémák

### 1. Hiányzó `type` mező
A Kernel `RegisterWorkStationRequest`-ben kötelező a `type` mező (`WorkStationType` enum: `Generic`, `Joinery`, `MEP`, `Electrical`, `Architecture`). A frontend form nem tartalmazza.

### 2. UX — Facility kiválasztás
A felhasználó Facility ID-t lát a dropdown-ban, nem a facility nevét. A WorkStation form-nak facility-t kell választatnia (név alapján), nem ID-t begépeltetnie.

## Elvárt megoldás

1. A WorkStation form-hoz adj hozzá egy `type` dropdown-t (értékek: `Generic`, `Joinery`, `MEP`, `Electrical`, `Architecture`, default: `Generic`)
2. A Facility kiválasztás legyen tenant→facility kaszkád dropdown (előbb tenant, aztán facility névvel)
3. A `RegisterWorkStationRequest` típusban legyen `type` mező

## Kernel API referencia

```
POST /api/facilities/:facilityId/work-stations
Body: { "name": "WS-001", "type": "Generic" }
→ 201 Created
```

## Pipeline

CODE → TEST. Outbox status-update.
