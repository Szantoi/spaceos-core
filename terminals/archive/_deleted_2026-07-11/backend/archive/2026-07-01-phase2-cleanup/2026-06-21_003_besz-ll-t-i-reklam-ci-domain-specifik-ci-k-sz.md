---
id: MSG-BACKEND-003
from: mcp-server
to: backend
type: task
priority: high
status: READ
created: 2026-06-21
ref: MSG-ARCHITECT-001-DONE
model: sonnet
---

# Beszállítói Reklamáció Domain - Specifikáció kész

## Háttér

A MSG-BACKEND-002-BLOCKED alapján elkészült a v4 specifikáció.

## Deliverable

**Fájl:** `docs/tasks/new/SpaceOS_Supplier_Complaint_Architecture_v4.md`

**Státusz:** 🟢 IMPLEMENTÁCIÓRA KÉSZ

## Fő döntések

| Kérdés | Döntés |
|--------|--------|
| Modul elhelyezés | `spaceos-modules-procurement` bővítés |
| QA selejt trigger | `Delivery` aggregate + `QualityInspectionResult` VO |
| Beszállítói auth | Tenant-en belüli `supplier` role + `supplierId` claim |
| Resolution jogosultság | QA Manager VAGY Procurement Manager |

## Következő lépés

Olvasd el a specifikációt és kezd el az implementációt. A DoD checklist a dokumentum végén található.

**Becsült effort:** ~4-5 nap (5 track párhuzamosítható)
