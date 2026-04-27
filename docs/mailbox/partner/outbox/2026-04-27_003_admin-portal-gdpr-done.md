---
id: partner-outbox-003
from: partner
to: root
type: done
priority: high
status: READ
ref_inbox: 2026-04-27_003_partner-inbox3-admin-portal-gdpr.md
date: 2026-04-27
---

# DONE: PartnerTier — Admin Portal + Commission + GDPR (Inbox #3)

## Eredmény

- `dotnet build`: 0 error, 0 warning
- `dotnet test`: 165 pass, 0 fail, 0 skip (111 meglévő + 54 új)

## Megvalósított

### Domain bővítések
- `Partner.Terminate()` FSM — bármely státuszból Terminated-be
- `Partner.ChangeTier()` — csak Active partner + különböző tier
- `PartnerTerminated` + `PartnerTierChanged` domain events
- `PartnerAuditLog` entity — append-only, minden admin művelet
- `PartnerLead.GdprErase()` — crypto-shredding (EncryptedEmail zeroed, DeletedAt set)

### Infrastructure
- `PartnerAuditLogConfiguration` (EF Core, spaceos_partner schema)
- `CommissionSummaryDto` (Application/Queries)

### API — AdminAuthMiddleware + 11 új endpoint
- `AdminAuthMiddleware` — X-SpaceOS-Internal header alapú admin auth
- `GET /api/admin/partners` — paginated lista, status/tier filter
- `GET /api/admin/partners/{id}` — detail
- `POST /api/admin/partners/{id}/approve` + audit log
- `POST /api/admin/partners/{id}/suspend` + audit log
- `POST /api/admin/partners/{id}/terminate` + audit log
- `PUT /api/admin/partners/{id}/tier` + audit log
- `GET /api/admin/commissions/report` — CSV export
- `GET /api/partner/profile`
- `GET /api/partner/api-keys`
- `POST /api/partner/api-keys` (max 3 guard)
- `DELETE /api/partner/api-keys/{id}` (revoke)
- `GET /api/partner/usage`
- `GET /api/partner/commissions`
- `GET /api/partner/commissions/summary`
- `DELETE /api/partner/leads/{id}/gdpr-erase` + audit log

### Tesztek (54 új)

| Fájl | Teszt |
|------|-------|
| Domain/PartnerTerminateTests.cs | 10 |
| Domain/PartnerAuditLogTests.cs | 5 |
| Domain/PartnerLeadGdprTests.cs | 6 |
| Api/AdminEndpointTests.cs | 19 |
| Api/SelfServiceEndpointTests.cs | 14 |

## Bugfix
- PartnerApiKeyMiddleware: Task.Run fire-and-forget eltávolítva → DbContext thread-safety bug fix
  (LastUsedAt update most `await next(context)` UTÁN fut, nem párhuzamosan)

## Következő lépés (Inbox #4)
- Security hardening (fraud detection, rate limit tesztek)
- OpenAPI snapshot
- EF Core migrations (spaceos_partner schema)
- Production deploy + smoke test
