---
id: MSG-ARCHITECT-006-DONE
from: architect
to: conductor
type: done
priority: high
status: READ
ref: MSG-ARCHITECT-006
created: 2026-06-22
content_hash: 68b3f7a5e6f5474308db16756957ea0a19086a1123ac62db2370bba4fae93e5d
---

# DONE: ADR-046 — Consensus 2026-06-22 Architecture Decisions

## Összefoglaló

Elkészült az **ADR-046** architektúra döntés dokumentum a 2026-06-22 consensus-hoz.

## 3 Alignment Döntés

| Feature | Döntés |
|---------|--------|
| **EHS Incident Report** | Event sourcing MEGTARTÁSA + `SafetyIncidentProjection` view |
| **Assembly Variance Detection** | Új `VarianceEvent` aggregate (event sourcing) |
| **Catalog Diff View** | Meglévő `Version` mező + `CatalogEntryHistory` tábla |

## 10 Kérdés Megválaszolva

| # | Kérdés | Döntés |
|---|---|---|
| Q1 | WebSocket | SignalR Phase 2; 30s polling MVP |
| Q2 | File storage | S3Service kiterjesztés (5MB, content-type) |
| Q3 | Offline kvóta | 50MB IndexedDB, priority-based cleanup |
| Q4 | EHS compliance | 72h sync deadline, exponential backoff |
| Q5 | Variance threshold | 10% default, role-based override |
| Q6 | Catalog diff scope | Full semantic diff |
| Q7 | Polling frekvencia | 30s, SLA 60s |
| Q8 | Catalog limit | Backend-side diff >500 items |
| Q9 | GPS adatok | Opt-in, 90 nap retention |
| Q10 | Variance approval | Role-based + audit trail |

## Feature Prioritás

1. **EHS Incident Report** (P0, 3-4 nap)
2. **Assembly Variance** (P1, 4-5 nap)
3. **Catalog Diff** (P2, 3-4 nap)

## API Szerződések Definiálva

- **EHS:** `/api/ehs/incidents`, `/api/ehs/events/batch`
- **Variance:** `/api/joinery/work-orders/{id}/variances`, `/api/joinery/variances/{id}/approve`
- **Catalog:** `/api/catalog/items/{id}/versions`, `/api/catalog/items/{id}/diff`

## Létrehozott Fájlok

1. `docs/architecture/decisions/ADR-046-consensus-2026-06-22-ehs-assembly-catalog.md`
2. `docs/knowledge/architecture/ADR_CATALOGUE.md` (frissítve)

## Következő Lépések

1. Root approval az ADR-046-ra
2. Backend terminálnak EHS Phase 1 task
3. Frontend terminálnak IndexedDB sync component task
