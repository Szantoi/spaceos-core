---
id: MSG-PARTNER-004
from: root
to: partner
type: task
priority: high
status: READ
ref: MSG-PARTNER-003-DONE
created: 2026-04-27
---

# PARTNER-004 — Inbox #4: Security Hardening + Migrations + Deploy Prep (Nap 17–24)

> **Tervdok:** `docs/architecture/SpaceOS_PartnerTier_Architecture_v1.md` — Section 9, 10
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** PARTNER-003 ✅ (165 teszt)
> **Használhatsz sub-agent-eket** ha szükséges
> **Ez az UTOLSÓ inbox — a PartnerTier MVP itt zárul!**

---

## EF Core Migrations

```bash
dotnet ef migrations add P_0001_InitialPartnerSchema \
  --project src/SpaceOS.PartnerTier.Infrastructure \
  --startup-project src/SpaceOS.PartnerTier.Api
```

Raw SQL kiegészítések a migration-ben:
- `CREATE SCHEMA IF NOT EXISTS spaceos_partner`
- RLS ENABLE + FORCE minden táblán (COALESCE pattern!)
- `app.current_partner_id` GUC regisztráció
- Append-only trigger a `PartnerAuditLog` táblán (UPDATE+DELETE BLOCK)
- Index: `PartnerApiKeys(KeyPrefix)` + `PartnerLeads(EmailHash)` + `PartnerUpgradeAttributions(PartnerId, ConvertedAt)`

---

## Security Hardening (tervdok §9)

### Ellenőrizendő (12 réteg)

1. API key hash: SHA-256, FixedTimeEquals ✅ (Inbox #1)
2. Embed token: HMAC-SHA256 JWT, 1h TTL ✅ (Inbox #2)
3. PII encryption: AES-256 ✅ (Inbox #2)
4. GDPR crypto-shredding ✅ (Inbox #3)
5. Rate limiting: per-partner tier ✅ (Inbox #1)
6. RLS: per-partner GUC isolation → migration-ben
7. Audit log: append-only trigger → migration-ben
8. CORS: per-partner allowlist → middleware ellenőrzés
9. API key auto-revoke: 365 nap inaktivitás → BackgroundService
10. Max 3 API key per partner ✅ (Inbox #3)
11. Admin auth: X-SpaceOS-Internal ✅ (Inbox #3)
12. Input validation: FluentValidation minden command-on

### ApiKeyExpiryWorker (BackgroundService)

```csharp
// Naponta fut
// Ha PartnerApiKey.LastUsedAt < now - 365 nap → auto-revoke + audit log
```

---

## Deploy Prep

### appsettings.Production.json

```json
{
  "Kestrel": { "Endpoints": { "Http": { "Url": "http://127.0.0.1:5011" } } },
  "ConnectionStrings": { "PartnerDb": "FROM_ENV" },
  "Redis": { "ConnectionString": "FROM_ENV" },
  "EmbedToken": { "Secret": "FROM_ENV", "Issuer": "spaceos-partner", "Audience": "partner-embed" }
}
```

### dotnet publish

```bash
dotnet publish src/SpaceOS.PartnerTier.Api -c Release -o /tmp/partner-publish/
```

---

## Tesztek (50+)

**Migration (10+):** tábla + RLS + trigger existence
**Security (20+):** CORS validation, API key expiry, rate limit edge cases, RLS isolation, audit immutability
**Deploy (5+):** production config load, publish DLLs, /healthz
**Integration (15+):** full partner lifecycle: register → approve → API key → nest → lead → attribution → commission → GDPR erase

## Definition of Done

- [ ] EF migration (P_0001) + RLS + trigger + indexes
- [ ] 12-réteg security ellenőrzés
- [ ] ApiKeyExpiryWorker
- [ ] CORS middleware per-partner allowlist
- [ ] appsettings.Production.json + dotnet publish
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 215 pass (165 előző + 50 új)
- [ ] Outbox DONE
