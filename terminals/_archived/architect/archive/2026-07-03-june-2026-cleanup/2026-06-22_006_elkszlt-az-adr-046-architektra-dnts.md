---
id: MSG-ARCHITECT-006-DONE
from: architect
to: root
type: done
priority: high
status: READ
ref: MSG-ARCHITECT-006
created: 2026-06-22
completed: 2026-06-22
---

# MSG-ARCHITECT-006 — DONE

## Summary

Elkészült az ADR-046 architektúra döntés dokumentum a 2026-06-22 consensus-hoz. 

**3 Alignment Döntés:**
1. **EHS:** Event sourcing MEGTARTÁSA + SafetyIncidentProjection view
2. **Assembly Variance:** Új VarianceEvent aggregate (event sourcing)  
3. **Catalog Diff:** Meglévő Version mező + CatalogEntryHistory tábla

**10 Kérdés Megválaszolva:**
- Q1: SignalR Phase 2, 30s polling MVP
- Q2: S3Service kiterjesztés (5MB limit, content-type validation)
- Q3: 50MB IndexedDB, priority-based cleanup
- Q4: 72h sync deadline, exponential backoff retry
- Q5: 10% threshold default, role-based override
- Q6: Full semantic diff (price, leadtime, dimensions, materials)
- Q7: 30s polling, 60s SLA, WebSocket trigger >10 users
- Q8: Backend-side diff >500 items
- Q9: GPS opt-in, 90 nap retention, anonymization
- Q10: Role-based approval + audit trail

**Feature Prioritás Megerősítve:** EHS → Assembly Variance → Catalog Diff

**API szerződések definiálva:** 8 új/bővített endpoint (EHS: 3, Variance: 3, Catalog: 2)

## Files Changed

- docs/architecture/decisions/ADR-046-consensus-2026-06-22-ehs-assembly-catalog.md

---

**Timestamp:** 2026-06-22T18:00:47.644Z
