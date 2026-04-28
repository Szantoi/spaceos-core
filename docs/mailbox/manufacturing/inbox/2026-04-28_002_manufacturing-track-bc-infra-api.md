---
id: MSG-MFG-002
from: root
to: manufacturing
type: task
priority: high
status: UNREAD
ref: MSG-MFG-OUT-001
created: 2026-04-28
---

# MFG-002 — Manufacturing Phase 1 Track B+C: Infrastructure + API (Day 10–22)

> **Tervdok:** `docs/tasks/active/SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4.md`
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** MFG-001 ✅ (134 teszt, Domain + Application)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Track B: Infrastructure + Persistence

### EF Core + Migrations (M-0001..M-0004)

- `ManufacturingDbContext` — `spaceos_manufacturing` séma
- Configurations: ManufacturingOrder, EdgeBandingTask, CncTask + owned VOs
- RLS FORCE minden táblán (COALESCE pattern!)
- `OutboxSaveChangesInterceptor` (Kernel outbox re-use)
- Repository implementációk (3 repo, spec-only query, AsSplitQuery)

### Inbox endpoint (mTLS)

```csharp
// POST /internal/inbox/cutting
// Header: X-SpaceOS-Internal + X-SpaceOS-Hmac
// Body: CuttingPanelCompleted event
// → ProcessInboxEventCommand dispatch
```

`InboxHmacVerifier` — BE-05: Singleton, FixedTimeEquals

### IHttpClientFactory named clients (BE-04)

- `"cutting-internal"` — Cutting API hívások
- `"workers-identity"` — Workers.Identity hívások (future)

---

## Track C: API + Tests

### Minimal API endpoints

```
POST /api/manufacturing/orders                    — CreateOrder
POST /api/manufacturing/orders/{id}/cancel        — CancelOrder
GET  /api/manufacturing/orders/{id}               — GetOrder
GET  /api/manufacturing/orders                    — ListOrders

POST /api/manufacturing/edge-banding/{id}/schedule — ScheduleEdgeBanding
POST /api/manufacturing/edge-banding/{id}/start    — StartEdgeBanding
POST /api/manufacturing/edge-banding/{id}/complete — CompleteEdgeBanding
POST /api/manufacturing/edge-banding/{id}/fail     — FailEdgeBanding

POST /api/manufacturing/cnc/{id}/schedule          — ScheduleCnc
POST /api/manufacturing/cnc/{id}/start             — StartCnc
POST /api/manufacturing/cnc/{id}/complete          — CompleteCnc
POST /api/manufacturing/cnc/{id}/fail              — FailCnc

POST /internal/inbox/cutting                       — InboxEndpoint (mTLS)
GET  /healthz                                      — HealthCheck
```

Minden endpoint: `RequireAuthorization("ManufacturerOnly")` (kivéve /internal/* és /healthz)

### Program.cs

- MediatR + FluentValidation + EF Core + Redis + SignalR
- `AddManufacturingInfrastructure()` DI extension
- Kestrel: `127.0.0.1:5007`

---

## Tesztek (115+)

**Infrastructure (40+):** EF configs, RLS isolation, repository CRUD, inbox HMAC verify, outbox interceptor
**API (40+):** 14 endpoint routing, auth, inbox mTLS, /healthz
**Integration (35+):** full flow: inbox event → order → edge banding → CNC → complete

## Definition of Done

- [ ] EF Core + 4 migration (RLS FORCE, COALESCE!)
- [ ] 3 repository implementáció (spec-only)
- [ ] OutboxSaveChangesInterceptor
- [ ] Inbox endpoint (mTLS + HMAC verify)
- [ ] 14 API endpoint + /healthz
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 249 pass (134 + 115 új)
- [ ] Outbox DONE
