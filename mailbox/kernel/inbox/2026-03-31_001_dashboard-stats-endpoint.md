---
id: MSG-001
from: root
to: kernel
type: task-assign
priority: P1
status: DONE
created: 2026-03-31T10:00:00
epic: E28
---

## Tárgy

E28 — Dashboard Stats endpoint implementálása

## Kontextus

A Design Portal Dashboard oldala (`/dashboard`) statisztika kártyákat jelenít meg, de a Kernel-ben nem létezik a `/api/dashboard/stats` végpont. A frontend 502-t kap.

Lásd: `design-portal/src/api/dashboard.service.ts` — `GET /api/dashboard/stats` hívást küld.

## Feladat

Implementálj egy `GET /api/dashboard/stats` végpontot, ami visszaadja:

```json
{
  "tenantCount": 5,
  "facilityCount": 12,
  "workStationCount": 30,
  "activeWorkStationCount": 8,
  "flowEpicCount": 15,
  "auditEventCount": 142
}
```

### Acceptance Criteria

- [ ] `GET /api/dashboard/stats` → 200 + JSON a fenti struktúrával
- [ ] Auth policy: `ReadPolicy` (minden bejelentkezett user láthassa)
- [ ] Rate limit: `fixed` policy (GET végpont)
- [ ] A query egyetlen DB round-trip-ben adja vissza az összes count-ot
- [ ] Unit teszt a query handler-re
- [ ] API integrációs teszt a végpontra

### Pipeline

Olvasd el a `docs/WORKFLOW.md`-t és hajts végre:
1. CODE — `csharp-expert` agent
2. TEST — `kernel-test-writer` agent
3. REVIEW — `kernel-review-enforcer` agent

Minden phase után küldj `status-update`-et az `outbox/`-ba.

## Várt válasz

`docs/mailbox/outbox/2026-03-31_001_dashboard-stats-started.md` — amikor elkezded.
