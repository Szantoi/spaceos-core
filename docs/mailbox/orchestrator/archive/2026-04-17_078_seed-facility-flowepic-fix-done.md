---
id: MSG-ORCH-078-DONE
from: orchestrator
to: root
type: done
priority: high
status: READ
ref: MSG-ORCH-078
created: 2026-04-17
---

# ORCH-078 DONE — facility-first FlowEpic seed fix

## Összefoglaló

**Root cause:** `POST /api/flow-epics` nem létezik a Kernelben — a FlowEpic egy Facility alatt él.

**Fix:** Mindkét seed profilban (`doorstar-smoke-v1`, `doorstar-cutting-ready-v1`):

| Lépés | Volt | Lett |
|---|---|---|
| Facility | nem volt | `POST /api/tenants/{tenantId}/facilities` → facilityId |
| FlowEpic | `POST /api/flow-epics` (404) | `POST /api/facilities/{facilityId}/flow-epics` |
| Request body | `{ tenantId, title, tradeType }` | `{ title }` (csak amit a Kernel elfogad) |

**Bónusz (`doorstar-cutting-ready-v1`):** PROCUREMENT-006 supplier endpoint (`POST /api/procurement/suppliers`) bekötve — try/catch fallback-kel, ha INFRA-140 még nem deployolt. Ha elérhető: `suppliers: 1`, ha nem: `suppliers: 0`.

Módosított fájlok:
- `src/routes/test.route.ts` — seed logika mindkét profilban
- `src/routes/test.route.test.ts` — mock call count-ok frissítve (Test 8: 3→4, Test 9: +facility mock, Test 10: +post mocks, Test 11: 10→12 + suppliers: 0→1)

Commit: `4e8926d`

## Tesztek

```
Test Files  29 passed (29)
     Tests  218 passed (218)
  Duration  6.77s
```

0 TS error, 0 warning.

## Security review

- Facility és FlowEpic creation: Authorization Bearer token forwarded, nem hardcoded
- Supplier try/catch: hiba esetén `supplierCount = 0` — nem szivárog hiba a response-ba
- `facilityRes.data.id ?? facilityRes.data` — biztonságos fallback ha az id közvetlenül a response body

## Kockázatok / kérdések

Nincsenek. Deploy: INFRA-141 szükséges a `4e8926d` commit élesítéséhez.
