---
id: MSG-PARTNER-003
from: root
to: partner
type: task
priority: high
status: READ
ref: MSG-PARTNER-002-DONE
created: 2026-04-27
---

# PARTNER-003 — Inbox #3: Admin Portal + Commission Report + GDPR (Nap 11–16)

> **Tervdok:** `docs/architecture/SpaceOS_PartnerTier_Architecture_v1.md` — Section 7, 8, 9
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** PARTNER-002 ✅ (111 teszt)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Nap 11–12 — SpaceOS Admin Portal endpoints

### Partner management (SpaceOS admin)

```csharp
// GET  /api/admin/partners              — lista (paginated, filterable by status/tier)
// GET  /api/admin/partners/{id}         — detail
// POST /api/admin/partners/{id}/approve — PendingReview → Active
// POST /api/admin/partners/{id}/suspend — Active → Suspended
// POST /api/admin/partners/{id}/terminate — → Terminated
// PUT  /api/admin/partners/{id}/tier    — tier upgrade/downgrade
```

**Auth:** Ezek SpaceOS admin endpointok — `[Authorize(Policy = "SpaceOsAdmin")]` vagy `X-SpaceOS-Internal` header.

### Partner self-service endpoints

```csharp
// GET  /api/partner/profile             — saját partner profil
// GET  /api/partner/api-keys            — aktív API kulcsok listája
// POST /api/partner/api-keys            — új kulcs generálás (max 3)
// DELETE /api/partner/api-keys/{id}     — kulcs revoke
// GET  /api/partner/usage               — nesting használati statisztikák
```

---

## Nap 13–14 — Commission Report

### Query handlers

```csharp
// GET /api/partner/commissions                    — saját commission lista
// GET /api/partner/commissions/summary            — havi összesítő
// GET /api/admin/commissions/report?month=2026-04 — admin havi report (CSV export)
```

### CommissionSummaryDto

```csharp
public record CommissionSummaryDto(
    int TotalLeads,
    int ConvertedLeads,
    decimal ConversionRate,
    decimal TotalCommission,  // ConvertedLeads × CommissionRate × subscription_price
    string Period             // "2026-04"
);
```

---

## Nap 15–16 — GDPR + Security hardening

### Crypto-shredding

```csharp
// DELETE /api/partner/leads/{id}/gdpr-erase
// Flow:
// 1. PartnerLead.EncryptedEmail → null
// 2. PartnerLead.DeletedAt → now
// 3. Ha van attribution → attribution marad (anonimizálva)
```

### Audit log

- `PartnerAuditLog` entity — append-only (DB-trigger ha EF migration-ben van)
- Minden admin művelet logolva: approve, suspend, terminate, tier change, gdpr-erase

### Security review endpoints

- Rate limit ellenőrzés minden endpointon
- API key hash timing-safe (FixedTimeEquals) — már megvan
- Embed token JWT validation — már megvan
- PII: csak encrypted email, hash for lookup

---

## Tesztek (50+)

**Admin (15+):** partner approve/suspend/terminate, tier change, lista filter
**Self-service (10+):** profile, API key CRUD, max 3 limit, usage stats
**Commission (10+):** summary calculation, CSV export, attribution window
**GDPR (10+):** crypto-shredding, soft-delete, audit log, anonimizált attribution
**Integration (5+):** full admin flow

## Definition of Done

- [ ] Admin partner management (6 endpoint)
- [ ] Partner self-service (5 endpoint)
- [ ] Commission report + summary + CSV
- [ ] GDPR crypto-shredding
- [ ] Audit log (append-only)
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 161 pass (111 előző + 50 új)
- [ ] Outbox DONE
