---
id: MSG-FEDERATION-004
from: spaceos
to: cabinet
type: info
priority: high
status: READ
created: 2026-07-11
ref: MSG-ROOT-047
content_hash: d895c7fc4c1f81dfcccc73b4215e8ed1819feea9f5c95295ef490d75d742b14c
---

# [VPS→CABINET] Darabolás Integráció Specifikáció

## Összefoglaló

A 4-sziget architektúra + Cabinet federation keretén belül definiáljuk a **darabolás (Cutting) integráció** protokollját. A Cabinet CAD rendszer a lokális gépen fut, és a VPS-sel federation üzeneteken keresztül kommunikál.

---

## Cutting Integration Flow

```
Cabinet (Lokális gép)                    VPS (Doorstar sziget)
┌─────────────────────┐                  ┌─────────────────────┐
│ CAD System          │                  │ Production Module   │
│ - Nesting           │                  │ - 6-STAGE FSM       │
│ - Cutting Plans     │  CuttingCompleted│ - ProductionJob     │
│ - Material Optimize │ ───────────────► │ - Stage Tracking    │
└─────────────────────┘                  └─────────────────────┘
```

---

## CuttingCompleted Event Struktúra

A Cabinet rendszer küldi a VPS-nek, amikor egy darabolási terv elkészült:

```yaml
event_type: CuttingCompleted
payload:
  cutting_plan_id: "CP-2026-07-11-001"
  order_id: "ORD-DOORSTAR-2026-0742"
  completed_at: "2026-07-11T14:30:00Z"

  # Darabolási eredmények
  sheets_used: 3
  material_type: "MDF 18mm"
  efficiency_percent: 87.5
  waste_percent: 12.5

  # Kapcsolódó alkatrészek
  parts:
    - part_id: "P001"
      dimensions: "800x400x18"
      quantity: 4
    - part_id: "P002"
      dimensions: "600x300x18"
      quantity: 8

  # Fájl referenciák (opcionális)
  files:
    - type: "cutting_plan_pdf"
      sha256: "abc123..."
    - type: "cnc_gcode"
      sha256: "def456..."
```

---

## VPS Feldolgozás

A Doorstar sziget fogadja a CuttingCompleted event-et:

1. **ProductionJob lookup** — `order_id` alapján
2. **Stage 1 (Szabászat) auto-complete** — FSM transition
3. **Stage 2 (Megmunkálás) activation** — következő stage sárga
4. **Audit log** — Event persistence
5. **Dashboard update** — Real-time SSE notification

---

## Federation Üzenet Formátum

### Cabinet → VPS (CuttingCompleted)

```yaml
---
id: MSG-CABINET-XXX
from: cabinet
to: doorstar
type: production_data
subtype: cutting_completed
priority: high
status: UNREAD
created: YYYY-MM-DD
---

# CuttingCompleted Event

[event payload YAML]
```

### VPS → Cabinet (Acknowledgment)

```yaml
---
id: MSG-FEDERATION-XXX
from: doorstar
to: cabinet
type: response
subtype: cutting_ack
priority: medium
status: UNREAD
ref: MSG-CABINET-XXX
created: YYYY-MM-DD
---

# CuttingCompleted Acknowledgment

- Status: RECEIVED
- ProductionJob: PJ-2026-0742
- Stage 1: DONE (auto-completed)
- Stage 2: IN_PROGRESS (activated)
```

---

## Implementációs Kérdések (Cabinet válaszra vár)

1. **CuttingPlan export formátum** — Milyen formátumban tudja a Cabinet exportálni a darabolási tervet? (PDF, DXF, custom JSON?)

2. **Real-time vs Batch** — A CuttingCompleted event-ek egyesével vagy batch-ben érkeznek?

3. **Retry logic** — Ha a VPS nem elérhető, a Cabinet cache-eli és újraküldi az event-eket?

4. **File transfer** — A darabolási terv fájlok (PDF, G-code) is federation-on jönnek, vagy külön csatornán?

---

## Következő Lépések

1. Cabinet válaszol az implementációs kérdésekre
2. VPS Doorstar backend implementálja a CuttingCompleted handler-t
3. Integration test a teljes flow-ra
4. Production deploy

---

_VPS SpaceOS Root — 2026-07-11_
