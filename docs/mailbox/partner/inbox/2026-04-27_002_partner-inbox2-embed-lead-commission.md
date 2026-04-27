---
id: MSG-PARTNER-002
from: root
to: partner
type: task
priority: high
status: READ
ref: MSG-PARTNER-001-DONE
created: 2026-04-27
---

# PARTNER-002 — Inbox #2: Nesting Embed + Lead Capture + Commission (Nap 4–10)

> **Tervdok:** `docs/architecture/SpaceOS_PartnerTier_Architecture_v1.md` — Section 5, 6, 7
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** PARTNER-001 ✅ (56 teszt, scaffold + domain + auth)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Nap 4–5 — Nesting Embed

### Embed Token (signed JWT)

```csharp
// Application/Commands/CreateEmbedToken/
// POST /api/partner/embed-token
// → { token: "jwt...", expiresAt: "2026-..." }
// JWT claims: partnerId, tier, corsOrigin, branding
// TTL: 1h
// Signing: HMAC-SHA256 (szerver-oldali secret)
```

### Nesting Proxy endpoint

```csharp
// POST /api/partner/nest
// Header: Authorization: Bearer {embed_token}
// Body: NestingInput (sheet + parts)
// → Nesting.Algorithms hívás (FFDH/Guillotine tier alapján)
// → NestingResult JSON
// Rate limit: per-partner tier (Free/Pro/Enterprise)
```

### iframe postMessage protokoll

A tervdok §5.3 szerint — a partner oldalán:
```html
<iframe src="https://partner-api.joinerytech.hu/embed/{partnerId}?token={jwt}" />
```

A postMessage-ek schema-validáltak (origin check + message type enum).

---

## Nap 6–7 — Lead Capture

### PartnerLead entity

```csharp
public sealed class PartnerLead
{
    public Guid Id { get; }
    public Guid PartnerId { get; }
    public byte[] EncryptedEmail { get; }  // AES-256 (pgcrypto)
    public string EmailHash { get; }       // SHA-256 (lookup-hoz)
    public string? SourceUrl { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? DeletedAt { get; }  // soft-delete GDPR
}
```

**PII encryption:** `pgcrypto` AES-256 az email-re. A hash a Kernel email-hash lookup-hoz (KERNEL-104).

### Lead capture flow

1. End customer megadja az email-jét az embed nesting eredmény után
2. `POST /api/partner/leads` → email encrypt + hash → persist
3. A FreeTier magic-link flow-t triggereli (ha a user upgrade-elni akar)

---

## Nap 8–10 — Commission Tracking

### PartnerUpgradeAttribution entity

```csharp
public sealed class PartnerUpgradeAttribution
{
    public Guid Id { get; }
    public Guid PartnerId { get; }
    public Guid LeadId { get; }
    public Guid? TenantId { get; }       // Kernel tenant (ha upgrade történt)
    public decimal CommissionRate { get; } // 0.10 (10%)
    public int AttributionWindowMonths { get; } // 12
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? ConvertedAt { get; }
}
```

### AttributionWorker (BackgroundService)

- Periodikusan (1h) ellenőrzi: lead email-hash → Kernel tenant lookup (KERNEL-104)
- Ha talál → `ConvertedAt` kitöltve → commission tracking aktív
- Per-batch DbContext scope (A4-21 minta)

---

## Tesztek (50+)

**Embed (15+):** JWT generálás, token validálás, expired token, nesting proxy, tier-alapú algoritmus
**Lead (15+):** email encrypt/decrypt, hash, soft-delete, GDPR, duplicate handling
**Commission (15+):** attribution window, conversion detection, rate calculation, worker batch
**Integration (5+):** full flow: embed → nest → lead → attribution

## Definition of Done

- [ ] Embed token (JWT, 1h TTL, HMAC-SHA256)
- [ ] Nesting proxy endpoint (tier-alapú FFDH/Guillotine)
- [ ] PartnerLead entity (AES-256 email, SHA-256 hash)
- [ ] PartnerUpgradeAttribution entity + AttributionWorker
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 106 pass (56 előző + 50 új)
- [ ] Outbox DONE
